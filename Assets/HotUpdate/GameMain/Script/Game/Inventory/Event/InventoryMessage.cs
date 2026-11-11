namespace GameMain.Script.Inventory
{
    /// <summary>
    /// 单个道具数量变化的消息（OnItemChanged 的载荷），只读快照。
    /// </summary>
    public class ItemChangedMsg
    {
        /// <summary>道具 ID</summary>
        public int m_ItemId;

        /// <summary>变化前的数量</summary>
        public int m_OldCount;

        /// <summary>变化后的数量</summary>
        public int m_NewCount;

        /// <summary>本次变化量（新值 - 旧值，可能为负）</summary>
        public int m_Delta;

        /// <summary>变化来源</summary>
        public InventoryChangeReason m_Reason;

        public ItemChangedMsg(int _itemId, int _oldCount, int _newCount, InventoryChangeReason _reason)
        {
            m_ItemId = _itemId;
            m_OldCount = _oldCount;
            m_NewCount = _newCount;
            m_Delta = _newCount - _oldCount;
            m_Reason = _reason;
        }

        public override string ToString()
        {
            return "{ itemId:" + m_ItemId + ", old:" + m_OldCount + ", new:" + m_NewCount
                + ", delta:" + m_Delta + ", reason:" + m_Reason + " }";
        }
    }

    /// <summary>
    /// 仓库整体状态变化的消息（OnInventoryChanged 的载荷），只读快照。
    /// </summary>
    public class InventoryChangedMsg
    {
        /// <summary>数据版本号，每成功变更一次 +1</summary>
        public long m_Version;

        /// <summary>已占用格子数（数量 &gt; 0 的道具种类）</summary>
        public int m_UsedSlots;

        /// <summary>已占用总重量</summary>
        public int m_TotalWeight;

        /// <summary>变化来源</summary>
        public InventoryChangeReason m_Reason;

        public InventoryChangedMsg(long _version, int _usedSlots, int _totalWeight, InventoryChangeReason _reason)
        {
            m_Version = _version;
            m_UsedSlots = _usedSlots;
            m_TotalWeight = _totalWeight;
            m_Reason = _reason;
        }

        public override string ToString()
        {
            return "{ version:" + m_Version + ", usedSlots:" + m_UsedSlots
                + ", totalWeight:" + m_TotalWeight + ", reason:" + m_Reason + " }";
        }
    }
}
