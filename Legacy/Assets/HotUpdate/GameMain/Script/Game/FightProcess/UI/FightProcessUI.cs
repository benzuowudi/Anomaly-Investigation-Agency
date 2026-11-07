using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Framework.Script.UI;
using UnityEngine.UI;
using GameMain.Script.Class;
using cfg.GameType;
using GameMain.Script.State;
using GameMain.Script.Data;
using cfg.GameData_Story;

namespace GameMain.Script.UI
{
    public class FightProcessUI : UIWindow
    {
        //ս�����̱��⣨���ģ�
        public Text txt_subtitle_cn;
        //ս�����̱��⣨Ӣ�ģ�
        public Text txt_subtitle_en;

        //���ؿ�ͼƬ
        public Image img_map;
        //�ؿ����ƣ����ģ�
        public Text txt_name_cn;
        //�ؿ����ƣ�Ӣ�ģ�
        public Text txt_name_en;
        //�����е�����״̬
        public Image img_mapstatus;

        //�ؿ�����
        public Text txt_leveldes;

        //�ؿ�Ч��-���ڵ㣨���ڶ�̬����������ʾ��Χ��
        public Transform m_leveleffectParent;
        //�ؿ�Ч��-����
        public Text txt_leveleffect;

        //�����б�-���ڵ㣨�����������޹������ڣ�
        public Transform m_enemyParent;
        //�����б��������Ὣ�б��д���������޸�Ϊ��Ӧ�Ĺ�����ṹ�壩
        public List<GameObject> m_enemyList = new List<GameObject>();

        //�ؿ�Ŀ��
        public Text txt_tag;
        //�ؿ�Ŀ�����״̬
        public List<Image> m_tagList = new List<Image>();

        //����ս��׼�����水ť
        public Button btn_right;
        //�Ƴ�ս�����̰�ť��
        public Button btn_quit;

        private FightProcessData m_Data;
        private LevelData m_LevelData;
        protected internal override void Init(object o)
        {
            base.Init(o);
            m_Data = (FightProcessData)Game.Data.GetGameData<FightProcessData>(null);
            m_LevelData = Game.Data.GetData.level.DataMap[m_Data.m_BattleClass.m_Level];
            btn_right.onClick.AddListener(OnClickRight);
            Debug.Log(m_Data.m_BattleClass.m_Group + "___" + m_Data.m_BattleClass.m_Chapter + "___" + m_Data.m_BattleClass.m_Level);
        }

        protected internal override void Enter(object o)
        {
            base.Enter(o);
            txt_subtitle_cn.text = m_LevelData.NameCn;
            txt_subtitle_en.text = m_LevelData.NameEn;

            txt_leveldes.text = m_LevelData.Desc;
            txt_leveleffect.text = m_LevelData.Deta;
        }

        protected internal override void Leave()
        {
            base.Leave();
        }

        protected internal override void Destroy()
        {
            base.Destroy();
            btn_right.onClick.RemoveListener(OnClickRight);
        }

        private void OnClickRight()
        {
            //Game.UI.ShowSystemUI(UINameType.TestFightPanel, null);
            //Game.Scene.ChangeScene(SceneType.Game, EnterFight);
        }

        private void EnterFight()
        {
            Game.UI.CloseAllUIByType(UIType.menu);
            Game.UI.CloseAllUIByType(UIType.universal);
            Game.State.StartState<BattleStartState>("BattleState");
        }
    }
}