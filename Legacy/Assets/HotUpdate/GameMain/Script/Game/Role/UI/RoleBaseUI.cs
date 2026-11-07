using UnityEngine;
using Framework.Script.UI;
using UnityEngine.UI;
using cfg.GameType;
using GameMain.Script.Data;

namespace GameMain.Script.UI
{
    public class RoleBaseUI : UIWindow
    {
        private RoleSystemData m_RoleSystemData;

        public Transform part_back;
        public Transform ScrollView_roleselect;
        public Transform part_roleselect;

        public Button btn_back;
        public Button btn_home;

        public Button btn_detail;
        public Button btn_nourish;
        public Button btn_destiny;
        public Button btn_weapon;
        public Button btn_medal;
        public Button btn_skill;
        public Button btn_item;
        public Button btn_equip;
        public Button btn_fashion;
        protected internal override void Init(object o)
        {
            base.Init(o);

            part_back = Game.UI.GetNode<Transform>(this, "part_back");
            ScrollView_roleselect = Game.UI.GetNode<Transform>(this, "ScrollView_roleselect");
            part_roleselect = Game.UI.GetNode<Transform>(this, "part_roleselect");

            btn_back = Game.UI.GetNode<Button>(this, "btn_back");
            btn_home = Game.UI.GetNode<Button>(this, "btn_home");

            btn_detail = Game.UI.GetNode<Button>(this, "btn_detail");
            btn_nourish = Game.UI.GetNode<Button>(this, "btn_nourish");
            btn_destiny = Game.UI.GetNode<Button>(this, "btn_destiny");
            btn_weapon = Game.UI.GetNode<Button>(this, "btn_weapon");
            btn_medal = Game.UI.GetNode<Button>(this, "btn_medal");
            btn_skill = Game.UI.GetNode<Button>(this, "btn_skill");
            btn_item = Game.UI.GetNode<Button>(this, "btn_item");
            btn_equip = Game.UI.GetNode<Button>(this, "btn_equip");
            btn_fashion = Game.UI.GetNode<Button>(this, "btn_fashion");
        }

        protected internal override void Enter(object o)
        {
            base.Enter(o);

            btn_back.onClick.AddListener(OnClickBack);
            btn_home.onClick.AddListener(OnClickHome);

            btn_detail.onClick.AddListener(OnClickDetail);
            btn_nourish.onClick.AddListener(OnClickNourish);
            btn_destiny.onClick.AddListener(OnClickDestiny);
            btn_weapon.onClick.AddListener(OnClickWeapon);
            btn_medal.onClick.AddListener(OnClickMedal);
            btn_skill.onClick.AddListener(OnClickSkill);
            btn_item.onClick.AddListener(OnClickItem);
            btn_equip.onClick.AddListener(OnClickEquip);
            btn_fashion.onClick.AddListener(OnClickFashion);
        }

        protected internal override void Leave()
        {
            base.Leave();
            btn_back.onClick.RemoveListener(OnClickBack);
            btn_home.onClick.RemoveListener(OnClickHome);

            btn_detail.onClick.RemoveListener(OnClickDetail);
            btn_nourish.onClick.RemoveListener(OnClickNourish);
            btn_destiny.onClick.RemoveListener(OnClickDestiny);
            btn_weapon.onClick.RemoveListener(OnClickWeapon);
            btn_medal.onClick.RemoveListener(OnClickMedal);
            btn_skill.onClick.RemoveListener(OnClickSkill);
            btn_item.onClick.RemoveListener(OnClickItem);
            btn_equip.onClick.RemoveListener(OnClickEquip);
            btn_fashion.onClick.RemoveListener(OnClickFashion);

            Game.Data.GetGameData<RoleSystemData>().SetCurrentType(UINameType.RoleInfo);
        }

        protected internal override void Destroy()
        {
            base.Destroy();
        }

        private void OnClickBack()
        {
            Game.UI.ShowWindow(UINameType.CommandRoom);
        }

        private void OnClickHome()
        {
            Game.UI.ShowWindow(UINameType.CommandRoom);
        }

        private void OnClickDetail()
        {
            Game.Data.GetGameData<RoleSystemData>().ChangeUIState(UINameType.RoleInfo);
        }

        private void OnClickNourish()
        {
            Game.Data.GetGameData<RoleSystemData>().ChangeUIState(UINameType.RoleGrow);
        }

        private void OnClickDestiny()
        {
            Game.Data.GetGameData<RoleSystemData>().ChangeUIState(UINameType.RoleDestiny);
        }
        private void OnClickWeapon()
        {
            Game.Data.GetGameData<RoleSystemData>().ChangeUIState(UINameType.RoleInfo);
        }
        private void OnClickMedal()
        {
            Game.Data.GetGameData<RoleSystemData>().ChangeUIState(UINameType.RoleInfo);
        }
        private void OnClickSkill()
        {
            Game.Data.GetGameData<RoleSystemData>().ChangeUIState(UINameType.RoleInfo);
        }
        private void OnClickItem()
        {
            Game.Data.GetGameData<RoleSystemData>().ChangeUIState(UINameType.RoleInfo);
        }
        private void OnClickEquip()
        {
            Game.Data.GetGameData<RoleSystemData>().ChangeUIState(UINameType.RoleInfo);
        }
        private void OnClickFashion()
        {
            Game.Data.GetGameData<RoleSystemData>().ChangeUIState(UINameType.RoleInfo);
        }
    }
}