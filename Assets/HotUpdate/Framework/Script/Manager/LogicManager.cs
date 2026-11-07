using Framework.Script.Base;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Framework.Script.Logic;
using System;

namespace Framework.Script.Manager
{
    public class LogicManager : Singleton<LogicManager>
    {
        private Dictionary<Type, GameLogic> m_LogicDic = new Dictionary<Type, GameLogic>();

        protected override void OnInit()
        {
            LoadLogic();
        }

        private void LoadLogic()
        {
            // RegisterLogic<BattleLogic>();
            // RegisterLogic<ATLogic>();
        }

        /// <summary>
        /// 将logic保存在manager中统一调用其中方法
        /// </summary>
        /// <param name="_name"></param>
        /// <param name="_action"></param>
        private void RegisterLogic<T>() where T : GameLogic, new()
        {
            T KLogic = new T();
            m_LogicDic.Add(typeof(T), KLogic);
            KLogic.Init();
        }

        public T GetLogic<T>(object o = null) where T : GameLogic
        {
            GameLogic kLogic = null;
            m_LogicDic.TryGetValue(typeof(T), out kLogic);
            return kLogic as T;
        }

        public void DestroyLogic<T>() where T : GameLogic
        {
            GameLogic kLogic = null;
            m_LogicDic.TryGetValue(typeof(T), out kLogic);
            kLogic.Destroy();
        }
    }
}