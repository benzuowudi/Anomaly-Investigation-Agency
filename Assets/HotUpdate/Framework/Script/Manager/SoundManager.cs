using FairyGUI;
using Framework.Script.Base;
using System.Collections.Generic;
using UnityEngine;

namespace Framework.Script.Manager
{
    /// <summary>
    /// 声音管理器：背景音乐（BGM）与音效（SFX）两条完全独立的通道。
    ///
    /// - BGM：独占一个 AudioSource，同一时间只会播放一首；切歌支持淡出 → 换曲 → 淡入。
    /// - SFX：AudioSource 通道池，同一时间可以播放多个音效，互不打断；播完自动回收通道（清掉 clip 引用），
    ///   通道用满时抢占最早开始的一路，空闲通道超过一定时长自动销毁（池子里至少保留 2 路）。
    /// - 两路的音量 / 静音互不影响，修改后立即生效并写入 PlayerPrefs，下次启动自动恢复。
    /// - 音频按 Addressables 地址（一般是资源名）加载，也可以直接传 AudioClip。
    /// - FairyGUI 的按钮、界面音效走 Stage 自己的 AudioSource，
    ///   这里在音效音量变化时同步 Stage.soundVolume，让 UI 音效同样受音效音量 / 静音控制。
    ///
    /// 用法：
    ///   Game.Sound.PlayBGM("bgm_main");
    ///   Game.Sound.PlayBGM("bgm_battle", _fadeTime: 1.5f);
    ///   Game.Sound.PlaySFX("sfx_click", 0.8f);
    ///   Game.Sound.SFXVolume = 0.5f;
    /// </summary>
    public class SoundManager : Singleton<SoundManager>
    {
        // 音效同时最多播放的通道数
        private const int c_MaxSFXChannelCount = 8;

        // 通道池至少保留的空闲通道数（低于这个数量不再销毁，避免频繁创建 / 销毁）
        private const int c_MinSFXChannelCount = 2;

        // 空闲通道自动销毁的等待时长（秒）：播放结束的通道先归还池子，空闲超过这个时长才销毁
        private const float c_SFXIdleRecycleTime = 30f;

        // 默认音量：背景音乐比音效略低，避免盖住音效；玩家调整后以 PlayerPrefs 里的值为准
        private const float c_DefaultBGMVolume = 0.6f;
        private const float c_DefaultSFXVolume = 1f;

        // 音量 / 静音的持久化键
        private const string c_PrefBGMVolume = "Sound.BGMVolume";
        private const string c_PrefSFXVolume = "Sound.SFXVolume";
        private const string c_PrefBGMMute = "Sound.BGMMute";
        private const string c_PrefSFXMute = "Sound.SFXMute";

        // 所有 AudioSource 的挂载节点（跟着 Game 常驻）
        private Transform m_Root;

        // 背景音乐通道：同一时间只会有这一路在响
        private AudioSource m_BGMSource;
        private string m_BGMKey;
        private bool m_IsBGMPaused;

        // 背景音乐的淡变状态
        private BGMFadeState m_BGMFadeState = BGMFadeState.None;
        private float m_BGMFadeTime;
        private float m_BGMFadeElapsed;
        private float m_BGMFadeStartVolume;

        // 淡出结束后要切换到的曲目（clip 为空表示淡出后直接停止）
        private AudioClip m_PendingBGMClip;
        private string m_PendingBGMKey;
        private bool m_PendingBGMLoop;

        // 音效通道池
        private readonly List<SFXChannel> m_SFXChannelList = new List<SFXChannel>();

        // 音效播放序号，通道用满时用它找出最早开始的一路
        private long m_SFXPlayOrder;

        // 音效通道编号，只用于给通道节点命名（销毁后重建不会重名）
        private int m_SFXChannelId;

        // 音频地址 -> 已加载的 AudioClip（Addressables 的句柄由 ResManager 管理）
        private readonly Dictionary<string, AudioClip> m_ClipDic = new Dictionary<string, AudioClip>();

        // 两路音量（0~1）与静音，完全独立
        private float m_BGMVolume = c_DefaultBGMVolume;
        private float m_SFXVolume = c_DefaultSFXVolume;
        private bool m_IsBGMMute;
        private bool m_IsSFXMute;

        /// <summary>
        /// 初始化：读取玩家设置，创建常驻的声音节点与背景音乐通道
        /// </summary>
        protected override void OnInit()
        {
            LoadSettings();

            GameObject kRoot = new GameObject("[SoundRoot]");
            // 挂在 DontDestroy 下，随 Game 一起跨场景常驻；GameManager 还没绑定时兜底自己常驻
            if (GameManager.IsCreated && GameManager.Instance.m_Dontdestroy != null)
            {
                kRoot.transform.SetParent(GameManager.Instance.m_Dontdestroy, false);
            }
            else
            {
                UnityEngine.Object.DontDestroyOnLoad(kRoot);
            }
            m_Root = kRoot.transform;

            // 管理器本身不是 MonoBehaviour，淡变需要逐帧推进，用驱动组件把 Update 转过来
            SoundRunner kRunner = kRoot.AddComponent<SoundRunner>();
            kRunner.OnUpdate = Tick;

            m_BGMSource = CreateAudioSource("BGM", true);
            // 背景音乐只有一路，给最高优先级，避免被大量音效挤掉
            m_BGMSource.priority = 0;
            ApplySFXVolume();

            Debug.Log("[SoundManager] 初始化完成，背景音乐音量：" + m_BGMVolume + "，音效音量：" + m_SFXVolume);
        }

        #region 音量与静音

        /// <summary>
        /// 背景音乐音量（0~1），与音效音量互不影响
        /// </summary>
        public float BGMVolume
        {
            get { return m_BGMVolume; }
            set
            {
                m_BGMVolume = Mathf.Clamp01(value);
                PlayerPrefs.SetFloat(c_PrefBGMVolume, m_BGMVolume);
                ApplyBGMVolume();
            }
        }

        /// <summary>
        /// 音效音量（0~1），与背景音乐音量互不影响
        /// </summary>
        public float SFXVolume
        {
            get { return m_SFXVolume; }
            set
            {
                m_SFXVolume = Mathf.Clamp01(value);
                PlayerPrefs.SetFloat(c_PrefSFXVolume, m_SFXVolume);
                ApplySFXVolume();
            }
        }

        /// <summary>
        /// 背景音乐是否静音（不改变 BGMVolume，取消静音后恢复原音量）
        /// </summary>
        public bool IsBGMMute
        {
            get { return m_IsBGMMute; }
            set
            {
                m_IsBGMMute = value;
                PlayerPrefs.SetInt(c_PrefBGMMute, m_IsBGMMute ? 1 : 0);
                ApplyBGMVolume();
            }
        }

        /// <summary>
        /// 音效是否静音（不改变 SFXVolume，取消静音后恢复原音量）
        /// </summary>
        public bool IsSFXMute
        {
            get { return m_IsSFXMute; }
            set
            {
                m_IsSFXMute = value;
                PlayerPrefs.SetInt(c_PrefSFXMute, m_IsSFXMute ? 1 : 0);
                ApplySFXVolume();
            }
        }

        #endregion

        #region 背景音乐

        /// <summary>
        /// 背景音乐是否正在播放（暂停时返回 false）
        /// </summary>
        public bool IsBGMPlaying
        {
            get { return m_BGMSource != null && m_BGMSource.isPlaying; }
        }

        /// <summary>
        /// 当前背景音乐（Address 或 clip.name），没有播放时为空
        /// </summary>
        public string CurrentBGM
        {
            get { return m_BGMKey; }
        }

        /// <summary>
        /// 播放背景音乐：同一时间只会有一首，重复播放当前曲目会被忽略
        /// </summary>
        /// <param name="_name">Addressables 地址 / 资源名</param>
        /// <param name="_loop">是否循环</param>
        /// <param name="_fadeTime">淡入淡出时长，0 表示立即切换</param>
        public void PlayBGM(string _name, bool _loop = true, float _fadeTime = 0f)
        {
            if (string.IsNullOrEmpty(_name))
            {
                Debug.LogWarning("[SoundManager] 背景音乐名不能为空");
                return;
            }

            // 同一首已经在播（或正在淡出切到它），不重播
            if (m_BGMKey == _name && m_BGMSource != null && m_BGMSource.isPlaying
                && m_BGMFadeState != BGMFadeState.FadeOut)
            {
                return;
            }
            if (m_BGMFadeState == BGMFadeState.FadeOut && m_PendingBGMKey == _name)
            {
                return;
            }

            AudioClip kClip = GetClip(_name);
            if (kClip != null)
            {
                PlayBGMInternal(kClip, _name, _loop, _fadeTime);
            }
        }

        /// <summary>
        /// 播放背景音乐（直接传入 AudioClip，曲名取 clip.name）
        /// </summary>
        public void PlayBGM(AudioClip _clip, bool _loop = true, float _fadeTime = 0f)
        {
            if (_clip == null)
            {
                return;
            }

            PlayBGMInternal(_clip, _clip.name, _loop, _fadeTime);
        }

        /// <summary>
        /// 停止背景音乐
        /// </summary>
        /// <param name="_fadeTime">淡出时长，0 表示立即停止</param>
        public void StopBGM(float _fadeTime = 0f)
        {
            if (m_BGMSource == null)
            {
                return;
            }

            ClearPendingBGM();
            m_BGMKey = null;
            m_IsBGMPaused = false;

            if (_fadeTime > 0f && m_BGMSource.isPlaying)
            {
                StartBGMFadeOut(_fadeTime);
                return;
            }

            m_BGMFadeState = BGMFadeState.None;
            m_BGMSource.Stop();
            m_BGMSource.volume = GetBGMVolume();
        }

        /// <summary>
        /// 暂停背景音乐（音效不受影响）
        /// </summary>
        public void PauseBGM()
        {
            if (m_BGMSource == null || !m_BGMSource.isPlaying)
            {
                return;
            }

            m_BGMSource.Pause();
            m_IsBGMPaused = true;
        }

        /// <summary>
        /// 继续播放暂停中的背景音乐
        /// </summary>
        public void ResumeBGM()
        {
            if (m_BGMSource == null || !m_IsBGMPaused)
            {
                return;
            }

            m_BGMSource.UnPause();
            m_IsBGMPaused = false;
        }

        #endregion

        #region 音效

        /// <summary>
        /// 播放音效：可以同时播放多个，互不打断
        /// </summary>
        /// <param name="_name">Addressables 地址 / 资源名</param>
        /// <param name="_volume">单次音量（0~1），会再乘以音效总音量</param>
        /// <param name="_pitch">音调，1 为原声</param>
        public void PlaySFX(string _name, float _volume = 1f, float _pitch = 1f)
        {
            if (string.IsNullOrEmpty(_name))
            {
                Debug.LogWarning("[SoundManager] 音效名不能为空");
                return;
            }

            AudioClip kClip = GetClip(_name);
            if (kClip != null)
            {
                PlaySFXInternal(kClip, _name, _volume, _pitch);
            }
        }

        /// <summary>
        /// 播放音效（直接传入 AudioClip，停止时用 clip.name 作为名字）
        /// </summary>
        public void PlaySFX(AudioClip _clip, float _volume = 1f, float _pitch = 1f)
        {
            if (_clip == null)
            {
                return;
            }

            PlaySFXInternal(_clip, _clip.name, _volume, _pitch);
        }

        /// <summary>
        /// 停止指定名字的音效（只停这个音效，其他音效继续播放）
        /// </summary>
        /// <param name="_name">播放时用的 Address / 资源名</param>
        public void StopSFX(string _name)
        {
            if (string.IsNullOrEmpty(_name))
            {
                return;
            }

            for (int i = 0; i < m_SFXChannelList.Count; i++)
            {
                SFXChannel kChannel = m_SFXChannelList[i];
                if (kChannel.m_IsPlaying && kChannel.m_Key == _name)
                {
                    RecycleSFXChannel(kChannel);
                }
            }
        }

        /// <summary>
        /// 停止全部音效（背景音乐不受影响）
        /// </summary>
        public void StopAllSFX()
        {
            for (int i = 0; i < m_SFXChannelList.Count; i++)
            {
                SFXChannel kChannel = m_SFXChannelList[i];
                if (kChannel.m_IsPlaying)
                {
                    RecycleSFXChannel(kChannel);
                }
            }
        }

        #endregion

        #region 资源

        /// <summary>
        /// 清空音频缓存（例如调用 ResManager.UnLoadAll 之后），不影响正在播放的声音
        /// </summary>
        public void ClearClipCache()
        {
            m_ClipDic.Clear();
        }

        /// <summary>
        /// 按 Address 取 AudioClip，加载过的会缓存，避免重复走 Addressables
        /// </summary>
        private AudioClip GetClip(string _name)
        {
            if (string.IsNullOrEmpty(_name))
            {
                return null;
            }

            AudioClip kClip;
            if (m_ClipDic.TryGetValue(_name, out kClip) && kClip != null)
            {
                return kClip;
            }

            kClip = Game.Res.LoadAsset<AudioClip>(_name);
            if (kClip == null)
            {
                Debug.LogWarning("[SoundManager] 找不到音频资源：" + _name + "（需要先加入 Addressables）");
                return null;
            }

            m_ClipDic[_name] = kClip;
            return kClip;
        }

        #endregion

        #region 内部实现

        /// <summary>
        /// 把 Update 转给管理器：推进背景音乐淡变、回收播放结束的音效通道
        /// </summary>
        private void Tick(float _deltaTime)
        {
            UpdateBGMFade(_deltaTime);
            UpdateSFXChannelRecycle(_deltaTime);
        }

        /// <summary>
        /// 播放背景音乐的实际实现
        /// </summary>
        private void PlayBGMInternal(AudioClip _clip, string _key, bool _loop, float _fadeTime)
        {
            if (_clip == null || m_BGMSource == null)
            {
                return;
            }

            // 正在淡出切别的曲子，又要求播放当前这首：取消切换并淡回来
            if (m_BGMFadeState == BGMFadeState.FadeOut && m_BGMKey == _key && m_BGMSource.isPlaying)
            {
                ClearPendingBGM();
                StartBGMFadeIn(m_BGMFadeTime, m_BGMSource.volume);
                return;
            }

            // 同一首已经在播（或正在淡出切到它），不重播
            if (m_BGMKey == _key && m_BGMSource.isPlaying)
            {
                return;
            }
            if (m_BGMFadeState == BGMFadeState.FadeOut && m_PendingBGMKey == _key)
            {
                return;
            }

            // 没有在播（或要求立即切换）：直接换曲
            if (!m_BGMSource.isPlaying || _fadeTime <= 0f)
            {
                m_BGMFadeState = BGMFadeState.None;
                ClearPendingBGM();

                if (_fadeTime > 0f)
                {
                    SetBGMClip(_clip, _key, _loop, 0f);
                    StartBGMFadeIn(_fadeTime);
                }
                else
                {
                    SetBGMClip(_clip, _key, _loop, GetBGMVolume());
                }
                return;
            }

            // 正在播放别的曲子：先淡出，结束后在 UpdateBGMFade 里换曲并淡入
            m_PendingBGMClip = _clip;
            m_PendingBGMKey = _key;
            m_PendingBGMLoop = _loop;
            StartBGMFadeOut(_fadeTime);
        }

        /// <summary>
        /// 立即换曲并播放（不做淡变）
        /// </summary>
        private void SetBGMClip(AudioClip _clip, string _key, bool _loop, float _volume)
        {
            m_BGMKey = _key;
            m_IsBGMPaused = false;

            m_BGMSource.Stop();
            m_BGMSource.clip = _clip;
            m_BGMSource.loop = _loop;
            m_BGMSource.volume = _volume;
            m_BGMSource.Play();
        }

        /// <summary>
        /// 开始淡出：从当前音量降到 0
        /// </summary>
        private void StartBGMFadeOut(float _fadeTime)
        {
            m_BGMFadeState = BGMFadeState.FadeOut;
            m_BGMFadeTime = Mathf.Max(0.01f, _fadeTime);
            m_BGMFadeElapsed = 0f;
            m_BGMFadeStartVolume = m_BGMSource.volume;
        }

        /// <summary>
        /// 开始淡入：从指定音量升到当前设置的目标音量
        /// </summary>
        private void StartBGMFadeIn(float _fadeTime, float _fromVolume = 0f)
        {
            m_BGMFadeState = BGMFadeState.FadeIn;
            m_BGMFadeTime = Mathf.Max(0.01f, _fadeTime);
            m_BGMFadeElapsed = 0f;
            m_BGMFadeStartVolume = _fromVolume;
        }

        /// <summary>
        /// 推进一步淡变；淡出结束后换曲（没有待切换曲目则停止）
        /// </summary>
        private void UpdateBGMFade(float _deltaTime)
        {
            if (m_BGMFadeState == BGMFadeState.None || m_BGMSource == null || m_IsBGMPaused)
            {
                return;
            }

            m_BGMFadeElapsed += _deltaTime;
            float kProgress = Mathf.Clamp01(m_BGMFadeElapsed / m_BGMFadeTime);

            if (m_BGMFadeState == BGMFadeState.FadeOut)
            {
                m_BGMSource.volume = Mathf.Lerp(m_BGMFadeStartVolume, 0f, kProgress);
                if (kProgress < 1f)
                {
                    return;
                }

                float kFadeTime = m_BGMFadeTime;
                if (m_PendingBGMClip != null)
                {
                    AudioClip kClip = m_PendingBGMClip;
                    string kKey = m_PendingBGMKey;
                    bool kLoop = m_PendingBGMLoop;
                    ClearPendingBGM();

                    SetBGMClip(kClip, kKey, kLoop, 0f);
                    StartBGMFadeIn(kFadeTime);
                }
                else
                {
                    m_BGMFadeState = BGMFadeState.None;
                    m_BGMSource.Stop();
                    m_BGMSource.volume = GetBGMVolume();
                }
                return;
            }

            // 淡入：目标音量每帧重新取，播放中拖动音量条也能平滑收尾
            m_BGMSource.volume = Mathf.Lerp(m_BGMFadeStartVolume, GetBGMVolume(), kProgress);
            if (kProgress < 1f)
            {
                return;
            }

            m_BGMFadeState = BGMFadeState.None;
            m_BGMSource.volume = GetBGMVolume();
        }

        /// <summary>
        /// 清空待切换曲目
        /// </summary>
        private void ClearPendingBGM()
        {
            m_PendingBGMClip = null;
            m_PendingBGMKey = null;
            m_PendingBGMLoop = false;
        }

        /// <summary>
        /// 背景音乐的目标音量（静音时为 0）
        /// </summary>
        private float GetBGMVolume()
        {
            return m_IsBGMMute ? 0f : m_BGMVolume;
        }

        /// <summary>
        /// 立即应用背景音乐音量；淡变过程中由 UpdateBGMFade 接管，这里不抢 volume
        /// </summary>
        private void ApplyBGMVolume()
        {
            if (m_BGMSource == null || m_BGMFadeState != BGMFadeState.None)
            {
                return;
            }

            m_BGMSource.volume = GetBGMVolume();
        }

        /// <summary>
        /// 音效通道的目标音量：单次音量 × 音效总音量（静音时为 0）
        /// </summary>
        private float GetSFXVolume(SFXChannel _channel)
        {
            return m_IsSFXMute ? 0f : _channel.m_BaseVolume * m_SFXVolume;
        }

        /// <summary>
        /// 立即应用音效音量：正在播放的音效跟着变，FairyGUI 的界面音效也同步
        /// </summary>
        private void ApplySFXVolume()
        {
            for (int i = 0; i < m_SFXChannelList.Count; i++)
            {
                SFXChannel kChannel = m_SFXChannelList[i];
                kChannel.m_Source.volume = GetSFXVolume(kChannel);
            }

            // FairyGUI 的按钮 / 界面音效由 Stage 自己的 AudioSource 播放，同步后同样受音效音量控制。
            // Stage 还没创建时（StageCamera.main 为空）先不同步，UIManager 初始化时会再读一次当前音量。
            if (StageCamera.main != null)
            {
                Stage.inst.soundVolume = m_IsSFXMute ? 0f : m_SFXVolume;
            }
        }

        /// <summary>
        /// 播放音效的实际实现
        /// </summary>
        private void PlaySFXInternal(AudioClip _clip, string _key, float _volume, float _pitch)
        {
            if (_clip == null)
            {
                return;
            }

            // 静音或音量为 0 时不占用通道
            if (m_IsSFXMute || m_SFXVolume <= 0f)
            {
                return;
            }

            SFXChannel kChannel = GetSFXChannel();
            kChannel.m_Key = _key;
            kChannel.m_BaseVolume = Mathf.Clamp01(_volume);
            kChannel.m_PlayOrder = ++m_SFXPlayOrder;
            kChannel.m_IsPlaying = true;
            kChannel.m_IdleTime = 0f;

            AudioSource kSource = kChannel.m_Source;
            kSource.clip = _clip;
            kSource.pitch = Mathf.Clamp(_pitch, 0.1f, 3f);
            kSource.volume = GetSFXVolume(kChannel);
            kSource.Play();
        }

        /// <summary>
        /// 取一个音效通道：优先空闲的（含刚播完还没被回收的），没有就按上限新建，
        /// 上限用满时抢占最早开始的一路
        /// </summary>
        private SFXChannel GetSFXChannel()
        {
            for (int i = 0; i < m_SFXChannelList.Count; i++)
            {
                SFXChannel kChannel = m_SFXChannelList[i];
                if (kChannel.m_IsPlaying && kChannel.m_Source.isPlaying)
                {
                    continue;
                }

                // 刚播完、还没等到 Update 回收的通道，这里先清干净再复用
                if (kChannel.m_IsPlaying)
                {
                    RecycleSFXChannel(kChannel);
                }
                return kChannel;
            }

            if (m_SFXChannelList.Count < c_MaxSFXChannelCount)
            {
                SFXChannel kNewChannel = new SFXChannel();
                kNewChannel.m_Source = CreateAudioSource("SFX_" + m_SFXChannelId++, false);
                m_SFXChannelList.Add(kNewChannel);
                return kNewChannel;
            }

            SFXChannel kOldestChannel = m_SFXChannelList[0];
            for (int i = 1; i < m_SFXChannelList.Count; i++)
            {
                if (m_SFXChannelList[i].m_PlayOrder < kOldestChannel.m_PlayOrder)
                {
                    kOldestChannel = m_SFXChannelList[i];
                }
            }

            kOldestChannel.m_Source.Stop();
            return kOldestChannel;
        }

        /// <summary>
        /// 音效通道的自动回收：
        /// 1. 播放结束的通道立即停掉并清空 clip / key，归还通道池等待复用；
        /// 2. 池子超过保留数量的空闲通道，连续空闲满 c_SFXIdleRecycleTime 秒后销毁（真正释放 GameObject）。
        /// </summary>
        private void UpdateSFXChannelRecycle(float _deltaTime)
        {
            for (int i = m_SFXChannelList.Count - 1; i >= 0; i--)
            {
                SFXChannel kChannel = m_SFXChannelList[i];

                if (kChannel.m_IsPlaying)
                {
                    // 还在响就下一帧再看
                    if (kChannel.m_Source.isPlaying)
                    {
                        continue;
                    }

                    RecycleSFXChannel(kChannel);
                }

                // 池子已经缩到保留数量，空闲通道不再销毁，留着重用
                if (m_SFXChannelList.Count <= c_MinSFXChannelCount)
                {
                    continue;
                }

                kChannel.m_IdleTime += _deltaTime;
                if (kChannel.m_IdleTime < c_SFXIdleRecycleTime)
                {
                    continue;
                }

                UnityEngine.Object.Destroy(kChannel.m_Source.gameObject);
                m_SFXChannelList.RemoveAt(i);
                Debug.Log("[SoundManager] 空闲音效通道已销毁，当前通道数：" + m_SFXChannelList.Count);
            }
        }

        /// <summary>
        /// 回收一个音效通道：停止播放、清掉 AudioClip 引用，放回通道池等待复用
        /// </summary>
        private void RecycleSFXChannel(SFXChannel _channel)
        {
            _channel.m_IsPlaying = false;
            _channel.m_Key = null;
            _channel.m_BaseVolume = 1f;
            _channel.m_PlayOrder = 0;
            _channel.m_IdleTime = 0f;

            _channel.m_Source.Stop();
            _channel.m_Source.clip = null;
        }

        /// <summary>
        /// 新建一个 2D 的 AudioSource 并挂到 [SoundRoot] 下
        /// </summary>
        private AudioSource CreateAudioSource(string _name, bool _loop)
        {
            GameObject kObj = new GameObject(_name);
            kObj.transform.SetParent(m_Root, false);

            AudioSource kSource = kObj.AddComponent<AudioSource>();
            kSource.playOnAwake = false;
            kSource.loop = _loop;
            kSource.spatialBlend = 0f;
            return kSource;
        }

        /// <summary>
        /// 读取玩家保存的音量 / 静音设置
        /// </summary>
        private void LoadSettings()
        {
            m_BGMVolume = PlayerPrefs.GetFloat(c_PrefBGMVolume, c_DefaultBGMVolume);
            m_SFXVolume = PlayerPrefs.GetFloat(c_PrefSFXVolume, c_DefaultSFXVolume);
            m_IsBGMMute = PlayerPrefs.GetInt(c_PrefBGMMute, 0) == 1;
            m_IsSFXMute = PlayerPrefs.GetInt(c_PrefSFXMute, 0) == 1;
        }

        #endregion

        /// <summary>
        /// 音效通道：一个 AudioSource 同一时间只播一个音效，多个通道配合实现"音效可以同时存在多个"
        /// </summary>
        private class SFXChannel
        {
            public AudioSource m_Source;
            public float m_BaseVolume = 1f;
            public string m_Key;
            public long m_PlayOrder;

            // 是否有一路音效占着这个通道（播完后由 UpdateSFXChannelRecycle 自动归还）
            public bool m_IsPlaying;

            // 连续空闲时长（秒），用于销毁池子里多余的通道
            public float m_IdleTime;
        }

        /// <summary>
        /// 背景音乐的淡变状态
        /// </summary>
        private enum BGMFadeState
        {
            None,
            FadeOut,
            FadeIn,
        }
    }
}
