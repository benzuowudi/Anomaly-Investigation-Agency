using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Framework.Script.UI;
using UnityEngine.UI;

namespace GameMain.Script.UI
{
    public class MainStory_LevelBtnUI : UIWindow
    {
        public MainStoryUI m_MainStoryUI;
        public int m_LevelID = 0;
        public Button MainStory_LevelBtn;
        public Text txt_level;
        public Text txt_level_num;
        public List<Image> m_StarList = new List<Image>();
        protected internal override void Init(object o)
        {
            base.Init(o);
            m_MainStoryUI = (MainStoryUI)o;
            MainStory_LevelBtn.onClick.AddListener(ClickLevelBtn);
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
            MainStory_LevelBtn.onClick.RemoveAllListeners();
        }

        private void ClickLevelBtn()
        {
            m_MainStoryUI.SelectLevel(m_LevelID);
        }
    }
}