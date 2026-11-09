using Framework.Script.Base;
using System.Collections.Generic;
using FairyGUI;
using Framework.Script.UI;
using UnityEngine;

namespace Framework.Script.Manager
{
    /// <summary>
    /// UI 管理器（UGUI 预制体窗口 + FairyGUI 内容）。
    /// - 窗口是预制体：实例化后挂到 DontDestroy 下 Canvas 的 B/M/T Layer，隐藏时回收到 m_Pool
    /// - 首次 ShowWindow 才按需加载预制体（Addressables）以及它依赖的 FairyGUI 包
    /// - 预制体里的 FairyGUI.UIPanel 负责渲染 GComponent 内容，对应 UIWindow.Content
    /// - 所有窗口由本管理器统一管理，不使用 FairyGUI 自己的 Window / GRoot 窗口体系
    /// </summary>
    public class UIManager : Singleton<UIManager>
    {
        // 默认层（m_MLayer）
        private Transform m_Layer;

        // 当前界面
        private string m_CurrentUI;

        // 已显示过的界面名
        private readonly List<string> m_WindowList = new List<string>();

        // 已创建的窗口实例（隐藏后仍在池里，复用不再实例化）
        private readonly Dictionary<string, UIWindow> m_UIPoolDic = new Dictionary<string, UIWindow>();

        // 界面名 -> 预制体模板
        private readonly Dictionary<string, GameObject> m_UIWindowDic = new Dictionary<string, GameObject>();

        // 界面名 -> 预制体 Addressables 地址（没注册时直接用界面名当地址）
        private readonly Dictionary<string, string> m_WindowAddressDic = new Dictionary<string, string>();

        // 层 -> 该层已创建窗口数量（用于给 FairyGUI 面板分配递增的 sortingOrder）
        private readonly Dictionary<Transform, int> m_LayerSortingCount = new Dictionary<Transform, int>();

        // 弹窗栈（后进先出，栈顶 = 最近打开的弹窗）
        private readonly List<string> m_PopupStack = new List<string>();

        /// <summary>
        /// UI 初始化（首次访问 UIManager.Instance 时执行）
        /// </summary>
        protected override void OnInit()
        {
            m_Layer = Game.Manager.m_MLayer;

            // FairyGUI 全局默认字体：FGUI 工程 settings/Common.json 里设置的默认字体是 FZYTK，
            // 编辑器导出时"默认字体"不会写进包（文本字段不带 font 属性），只能在运行时补上。
            // 字体文件放在 Assets/Resources/Fonts 下，FairyGUI 的 FontManager 会按名字自动 Resources.Load。
            // 注意：Resources/Fonts/FZYTK.TTF 已经用 fontTools 合并过——
            //   汉字和中文标点取自方正姚体，ASCII 字母/数字/半角标点和通用标点（引号、省略号、破折号、间隔号）取自 Agency FB，
            //   所以一个 GTextField 里就能自动中英混排；不要再给动态文本单独设 AGENCYR/AGENCYB。
            UIConfig.defaultFont = "FZYTK";

            // 触发 FairyGUI Stage / StageCamera 创建（UIPanel 渲染依赖它），并让相机跨场景常驻
            _ = Stage.inst;
            if (StageCamera.main != null)
            {
                UnityEngine.Object.DontDestroyOnLoad(StageCamera.main.gameObject);
            }

            // FairyGUI 的按钮 / 界面音效由 Stage 自己的 AudioSource 播放，
            // 同步一次音量后，UI 音效同样受 SoundManager 的音效音量 / 静音控制
            Stage.inst.soundVolume = Game.Sound.IsSFXMute ? 0f : Game.Sound.SFXVolume;

            Debug.Log("[UIManager] 初始化完成，默认层：" + (m_Layer != null ? m_Layer.name : "null") + "，默认字体：" + UIConfig.defaultFont);
        }

        /// <summary>
        /// 注册界面：界面名 -> 预制体 Addressables 地址（不注册时界面名即地址）
        /// </summary>
        public void RegisterWindow(string _uiName, string _address)
        {
            if (string.IsNullOrEmpty(_uiName) || string.IsNullOrEmpty(_address))
            {
                return;
            }

            m_WindowAddressDic[_uiName] = _address;
        }

        /// <summary>
        /// 注册 FairyGUI 内容类：包/组件 -> GComponent 子类。
        /// 注册后 UIPanel 创建内容时会直接实例化这个类，UIWindow.GetContent&lt;T&gt;() 就能拿到。
        /// </summary>
        public void RegisterContent<T>(string _packageName, string _componentName) where T : GComponent
        {
            if (string.IsNullOrEmpty(_packageName) || string.IsNullOrEmpty(_componentName))
            {
                return;
            }

            string kUrl = UIPackage.URL_PREFIX + _packageName + "/" + _componentName;
            UIObjectFactory.SetPackageItemExtension(kUrl, typeof(T));
            Debug.Log("[UIManager] 注册 FairyGUI 内容类：" + kUrl + " -> " + typeof(T).Name);
        }

        /// <summary>
        /// 获取已创建的窗口（未创建返回 null，不触发加载）
        /// </summary>
        public UIWindow GetUI(string _uiName)
        {
            if (string.IsNullOrEmpty(_uiName))
            {
                return null;
            }

            m_UIPoolDic.TryGetValue(_uiName, out UIWindow kWindow);
            return kWindow;
        }

        /// <summary>
        /// 按需加载：加载预制体，并先把它 UIPanel 依赖的 FairyGUI 包加载好
        /// </summary>
        private bool EnsureLoaded(string _uiName)
        {
            if (m_UIWindowDic.ContainsKey(_uiName))
            {
                return true;
            }

            string kAddress = m_WindowAddressDic.TryGetValue(_uiName, out string kRegistered) ? kRegistered : _uiName;
            ResHandle kHandle = Game.Res.Load(kAddress);
            GameObject kPrefab = Game.Res.GetObject<GameObject>(kHandle, kAddress);
            if (kPrefab == null)
            {
                Debug.LogError("[UIManager] 找不到界面预制体：" + _uiName + "（Addressables 地址：" + kAddress + "）");
                return false;
            }

            // 预制体里的 UIPanel 会在 OnEnable/Start 创建内容，所以包必须先加载好
            UIPanel[] kPanels = kPrefab.GetComponentsInChildren<UIPanel>(true);
            for (int i = 0; i < kPanels.Length; i++)
            {
                string kPackageName = kPanels[i].packageName;
                if (string.IsNullOrEmpty(kPackageName))
                {
                    continue;
                }

                if (!FairyGUIManager.Instance.LoadPackage(kPackageName))
                {
                    Debug.LogWarning("[UIManager] FairyGUI 包加载失败：" + kPackageName +
                                     "，界面内容会保持空白：" + _uiName);
                }
            }

            m_UIWindowDic[_uiName] = kPrefab;
            Debug.Log("[UIManager] 界面已加载：" + _uiName + "（地址：" + kAddress + "）");
            return true;
        }

        /// <summary>
        /// 显示界面（首次调用时加载预制体与 FairyGUI 包）
        /// </summary>
        /// <param name="_ui">界面名</param>
        /// <param name="_parent">父界面名，为空则挂在层节点上</param>
        /// <param name="o">传给 Init / Enter 的参数</param>
        /// <param name="_isHideCurrentUI">是否隐藏当前界面</param>
        /// <param name="_layer">层节点，为空用 m_MLayer</param>
        /// <param name="_sortingOrder">可选：设置窗口 Canvas 的 sortingOrder</param>
        public UIWindow ShowWindow(string _ui, string _parent = null, object o = null,
            bool _isHideCurrentUI = true, Transform _layer = null, int? _sortingOrder = null)
        {
            // 弹窗跳转到普通界面时，弹窗关闭自己
            CloseAllPopups();

            if (!EnsureLoaded(_ui))
            {
                return null;
            }

            // 关掉父界面和当前界面下挂着的子界面
            if (!string.IsNullOrEmpty(_parent) && m_UIPoolDic.TryGetValue(_parent, out UIWindow kParentCheck))
            {
                CloseAllChildUI(kParentCheck.m_ChildrenUIList);
            }

            if (m_CurrentUI != null && m_CurrentUI != _ui && m_UIPoolDic.TryGetValue(m_CurrentUI, out UIWindow kCurrent))
            {
                CloseAllChildUI(kCurrent.m_ChildrenUIList);
            }

            if (_isHideCurrentUI)
            {
                if (m_CurrentUI != null)
                {
                    Hide(m_CurrentUI);
                }

                m_CurrentUI = _ui;
            }

            bool kIsNew = !m_UIPoolDic.ContainsKey(_ui);
            UIWindow kUIWindow;
            if (kIsNew)
            {
                GameObject kObj = UnityEngine.Object.Instantiate(m_UIWindowDic[_ui]);
                kObj.name = _ui;
                kObj.SetActive(true);

                kUIWindow = kObj.GetComponent<UIWindow>();
                if (kUIWindow == null)
                {
                    // 纯 UIPanel 预制体没有逻辑脚本时，由框架补一个
                    kUIWindow = kObj.AddComponent<UIWindow>();
                }

                kUIWindow.m_UIName = _ui;

                if (_sortingOrder.HasValue)
                {
                    Canvas kCanvas = kObj.GetComponent<Canvas>();
                    if (kCanvas != null)
                    {
                        kCanvas.sortingOrder = _sortingOrder.Value;
                    }
                }

                m_UIPoolDic.Add(_ui, kUIWindow);
                m_WindowList.Add(_ui);
            }
            else
            {
                kUIWindow = m_UIPoolDic[_ui];
                kUIWindow.gameObject.SetActive(true);
            }

            // 父窗口 > 指定层 > 默认层
            Transform kParent = null;
            UIWindow kParentWindow = null;
            if (!string.IsNullOrEmpty(_parent) && m_UIPoolDic.TryGetValue(_parent, out kParentWindow))
            {
                if (!kParentWindow.m_ChildrenUIList.Contains(kUIWindow))
                {
                    kParentWindow.m_ChildrenUIList.Add(kUIWindow);
                }

                kParent = kParentWindow.transform;
            }
            else
            {
                m_Layer = _layer != null ? _layer : Game.Manager.m_MLayer;
                kParent = m_Layer;
            }

            kUIWindow.transform.SetParent(kParent, false);
            kUIWindow.transform.localPosition = Vector3.zero;
            kUIWindow.transform.localRotation = Quaternion.identity;

            // FairyGUI 用 UIPanel.sortingOrder 决定叠放顺序，而且新建的面板会被插到 Stage 列表最前面
            // （等于画在最底层），所以必须在窗口创建时显式指定层级，否则后显示的界面会被背景盖住。
            if (kIsNew)
            {
                ApplySortingOrder(kUIWindow, kParentWindow, kParent);
                kUIWindow.BindContent();
                kUIWindow.Init(o);
            }

            kUIWindow.Enter(o);

            Debug.Log("[UIManager] 显示界面：" + _ui);
            return kUIWindow;
        }

        #region 弹窗（UIPopup）

        /// <summary>
        /// 打开弹窗：不隐藏当前界面，挂在顶层（T 层），并压入弹窗栈。
        /// 弹窗之间跳转时旧的弹窗会关闭自己（_closePreviousPopup）。
        /// </summary>
        public UIWindow ShowPopup(string _ui, object o = null, bool _closePreviousPopup = true)
        {
            if (string.IsNullOrEmpty(_ui))
            {
                return null;
            }

            if (_closePreviousPopup)
            {
                CloseTopPopup();
            }

            UIWindow kPopup = ShowWindow(_ui, null, o, false, Game.Manager.m_TLayer);
            if (kPopup == null)
            {
                return null;
            }

            m_PopupStack.Remove(_ui);
            m_PopupStack.Add(_ui);
            return kPopup;
        }

        /// <summary>关闭指定弹窗</summary>
        public void ClosePopup(string _ui)
        {
            if (string.IsNullOrEmpty(_ui))
            {
                return;
            }

            m_PopupStack.Remove(_ui);
            Hide(_ui);
        }

        /// <summary>关闭栈顶弹窗</summary>
        public void CloseTopPopup()
        {
            if (m_PopupStack.Count == 0)
            {
                return;
            }

            ClosePopup(m_PopupStack[m_PopupStack.Count - 1]);
        }

        /// <summary>关闭全部弹窗（普通界面打开时自动调用）</summary>
        public void CloseAllPopups()
        {
            while (m_PopupStack.Count > 0)
            {
                ClosePopup(m_PopupStack[m_PopupStack.Count - 1]);
            }
        }

        /// <summary>当前是否有弹窗打开</summary>
        public bool IsPopupOpen(string _ui)
        {
            return !string.IsNullOrEmpty(_ui) && m_PopupStack.Contains(_ui);
        }

        #endregion

        /// <summary>
        /// 给窗口的 FairyGUI 面板设置渲染层级：子界面永远在父界面之上，
        /// 没有父界面时按所在层决定（底层 &lt; 中层 &lt; 顶层）。
        /// </summary>
        private void ApplySortingOrder(UIWindow _window, UIWindow _parentWindow, Transform _layer)
        {
            if (_window == null)
            {
                return;
            }

            UIPanel kPanel = _window.Panel;
            if (kPanel == null)
            {
                return;
            }

            int kOrder;
            if (_parentWindow != null)
            {
                UIPanel kParentPanel = _parentWindow.Panel;
                kOrder = (kParentPanel != null ? kParentPanel.sortingOrder : 0) + 1;
            }
            else
            {
                kOrder = GetLayerSortingOrder(_layer);
            }

            kPanel.SetSortingOrder(kOrder, true);
        }

        /// <summary>
        /// 层 -> FairyGUI 渲染层级。底层从 0、中层从 100、顶层从 200 起，
        /// 同一层里按创建顺序 +1 叠加（保证后显示的装饰/界面在上面）。
        /// 数值都远小于 GRoot 的 1000，界面始终在 FairyGUI 弹窗之下。
        /// </summary>
        private int GetLayerSortingOrder(Transform _layer)
        {
            int kBase;
            if (_layer == Game.Manager.m_BLayer)
            {
                kBase = 0;
            }
            else if (_layer == Game.Manager.m_TLayer)
            {
                kBase = 200;
            }
            else if (_layer == Game.Manager.m_MLayer)
            {
                kBase = 100;
            }
            else
            {
                kBase = 300;
            }

            if (_layer == null)
            {
                return kBase;
            }

            int kCount;
            m_LayerSortingCount.TryGetValue(_layer, out kCount);
            m_LayerSortingCount[_layer] = kCount + 1;
            return kBase + kCount;
        }

        /// <summary>
        /// 隐藏界面（回收到对象池，下次显示直接复用）
        /// </summary>
        public void Hide(string _ui)
        {
            if (string.IsNullOrEmpty(_ui))
            {
                return;
            }

            if (!m_UIPoolDic.TryGetValue(_ui, out UIWindow kWindow))
            {
                return;
            }

            if (Game.Manager.m_Pool != null)
            {
                kWindow.transform.SetParent(Game.Manager.m_Pool, false);
            }

            kWindow.gameObject.SetActive(false);
            kWindow.Leave();

            if (m_CurrentUI == _ui)
            {
                m_CurrentUI = null;
            }
        }

        /// <summary>
        /// 隐藏所有界面
        /// </summary>
        public void CloseAllUI()
        {
            if (m_UIPoolDic.Count == 0)
            {
                return;
            }

            List<string> kNames = new List<string>(m_UIPoolDic.Keys);
            for (int i = 0; i < kNames.Count; i++)
            {
                Hide(kNames[i]);
            }

            m_CurrentUI = null;
        }

        /// <summary>
        /// 销毁界面
        /// </summary>
        public void DestroyUI(UIWindow _ui)
        {
            if (_ui == null)
            {
                return;
            }

            string kName = _ui.m_UIName;
            m_UIPoolDic.Remove(kName);
            m_WindowList.Remove(kName);

            if (m_CurrentUI == kName)
            {
                m_CurrentUI = null;
            }

            _ui.Destroy();
            UnityEngine.Object.Destroy(_ui.gameObject);
            Debug.Log("[UIManager] 销毁界面：" + kName);
        }

        /// <summary>
        /// 卸载界面预制体（预制体模板和 Addressables 句柄）
        /// </summary>
        public void UnloadWindow(string _ui)
        {
            if (string.IsNullOrEmpty(_ui))
            {
                return;
            }

            m_UIWindowDic.Remove(_ui);

            string kAddress = m_WindowAddressDic.TryGetValue(_ui, out string kRegistered) ? kRegistered : _ui;
            Game.Res.Unload(kAddress);
        }

        /// <summary>
        /// 关闭指定列表里的子界面
        /// </summary>
        private void CloseAllChildUI(List<UIWindow> _childrenList)
        {
            for (int i = _childrenList.Count - 1; i >= 0; i--)
            {
                if (_childrenList[i] != null)
                {
                    Hide(_childrenList[i].m_UIName);
                }
            }

            _childrenList.Clear();
        }

    }
}
