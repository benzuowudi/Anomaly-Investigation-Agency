using FairyGUI;
using FairyGUI.Utils;
using Framework.Script.UI.Anim;
using GameMain.Script.Role;
using cfg.GameData_Skill;

namespace GameMain.Script.UI
{
    /// <summary>
    /// 角色列表 / 招募结果里的单张角色卡（FairyGUI 包 Base / RoleCardCom，209×323）。
    ///
    /// 新结构（2026-11-11）：
    ///   n15         遮罩
    ///   n0/n14      卡面与底部色块
    ///   com_anim    RoleAnimCom（Spine，底边中心对齐）
    ///   com_pos     职称徽章
    ///   txt_name / txt_NO
    ///   com_equip   装备格（CommonItemCom，54×54）
    ///   com_skill_1 主动技能格（CommonItemCom）
    ///   com_skill_2 被动天赋格（CommonItemCom）
    ///
    /// 骨架暂时统一用 role1，皮肤按角色 ID 在 01~03 之间轮换。
    /// </summary>
    public class RoleCardCom : GComponent
    {
        /// <summary>角色骨架地址（现有资源只有 role1）</summary>
        private const string c_SpineAddress = "role1";

        /// <summary>动画在卡面里的目标区域（宽、高，等比缩放）</summary>
        private const float c_AnimWidth = 190f;
        private const float c_AnimHeight = 300f;

        private RoleAnimCom com_anim;
        private CommonPosCom com_pos;
        private GTextField txt_name, txt_NO;
        private CommonItemCom com_equip, com_skill_1, com_skill_2;
        private Controller controller_state;

        public override void ConstructFromXML(XML _xml)
        {
            base.ConstructFromXML(_xml);

            controller_state = GetController("state");
            com_anim = GetChild("com_anim") as RoleAnimCom;
            com_pos = GetChild("com_pos") as CommonPosCom;
            txt_name = GetChild("txt_name").asTextField;
            txt_NO = GetChild("txt_NO").asTextField;
            com_equip = GetChild("com_equip") as CommonItemCom;
            com_skill_1 = GetChild("com_skill_1") as CommonItemCom;
            com_skill_2 = GetChild("com_skill_2") as CommonItemCom;
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
            if (_role == null)
            {
                txt_NO.text = string.Empty;
                txt_name.text = string.Empty;
                com_equip.SetCom(CommonItemCom.StateEmpty);
                com_skill_1.SetCom(CommonItemCom.StateEmpty);
                com_skill_2.SetCom(CommonItemCom.StateEmpty);
                return;
            }

            txt_NO.text = "NO." + _role.m_Id.ToString("D4");
            txt_name.text = _role.GetDisplayName();

            // 角色动画：role1 骨架，皮肤按 ID 在 01~03 轮换，等比缩放到卡面区域
            string kSkin = ((_role.m_Id % 3) + 1).ToString("D2");
            com_anim.SetRole(c_SpineAddress, null, kSkin);
            com_anim.SetFit(UIAnimFit.Contain, c_AnimWidth, c_AnimHeight, UIAnimAnchor.BottomCenter);

            com_pos.SetCom(_role.GetTitleName(), (int)_role.GetTitleLevel());

            // 装备格：优先显示非护甲武器，没有就显示第一件
            cfg.GameData_Item.EquipData kEquip = PickEquip(_role);
            if (kEquip == null)
            {
                com_equip.SetCom(CommonItemCom.StateEmpty);
            }
            else
            {
                com_equip.SetEquip(kEquip.ID);
            }

            // 主动技能 / 被动天赋
            SkillData kActive = RoleSystem.GetSkill(_role.m_ActiveSkillId);
            if (kActive == null)
            {
                com_skill_1.SetCom(CommonItemCom.StateEmpty);
            }
            else
            {
                com_skill_1.SetCom(CommonItemCom.StateItem, kActive.Icon, 1, kActive.Name);
            }

            TalentData kTalent = RoleSystem.GetTalent(_role.m_TalentId);
            if (kTalent == null)
            {
                com_skill_2.SetCom(CommonItemCom.StateEmpty);
            }
            else
            {
                com_skill_2.SetCom(CommonItemCom.StateItem, kTalent.Icon, 1, kTalent.Name);
            }
        }

        private static cfg.GameData_Item.EquipData PickEquip(RoleRecord _role)
        {
            System.Collections.Generic.List<cfg.GameData_Item.EquipData> kEquips =
                RoleSystem.GetEquips(_role);
            if (kEquips == null || kEquips.Count == 0)
            {
                return null;
            }

            for (int i = 0; i < kEquips.Count; i++)
            {
                if (!RoleTitleRule.IsArmor(kEquips[i].Type1))
                {
                    return kEquips[i];
                }
            }

            return kEquips[0];
        }
    }
}
