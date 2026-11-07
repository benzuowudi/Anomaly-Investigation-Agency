using Framework.Script.UI;
using UnityEngine.UI;
using UnityEngine;
using System.Collections.Generic;
using cfg.GameType;
using Cysharp.Threading.Tasks;

namespace GameMain.Script.UI
{
    public class CommandUI : UIWindow
    {
        public Transform part_left;
        public Transform part_right;

        public Text txt_RoleInfo_Name;
        public Text txt_RoleInfo_UID;
        public Text txt_RoleInfo_LV;
        public Button btn_Roleinfo_Profile;
        public Button btn_Roleinfo_Cup;
        public Text txt_Roleinfoe_CupNum;

        public Button btn_Freind;
        public Text txt_FriendNum;

        public Button btn_Dormitory;
        public Text txt_Dormitory;

        public Button btn_Lottery;

        public Button btn_Inventory;
        public Text txt_Inventory;

        public Button btn_Store;

        public Button btn_Attack;
        public Text txt_Attack;

        public Button btn_Activity;

        public Button btn_RoutineAction;

        public Button btn_Emergencies;
        public Text txt_Emergencies;

        public Button btn_Playerbook;
        public Text txt_Playerbook;

        public Button btn_mail;
        public Button btn_Reward;
        public Button btn_guide;
        public Button btn_news;

        public Button btn_coin_plus;
        public Text txt_coin_num;

        public Button btn_euro_plus;
        public Text txt_euro_num;

        public Button btn_show;
        public Button btn_hide;
        public Button btn_Exit;
        public Button btn_opition;
        public Button btn_share;

        private List<Transform> m_SystemBtnList = new List<Transform>();


        protected internal override void Init(object o)
        {
            base.Init(o);
            part_left = Game.UI.GetNode<Transform>(this, "part_left");
            part_right = Game.UI.GetNode<Transform>(this, "part_right");

            txt_RoleInfo_Name = Game.UI.GetNode<Text>(this, "txt_RoleInfo_Name");
            txt_RoleInfo_UID = Game.UI.GetNode<Text>(this, "txt_RoleInfo_UID");
            txt_RoleInfo_LV = Game.UI.GetNode<Text>(this, "txt_RoleInfo_LV");
            btn_Roleinfo_Profile = Game.UI.GetNode<Button>(this, "btn_Roleinfo_Profile");
            btn_Roleinfo_Cup = Game.UI.GetNode<Button>(this, "btn_Roleinfo_Cup");
            txt_Roleinfoe_CupNum = Game.UI.GetNode<Text>(this, "txt_Roleinfoe_CupNum");

            btn_Freind = Game.UI.GetNode<Button>(this, "btn_Freind");
            txt_FriendNum = Game.UI.GetNode<Text>(this, "txt_FriendNum");

            btn_Dormitory = Game.UI.GetNode<Button>(this, "btn_Dormitory");
            txt_Dormitory = Game.UI.GetNode<Text>(this, "txt_Dormitory");

            btn_Lottery = Game.UI.GetNode<Button>(this, "btn_Lottery");

            btn_Inventory = Game.UI.GetNode<Button>(this, "btn_Inventory");
            txt_Inventory = Game.UI.GetNode<Text>(this, "txt_Inventory");

            btn_Store = Game.UI.GetNode<Button>(this, "btn_Store");

            btn_Attack = Game.UI.GetNode<Button>(this, "btn_Attack");
            txt_Attack = Game.UI.GetNode<Text>(this, "txt_Attack");

            btn_RoutineAction = Game.UI.GetNode<Button>(this, "btn_RoutineAction");

            btn_Emergencies = Game.UI.GetNode<Button>(this, "btn_Emergencies");
            txt_Emergencies = Game.UI.GetNode<Text>(this, "txt_Emergencies");

            btn_Playerbook = Game.UI.GetNode<Button>(this, "btn_Playerbook");
            txt_Playerbook = Game.UI.GetNode<Text>(this, "txt_Playerbook");

            btn_mail = Game.UI.GetNode<Button>(this, "btn_mail");
            btn_Reward = Game.UI.GetNode<Button>(this, "btn_Reward");
            btn_guide = Game.UI.GetNode<Button>(this, "btn_guide");
            btn_news = Game.UI.GetNode<Button>(this, "btn_news");

            btn_coin_plus = Game.UI.GetNode<Button>(this, "btn_coin_plus");
            txt_coin_num = Game.UI.GetNode<Text>(this, "txt_coin_num");

            btn_euro_plus = Game.UI.GetNode<Button>(this, "btn_euro_plus");
            txt_euro_num = Game.UI.GetNode<Text>(this, "txt_euro_num");

            btn_show = Game.UI.GetNode<Button>(this, "btn_show");
            btn_hide = Game.UI.GetNode<Button>(this, "btn_hide");
            btn_Exit = Game.UI.GetNode<Button>(this, "btn_Exit");
            btn_opition = Game.UI.GetNode<Button>(this, "btn_opition");
            btn_share = Game.UI.GetNode<Button>(this, "btn_share");
        }

        protected internal override void Enter(object o)
        {
            base.Enter(o);

            m_SystemBtnList.Add(btn_Exit.transform);
            m_SystemBtnList.Add(btn_opition.transform);
            m_SystemBtnList.Add(btn_share.transform);
            btn_show.onClick.AddListener(() => OnClickShowBtn().Forget());
            btn_hide.onClick.AddListener(() => OnOnClickHideBtn().Forget());
            btn_Freind.onClick.AddListener(OnClickRoleUI);

            btn_show.gameObject.SetActive(true);
            btn_hide.gameObject.SetActive(false);
        }

        protected internal override void Leave()
        {
            base.Leave();
            m_SystemBtnList.Clear();
            btn_show.onClick.RemoveAllListeners();
            btn_hide.onClick.RemoveAllListeners();
            btn_Freind.onClick.RemoveAllListeners();
        }

        protected internal override void Destroy()
        {
            base.Destroy();
        }

        private async UniTaskVoid OnClickShowBtn()
        {
            btn_show.gameObject.SetActive(false);
            btn_hide.gameObject.SetActive(true);
            for (int i = 0; i < m_SystemBtnList.Count; i++)
            {
                await m_SystemBtnList[i].GetComponent<UIAnim>().Anim();
            }
        }

        private async UniTaskVoid OnOnClickHideBtn()
        {
            btn_show.gameObject.SetActive(true);
            btn_hide.gameObject.SetActive(false);
            for (int i = m_SystemBtnList.Count - 1; i >= 0; i--)
            {
                await m_SystemBtnList[i].GetComponent<UIAnim>().Anim();
            }
        }

        private void OnClickRoleUI()
        {
            Game.UI.ShowWindow(UINameType.RoleBase);
            Game.UI.ShowWindow(UINameType.RoleInfo, UINameType.RoleBase, _isHideCurrentUI: false);
        }
    }
}