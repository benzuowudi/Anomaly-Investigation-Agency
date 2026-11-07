using System.Text;
using FairyGUI;
using FairyGUI.Utils;
using GameMain.Script.Role;
using UnityEngine;
using cfg.GameData_Item;
using cfg.GameData_Skill;

namespace GameMain.Script.UI
{
    /// <summary>
    /// 角色详情页（FairyGUI 包 Role / RoleInfoCom）。
    ///
    /// 数据全部来自 RoleSystem.GetRoleData(roleId) 返回的 RoleSnapshot：
    ///   NO. / 职称序号 / 中英文名 / 属性列表 / 装备 / 勋章 / 主动技能 / 被动天赋
    /// 这里只负责把快照刷到控件上，不修改任何角色数据。
    /// </summary>
    public class RoleInfoCom : GComponent
    {
        /// <summary>右侧属性列表固定展示的 5 条核心属性（与设计稿一致）</summary>
        private static readonly int[] c_CoreAttrIds =
        {
            RoleAttrId.Hp, RoleAttrId.Atk, RoleAttrId.Agi, RoleAttrId.Per, RoleAttrId.San,
        };

        private const string c_DefaultSkillIcon = "ui://CommonUI/Potion-Icon-Small";

        private GTextField txt_titleC, txt_titleE;
        private GTextField txt_NO, txt_position, txt_nameC, txt_nameE;
        private GTextField txt_equipName, txt_equipType;

        private RoleTitleCom com_title_attr, com_title_equip, com_title_medal;
        private GComponent com_equip, com_medal_1, com_medal_2, com_medal_3;
        private GList list_attr, list_skill;

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            txt_titleC = GetChild("txt_title_1").asTextField;
            txt_titleE = GetChild("txt_title_2").asTextField;
            txt_NO = GetChild("txt_NO").asTextField;
            txt_position = GetChild("txt_position").asTextField;
            txt_nameC = GetChild("txt_nameC").asTextField;
            txt_nameE = GetChild("txt_nameE").asTextField;
            txt_equipName = GetChild("txt_equipName").asTextField;
            txt_equipType = GetChild("txt_equipType").asTextField;

            com_title_attr = GetChild("com_title_attr") as RoleTitleCom;
            com_title_equip = GetChild("com_title_equip") as RoleTitleCom;
            com_title_medal = GetChild("com_title_medal") as RoleTitleCom;

            com_equip = GetChild("com_equip") as GComponent;
            com_medal_1 = GetChild("com_medal_1") as GComponent;
            com_medal_2 = GetChild("com_medal_2") as GComponent;
            com_medal_3 = GetChild("com_medal_3") as GComponent;

            list_attr = GetChild("list_attr").asList;
            list_skill = GetChild("list_skill").asList;

            txt_titleC.text = "详情";
            txt_titleE.text = "DETAIL";
            com_title_attr.SetCom("角色属性", "ATTRIBUTES");
            com_title_equip.SetCom("装备", "EQUIP");
            com_title_medal.SetCom("勋章", "MEDAL");
        }

        /// <summary>
        /// 用角色快照刷新整个详情页；传 null 时清空。
        /// </summary>
        public void SetCom(RoleSnapshot _snapshot)
        {
            if (_snapshot == null || _snapshot.m_Role == null)
            {
                Clear();
                return;
            }

            RoleRecord kRole = _snapshot.m_Role;

            txt_NO.text = "NO." + kRole.m_Id.ToString("D4");
            txt_position.text = GetOrdinal((int)_snapshot.m_TitleLevel + 1);
            SetNameText(kRole.GetDisplayName());
            txt_nameE.text = RoleTitleRule.GetNameEn(_snapshot.m_TitleLevel);

            FillAttributes(_snapshot);
            FillEquip(_snapshot);
            FillMedals(_snapshot);
            FillSkills(_snapshot);
        }

        /// <summary>
        /// 没有角色时清空展示。
        /// </summary>
        public void Clear()
        {
            txt_NO.text = "NO.----";
            txt_position.text = "--";
            txt_nameC.text = "--";
            txt_nameE.text = string.Empty;
            txt_equipName.text = "未装备";
            txt_equipType.text = string.Empty;

            SetItemState(com_equip, 0);
            SetItemState(com_medal_1, 0);
            SetItemState(com_medal_2, 0);
            SetItemState(com_medal_3, 0);

            list_attr.RemoveChildrenToPool();
            list_skill.RemoveChildrenToPool();
        }

        #region 属性

        private void FillAttributes(RoleSnapshot _snapshot)
        {
            list_attr.RemoveChildrenToPool();
            for (int i = 0; i < c_CoreAttrIds.Length; i++)
            {
                int kAttrId = c_CoreAttrIds[i];
                RoleAttrCom kItem = list_attr.AddItemFromPool() as RoleAttrCom;
                kItem.SetCom(GetAttrIcon(kAttrId), RoleSystem.GetAttributeName(kAttrId),
                    _snapshot.GetFinalAttr(kAttrId).ToString());
            }
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

        #region 装备 / 勋章

        private void FillEquip(RoleSnapshot _snapshot)
        {
            EquipData kEquip = null;
            for (int i = 0; i < _snapshot.m_Equips.Count; i++)
            {
                EquipData kItem = _snapshot.m_Equips[i];
                if (kItem != null && !RoleTitleRule.IsArmor(kItem.Type1))
                {
                    kEquip = kItem;
                    break;
                }
            }

            if (kEquip == null && _snapshot.m_Equips.Count > 0)
            {
                kEquip = _snapshot.m_Equips[0];
            }

            SetItemState(com_equip, kEquip == null ? 0 : 3);
            txt_equipName.text = kEquip == null ? "未装备" : kEquip.NameCn;
            txt_equipType.text = kEquip == null ? string.Empty : (RoleTitleRule.IsArmor(kEquip.Type1) ? "护甲" : "武器");
        }

        private void FillMedals(RoleSnapshot _snapshot)
        {
            FillMedal(com_medal_1, _snapshot, 0);
            FillMedal(com_medal_2, _snapshot, 1);
            FillMedal(com_medal_3, _snapshot, 2);
        }

        private void FillMedal(GComponent _com, RoleSnapshot _snapshot, int _index)
        {
            MedalData kMedal = null;
            if (_index < _snapshot.m_Medals.Count)
            {
                kMedal = _snapshot.m_Medals[_index];
            }

            SetItemState(_com, kMedal == null ? 0 : 1);
        }

        /// <summary>
        /// Base/CommonItemCom 的 state 控制器：0 空 / 1 道具 / 2 角色 / 3 装备
        /// </summary>
        private static void SetItemState(GComponent _com, int _state)
        {
            _com.GetController("state").selectedIndex = _state;
        }

        #endregion

        #region 技能 / 天赋

        private void FillSkills(RoleSnapshot _snapshot)
        {
            list_skill.RemoveChildrenToPool();

            SkillData kSkill = _snapshot.m_ActiveSkill;
            RoleSkillCom kActive = list_skill.AddItemFromPool() as RoleSkillCom;
            kActive.SetCom("主动技能", "ACTIVE SKILL",
                ResolveIcon(kSkill == null ? null : kSkill.Icon, c_DefaultSkillIcon),
                kSkill == null ? "无" : kSkill.NameCn,
                BuildSkillDesc(kSkill));

            TalentData kTalentData = _snapshot.m_Talent;
            RoleSkillCom kTalent = list_skill.AddItemFromPool() as RoleSkillCom;
            kTalent.SetCom("被动天赋", "PASSIVE TALENT",
                ResolveIcon(kTalentData == null ? null : kTalentData.Icon, c_DefaultSkillIcon),
                kTalentData == null ? "无天赋" : kTalentData.NameCn,
                kTalentData == null || string.IsNullOrEmpty(kTalentData.Desc) ? "无描述" : kTalentData.Desc);
        }

        /// <summary>
        /// 技能表没有描述字段，用技能 effect 里引用的 buff 说明拼出展示文本。
        /// </summary>
        private static string BuildSkillDesc(SkillData _skill)
        {
            if (_skill == null)
            {
                return "尚未配置主动技能";
            }

            if (_skill.Effect != null && _skill.Effect.Count > 0)
            {
                StringBuilder kBuilder = new StringBuilder();
                for (int i = 0; i < _skill.Effect.Count; i++)
                {
                    buffData kBuff = RoleSystem.GetBuff(_skill.Effect[i]);
                    if (kBuff == null)
                    {
                        continue;
                    }

                    if (kBuilder.Length > 0)
                    {
                        kBuilder.Append('\n');
                    }
                    kBuilder.Append(string.IsNullOrEmpty(kBuff.Desc) ? kBuff.NameCn : kBuff.Desc);
                }

                if (kBuilder.Length > 0)
                {
                    return kBuilder.ToString();
                }
            }

            return string.IsNullOrEmpty(_skill.NameEn) ? "无描述" : _skill.NameEn;
        }

        /// <summary>
        /// 配置里的 icon 可能是包内资源名，也可能是完整 ui:// 地址；解析不到时用默认图标。
        /// </summary>
        private static string ResolveIcon(string _icon, string _fallback)
        {
            if (!string.IsNullOrEmpty(_icon))
            {
                if (_icon.StartsWith("ui://"))
                {
                    if (UIPackage.GetItemByURL(_icon) != null)
                    {
                        return _icon;
                    }
                }
                else
                {
                    // 配置里可能写的是资源名或带目录/后缀的路径，统一取文件名再拼 CommonUI 包地址
                    string kName = _icon;
                    int kSlash = kName.LastIndexOf('/');
                    if (kSlash >= 0)
                    {
                        kName = kName.Substring(kSlash + 1);
                    }
                    int kDot = kName.LastIndexOf('.');
                    if (kDot > 0)
                    {
                        kName = kName.Substring(0, kDot);
                    }

                    string kUrl = "ui://CommonUI/" + kName;
                    if (UIPackage.GetItemByURL(kUrl) != null)
                    {
                        return kUrl;
                    }
                }
            }

            return _fallback;
        }

        #endregion

        #region 文本

        private void SetNameText(string _name)
        {
            txt_nameC.text = _name;

            // 姓名超过 5 个字时缩小字号，避免竖排文字超出设计稿高度
            int kSize = 100;
            if (_name != null && _name.Length > 5)
            {
                kSize = Mathf.Max(48, 100 * 5 / _name.Length);
            }

            TextFormat kFormat = txt_nameC.textFormat;
            kFormat.size = kSize;
            txt_nameC.textFormat = kFormat;
        }

        private static string GetOrdinal(int _number)
        {
            switch (_number)
            {
                case 1:
                    return "1st";
                case 2:
                    return "2nd";
                case 3:
                    return "3rd";
                default:
                    return _number + "th";
            }
        }

        #endregion
    }
}
