using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Framework.Script.UI;

namespace GameMain.Script.UI
{
    public class LotteryItem : UIWindow
    {
        public Image m_ItemImage;
        public Text m_ItemNum;
        public Text m_ItemName;
        public Image m_UnReward;

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