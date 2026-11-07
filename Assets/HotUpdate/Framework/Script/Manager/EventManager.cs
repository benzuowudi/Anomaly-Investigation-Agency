using Framework.Script.Base;
using System.Collections.Generic;
using System;

namespace Framework.Script.Manager
{
    public class EventManager : Singleton<EventManager>
    {
        public Dictionary<string, List<Observer>> m_ObserverDic = new Dictionary<string, List<Observer>>();

        /// <summary>
        /// 订阅事件
        /// </summary>
        /// <param name="_msg">事件名</param>
        /// <param name="_action">回调函数</param>
        public void Subscribe(string _msg, Action<object> _action)
        {
            Observer kObserver = new Observer(_action);
            Dictionary<string, List<Observer>>.Enumerator kDicE = m_ObserverDic.GetEnumerator();
            while (kDicE.MoveNext())
            {
                if (_msg == kDicE.Current.Key)
                {
                    kDicE.Current.Value.Add(kObserver);
                    return;
                }
            }
            List<Observer> kList = new List<Observer> { kObserver };
            m_ObserverDic.Add(_msg, kList);
        }

        /// <summary>
        /// 注销订阅事件
        /// </summary>
        /// <param name="_msg">事件名</param>
        /// <param name="_action">回调函数</param>
        public void UnSubscribe(string _msg, Action<object> _action)
        {
            if (!m_ObserverDic.ContainsKey(_msg))
            {
                return;
            }
            List<Observer> kList = m_ObserverDic[_msg];
            for (int i = 0; i < kList.Count; i++)
            {
                int temp = i;
                if (_action == kList[temp].m_Action)
                {
                    m_ObserverDic[_msg].RemoveAt(temp);
                    return;
                }
            }
        }

        /// <summary>
        /// 声明事件
        /// </summary>
        /// <param name="_msg">事件名</param>
        /// <param name="_object">传入参数</param>
        public void SendMessage(string _msg, object _object)
        {
            if (!m_ObserverDic.ContainsKey(_msg))
            {
                return;
            }
            List<Observer> kList = m_ObserverDic[_msg];
            for (int i = 0; i < kList.Count; i++)
            {
                int temp = i;
                kList[temp].m_Action(_object);
            }
        }
    }

    /// <summary>
    /// 观察者实例
    /// </summary>
    public class Observer
    {
        public Action<object> m_Action;
        public Observer(Action<object> _action)
        {
            m_Action = _action;
        }
    }
}