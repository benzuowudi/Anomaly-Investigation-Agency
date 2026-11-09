using System;
using Framework.Script.Manager;
using Spine.Unity;
using UnityEngine;

namespace Framework.Script.UI.Anim
{
    /// <summary>
    /// <see cref="IUIAnimPlayer"/> 的 Spine 实现（spine-unity 4.x 运行时，见 Assets/Spine）。
    ///
    /// 这里刻意只用了 spine-unity 最稳定的一层 API（SkeletonAnimation / AnimationState / Skeleton），
    /// 以后升级运行时如果需要调整，改这一个文件就够了。
    /// </summary>
    public class SpineUIAnimPlayer : MonoBehaviour, IUIAnimPlayer
    {
        private SkeletonAnimation m_Animation;
        private SkeletonDataAsset m_Asset;
        private MeshFilter m_MeshFilter;
        private MeshRenderer m_MeshRenderer;
        private string[] m_AnimationNames = new string[0];

        public event Action<string> onComplete;
        public event Action<string, string> onEvent;

        #region 创建

        /// <summary>
        /// 按 Addressables 地址加载 SkeletonDataAsset 并创建播放器。
        /// </summary>
        public static SpineUIAnimPlayer Load(string _address)
        {
            SkeletonDataAsset kAsset = ResManager.Instance.LoadAsset<SkeletonDataAsset>(_address);
            if (kAsset == null)
            {
                Debug.LogError("[SpineUIAnimPlayer] 加载 SkeletonDataAsset 失败：" + _address);
                return null;
            }

            return Create(kAsset);
        }

        /// <summary>用已经加载好的资源创建播放器（父节点由调用方决定）</summary>
        public static SpineUIAnimPlayer Create(SkeletonDataAsset _asset)
        {
            if (_asset == null)
            {
                return null;
            }

            GameObject kGameObject = new GameObject("Spine_" + _asset.name);
            SpineUIAnimPlayer kPlayer = kGameObject.AddComponent<SpineUIAnimPlayer>();
            kPlayer.Init(_asset);
            return kPlayer;
        }

        private void Init(SkeletonDataAsset _asset)
        {
            m_Asset = _asset;

            m_Animation = gameObject.AddComponent<SkeletonAnimation>();
            m_Animation.skeletonDataAsset = _asset;
            m_Animation.Initialize(true);

            m_MeshFilter = gameObject.GetComponent<MeshFilter>();
            m_MeshRenderer = gameObject.GetComponent<MeshRenderer>();

            if (m_Animation.AnimationState != null)
            {
                m_Animation.AnimationState.Complete += OnTrackComplete;
                m_Animation.AnimationState.Event += OnTrackEvent;
            }

            CacheAnimationNames();
        }

        private void CacheAnimationNames()
        {
            if (m_Animation == null || m_Animation.Skeleton == null || m_Animation.Skeleton.Data == null)
            {
                m_AnimationNames = new string[0];
                return;
            }

            var kAnimations = m_Animation.Skeleton.Data.Animations;
            m_AnimationNames = new string[kAnimations.Count];
            for (int i = 0; i < kAnimations.Count; i++)
            {
                m_AnimationNames[i] = kAnimations.Items[i].Name;
            }
        }

        #endregion

        #region IUIAnimPlayer

        public GameObject root
        {
            get { return gameObject; }
        }

        public bool isReady
        {
            get { return m_Animation != null && m_Animation.AnimationState != null && m_Animation.Skeleton != null; }
        }

        public string[] animationNames
        {
            get { return m_AnimationNames; }
        }

        public float unitScale
        {
            get
            {
                // spine-unity 会把骨架按 SkeletonDataAsset.scale 缩放（本项目角色是 0.01），
                // 这里换算回 UI 像素：1 / 0.01 = 100。
                float kScale = m_Asset == null ? 0f : m_Asset.scale;
                return kScale <= 0f ? 1f : 1f / kScale;
            }
        }

        public bool HasAnimation(string _name)
        {
            if (!isReady || string.IsNullOrEmpty(_name))
            {
                return false;
            }

            return m_Animation.Skeleton.Data.FindAnimation(_name) != null;
        }

        public bool HasSkin(string _skin)
        {
            if (!isReady || string.IsNullOrEmpty(_skin))
            {
                return false;
            }

            return m_Animation.Skeleton.Data.FindSkin(_skin) != null;
        }

        public void Play(string _name, bool _loop, float _timeScale, int _track)
        {
            if (!isReady || string.IsNullOrEmpty(_name))
            {
                return;
            }

            if (m_Animation.Skeleton.Data.FindAnimation(_name) == null)
            {
                Debug.LogWarning("[SpineUIAnimPlayer] 资源里没有动画：" + _name + "（" + gameObject.name + "）");
                return;
            }

            Spine.TrackEntry kEntry = m_Animation.AnimationState.SetAnimation(_track, _name, _loop);
            if (kEntry != null)
            {
                kEntry.TimeScale = _timeScale <= 0f ? 1f : _timeScale;
            }
        }

        public void Stop()
        {
            if (!isReady)
            {
                return;
            }

            m_Animation.AnimationState.ClearTracks();
        }

        public void SetTimeScale(float _timeScale)
        {
            if (m_Animation == null)
            {
                return;
            }

            m_Animation.timeScale = _timeScale;
        }

        public void SetFlipX(bool _flip)
        {
            if (!isReady)
            {
                return;
            }

            m_Animation.Skeleton.ScaleX = _flip ? -1f : 1f;
        }

        public void SetSkin(string _skin)
        {            if (!isReady)
            {
                return;
            }

            Spine.Skeleton kSkeleton = m_Animation.Skeleton;
            Spine.Skin kSkin = string.IsNullOrEmpty(_skin)
                ? kSkeleton.Data.DefaultSkin
                : kSkeleton.Data.FindSkin(_skin);

            if (kSkin == null)
            {
                Debug.LogWarning("[SpineUIAnimPlayer] 资源里没有皮肤：" + _skin + "（" + gameObject.name + "）");
                return;
            }

            kSkeleton.SetSkin(kSkin);
            kSkeleton.SetSlotsToSetupPose();
        }

        public void SetAlpha(float _alpha)
        {
            if (!isReady)
            {
                return;
            }

            // Skeleton.A 是整条骨架的透明度乘数，Spine 的 mesh 颜色会乘上它
            m_Animation.Skeleton.A = Mathf.Clamp01(_alpha);
        }

        public bool TryGetLocalBounds(out Bounds _bounds)
        {
            _bounds = default(Bounds);

            if (m_MeshFilter == null)
            {
                return false;
            }

            Mesh kMesh = m_MeshFilter.sharedMesh;
            if (kMesh == null || kMesh.vertexCount == 0)
            {
                return false;
            }

            // 不要读 mesh.vertices：spine-unity 是用 Mesh 顶点缓冲上传的，
            // CPU 侧读回来会是未初始化数据（NaN）。mesh.bounds 是有效的。
            Bounds kMeshBounds = kMesh.bounds;
            if (kMeshBounds.size.x <= 0.0001f && kMeshBounds.size.y <= 0.0001f)
            {
                return false;
            }

            _bounds = kMeshBounds;
            return true;
        }

        public void Dispose()
        {
            if (m_Animation != null && m_Animation.AnimationState != null)
            {
                m_Animation.AnimationState.Complete -= OnTrackComplete;
                m_Animation.AnimationState.Event -= OnTrackEvent;
            }

            m_Animation = null;
            m_Asset = null;
            m_MeshFilter = null;
            m_MeshRenderer = null;
            onComplete = null;
            onEvent = null;

            if (this != null)
            {
                Destroy(gameObject);
            }
        }

        #endregion

        #region 回调

        private void OnTrackComplete(Spine.TrackEntry _entry)
        {
            if (_entry == null || _entry.TrackIndex != 0)
            {
                return;
            }

            string kAnimation = _entry.Animation == null ? null : _entry.Animation.Name;
            if (onComplete != null)
            {
                onComplete(kAnimation);
            }
        }

        private void OnTrackEvent(Spine.TrackEntry _entry, Spine.Event _event)
        {
            if (_event == null || _event.Data == null)
            {
                return;
            }

            if (onEvent != null)
            {
                onEvent(_event.Data.Name, _event.String);
            }
        }

        #endregion
    }
}
