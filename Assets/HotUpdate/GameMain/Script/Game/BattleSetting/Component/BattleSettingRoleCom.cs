using System;
using FairyGUI;
using FairyGUI.Utils;
using GameMain.Script.BattleSetting;
using GameMain.Script.Role;

namespace GameMain.Script.UI
{
    /// <summary>
    /// 队员条（FairyGUI 包 BattleSetting / BattleSettingRoleCom）。
    ///
    /// controller "state"：0 = show（有队员），1 = empty（空槽位）
    /// 有队员：txt_name_chinese / txt_name_english / com_pos / list_attr / list_role 可见，btn_cut 用来移除
    /// 空槽位：btn_add（整格按钮）+ 加号，点击由 View 打开角色选择弹窗
    ///
    /// 数据来自 BattleSettingTeamEntry（内含 RoleSnapshot），只渲染不改数据。
    /// </summary>
    public class BattleSettingRoleCom : GComponent
    {
        /// <summary>与角色详情页一致：生命 / 攻击 / 敏捷 / 感知 / 理智</summary>
        private static readonly int[] c_AttrIds =
        {
            RoleAttrId.Hp, RoleAttrId.Atk, RoleAttrId.Agi, RoleAttrId.Per, RoleAttrId.San,
        };

        /// <summary>controller state 的两页</summary>
        private const int c_StateShow = 0;
        private const int c_StateEmpty = 1;

        private Controller m_State;
        private GTextField txt_name_chinese, txt_name_english;
        private CommonPosCom com_pos;
        private GList list_attr, list_role;
        private GButton btn_add, btn_cut;

        /// <summary>当前显示的队员（null = 空槽位）</summary>
        private BattleSettingTeamEntry m_Entry;

        /// <summary>在队伍列表里的槽位</summary>
        private int m_Slot;

        private Action<int> m_OnAdd;
        private Action<int> m_OnCut;

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            m_State = GetController("state");
            txt_name_chinese = GetChild("txt_name_chinese").asTextField;
            txt_name_english = GetChild("txt_name_english").asTextField;
            com_pos = GetChild("com_pos") as CommonPosCom;
            list_attr = GetChild("list_attr").asList;
            list_role = GetChild("list_role").asList;
            btn_add = GetChild("btn_add").asButton;
            btn_cut = GetChild("btn_cut").asButton;

            list_attr.itemRenderer = AttrItemRenderer;
            list_role.itemRenderer = ItemRenderer;
            btn_add.onClick.Add(OnClickAdd);
            btn_cut.onClick.Add(OnClickCut);
        }

        /// <summary>
        /// 刷新一行。_entry 为 null 时显示空槽位（加号），点击回调 onAdd。
        /// </summary>
        public void SetCom(BattleSettingTeamEntry _entry, int _slot, Action<int> _onAdd, Action<int> _onCut)
        {
            m_Entry = _entry;
            m_Slot = _slot;
            m_OnAdd = _onAdd;
            m_OnCut = _onCut;

            bool kFilled = _entry != null && _entry.m_Role != null;
            m_State.selectedIndex = kFilled ? c_StateShow : c_StateEmpty;
            btn_add.visible = !kFilled;
            btn_cut.visible = kFilled;

            if (!kFilled)
            {
                Clear();
                return;
            }

            RoleSnapshot kRole = _entry.m_Role;
            txt_name_chinese.text = kRole.m_Role.GetDisplayName();
            txt_name_english.text = RoleTitleRule.GetNameEn(kRole.m_TitleLevel);
            com_pos.SetCom(kRole.m_TitleName, (int)kRole.m_TitleLevel);

            list_attr.numItems = c_AttrIds.Length;
            list_role.numItems = 3;
        }

        private void Clear()
        {
            txt_name_chinese.text = string.Empty;
            txt_name_english.text = string.Empty;
            list_attr.numItems = 0;
            list_role.numItems = 0;
        }

        private void OnClickAdd()
        {
            if (m_OnAdd != null)
            {
                m_OnAdd(m_Slot);
            }
        }

        private void OnClickCut()
        {
            if (m_OnCut != null)
            {
                m_OnCut(m_Slot);
            }
        }

        #region 属性

        private void AttrItemRenderer(int _index, GObject _obj)
        {
            if (_obj is not CommonAttrCom kCom)
            {
                return;
            }

            if (m_Entry == null || m_Entry.m_Role == null || _index < 0 || _index >= c_AttrIds.Length)
            {
                return;
            }

            int kAttrId = c_AttrIds[_index];
            kCom.SetCom(GetAttrIcon(kAttrId), m_Entry.m_Role.GetFinalAttr(kAttrId).ToString());
        }

        private static string GetAttrIcon(int _attrId)
        {
            switch (_attrId)
            {
                case RoleAttrId.Hp:
                    return "ui://CommonUI/ui_icon_hp";
                case RoleAttrId.Atk:
                    return "ui://CommonUI/ui_icon_atk";
                case RoleAttrId.Agi:
                    return "ui://CommonUI/ui_icon_spd";
                case RoleAttrId.Per:
                    return "ui://CommonUI/ui_icon_per";
                case RoleAttrId.San:
                    return "ui://CommonUI/ui_icon_san";
                default:
                    return "ui://CommonUI/ui_icon_atk";
            }
        }

        #endregion

        #region 装备 / 技能

        private void ItemRenderer(int _index, GObject _obj)
        {
            if (_obj is not CommonItemCom kCom)
            {
                return;
            }

            if (m_Entry == null || m_Entry.m_Role == null)
            {
                kCom.SetCom(CommonItemCom.StateEmpty);
                return;
            }

            RoleSnapshot kRole = m_Entry.m_Role;
            switch (_index)
            {
                case 0:
                {
                    cfg.GameData_Item.EquipData kEquip = PickEquip(kRole);
                    kCom.SetCom(kEquip == null ? CommonItemCom.StateEmpty : CommonItemCom.StateEquip,
                        kEquip == null ? null : kEquip.Icon,
                        kEquip == null ? "未装备武器" : kEquip.Name);
                    break;
                }
                case 1:
                {
                    cfg.GameData_Skill.SkillData kSkill = kRole.m_ActiveSkill;
                    kCom.SetCom(kSkill == null ? CommonItemCom.StateEmpty : CommonItemCom.StateItem,
                        kSkill == null ? null : CommonSkillIconCom.ResolveIconUrl(kSkill.Icon),
                        kSkill == null ? "无主动技能" : kSkill.Name);
                    break;
                }
                default:
                {
                    cfg.GameData_Skill.TalentData kTalent = kRole.m_Talent;
                    kCom.SetCom(kTalent == null ? CommonItemCom.StateEmpty : CommonItemCom.StateItem,
                        kTalent == null ? null : CommonSkillIconCom.ResolveIconUrl(kTalent.Icon),
                        kTalent == null ? "无被动天赋" : kTalent.Name);
                    break;
                }
            }
        }

        /// <summary>和角色详情页一致：优先显示非护甲的装备，没有就退回第一件</summary>
        private static cfg.GameData_Item.EquipData PickEquip(RoleSnapshot _role)
        {
            if (_role == null || _role.m_Equips == null || _role.m_Equips.Count == 0)
            {
                return null;
            }

            for (int i = 0; i < _role.m_Equips.Count; i++)
            {
                if (!RoleTitleRule.IsArmor(_role.m_Equips[i].Type1))
                {
                    return _role.m_Equips[i];
                }
            }

            return _role.m_Equips[0];
        }

        #endregion
    }
}
