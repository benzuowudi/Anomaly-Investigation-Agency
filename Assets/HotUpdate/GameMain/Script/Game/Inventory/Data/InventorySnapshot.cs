using System.Collections.Generic;

namespace GameMain.Script.Inventory
{
    /// <summary>
    /// 仓库界面的一行展示数据（道具或装备摊平后的只读视图）。
    /// 由 InventorySystem.BuildSnapshot 组装，界面只读不改。
    /// </summary>
    public class InventoryEntry
    {
        /// <summary>道具 ID（item 表）或装备 ID（equip 表）</summary>
        public int m_Id;

        /// <summary>true = 装备（equip 表 + ArmoryData），false = 道具（item 表）</summary>
        public bool m_IsEquip;

        /// <summary>持有数量</summary>
        public int m_Count;

        /// <summary>配置名</summary>
        public string m_Name;

        /// <summary>配置图标（item.icon / equip.icon）</summary>
        public string m_Icon;

        /// <summary>品质 1~7</summary>
        public int m_Quality;

        public override string ToString()
        {
            return "{ id:" + m_Id + ", equip:" + m_IsEquip + ", count:" + m_Count
                + ", quality:" + m_Quality + ", name:" + m_Name + " }";
        }
    }

    /// <summary>
    /// 仓库界面的一次性只读快照：当前页签的条目 + 容量信息。
    /// </summary>
    public class InventorySnapshot
    {
        /// <summary>快照对应的页签</summary>
        public InventoryTab m_Tab;

        /// <summary>数据版本号</summary>
        public long m_Version;

        /// <summary>已占用格子数</summary>
        public int m_UsedSlots;

        /// <summary>格子上限（0 = 不限制）</summary>
        public int m_MaxSlots;

        /// <summary>已占用总重量（暂时每件按 1 计）</summary>
        public int m_TotalWeight;

        /// <summary>负重上限（0 = 不限制）</summary>
        public int m_MaxWeight;

        /// <summary>当前页签的条目（已按品质降序、同品质 ID 升序排好）</summary>
        public readonly List<InventoryEntry> m_Items = new List<InventoryEntry>();

        public bool IsEmpty
        {
            get { return m_Items.Count == 0; }
        }

        public override string ToString()
        {
            return "{ tab:" + m_Tab + ", items:" + m_Items.Count + ", version:" + m_Version
                + ", slots:" + m_UsedSlots + "/" + m_MaxSlots
                + ", weight:" + m_TotalWeight + "/" + m_MaxWeight + " }";
        }
    }
}
