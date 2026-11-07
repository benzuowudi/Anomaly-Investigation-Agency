using UnityEngine;
using cfg.GameType;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace Framework.Script.UI
{
    public class UIWindow : MonoBehaviour
    {
        [HideInInspector]
        public UINameType m_UIName;
        [HideInInspector]
        public UIType m_Type;
        [HideInInspector]
        public Dictionary<string, Transform> m_TransformDic = new Dictionary<string, Transform>();
        [HideInInspector]
        public List<UINameType> m_ChildrenUIList = new List<UINameType>();
        [HideInInspector]
        public List<UIAnim> m_AnimList = new List<UIAnim>();

        /// <summary>
        /// 创建UI实例时执行一次
        /// </summary>
        /// <param name="o"></param>
        protected internal virtual void Init(object o)
        {
            foreach (Transform element in Game.UI.GetAllNode(transform))
            {
                if (element.tag != "DoNotGetChild")
                {
                    m_TransformDic.Add(element.name, element);
                    if (element.GetComponent<Anim_Reveal>() != null)
                        m_AnimList.Add(element.GetComponent<Anim_Reveal>());
                    if (element.GetComponent<Anim_Pan>() != null)
                        m_AnimList.Add(element.GetComponent<Anim_Pan>());
                    if (element.GetComponent<Anim_Deformation>() != null)
                        m_AnimList.Add(element.GetComponent<Anim_Deformation>());
                    if (element.GetComponent<Anim_Change>() != null)
                        m_AnimList.Add(element.GetComponent<Anim_Change>());
                }
            }
        }
        /// <summary>
        /// 每次进入UI时执行一次
        /// </summary>
        /// <param name="o"></param>
        protected internal virtual void Enter(object o)
        {
            EnterAnim().Forget();
        }

        /// <summary>
        /// When you change ui, the function executes once
        /// </summary>
        /// <param name="o"></param>
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
            m_TransformDic.Clear();
            m_AnimList.Clear();
        }

        /// <summary>
        /// When you show the UI, play the UI entrance animation
        /// </summary>
        protected internal virtual async UniTaskVoid EnterAnim()
        {
            for (int i = 0; i < m_AnimList.Count; i++)
            {
                m_AnimList[i].Init();
                if (m_AnimList[i].m_IsStartShow)
                {
                    await m_AnimList[i].Anim();
                }
            }
        }
    }
}