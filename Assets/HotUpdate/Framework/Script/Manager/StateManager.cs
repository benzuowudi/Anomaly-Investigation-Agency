using Framework.Script.Base;
using Framework.Script.State;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Xml.Linq;

namespace Framework.Script.Manager
{
    public class StateManager : Singleton<StateManager>
    {
        //保存状态机的字典，可以同时开启多个有限状态机，根据状态机标识符获取对应的状态机，
        //状态机标识符可以任意命名，但建议使用状态机开始是状态命名
        private Dictionary<string, List<string>> m_GameStateDic = new Dictionary<string, List<string>>();
        private Dictionary<string, GameState> m_GameStatePool = new Dictionary<string, GameState>();

        /// <summary>
        /// 状态机开始执行
        /// </summary>
        /// <param name="_name">有限状态机对应的标识符</param>
        /// <param name="_state">开始时执行的状态</param>
        /// <param name="o">状态机开始执行时传入的初始封装数据</param>
        public void StartState<T>(string _name, object o = null) where T : GameState, new()
        {
            T kState = new T();
            Dictionary<string, List<string>>.Enumerator kDic = m_GameStateDic.GetEnumerator();
            while (kDic.MoveNext())
            {
                if (_name == kDic.Current.Key)
                {
                    kDic.Current.Value.Add(kState.ToString());
                    m_GameStatePool.Add(kState.ToString(), kState);
                    return;
                }
            }
            List<string> kStateList = new List<string>() { kState.ToString() };
            m_GameStateDic.Add(_name, kStateList);
            m_GameStatePool.Add(kState.ToString(), kState);
            kState.Init(o);
        }

        /// <summary>
        /// 切换状态
        /// </summary>
        /// <typeparam name="T">GameSatate</typeparam>
        /// <param name="o">传入状态机中的封装数据</param>
        public void ChangeState<T>(string _name, object o = null) where T : GameState, new()
        {
            if (!m_GameStateDic.ContainsKey(_name))
            {
                return;
            }
            List<string> kCurrentList = null;
            m_GameStateDic.TryGetValue(_name, out kCurrentList);
            string kCurrentName = kCurrentList[kCurrentList.Count - 1];
            GameState kCurrent = m_GameStatePool[kCurrentName];
            kCurrent.Change();
            if (m_GameStatePool.ContainsKey(typeof(T).ToString()))
            {
                kCurrent = m_GameStatePool[typeof(T).ToString()];
                kCurrent.Enter(o);
            }
            else
            {
                kCurrent = new T();
                kCurrent.Init(o);
                kCurrent.Enter(o);
                m_GameStatePool.Add(kCurrent.ToString(), kCurrent);
            }
            Debug.Log(_name);
            if (m_GameStateDic.ContainsKey(_name))
            {
                m_GameStateDic[_name].Add(kCurrent.ToString());
            }
        }

        public void DestroyState(string _name)
        {
            List<string> kStateList = m_GameStateDic[_name];
            foreach (string element in kStateList)
            {
                m_GameStatePool[element].Destroy();
                m_GameStatePool[element] = null;
                m_GameStatePool.Remove(element);
            }
            m_GameStateDic.Remove(_name);
        }
    }
}