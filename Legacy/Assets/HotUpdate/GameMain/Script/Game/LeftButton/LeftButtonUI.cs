using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Framework.Script.UI;
using UnityEngine.UI;
using cfg.GameType;

namespace GameMain.Script.UI
{
    public class LeftButtonUI : UIWindow
    {
        public Button btn_back;
        public Button btn_home;
        public Button btn_option;
        public Button btn_exit;
        protected internal override void Init(object o)
        {
            base.Init(o);
            btn_back.onClick.AddListener(onClickBackBtn);
            btn_home.onClick.AddListener(onClickHomeBtn);
            btn_option.onClick.AddListener(onClickOptionBtn);
            btn_exit.onClick.AddListener(onClickExitBtn);
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

        public void onClickBackBtn()
        {
        }

        public void onClickHomeBtn()
        {
        }

        public void onClickOptionBtn()
        {

        }

        public void onClickExitBtn()
        {

        }
    }
}    