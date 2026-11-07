using cfg.GameType;
using cfg.GameData_Story;
using Framework.Script.UI;
using GameMain.Script.Data;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UI;

namespace GameMain.Script.UI
{
    public class MainStoryUI : UIWindow
    {
        public ScrollRect scroll_group;

        public Text txt_chapterdesc;
        public Image img_group_image;
        public Text txt_group_num_cn;
        public Text txt_group_num_en;
        public Text txt_group_name_cn;
        public Text txt_group_name_en;
        public ScrollRect scroll_level;
        public Button btn_backmenu;
        public Button btn_inchapter;

        public Transform m_GroupUnits;
        public Transform m_LevelUnits;

        private List<MainStory_GorupBtnUI> m_GroupList = new List<MainStory_GorupBtnUI>();
        private Dictionary<int, List<MainStory_ChapterBtnUI>> m_ChapterDic = new Dictionary<int, List<MainStory_ChapterBtnUI>>();
        private List<MainStory_LevelBtnUI> m_LevelList = new List<MainStory_LevelBtnUI>();

        private FightProcessData m_Data;
        private int m_CurrentGoupID = 0;
        private int m_CurrentChapterID = 0;
        private int m_CurrentLevelID = 0;

        protected internal override void Init(object o)
        {
            base.Init(o);
            m_Data = (FightProcessData)Game.Data.GetGameData<FightProcessData>(null);

            btn_backmenu.onClick.AddListener(OnClickBackMenuBtn);
            btn_inchapter.onClick.AddListener(OnClickInChapterBtn);

            ShowGroupList(m_Data.m_GroupData);
        }

        protected internal override void Enter(object o)
        {
            base.Enter(o);
            UpdateUIShow(1);
        }

        protected internal override void Leave()
        {
            base.Leave();
        }

        protected internal override void Destroy()
        {
            base.Destroy();
        }

        private void OnClickBackMenuBtn()
        {

        }

        private void OnClickInChapterBtn()
        {
            return;
            //m_Data.SetBattleClass(0, m_CurrentGoupID, m_CurrentChapterID, m_CurrentLevelID);
            //if (m_CurrentGoupID == 0 && m_CurrentChapterID == 0 && m_CurrentLevelID == 0)
            //{
            //    return;
            //}
            //Game.UI.ShowSoloUI(UINameType.FightProcess, null);
        }

        /// <summary>
        /// ���½����е�UIչʾ
        /// </summary>
        /// <param name="_groupIndex">��ID</param>
        private void UpdateUIShow(int _groupIndex)
        {
            txt_chapterdesc.text = m_Data.m_GroupData[_groupIndex].Desc;
            txt_group_num_cn.text = m_Data.m_GroupData[_groupIndex].NumCn;
            txt_group_num_en.text = m_Data.m_GroupData[_groupIndex].NumEn;
            txt_group_name_cn.text = m_Data.m_GroupData[_groupIndex].NameCn;
            txt_group_name_en.text = m_Data.m_GroupData[_groupIndex].NameEn;
        }

        /// <summary>
        /// չʾ���б�
        /// </summary>
        /// <param name="_groupLDic">������б����ֵ�</param>
        private void ShowGroupList(Dictionary<int, GroupData> _groupLDic)
        {
            int temp = 0;
            foreach (var element in _groupLDic)
            {
                //MainStory_GorupBtnUI kGroupUI = (MainStory_GorupBtnUI)Game.UI.ShowItemUI(UINameType.MainStory_GorupBtn, this, m_GroupUnits, 13, -(temp * 100) - 12);
                //kGroupUI.gameObject.name = "Group_" + element.Key.ToString();
                //kGroupUI.SetIndex(element.Key);
                //kGroupUI.txt_group_num.text = "GROUP 0" + kGroupUI.GetIndex();
                //kGroupUI.img_chapter_show.transform.localRotation = Quaternion.identity;
                //kGroupUI.txt_chapter_state.text = element.Key >= 0 ? element.Key == m_Data.m_MainStory.m_CurrentGroupID ? "CURRENT" : "NOTARRIVED" : "FINISH";
                //kGroupUI.txt_chapter_name.text = element.Value.NameCn;
                //m_GroupList.Add(kGroupUI);
                temp++;
            }

            m_GroupUnits.GetComponent<RectTransform>().sizeDelta = new Vector2(0, (90 + 12) * _groupLDic.Count - 1);
        }

        /// <summary>
        /// �������ť��չʾ�½�
        /// </summary>
        /// <param name="_show">չʾ�����أ�-1��չʾ��1������</param>
        /// <param name="_groupIndex">��ID</param>
        /// <param name="_chapterList">�½ڰ�ť�б�</param>
        public void ShowChapterList(int _show, int _groupIndex, List<int> _chapterList)
        {
            m_CurrentGoupID = _groupIndex;
            AvoidGroupBtn(_show, _groupIndex, _chapterList.Count);
            if (_show < 0)
            {
                UpdateUIShow(_groupIndex);

                int temp = 0;
                foreach (int element in _chapterList)
                {
                    float _y = m_GroupList[_groupIndex - 1].transform.localPosition.y;
                    //MainStory_ChapterBtnUI kChapterUI = (MainStory_ChapterBtnUI)Game.UI.ShowItemUI(UINameType.MainStory_ChapterBtn, this, _parent: m_GroupList[_groupIndex - 1].obj_chapter_parent, _x: 6.5f, _y: -temp * 50 - 12);
                    //kChapterUI.transform.name = "Chapter_" + element.ToString();
                    //kChapterUI.m_ButtonID = element;
                    //kChapterUI.txt_chapter_name.text = m_Data.m_ChapterData[element].Num;
                    //if (m_ChapterDic.ContainsKey(_groupIndex))
                    //{
                    //    m_ChapterDic[_groupIndex].Add(kChapterUI);
                    //}
                    //else
                    //{
                    //    List<MainStory_ChapterBtnUI> kList = new List<MainStory_ChapterBtnUI>() { kChapterUI };
                    //    m_ChapterDic.Add(_groupIndex, kList);
                    //}
                    temp++;
                }
            }
            else
            {
                for (int i = 0; i < m_ChapterDic[_groupIndex].Count; i++)
                {
                    MonoBehaviour.Destroy(m_ChapterDic[_groupIndex][i].gameObject);
                }
                m_ChapterDic[_groupIndex].Clear();
            }
        }

        /// <summary>
        /// ����½ڰ�ť��չʾ�ؿ���ť
        /// </summary>
        /// <param name="_chapterIndex">�½�ID</param>
        /// <param name="_levelList">�ؿ���ť�б�</param>
        public void ShowLevelList(int _chapterIndex, List<int> _levelList)
        {
            m_CurrentChapterID = _chapterIndex;
            for (int i = 0; i < m_LevelList.Count; i++)
            {
                MonoBehaviour.Destroy(m_LevelList[i].gameObject);
            }
            m_LevelList.Clear();
            var kAB = Game.Res.Load("mainstory", "ui");
            for (int i = 0; i < _levelList.Count; i++)
            {
                //MainStory_LevelBtnUI kLevelUI = (MainStory_LevelBtnUI)Game.UI.ShowItemUI(UINameType.MainStory_LevelBtn, this, _parent: m_LevelUnits);
                //kLevelUI.m_LevelID = _levelList[i];
                //kLevelUI.transform.name = "Level_" + _levelList[i];
                //kLevelUI.txt_level.text = "LEVEL";
                //kLevelUI.txt_level_num.text = "0" + (i + 1);
                //for (int j = 0; j < kLevelUI.m_StarList.Count; j++)
                //{
                //    kLevelUI.m_StarList[j].sprite = Game.Res.GetObject<Sprite>(kAB, "img_star_dark");
                //}
                //m_LevelList.Add(kLevelUI);
            }
        }

        public void SelectLevel(int _levelIndex)
        {
            m_CurrentLevelID = _levelIndex;
            m_Data.SetBattleClass(0, m_CurrentGoupID, m_CurrentChapterID, m_CurrentLevelID);
            if (m_CurrentGoupID == 0 && m_CurrentChapterID == 0 && m_CurrentLevelID == 0)
            {
                return;
            }
            //Game.UI.ShowSystemUI(UINameType.FightProcess, null);
        }

        /// <summary>
        /// �ڵ������ťʱ�������·��ľ���ť�ƶ�
        /// </summary>
        /// <param name="_vector">-1��չʾ��1������</param>
        /// <param name="_groupIndex">����ľ���ťID</param>
        /// <param name="_chapterNum">��Ҫչʾ���½ڰ�ť������</param>
        private void AvoidGroupBtn(int _vector, int _groupIndex, int _chapterNum)
        {
            for (int i = _groupIndex; i < m_GroupList.Count; i++)
            {
                m_GroupList[i].transform.localPosition += new Vector3(0, (46 * _chapterNum) + (4 * _chapterNum - 1) + 12, 0) * _vector;
            }
            m_GroupUnits.GetComponent<RectTransform>().sizeDelta += new Vector2(0, (46 * _chapterNum) + (4 * _chapterNum - 1) + 12) * -_vector;
        }
    }
}