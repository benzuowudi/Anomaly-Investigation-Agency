using System.Collections.Generic;

namespace GameMain.Script.Role
{
    /// <summary>
    /// 角色的"整体数据"快照。
    ///
    /// 一次取到：属性（基础值 + 职称/装备加成后的最终值）、职称、装备、勋章、
    /// 主动/被动技能、天赋、行为逻辑、持久效果、持械手。
    /// 战斗 / 界面 / 展示都只拿这一个对象，不必再各自去拼 cfg 配置表和加成规则。
    ///
    /// 快照是只读视图（由 RoleSystem.BuildSnapshot 组装），改数据仍然走 RoleSystem 的写接口。
    /// </summary>
    public class RoleSnapshot
    {
        /// <summary>角色原始记录（权威数据，改之前请调 RoleSystem）</summary>
        public RoleRecord m_Role;

        #region 职称

        public RoleTitleLevel m_TitleLevel;
        public string m_TitleName = string.Empty;
        public int m_TitleScore;
        /// <summary>晋升下一职称所需评分，-1 表示已满级</summary>
        public int m_NextTitleScore;
        /// <summary>当前职称可佩戴的勋章数量</summary>
        public int m_MedalSlots;
        /// <summary>当前职称可穿戴的护甲件数</summary>
        public int m_ArmorSlots;

        #endregion

        #region 属性

        /// <summary>基础属性（role.xlsx 掷点/存档值，不含任何加成）</summary>
        public Dictionary<int, int> m_BaseAttributes = new Dictionary<int, int>();
        /// <summary>最终属性（基础 + 职称 + 装备，并按 maxValue 封顶）</summary>
        public Dictionary<int, int> m_FinalAttributes = new Dictionary<int, int>();
        /// <summary>加成差值（最终 - 基础），方便 UI 直接显示 +N</summary>
        public Dictionary<int, int> m_AttributeBonus = new Dictionary<int, int>();

        /// <summary>防御：装备提供的减伤，role.xlsx 里没有这条属性，单独放在这里</summary>
        public int m_Defense;
        /// <summary>已穿戴装备的总重量</summary>
        public int m_TotalWeight;

        #endregion

        #region 装备 / 勋章 / 技能 / 天赋

        public List<cfg.GameData_Item.EquipData> m_Equips = new List<cfg.GameData_Item.EquipData>();
        public List<cfg.GameData_Item.MedalData> m_Medals = new List<cfg.GameData_Item.MedalData>();

        /// <summary>装备 / 勋章 / 套装带出来的效果ID（已去重，对应 buff 表）</summary>
        public List<int> m_BuffIds = new List<int>();

        public cfg.GameData_Skill.TalentData m_Talent;
        public cfg.GameData_Skill.SkillData m_ActiveSkill;
        public cfg.GameData_Skill.SkillData m_PassiveSkill;
        public List<cfg.GameData_Skill.behaviorLogicData> m_Behaviors = new List<cfg.GameData_Skill.behaviorLogicData>();
        public List<RoleEffect> m_Effects = new List<RoleEffect>();
        public RoleHand m_MainHand = RoleHand.Right;

        #endregion

        #region 查询

        public int GetBaseAttr(int _attrId)
        {
            int kValue;
            return m_BaseAttributes.TryGetValue(_attrId, out kValue) ? kValue : 0;
        }

        public int GetFinalAttr(int _attrId)
        {
            int kValue;
            return m_FinalAttributes.TryGetValue(_attrId, out kValue) ? kValue : 0;
        }

        public int GetAttrBonus(int _attrId)
        {
            int kValue;
            return m_AttributeBonus.TryGetValue(_attrId, out kValue) ? kValue : 0;
        }

        /// <summary>装备 / 勋章 / 套装是否带出了某个效果</summary>
        public bool HasBuff(int _buffId)
        {
            return m_BuffIds.Contains(_buffId);
        }

        /// <summary>角色身上的持久效果层数</summary>
        public int GetEffectStacks(int _effectId)
        {
            for (int i = 0; i < m_Effects.Count; i++)
            {
                if (m_Effects[i].m_Id == _effectId)
                {
                    return m_Effects[i].m_Stacks;
                }
            }
            return 0;
        }

        public string GetDisplayName()
        {
            return m_Role == null ? string.Empty : m_Role.GetDisplayName();
        }

        #endregion

        public override string ToString()
        {
            return "{ role:" + GetDisplayName()
                + ", title:" + m_TitleName + "(" + m_TitleScore + ")"
                + ", equips:" + m_Equips.Count
                + ", medals:" + m_Medals.Count
                + ", buffs:" + m_BuffIds.Count
                + ", talent:" + (m_Talent == null ? 0 : m_Talent.ID)
                + " }";
        }
    }
}
