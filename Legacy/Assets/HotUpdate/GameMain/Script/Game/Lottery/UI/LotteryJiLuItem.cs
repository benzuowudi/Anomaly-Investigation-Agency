using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Framework.Script.UI;
using UnityEngine.UI;

namespace GameMain.Script.UI
{
    public class LotteryJiLuItem : UIWindow
    {
        public Text txt_jilu_item;
        public Button btn_shownum;
        public GameObject bg_shownum;
        public Text txt_shownum;
        protected internal override void Init(object o)
        {
            base.Init(o);
        }

        protected internal override void Leave()
        {
            base.Leave();
        }
    }
}