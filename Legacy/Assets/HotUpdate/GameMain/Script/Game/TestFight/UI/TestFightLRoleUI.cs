using Framework.Script.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GameMain.Script.UI
{
    public class TestFightLRoleUI : UIWindow
    {
        public Image img_Heat;
        public Text txt_Name;
        public Text txt_Lv;
        public Button btn_Action;

        protected internal override void Init(object o)
        {
            base.Init(o);
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

        //角色点击处理
    }
}