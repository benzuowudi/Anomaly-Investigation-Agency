using cfg.GameType;
using Framework.Script.Data;
using GameMain.Script.Class;
using System.Collections.Generic;
using UnityEngine;

namespace GameMain.Script.Data
{
    public class TestFightData : GameData
    {
        public  BattleClass m_BattleClass { get; private set; }
        private List<int> m_AliveRoleList = new List<int>();
        private List<int> m_DeadRoleList = new List<int>();
        protected internal override void Init()
        {
            base.Init();
        }

        protected internal override void Destroy()
        {
            base.Destroy();
        }

        protected internal override void Update(object o)
        {
            base.Update(o);
        }

        /// <summary>
        /// 获取对战双方的数据
        /// </summary>
        private void LoadRoleData()
        {

        }

        /// <summary>
        /// 获取对战双方其中之一角色的数据的方法
        /// </summary>
        /// <param name="_index">角色ID</param>
        /// <returns></returns>
        public BattleRoleClass GetRoleData(int _index)
        {
            if (!m_BattleClass.m_BattleRoleClassDic.ContainsKey(_index))
            {
                Debug.LogError("没有找到角色ID：" + _index + "的角色");
                return null;
            }
            return m_BattleClass.m_BattleRoleClassDic[_index];
        }

        /// <summary>
        /// 角色出局，将角色数据移动到阵亡列表
        /// </summary>
        /// <param name="_index">角色ID</param>
        /// <returns></returns>
        public bool RoleDead(int _index)
        {
            if (!m_DeadRoleList.Contains(_index))
            {
                m_DeadRoleList.Add(_index);
                return true;
            }
            return false;
        }

        /// <summary>
        /// 角色复活，将角色数据移动到生者列表
        /// </summary>
        /// <param name="_index">角色ID</param>
        /// <returns></returns>
        public bool RoleAlive(int _index)
        {
            if (!m_AliveRoleList.Contains(_index))
            {
                m_AliveRoleList.Add(_index);
                return true;
            }
            return false;
        }

        private void TestBattleData()
        {
            BattleClass kClass = new BattleClass();
            kClass.m_BattleID = 1;
            kClass.m_Group = 1;
            kClass.m_Chapter = 1;
            kClass.m_Level = 1;
            kClass.m_WeatherType = WeatherType.sunny;
            kClass.m_Map = 1;

            BattleRoleClass kBattleRoleClass_1 = new BattleRoleClass();
            kBattleRoleClass_1.m_BattleRoleID = 1;
            kBattleRoleClass_1.m_Camp = 0;
            RoleClass kRoleClass_1 = new RoleClass();

        }
    }
}