using System.Collections.Generic;
using UnityEngine;
using Framework.Script.UI;
using Framework.Factory.UI;
using cfg.GameType;

namespace Framework.Script.Manager
{
    public class UIManager
    {
        private static UIManager m_Instance;

        public static UIManager GetInstance()
        {
            if (m_Instance == null)
            {
                m_Instance = new UIManager();
            }

            return m_Instance;
        }

        //
        private Transform m_Layer;

        //当前UI
        private UINameType m_CurrentUI = UINameType.none;

        //UI列表
        private List<UINameType> m_WindowList = new();

        //UI对象池
        private Dictionary<UINameType, UIWindow> m_UIPoolDic = new();

        //UI窗口字典
        private Dictionary<UINameType, UIWindow> m_UIWindowDic = new();

        //记录查询到的子节点列表
        private List<Transform> m_ChildrenList = new();

        /// <summary>
        /// UI初始化
        /// </summary>
        public void Init()
        {
            m_Layer = Game.Manager.m_MLayer;
        }

        /// <summary>
        /// 加载UI
        /// </summary>
        private void LoadUI()
        {
            foreach (var element in Game.Data.GetData.ui.DataList)
            {
                UINameType kname = element.Name;
                string[] kResList = element.Res.Split(',');
                for (int i = 0; i < kResList.Length; i++)
                {
                    if (kResList[i] == "")
                    {
                        continue;
                    }

                    Game.Res.Load(kResList[i], "ui");
                }

                ResHandle kResHandle = Game.Res.Load(kname.ToString());
                GameObject ui = Game.Res.GetObject<GameObject>(kResHandle, kname.ToString());
                if (ui == null)
                {
                    Game.Log.LogError("UI加载失败 UIName=" + kname);
                    continue;
                }

                ui.GetComponent<IUIFactory>().AddComponent();
                UIWindow window = ui.GetComponent<UIWindow>();
                Game.Log.LogError("UI加载失败 UIName=" + kname, window == null);
                Game.Log.LogInfo("UI已加载 UIName：" + kname, window != null);
                if (window == null)
                {
                    continue;
                }
                window.m_UIName = kname;
                window.m_Type = element.Type;
                m_UIWindowDic.Add(kname, window);
            }
        }

        /// <summary>
        ///Get UIWindow
        /// </summary>
        /// <param name="_uiName">UI名称</param>
        public UIWindow GetUI(UINameType _uiName)
        {
            if (!m_UIPoolDic.ContainsKey(_uiName))
            {
                return null;
            }

            return m_UIPoolDic[_uiName];
        }

        public UIWindow ShowWindow(UINameType _ui, UINameType _parent = UINameType.none, object o = null,
            bool _isHideCurrentUI = true, Transform _layer = null)
        {
            Game.Log.LogError("not found ComponentUI , UIName is " + _ui, !m_UIWindowDic.ContainsKey(_ui));
            Game.Log.LogError("not yet loaded SystemUI , UIName is " + _parent,
                !m_WindowList.Contains(_parent) && _parent != UINameType.none);
            Game.Log.LogError("not yet loaded SystemUI , UIName is " + _parent,
                !m_UIPoolDic.ContainsKey(_parent) && _parent != UINameType.none);

            if (!m_UIWindowDic.ContainsKey(_ui)) return null;

            //when current UI is not "UINameType.none", Hide All Children UI what in current UI
            if (m_CurrentUI != UINameType.none)
                CloseAllChildUI(m_UIPoolDic[m_CurrentUI].m_ChildrenUIList);
            //获取父UI节点，并关闭掉父节点下所有的子UI节点
            if (m_UIPoolDic.ContainsKey(_parent))
                CloseAllChildUI(m_UIPoolDic[_parent].m_ChildrenUIList);

            if (_isHideCurrentUI)
            {
                Hide(m_CurrentUI);
                m_CurrentUI = _ui;
            }

            UIWindow kUIWindow;
            if (!m_UIPoolDic.ContainsKey(_ui))
            {
                GameObject obj = MonoBehaviour.Instantiate<GameObject>(m_UIWindowDic[_ui].gameObject);
                obj.GetComponent<Canvas>().sortingOrder = Game.Data.GetData.ui.DataMap[(int)(object)_ui].Order;
                kUIWindow = obj.GetComponent<UIWindow>();
                if (m_UIPoolDic.ContainsKey(_parent) && _parent != UINameType.none)
                {
                    m_UIPoolDic[_parent].m_ChildrenUIList.Add(_ui);
                }

                m_WindowList.Add(_ui);
                if (!m_UIPoolDic.ContainsKey(_ui))
                {
                    m_UIPoolDic.Add(_ui, kUIWindow);
                }

                kUIWindow.Init(o);
                kUIWindow.Enter(o);
            }
            //if ComponentUI is loaded , Run ComponentUI's Enter function
            else
            {
                kUIWindow = m_UIPoolDic[_ui];
                kUIWindow.gameObject.SetActive(true);
                if (m_UIPoolDic.ContainsKey(_parent) && _parent != UINameType.none)
                {
                    m_UIPoolDic[_parent].m_ChildrenUIList.Add(_ui);
                }

                kUIWindow.Enter(o);
            }

            m_Layer = _layer == null ? Game.Manager.m_MLayer : _layer;

            if (m_UIPoolDic.ContainsKey(_parent))
            {
                kUIWindow.transform.SetParent(m_UIPoolDic[_parent].transform, false);
            }
            else
            {
                kUIWindow.transform.SetParent(m_Layer, false);
            }

            kUIWindow.transform.localPosition = Vector3.zero;
            kUIWindow.transform.localRotation = Quaternion.identity;

            Game.Log.LogError($"UIWindow {_ui} is null !", kUIWindow == null);
            Game.Log.LogInfo("Create UI Success, UIName is " + _ui);
            return kUIWindow;
        }

        /// <summary>
        /// Hide UI
        /// </summary>
        public void Hide(UINameType _ui)
        {
            if (_ui == UINameType.none) return;
            if (!m_UIPoolDic.ContainsKey(_ui))
            {
                Game.Log.LogInfo("not found ComponentUI , UIName is " + _ui);
                return;
            }

            m_UIPoolDic[_ui].transform.SetParent(Game.Manager.m_Pool);
            m_UIPoolDic[_ui].gameObject.SetActive(false);
            m_UIPoolDic[_ui].Leave();
        }

        /// <summary>
        /// 关闭掉指定类型的全部UI
        /// </summary>
        /// <param name="_type"></param>
        public void CloseAllUIByType(UIType _type)
        {
            foreach (var window in m_UIPoolDic)
            {
                if (window.Value.m_Type == _type)
                {
                    window.Value.Leave();
                    window.Value.gameObject.SetActive(false);
                }
            }
        }

        /// <summary>
        /// 卸载UI
        /// </summary>
        /// <param name="_ui">UI名称</param>
        public void DestroyUI(UIWindow _ui)
        {
            _ui.Destroy();
            m_UIPoolDic.Remove(_ui.m_UIName);
            MonoBehaviour.Destroy(_ui.gameObject);
            Game.Log.LogInfo("Destory UI:" + _ui.m_UIName);
        }

        private void CloseAllChildUI(List<UINameType> _childrenList)
        {
            for (int i = 0; i < _childrenList.Count; i++)
            {
                Hide(_childrenList[i]);
            }
        }

        /// <summary>
        /// 获取父节点下全部的子节点（包括子节点的子节点）
        /// </summary>
        /// <param name="_parent">查询的节点</param>
        /// <returns></returns>
        public List<Transform> GetAllNode(Transform _parent)
        {
            m_ChildrenList.Clear();
            GetAllNodeInParent(_parent);
            return m_ChildrenList;
        }

        /// <summary>
        /// 循环父节点，并将全部子节点保存在一个列表中
        /// </summary>
        /// <param name="_parent">查询的节点</param>
        private void GetAllNodeInParent(Transform _parent)
        {
            for (int i = 0; i < _parent.childCount; i++)
            {
                if (_parent.GetChild(i).childCount > 0)
                {
                    GetAllNodeInParent(_parent.GetChild(i));
                }

                m_ChildrenList.Add(_parent.GetChild(i));
            }
        }

        /// <summary>
        /// 获取节点，用于赋值UI变量
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="_parent"></param>
        /// <param name="_nodeName"></param>
        /// <returns></returns>
        public T GetNode<T>(UIWindow _parent, string _nodeName) where T : UnityEngine.Object
        {
            return _parent.m_TransformDic[_nodeName].transform.GetComponent<T>();
        }
    }
}