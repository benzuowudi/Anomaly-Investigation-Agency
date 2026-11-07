using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Framework.Script.UI;
using UnityEngine.UI;
using System;

namespace GameMain.Script.UI
{
    public class MainStory_ChapterBtnUI : UIWindow
    {
        public MainStoryUI m_MainStoryUI;
        public Button MainStory_ChapterBtn;
        public int m_ButtonID = 0;
        public Image img_chapter_state;
        public Text txt_chapter_name;
        public Text txt_chapter_num;

        protected internal override void Init(object o)
        {
            base.Init(o);
            m_MainStoryUI = (MainStoryUI)o;
            MainStory_ChapterBtn.onClick.AddListener(OnClickChapterBtn);
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
            MainStory_ChapterBtn.onClick.RemoveAllListeners();
        }

        private void OnClickChapterBtn()
        {
            m_MainStoryUI.ShowLevelList(m_ButtonID, Game.Data.GetData.chapter.DataMap[m_ButtonID].Level);
        }
    }
}