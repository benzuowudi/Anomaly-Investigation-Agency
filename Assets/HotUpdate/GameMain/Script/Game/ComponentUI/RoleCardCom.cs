using System.Collections.Generic;
using FairyGUI;
using FairyGUI.Utils;
using GameMain.Script.Role;
using UnityEngine;
using cfg.GameData_Item;
using cfg.GameData_Skill;

namespace GameMain.Script.UI
{
    /// <summary>
    /// 角色列表 / 招募结果里的单张角色卡（FairyGUI 包 Base / RoleCardCom）。
    ///
    /// 展示：NO、竖排姓名、角色立绘、职称徽章、装备/主动/被动技能图标。
    /// </summary>
    public class RoleCardCom : GComponent
    {
        /// <summary>角色卡立绘，按角色 ID 轮换（角色配置接入立绘字段后可替换）</summary>
        private static readonly string[] c_CardImages =
        {
            "ui://Role/img_longnai_card",
            "ui://Role/img_hutao_card",
            "ui://Role/img_qianshu_card",
            "ui://Role/img_ruixi_card",
        };

        private GLoader load_role;
        private CommonPosCom com_pos;
        private GTextField txt_name, txt_NO;
        private CommonSkillIconCom com_equip, com_skill_1, com_skill_2;
        private Controller controller_state;

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            controller_state = GetController("state");
            load_role = GetChild("load_role").asLoader;
            load_role.fill = FillType.Scale;
            // 构造时 Role 包可能还没加载，先清空默认 URL，等 SetCom 时再设置
            load_role.url = null;
            com_pos = GetChild("com_pos") as CommonPosCom;
            txt_name = GetChild("txt_name").asTextField;
            txt_NO = GetChild("txt_NO").asTextField;
            com_equip = GetChild("com_equip") as CommonSkillIconCom;
            com_skill_1 = GetChild("com_skill_1") as CommonSkillIconCom;
            com_skill_2 = GetChild("com_skill_2") as CommonSkillIconCom;
        }

        /// <summary>page 0 选中，page 1 未选中（带黑色半透明遮罩）</summary>
        public void SetSelected(bool _selected)
        {
            controller_state.selectedIndex = _selected ? 0 : 1;
        }

        public bool IsSelected
        {
            get { return controller_state.selectedIndex == 0; }
        }

        public void SetCom(RoleRecord _role)
        {
            txt_NO.text = "NO." + _role.m_Id.ToString("D4");
            txt_name.text = _role.GetDisplayName();
            load_role.url = GetCardImage(_role.m_Id);

            com_pos.SetCom(_role.GetTitleName(), (int)_role.GetTitleLevel());

            List<EquipData> kEquips = RoleSystem.GetEquips(_role);
            SetIcon(com_equip, kEquips.Count > 0 && kEquips[0] != null ? kEquips[0].Icon : null);

            SkillData kActive = RoleSystem.GetSkill(_role.m_ActiveSkillId);
            SetIcon(com_skill_1, kActive != null ? kActive.Icon : null);

            SkillData kPassive = RoleSystem.GetSkill(_role.m_PassiveSkillId);
            SetIcon(com_skill_2, kPassive != null ? kPassive.Icon : null);
        }

        private static string GetCardImage(int _roleId)
        {
            int kIndex = Mathf.Abs(_roleId - 1) % c_CardImages.Length;
            return c_CardImages[kIndex];
        }

        private static void SetIcon(CommonSkillIconCom _com, string _icon)
        {
            string kUrl = ResolveIcon(_icon);
            _com.SetCom(kUrl, !string.IsNullOrEmpty(kUrl));
        }

        private static string ResolveIcon(string _icon)
        {
            if (string.IsNullOrEmpty(_icon))
            {
                return null;
            }

            if (_icon.StartsWith("ui://"))
            {
                return _icon;
            }

            return "ui://CommonUI/" + _icon;
        }
    }
}
