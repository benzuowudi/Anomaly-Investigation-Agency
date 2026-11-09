using System;
using UnityEngine;

namespace Framework.Script.UI.Anim
{
    /// <summary>
    /// UI 里原生动画对象（Spine / DragonBones / 普通 Animator……）的统一播放接口。
    ///
    /// 组件 <see cref="SpineAnimCom"/> 只依赖这个接口，不直接依赖任何第三方运行时；
    /// 具体实现见 SpineUIAnimPlayer（Spine），以后接 DragonBones 只需要再写一个实现类。
    /// </summary>
    public interface IUIAnimPlayer
    {
        /// <summary>动画对象的根节点（会被挂到 UI 挂点下面）</summary>
        GameObject root { get; }

        /// <summary>资源是否已就绪（可以播放）</summary>
        bool isReady { get; }

        /// <summary>资源里包含的全部动画名</summary>
        string[] animationNames { get; }

        /// <summary>
        /// 一个资源本地单位换算成多少 UI 像素。
        /// 例：Spine 的 SkeletonDataAsset.scale = 0.01 时这里返回 100，
        /// 组件会先乘上它再按需要做自适应缩放，保证「1 UI 像素 = 1 像素」。
        /// </summary>
        float unitScale { get; }

        /// <summary>资源里是否有这个动画</summary>
        bool HasAnimation(string _name);

        /// <summary>资源里是否有这个皮肤</summary>
        bool HasSkin(string _skin);

        /// <summary>播放动画</summary>
        /// <param name="_name">动画名</param>
        /// <param name="_loop">是否循环</param>
        /// <param name="_timeScale">单条轨道的播放速度</param>
        /// <param name="_track">轨道号，一般 0</param>
        void Play(string _name, bool _loop, float _timeScale, int _track);

        /// <summary>停止所有轨道</summary>
        void Stop();

        /// <summary>全局播放速度（0 = 暂停）</summary>
        void SetTimeScale(float _timeScale);

        /// <summary>水平翻转</summary>
        void SetFlipX(bool _flip);

        /// <summary>换皮肤，空字符串表示资源默认皮肤</summary>
        void SetSkin(string _skin);

        /// <summary>整条动画的透明度（跟随 FairyGUI 显示列表的淡入淡出）</summary>
        void SetAlpha(float _alpha);

        /// <summary>取当前姿势的局部包围盒，用于自适应缩放与对齐；拿不到时返回 false（通常是首帧还没生成网格）</summary>
        bool TryGetLocalBounds(out Bounds _bounds);

        /// <summary>销毁动画对象</summary>
        void Dispose();

        /// <summary>动画播完（参数：动画名）</summary>
        event Action<string> onComplete;

        /// <summary>动画帧事件（参数：事件名、字符串参数）</summary>
        event Action<string, string> onEvent;
    }
}
