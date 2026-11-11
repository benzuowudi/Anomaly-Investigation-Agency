using System;
using FairyGUI;
using FairyGUI.Utils;
using UnityEngine;

namespace Framework.Script.UI.Anim
{
    /// <summary>
    /// 通用 UI 动画组件（FairyGUI）。
    ///
    /// 一、FairyGUI 编辑器侧约定
    ///   组件里放一个 graph 当挂点，尺寸 0,0，名字必须是 "GameObjectGo"（见 <see cref="c_MountNodeName"/>）。
    ///   挂点在世界里的位置就是动画的对齐点（默认按包围盒底部居中对齐，适合角色脚底站位）。
    ///
    /// 二、C# 侧注册
    ///   在对应包的 XxxData.Init() 里：
    ///       UIObjectFactory.SetPackageItemExtension("ui://Base/RoleAnimCom", typeof(RoleAnimCom));
    ///   其中 RoleAnimCom 继承本类。
    ///
    /// 三、使用
    ///       com.SetSpine("role1", "readystand");   // Addressables 地址，资源类型是 SkeletonDataAsset
    ///       com.Play("attack", false);             // 播一次，播完触发 onComplete
    ///
    /// 本类负责：原生对象挂进 FairyGUI 显示列表（GoWrapper）、资源加载与释放、
    /// 自适应缩放与对齐、渲染层级（交给 GoWrapper.renderingOrder）、alpha 跟随界面淡入淡出。
    /// 真正的播放逻辑交给 <see cref="IUIAnimPlayer"/> 实现（Spine 的实现在 SpineUIAnimPlayer.cs）。
    /// </summary>
    public class SpineAnimCom : GComponent
    {
        /// <summary>FairyGUI 里当作挂点的 graph 名</summary>
        public const string c_MountNodeName = "GameObjectGo";

        /// <summary>首帧拿不到包围盒时的最大重试帧数（避免一直取网格顶点产生 GC）</summary>
        private const int c_MaxLayoutRetry = 30;

        /// <summary>挂点 graph</summary>
        protected GGraph m_Mount;

        /// <summary>原生对象的包装器，FairyGUI 显示对象，负责排序 / 裁剪 / 层级</summary>
        protected GoWrapper m_Wrapper;

        /// <summary>动画对象的容器，所有动画对象都挂在它下面，换资源时复用它</summary>
        protected GameObject m_Holder;

        /// <summary>当前播放器</summary>
        protected IUIAnimPlayer m_Player;

        /// <summary>当前资源地址</summary>
        protected string m_Address;

        /// <summary>默认动画（SetSpine 之后自动播）</summary>
        protected string m_DefaultAnimation;

        /// <summary>默认动画是否循环</summary>
        protected bool m_DefaultLoop = true;

        // ---- 自适应布局 ----
        protected UIAnimFit m_Fit = UIAnimFit.None;
        protected UIAnimAnchor m_Anchor = UIAnimAnchor.BottomCenter;
        protected Vector2 m_Offset = Vector2.zero;
        protected float m_ExtraScale = 1f;
        protected float m_FitWidth;
        protected float m_FitHeight;
        private bool m_NeedApplyLayout;
        private int m_LayoutRetry;
        /// <summary>同一套 Fit 参数是否已经成功应用过（动画姿态变化后不再重算，避免来回缩放）</summary>
        private bool m_FitApplied;

        /// <summary>动画播完回调（参数：动画名）</summary>
        public event Action<string> onComplete;

        /// <summary>动画帧事件回调（参数：事件名、字符串参数）</summary>
        public event Action<string, string> onEvent;

        /// <summary>当前播放器（没加载资源时为 null，可用于高级操作）</summary>
        public IUIAnimPlayer player
        {
            get { return m_Player; }
        }

        /// <summary>是否已经挂上动画对象</summary>
        public bool hasPlayer
        {
            get { return m_Player != null && m_Player.isReady; }
        }

        #region 生命周期

        public override void ConstructFromXML(XML _xml)
        {
            base.ConstructFromXML(_xml);

            m_Mount = GetChild(c_MountNodeName).asGraph;
            if (m_Mount == null)
            {
                // 兼容 FGUI 里把挂点节点改名的情况：退而取第一个 graph 子节点
                for (int i = 0; i < numChildren; i++)
                {
                    m_Mount = GetChildAt(i) as GGraph;
                    if (m_Mount != null)
                    {
                        break;
                    }
                }

                if (m_Mount != null)
                {
                    Debug.LogWarning("[SpineAnimCom] 挂点 " + c_MountNodeName +
                        " 不存在，改用第一个 graph 节点：" + m_Mount.name);
                }
                else
                {
                    Debug.LogError("[SpineAnimCom] 找不到挂点 graph：" + name);
                }
            }

            m_Holder = new GameObject("UIAnimHolder");
            m_Wrapper = new GoWrapper();
            m_Wrapper.gameObject.name = "UIAnimWrapper";
            m_Wrapper.onUpdate += OnWrapperUpdate;

            // cloneMaterial = true：GoWrapper 会把材质 renderQueue 改成 3000，
            // 用共享材质会污染同资源的其它界面，这里让它自己复制一份。
            m_Wrapper.SetWrapTarget(m_Holder, true);
            if (m_Mount != null)
            {
                m_Mount.SetNativeObject(m_Wrapper);
            }
        }

        public override void Dispose()
        {
            Clear();

            if (m_Wrapper != null)
            {
                m_Wrapper.onUpdate -= OnWrapperUpdate;
            }

            // GObject.Dispose 会 Dispose 掉 displayObject（也就是 m_Wrapper），
            // 连带销毁 m_Holder 与它下面所有动画对象。
            base.Dispose();

            m_Wrapper = null;
            m_Holder = null;
            m_Mount = null;
            onComplete = null;
            onEvent = null;
        }

        #endregion

        #region 挂载 / 释放

        /// <summary>
        /// 加载并挂上一个动画资源。
        /// </summary>
        /// <param name="_address">Addressables 地址（指向 SkeletonDataAsset）</param>
        /// <param name="_defaultAnimation">挂上后自动播放的动画，留空则不播</param>
        /// <param name="_loop">默认动画是否循环</param>
        /// <returns>是否挂成功</returns>
        public bool SetSpine(string _address, string _defaultAnimation = null, bool _loop = true)
        {
            if (string.IsNullOrEmpty(_address))
            {
                Clear();
                return false;
            }

            m_DefaultAnimation = _defaultAnimation;
            m_DefaultLoop = _loop;

            // 同一套资源不重建，直接重播默认动画
            if (m_Player != null && m_Address == _address)
            {
                PlayDefault();
                return true;
            }

            Clear();

            m_Address = _address;
            m_Player = CreatePlayer(_address);
            if (m_Player == null)
            {
                m_Address = null;
                return false;
            }

            m_Player.onComplete += OnPlayerComplete;
            m_Player.onEvent += OnPlayerEvent;

            AttachPlayer(m_Player);
            PlayDefault();
            return true;
        }

        /// <summary>换动画资源时先拿走旧的（保留 holder 复用）</summary>
        public void Clear()
        {
            if (m_Player == null)
            {
                return;
            }

            m_Player.onComplete -= OnPlayerComplete;
            m_Player.onEvent -= OnPlayerEvent;
            m_Player.Dispose();
            m_Player = null;
            m_Address = null;
            m_NeedApplyLayout = false;
            m_FitApplied = false;
        }

        /// <summary>把播放器挂到 UI 挂点下面，并把渲染器交给 GoWrapper 管理</summary>
        private void AttachPlayer(IUIAnimPlayer _player)
        {
            GameObject kRoot = _player.root;
            kRoot.transform.SetParent(m_Holder.transform, false);
            kRoot.transform.localPosition = Vector3.zero;
            kRoot.transform.localRotation = Quaternion.identity;
            kRoot.transform.localScale = Vector3.one;

            // 原生对象要落在 UI 相机能看到的层上，否则界面里渲染不出来
            int kLayer = m_Holder.layer;
            Transform[] kTransforms = kRoot.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < kTransforms.Length; i++)
            {
                kTransforms[i].gameObject.layer = kLayer;
            }

            // 渲染器是挂载之后才有的，必须重新收集，GoWrapper 才会接管排序 / 裁剪
            m_Wrapper.CacheRenderers();

            m_NeedApplyLayout = true;
            m_LayoutRetry = 0;
            m_FitApplied = false;
            ApplyLayout();
        }

        /// <summary>
        /// 创建播放器。接别的动画格式（例如 DragonBones）时，在子类里重写这个方法即可。
        /// </summary>
        protected virtual IUIAnimPlayer CreatePlayer(string _address)
        {
            return SpineUIAnimPlayer.Load(_address);
        }

        private void PlayDefault()
        {
            if (m_Player == null || string.IsNullOrEmpty(m_DefaultAnimation))
            {
                return;
            }

            Play(m_DefaultAnimation, m_DefaultLoop);
        }

        #endregion

        #region 播放控制

        /// <summary>播放动画</summary>
        public void Play(string _animation, bool _loop = true, float _speed = 1f, int _track = 0)
        {
            if (m_Player == null)
            {
                return;
            }

            m_Player.Play(_animation, _loop, _speed, _track);
        }

        /// <summary>停止所有轨道（停在当前帧，不隐藏）</summary>
        public void Stop()
        {
            if (m_Player == null)
            {
                return;
            }

            m_Player.Stop();
        }

        /// <summary>暂停（速度置 0）</summary>
        public void Pause()
        {
            SetTimeScale(0f);
        }

        /// <summary>恢复暂停前的速度</summary>
        public void Resume()
        {
            SetTimeScale(1f);
        }

        /// <summary>全局播放速度</summary>
        public void SetTimeScale(float _timeScale)
        {
            if (m_Player == null)
            {
                return;
            }

            m_Player.SetTimeScale(_timeScale);
        }

        /// <summary>换皮肤（Spine 的多套换装），传空串恢复默认皮肤</summary>
        public void SetSkin(string _skin)
        {
            if (m_Player == null)
            {
                return;
            }

            m_Player.SetSkin(_skin);
        }

        /// <summary>水平翻转（朝左 / 朝右）</summary>
        public void SetFlipX(bool _flip)
        {
            if (m_Player == null)
            {
                return;
            }

            m_Player.SetFlipX(_flip);
        }

        /// <summary>资源里是否有这个动画</summary>
        public bool HasAnimation(string _animation)
        {
            return m_Player != null && m_Player.HasAnimation(_animation);
        }

        /// <summary>资源里是否有这个皮肤</summary>
        public bool HasSkin(string _skin)
        {
            return m_Player != null && m_Player.HasSkin(_skin);
        }

        /// <summary>资源里全部动画名（用于调试 / 动态取列表）</summary>
        public string[] GetAnimations()
        {
            return m_Player == null ? null : m_Player.animationNames;
        }

        private void OnPlayerComplete(string _animation)
        {
            if (onComplete != null)
            {
                onComplete(_animation);
            }
        }

        private void OnPlayerEvent(string _eventName, string _stringParam)
        {
            if (onEvent != null)
            {
                onEvent(_eventName, _stringParam);
            }
        }

        #endregion

        #region 自适应布局

        /// <summary>
        /// 设置自适应方式与目标尺寸。
        /// </summary>
        /// <param name="_fit">缩放方式</param>
        /// <param name="_width">目标宽（UI 像素），传 0 用组件自身宽度</param>
        /// <param name="_height">目标高（UI 像素），传 0 用组件自身高度</param>
        /// <param name="_anchor">包围盒的哪个点对齐到挂点</param>
        public void SetFit(UIAnimFit _fit, float _width = 0f, float _height = 0f,
            UIAnimAnchor _anchor = UIAnimAnchor.BottomCenter)
        {
            bool kSame = m_FitApplied
                && m_Fit == _fit
                && Mathf.Approximately(m_FitWidth, _width)
                && Mathf.Approximately(m_FitHeight, _height)
                && m_Anchor == _anchor;

            m_Fit = _fit;
            m_FitWidth = _width;
            m_FitHeight = _height;
            m_Anchor = _anchor;

            // 参数没变、且已经按这套参数摆好，就不要再按当前动画姿态重新算一次缩放
            if (kSame)
            {
                return;
            }

            m_NeedApplyLayout = true;
            m_LayoutRetry = 0;
            ApplyLayout();
        }

        /// <summary>整体额外缩放（叠加在「1 单位 = 1 像素」与自适应结果上）</summary>
        public void SetScale(float _scale)
        {
            m_ExtraScale = _scale <= 0f ? 1f : _scale;
            m_NeedApplyLayout = true;
            m_LayoutRetry = 0;
            ApplyLayout();
        }

        /// <summary>在挂点坐标系里微调位置（UI 像素，X 向右、Y 向下）</summary>
        public void SetOffset(float _x, float _y)
        {
            m_Offset = new Vector2(_x, _y);
            m_NeedApplyLayout = true;
            m_LayoutRetry = 0;
            ApplyLayout();
        }

        /// <summary>强制重新算一次缩放与对齐（组件尺寸变化后调用）</summary>
        public void Refit()
        {
            m_NeedApplyLayout = true;
            m_LayoutRetry = 0;
            ApplyLayout();
        }

        /// <summary>
        /// 按当前设置摆放动画对象。首帧还没生成网格时拿不到包围盒，会留到下一帧重试。
        /// </summary>
        protected void ApplyLayout()
        {
            if (m_Player == null || m_Holder == null)
            {
                return;
            }

            Bounds kBounds;
            if (!m_Player.TryGetLocalBounds(out kBounds) ||
                (kBounds.size.x <= 0.0001f && kBounds.size.y <= 0.0001f))
            {
                // 首帧网格还没生成，下一帧再试；试够次数就放弃，免得一直取顶点产生 GC
                m_LayoutRetry++;
                if (m_LayoutRetry > c_MaxLayoutRetry)
                {
                    m_NeedApplyLayout = false;
                    Debug.LogWarning("[SpineAnimCom] 取不到动画网格包围盒，自适应布局已放弃：" + name);
                }
                return;
            }

            // 先把包围盒换算成 UI 像素，再算自适应比例，最后乘回「1 单位 = 多少像素」
            float kUnit = m_Player.unitScale;
            float kScale = m_ExtraScale * kUnit;
            Vector2 kTarget = GetFitTarget();
            if (m_Fit != UIAnimFit.None && kTarget.x > 0f && kTarget.y > 0f)
            {
                float kBoxWidth = kBounds.size.x * kUnit;
                float kBoxHeight = kBounds.size.y * kUnit;
                float kScaleX = kBoxWidth > 0.0001f ? kTarget.x / kBoxWidth : 1f;
                float kScaleY = kBoxHeight > 0.0001f ? kTarget.y / kBoxHeight : 1f;

                switch (m_Fit)
                {
                    case UIAnimFit.FitHeight:
                        kScale *= kScaleY;
                        break;
                    case UIAnimFit.FitWidth:
                        kScale *= kScaleX;
                        break;
                    case UIAnimFit.Contain:
                        kScale *= Mathf.Min(kScaleX, kScaleY);
                        break;
                    case UIAnimFit.Cover:
                        kScale *= Mathf.Max(kScaleX, kScaleY);
                        break;
                }
            }

            Vector2 kAnchorPoint = GetAnchorPoint(kBounds);

            // 挂点局部空间 Y 向上，UI 坐标 Y 向下：向下偏移要取负
            m_Player.root.transform.localScale = new Vector3(kScale, kScale, 1f);
            m_Player.root.transform.localPosition = new Vector3(
                -kAnchorPoint.x * kScale + m_Offset.x,
                -kAnchorPoint.y * kScale - m_Offset.y,
                0f);

            m_NeedApplyLayout = false;
            m_LayoutRetry = 0;
            m_FitApplied = true;
        }

        private Vector2 GetFitTarget()
        {
            float kWidth = m_FitWidth > 0f ? m_FitWidth : this.width;
            float kHeight = m_FitHeight > 0f ? m_FitHeight : this.height;
            return new Vector2(kWidth, kHeight);
        }

        private Vector2 GetAnchorPoint(Bounds _bounds)
        {
            float kLeft = _bounds.min.x;
            float kRight = _bounds.max.x;
            float kBottom = _bounds.min.y;
            float kTop = _bounds.max.y;
            float kCenterX = _bounds.center.x;
            float kCenterY = _bounds.center.y;

            switch (m_Anchor)
            {
                case UIAnimAnchor.TopLeft:
                    return new Vector2(kLeft, kTop);
                case UIAnimAnchor.TopCenter:
                    return new Vector2(kCenterX, kTop);
                case UIAnimAnchor.TopRight:
                    return new Vector2(kRight, kTop);
                case UIAnimAnchor.MiddleLeft:
                    return new Vector2(kLeft, kCenterY);
                case UIAnimAnchor.MiddleCenter:
                    return new Vector2(kCenterX, kCenterY);
                case UIAnimAnchor.MiddleRight:
                    return new Vector2(kRight, kCenterY);
                case UIAnimAnchor.BottomLeft:
                    return new Vector2(kLeft, kBottom);
                case UIAnimAnchor.BottomRight:
                    return new Vector2(kRight, kBottom);
                default:
                    return new Vector2(kCenterX, kBottom);
            }
        }

        #endregion

        #region 每帧同步

        private void OnWrapperUpdate(UpdateContext _context)
        {
            if (m_Player == null)
            {
                return;
            }

            // 透明度跟随 FairyGUI 显示列表（窗口淡入淡出、置灰……），
            // 原生渲染器不在这套材质体系里，只能手动同步。
            m_Player.SetAlpha(_context.alpha * m_Wrapper.alpha);

            if (m_NeedApplyLayout)
            {
                ApplyLayout();
            }
        }

        #endregion
    }
}
