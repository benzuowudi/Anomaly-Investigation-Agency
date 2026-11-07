using System.Collections.Generic;
using FairyGUI;
using UnityEngine;

namespace Framework.Script.UI
{
    /// <summary>
    /// UI 窗口基类（MonoBehaviour）。
    /// - 窗口本体是预制体，由 UIManager 实例化后挂到 DontDestroy 下 Canvas 的 B/M/T Layer（隐藏时回收到 Pool）
    /// - 窗口内的 FairyGUI UIPanel 负责创建 GComponent 内容，子类通过 Panel / GetChild 取节点
    /// - 业务界面继承本类，重写 Init / Enter / Leave / Destroy
    /// </summary>
    public class UIWindow : MonoBehaviour
    {
        [HideInInspector] public string m_UIName;
        [HideInInspector] public List<UIWindow> m_ChildrenUIList = new List<UIWindow>();

        /// <summary>
        /// 窗口绑定的 FairyGUI 面板。未在 Inspector 中指定时，会在本节点或子节点中自动查找。
        /// </summary>
        [SerializeField] protected UIPanel m_UIPanel;

        /// <summary>
        /// UIPanel 创建的 FairyGUI 内容（可为空），即“继承 GComponent 的 UI”挂载在这里。
        /// </summary>
        [HideInInspector] [System.NonSerialized]
        public GComponent m_Content;

        /// <summary>
        /// 当前窗口的 FairyGUI 面板。子类可直接通过 Panel.ui / Panel.GetChild 访问。
        /// </summary>
        public UIPanel Panel
        {
            get
            {
                EnsurePanel();
                return m_UIPanel;
            }
        }

        public GComponent ui => m_UIPanel != null ? m_UIPanel.ui : null;

        /// <summary>
        /// UIPanel 创建的 FairyGUI 内容（可为空）。
        /// </summary>
        public GComponent Content
        {
            get
            {
                if (m_Content == null)
                {
                    BindContent();
                }

                return m_Content;
            }
        }

        /// <summary>
        /// 创建 UI 实例时执行一次。此时 BindContent 已由 UIManager 调用，子类可直接用 GetChild 绑定元件。
        /// </summary>
        protected internal virtual void Init(object o)
        {
            AddEvent();
        }

        protected internal virtual void AddEvent()
        {
        }

        protected internal virtual void RemoveEvent()
        {
        }

        /// <summary>
        /// 每次进入 UI 时执行一次
        /// </summary>
        protected internal virtual void Enter(object o)
        {
        }

        /// <summary>
        /// When you change ui, the function executes once
        /// </summary>
        protected internal virtual void UpDate(object o)
        {
        }

        /// <summary>
        /// When you hide ui, the function executes once
        /// </summary>
        protected internal virtual void Leave()
        {
            m_ChildrenUIList.Clear();
        }

        protected internal virtual void Destroy()
        {
            RemoveEvent();
            m_ChildrenUIList.Clear();
            m_Content = null;
            m_UIPanel = null;
        }

        /// <summary>
        /// 绑定预制体里的 UIPanel，并创建/获取 FairyGUI 内容（UIPanel.Start 之前调用也没问题）。
        /// UIManager 会在 Init 之前调用。
        /// </summary>
        public void BindContent()
        {
            EnsurePanel();
            if (m_UIPanel == null)
            {
                m_Content = null;
                return;
            }

            m_Content = m_UIPanel.ui;
        }

        private void EnsurePanel()
        {
            if (m_UIPanel != null)
            {
                return;
            }

            m_UIPanel = GetComponent<UIPanel>();
            if (m_UIPanel == null)
            {
                m_UIPanel = GetComponentInChildren<UIPanel>(true);
            }
        }

        /// <summary>
        /// 取指定类型的 FairyGUI 内容（内容类需要先 UIManager.RegisterContent 注册）。
        /// </summary>
        public T GetContent<T>() where T : GComponent
        {
            return Content as T;
        }

        /// <summary>
        /// 按名字取 FairyGUI 直属子元件，等价于 Content.GetChild(name)。
        /// </summary>
        public GObject GetChild(string _name)
        {
            GComponent kContent = Content;
            if (kContent == null || string.IsNullOrEmpty(_name))
            {
                return null;
            }

            return kContent.GetChild(_name);
        }

        /// <summary>
        /// 按 FGUI 路径取嵌套元件，路径使用点号分隔，例如 "left.btn_ok"。
        /// </summary>
        public GObject GetChildByPath(string _path)
        {
            GComponent kContent = Content;
            if (kContent == null || string.IsNullOrEmpty(_path))
            {
                return null;
            }

            return kContent.GetChildByPath(_path);
        }

        /// <summary>
        /// 按名字取指定类型的 FairyGUI 直属子元件。
        /// </summary>
        public T GetChild<T>(string _name) where T : GObject
        {
            return GetChild(_name) as T;
        }

        /// <summary>
        /// 按路径取指定类型的 FairyGUI 嵌套元件。
        /// </summary>
        public T GetChildByPath<T>(string _path) where T : GObject
        {
            return GetChildByPath(_path) as T;
        }
    }
}