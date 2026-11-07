using Framework.Script.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GameMain.Script.UI
{
    public class item : UIWindow
    {
        public Image img_bg;
        public Image img_icon;
        public Text txt_num;
        protected internal override void Init(object o)
        {
            base.Init(o);
            img_bg = Game.UI.GetNode<Image>(this, "img_bg");
            img_icon = Game.UI.GetNode<Image>(this, "img_icon");
            txt_num = Game.UI.GetNode<Text>(this, "txt_num");
        }

        protected internal override void Leave()
        {
            base.Leave();
        }
    }
}