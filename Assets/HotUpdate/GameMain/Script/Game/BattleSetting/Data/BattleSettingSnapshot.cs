using System.Collections.Generic;
using GameMain.Script.Role;
using UnityEngine;

namespace GameMain.Script.BattleSetting
{
    /// <summary>
    /// 战前整备的只读快照（View 只拿这个，不直接读 BattleSettingData / 配置表）。
    ///
    /// 由 BattleSettingSystem.BuildSnapshot() 组装：把队伍、站位、物资、遭遇战一次性摊平成 UI 好用的结构。
    /// </summary>
    public class BattleSettingSnapshot
    {
        /// <summary>当前遭遇战 ID（0 = 未配置）</summary>
        public int m_EncounterId;

        /// <summary>遭遇战名（章节名，界面左上角）</summary>
        public string m_EncounterName = string.Empty;

        /// <summary>战场长度（站位换算用）</summary>
        public float m_FieldLength = 40f;

        /// <summary>出战队伍（按槽位顺序，最多 c_MaxTeamCount 人）</summary>
        public List<BattleSettingTeamEntry> m_Team = new List<BattleSettingTeamEntry>();

        /// <summary>机械骡货仓（固定 c_MuleItemCount 格，空槽 m_ItemId = 0）</summary>
        public List<BattleSettingItemSlot> m_MuleItems = new List<BattleSettingItemSlot>();

        /// <summary>队伍支援栏（固定 c_SupportItemCount 格，空槽 m_ItemId = 0）</summary>
        public List<BattleSettingItemSlot> m_SupportItems = new List<BattleSettingItemSlot>();

        /// <summary>是否满足开战条件</summary>
        public bool m_CanStart;

        /// <summary>不能开战时的原因（给界面/日志用）</summary>
        public string m_BlockReason = string.Empty;

        /// <summary>出战人数</summary>
        public int GetTeamCount()
        {
            return m_Team.Count;
        }

        public override string ToString()
        {
            return "{ encounter:" + m_EncounterId + "(" + m_EncounterName + "), team:" + m_Team.Count
                + ", mule:" + m_MuleItems.Count + ", support:" + m_SupportItems.Count
                + ", canStart:" + m_CanStart + " }";
        }
    }

    /// <summary>
    /// 队伍里的一名角色：角色快照 + 站位 + 槽位。
    /// </summary>
    public class BattleSettingTeamEntry
    {
        /// <summary>槽位（0 开始，等于队伍列表下标）</summary>
        public int m_Slot;

        /// <summary>角色 ID</summary>
        public int m_RoleId;

        /// <summary>角色只读快照（含职级/属性/装备/技能）</summary>
        public RoleSnapshot m_Role;

        /// <summary>归一化站位（0 = 战场最左，1 = 战场最右）</summary>
        public float m_Position = BattleSettingDefine.c_DefaultPosition;

        /// <summary>换算成战场坐标后的站位</summary>
        public float m_FieldPosition;

        public override string ToString()
        {
            return "{ slot:" + m_Slot + ", role:" + m_RoleId + ", pos:" + m_Position.ToString("F2") + " }";
        }
    }

    /// <summary>
    /// 物资格：物品 ID + 配置 + 持有数量。
    /// </summary>
    public class BattleSettingItemSlot
    {
        /// <summary>槽位下标</summary>
        public int m_Slot;

        /// <summary>物品 ID（0 = 空槽）</summary>
        public int m_ItemId;

        /// <summary>物品配置（空槽为 null）</summary>
        public cfg.GameData_Item.ItemData m_Item;

        /// <summary>仓库里当前持有数量</summary>
        public int m_Count;

        /// <summary>是否空槽</summary>
        public bool IsEmpty
        {
            get { return m_ItemId <= 0; }
        }

        /// <summary>图标 URL（空槽返回 null，交给 CommonItemCom 切成空态）</summary>
        public string GetIconUrl()
        {
            return m_Item == null || string.IsNullOrEmpty(m_Item.Icon) ? null : m_Item.Icon;
        }

        public override string ToString()
        {
            return "{ slot:" + m_Slot + ", item:" + m_ItemId + ", count:" + m_Count + " }";
        }
    }
}
