using cfg.GameType;
using Framework.Script.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GameMain.Script.Logic;
using System;

namespace GameMain.Script.UI
{
    public class TestFightUI : UIWindow
    {
        public Transform LeftRoleParent;
        public Transform RightRoleParent;

        public List<TestFightLRoleUI> m_LeftObjList = new List<TestFightLRoleUI>();
        public List<TestFightLRoleUI> m_RightObjList = new List<TestFightLRoleUI>();

        private TestFightLogic m_TestFightLogic;
        protected internal override void Init(object o)
        {
            base.Init(o);
            m_TestFightLogic = Game.Logic.GetLogic<TestFightLogic>();
        }

        protected internal override void Enter(object o)
        {
            base.Enter(o);
        }

        protected internal override void Leave()
        {
            base.Leave();
        }

        protected internal override void Destroy()
        {
            base.Destroy();
        }

        private void InitRole(int camp)
        {
            //Action<int> kAcion = m_TestFightLogic.RoleAction;
            //TestFightLRoleUI _TestFightLRole = null;
            switch (camp)
            {
                case 0:
                    //_TestFightLRole = (TestFightLRoleUI)Game.UI.ShowItemUI(UINameType.TestFightLRole, kAcion, _parent: LeftRoleParent);
                    //m_LeftObjList.Add(_TestFightLRole);
                    break;
                case 1:
                    //_TestFightLRole = (TestFightLRoleUI)Game.UI.ShowItemUI(UINameType.TestFightLRole, kAcion, _parent: RightRoleParent);
                    //m_RightObjList.Add(_TestFightLRole);
                    break;
            }
        }
    }
}