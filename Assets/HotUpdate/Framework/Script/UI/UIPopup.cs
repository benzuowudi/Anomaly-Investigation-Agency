using System.Collections;
using FairyGUI;
using UnityEngine;

namespace Framework.Script.UI
{
    /// <summary>
    /// 弹窗界面基类（与 UIWindow 同级、结构一致：预制体 + UIPanel + Init/Enter/Leave/Destroy）。
    ///
    /// 与 UIWindow 的区别：
    /// - 打开时**不隐藏**上一个界面（UIManager.ShowPopup 不会 Hide 当前界面）；
    /// - 弹窗之间跳转时，旧弹窗关闭自己（ShowPopup 会先关掉栈顶）；
    /// - 背景带高斯模糊：打开时抓一帧屏幕 → 逐级降采样再放大 → 铺在弹窗内容最底层；
    /// - 可在 Inspector 里选择「点击空白处关闭」：
    ///     开启：点弹窗主体（com_popup）外的空白区域关闭；
    ///     关闭：不响应空白点击，并隐藏界面里的 txt_tip；
    /// - 弹窗主体默认取 FGUI 里名为 com_popup 的组件（Base/PopupWindow），
    ///   它的 btn_close 会绑定到「关闭自己」。
    ///
    /// 子类只需要照常写 Init/Enter/Leave，需要关闭自己时调 Close()。
    /// </summary>
    public class UIPopup : UIWindow
    {
        /// <summary>弹窗主体节点名（Base/PopupWindow 的实例名）</summary>
        public const string c_BodyNodeName = "com_popup";

        /// <summary>是否允许点击空白处关闭</summary>
        [SerializeField]
        private bool m_CloseOnBlank = true;

        /// <summary>是否给背景加高斯模糊</summary>
        [SerializeField]
        private bool m_BlurBackground = true;

        /// <summary>模糊背景的压暗程度（0 = 不压暗）</summary>
        [SerializeField]
        [Range(0f, 0.9f)]
        private float m_BackgroundDarkness = 0.35f;

        /// <summary>弹窗主体（点它不关闭）</summary>
        private GComponent m_Body;

        /// <summary>全屏模糊背景 + 空白点击区</summary>
        private GLoader m_Backdrop;

        /// <summary>模糊贴图（每次打开重新生成）</summary>
        private RenderTexture m_BlurTexture;

        private Coroutine m_BlurRoutine;

        /// <summary>抓屏那一帧把整个弹窗藏起来了（Leave/中断时要恢复）</summary>
        private bool m_HiddenForCapture;

        /// <summary>弹窗主体</summary>
        public GComponent body
        {
            get { return m_Body; }
        }

        #region 生命周期

        protected internal override void Init(object o)
        {
            m_Body = ui.GetChild(c_BodyNodeName) as GComponent;

            // 空白/模糊底层：放在显示列表最底层，同时承担「点击空白关闭」的命中区域
            m_Backdrop = new GLoader();
            m_Backdrop.name = "__backdrop";
            m_Backdrop.SetSize(ui.width, ui.height);
            m_Backdrop.align = AlignType.Center;
            m_Backdrop.verticalAlign = VertAlignType.Middle;
            m_Backdrop.fill = FillType.ScaleFree;
            m_Backdrop.color = new Color(1f - m_BackgroundDarkness, 1f - m_BackgroundDarkness,
                1f - m_BackgroundDarkness, 1f);
            m_Backdrop.onClick.Add(OnClickBackdrop);
            ui.AddChildAt(m_Backdrop, 0);

            // 弹窗主体的关闭按钮
            GButton kClose = m_Body == null ? null : m_Body.GetChild("btn_close") as GButton;
            if (kClose != null)
            {
                kClose.onClick.Add(OnClickClose);
            }

            base.Init(o);
        }

        protected internal override void Enter(object o)
        {
            RestoreVisibleAfterCapture();
            base.Enter(o);
            ApplyCloseOnBlank();
            if (m_BlurBackground)
            {
                if (m_BlurRoutine != null)
                {
                    StopCoroutine(m_BlurRoutine);
                }

                m_BlurRoutine = StartCoroutine(CaptureAndBlur());
            }
        }

        protected internal override void Leave()
        {
            if (m_BlurRoutine != null)
            {
                StopCoroutine(m_BlurRoutine);
                m_BlurRoutine = null;
            }

            // 抓屏中途被关掉时，弹窗可能还停在"隐藏"状态
            RestoreVisibleAfterCapture();
            ReleaseBlur();
            base.Leave();
        }

        protected internal override void Destroy()
        {
            ReleaseBlur();
            if (m_Backdrop != null)
            {
                m_Backdrop.onClick.Remove(OnClickBackdrop);
                m_Backdrop = null;
            }

            base.Destroy();
        }

        #endregion

        #region 关闭

        /// <summary>关闭自己（弹窗从 UIManager 的弹窗栈里弹出）</summary>
        public void Close()
        {
            Game.UI.ClosePopup(m_UIName);
        }

        private void OnClickClose()
        {
            Close();
        }

        private void OnClickBackdrop(EventContext _context)
        {
            if (!m_CloseOnBlank)
            {
                return;
            }

            Close();
        }

        /// <summary>
        /// 按 Inspector 的选择决定空白点击与提示文案。
        /// </summary>
        private void ApplyCloseOnBlank()
        {
            // 关闭该功能时连命中区域一起关掉，避免挡住下面界面的点击
            m_Backdrop.touchable = m_CloseOnBlank;

            // 关掉「点击空白处关闭」时把提示文字一起隐藏
            GObject kTip = ui.GetChildByPath(c_BodyNodeName + ".txt_tip");
            if (kTip == null)
            {
                kTip = ui.GetChild("txt_tip");
            }

            if (kTip != null)
            {
                kTip.visible = m_CloseOnBlank;
            }
        }

        #endregion

        #region 背景模糊

        /// <summary>
        /// 抓当前帧画面 → 逐级降采样/放大（双线性 = 低成本高斯近似）→ 铺到底层。
        ///
        /// 两个关键点：
        /// 1) 抓屏前先把整个弹窗隐藏一帧，否则会把弹窗自己（含 txt_tip）也抓进背景里；
        /// 2) 从 backbuffer 抓出来的图 Y 方向和 FairyGUI 的 UV 相反，最后一步要垂直翻转一次。
        /// </summary>
        private IEnumerator CaptureAndBlur()
        {
            m_HiddenForCapture = true;
            ui.visible = false;
            m_Backdrop.visible = false;

            yield return new WaitForEndOfFrame();

            // 等这一帧的期间弹窗可能已经被关掉了，别再画
            if (this == null || gameObject == null || !gameObject.activeInHierarchy)
            {
                m_BlurRoutine = null;
                yield break;
            }

            int kWidth = Screen.width;
            int kHeight = Screen.height;
            if (kWidth <= 0 || kHeight <= 0)
            {
                RestoreVisibleAfterCapture();
                m_BlurRoutine = null;
                yield break;
            }

            RenderTexture kCapture = RenderTexture.GetTemporary(kWidth, kHeight, 0, RenderTextureFormat.ARGB32);
            RenderTexture kHalf = RenderTexture.GetTemporary(kWidth / 2, kHeight / 2, 0, RenderTextureFormat.ARGB32);
            RenderTexture kQuarter = RenderTexture.GetTemporary(kWidth / 4, kHeight / 4, 0, RenderTextureFormat.ARGB32);
            RenderTexture kBlur = RenderTexture.GetTemporary(kWidth / 2, kHeight / 2, 0, RenderTextureFormat.ARGB32);

            ScreenCapture.CaptureScreenshotIntoRenderTexture(kCapture);
            Graphics.Blit(kCapture, kHalf);
            Graphics.Blit(kHalf, kQuarter);
            Graphics.Blit(kQuarter, kBlur);

            ReleaseBlur();
            m_BlurTexture = new RenderTexture(kWidth, kHeight, 0, RenderTextureFormat.ARGB32);
            m_BlurTexture.filterMode = FilterMode.Bilinear;

            // scale(1,-1) + offset(0,1)：把 backbuffer 的 Y 轴翻回来
            Graphics.Blit(kBlur, m_BlurTexture, new Vector2(1f, -1f), new Vector2(0f, 1f));

            RenderTexture.ReleaseTemporary(kCapture);
            RenderTexture.ReleaseTemporary(kHalf);
            RenderTexture.ReleaseTemporary(kQuarter);
            RenderTexture.ReleaseTemporary(kBlur);

            if (this == null || gameObject == null || !gameObject.activeInHierarchy)
            {
                m_BlurRoutine = null;
                yield break;
            }

            m_Backdrop.texture = new NTexture(m_BlurTexture);
            m_Backdrop.visible = true;
            RestoreVisibleAfterCapture();
            m_BlurRoutine = null;
        }

        /// <summary>抓屏期间隐藏了整个弹窗，这里恢复（Leave/异常中断时兜底调用）</summary>
        private void RestoreVisibleAfterCapture()
        {
            if (!m_HiddenForCapture)
            {
                return;
            }

            m_HiddenForCapture = false;
            if (ui != null)
            {
                ui.visible = true;
            }
        }

        private void ReleaseBlur()
        {
            if (m_Backdrop != null && m_Backdrop.texture != null)
            {
                m_Backdrop.texture = null;
            }

            if (m_BlurTexture != null)
            {
                m_BlurTexture.Release();
                Destroy(m_BlurTexture);
                m_BlurTexture = null;
            }
        }

        #endregion
    }
}
