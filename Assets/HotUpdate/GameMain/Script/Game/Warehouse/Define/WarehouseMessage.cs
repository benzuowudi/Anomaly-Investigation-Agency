namespace GameMain.Script.Warehouse
{
    /// <summary>
    /// 道具数量变更消息（OnItemUpdate 的载荷）。
    /// 既是"请求"也是"回执"：业务侧填好 ItemId / Op / Count / Reason 后发出，
    /// 权威数据库 WarehouseData 监听并写库，再把 Result / OldCount / NewCount 回填，
    /// 因为 EventManager 是同步广播，调用方在 SendMessage 之后即可直接读取结果。
    /// </summary>
    public class ItemUpdateMsg
    {
        /// <summary>道具ID（必须存在于 item 表）</summary>
        public int m_ItemId;
        /// <summary>操作类型</summary>
        public ItemChangeOp m_Op;
        /// <summary>数量（Add/Remove 为增量，Set 为目标值，均要求 >= 0）</summary>
        public int m_Count;
        /// <summary>变更来源</summary>
        public ItemChangeReason m_Reason;
        /// <summary>执行结果，由权威数据库回填</summary>
        public ItemChangeResult m_Result;
        /// <summary>变更前的数量，由权威数据库回填</summary>
        public int m_OldCount;
        /// <summary>变更后的数量，由权威数据库回填</summary>
        public int m_NewCount;
        /// <summary>失败原因描述</summary>
        public string m_Error;

        public ItemUpdateMsg(int _itemId, ItemChangeOp _op, int _count, ItemChangeReason _reason = ItemChangeReason.None)
        {
            m_ItemId = _itemId;
            m_Op = _op;
            m_Count = _count;
            m_Reason = _reason;
            m_Result = ItemChangeResult.Pending;
            m_OldCount = 0;
            m_NewCount = 0;
            m_Error = string.Empty;
        }

        /// <summary>
        /// 本次实际变化的数量（新值 - 旧值，可能为负）
        /// </summary>
        public int GetDelta()
        {
            return m_NewCount - m_OldCount;
        }

        public bool IsSuccess
        {
            get { return m_Result == ItemChangeResult.Success; }
        }

        public override string ToString()
        {
            return "{ itemId:" + m_ItemId + ", op:" + m_Op + ", count:" + m_Count
                + ", reason:" + m_Reason + ", result:" + m_Result
                + ", old:" + m_OldCount + ", new:" + m_NewCount
                + ", error:" + m_Error + " }";
        }
    }

    /// <summary>
    /// 单个道具变更完成后的通知（OnItemChanged 的载荷），只读。
    /// </summary>
    public class ItemChangedMsg
    {
        public int m_ItemId;
        public int m_OldCount;
        public int m_NewCount;
        public int m_Delta;
        public ItemChangeReason m_Reason;

        public ItemChangedMsg(int _itemId, int _oldCount, int _newCount, ItemChangeReason _reason)
        {
            m_ItemId = _itemId;
            m_OldCount = _oldCount;
            m_NewCount = _newCount;
            m_Delta = _newCount - _oldCount;
            m_Reason = _reason;
        }
    }

    /// <summary>
    /// 仓库整体状态变化通知（OnWarehouseChanged 的载荷），只读。
    /// </summary>
    public class WarehouseChangedMsg
    {
        /// <summary>数据版本号，每成功变更一次 +1</summary>
        public long m_Version;
        /// <summary>已占用格子数（数量 > 0 的道具种类）</summary>
        public int m_UsedSlots;
        /// <summary>已占用总重量</summary>
        public int m_TotalWeight;
        /// <summary>触发本次变化的来源</summary>
        public ItemChangeReason m_Reason;

        public WarehouseChangedMsg(long _version, int _usedSlots, int _totalWeight, ItemChangeReason _reason)
        {
            m_Version = _version;
            m_UsedSlots = _usedSlots;
            m_TotalWeight = _totalWeight;
            m_Reason = _reason;
        }
    }
}
