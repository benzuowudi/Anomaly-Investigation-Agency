using cfg.GameType;
using Framework.Script.UI;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GameMain.Script.UI
{
    public class MainStory_GorupBtnUI : UIWindow
    {
        public MainStoryUI m_MainStoryUI;

        public Button MainStory_GorupBtn;
        public Text txt_group_num;
        public Image img_chapter_show;
        public Text txt_chapter_state;
        public Text txt_chapter_name;
        public Transform obj_chapter_parent;

        private int m_ButtonID = 0;
        private bool m_IsShow = false;
        protected internal override void Init(object o)
        {
            base.Init(o);
            m_MainStoryUI = (MainStoryUI)o;
            MainStory_GorupBtn.onClick.AddListener(OnClickGorupBtn);
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
            MainStory_GorupBtn.onClick.RemoveAllListeners();
        }

        public void SetIndex(int _index)
        {
            m_ButtonID = _index;
        }

        public int GetIndex()
        {
            return m_ButtonID;
        }

        private void OnClickGorupBtn()
        {
            if (!m_IsShow)
            {
                img_chapter_show.transform.localEulerAngles = new Vector3(0, 180, 0);
                m_MainStoryUI.ShowChapterList(-1, m_ButtonID, Game.Data.GetData.group.DataMap[m_ButtonID].Chapter);
                m_IsShow = true;
            }
            else
            {
                img_chapter_show.transform.localEulerAngles = new Vector3(0, 0, 0);
                m_MainStoryUI.ShowChapterList(1, m_ButtonID, Game.Data.GetData.group.DataMap[m_ButtonID].Chapter);
                m_IsShow = false;
            }
        }
    }
}