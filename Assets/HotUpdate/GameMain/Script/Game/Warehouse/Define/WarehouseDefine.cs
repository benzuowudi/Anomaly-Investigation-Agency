namespace GameMain.Script.Warehouse
{
    /// <summary>
    /// 仓库系统的事件主题。
    /// EventManager 以字符串作为契约，所有主题统一收敛到这里，避免字面量散落在业务代码中。
    /// </summary>
    public static class WarehouseEventDefine
    {
        /// <summary>
        /// 道具数量变更请求：由业务侧发出，唯一监听方是权威数据库 WarehouseData。
        /// 监听者收到后校验并写入权威数据，同时把执行结果回填到消息体上。
        /// </summary>
        public const string OnItemUpdate = "OnItemUpdate";

        /// <summary>
        /// 单个道具变更完成（权威数据已写入），供 UI / 任务 / 引导等刷新表现。
        /// </summary>
        public const string OnItemChanged = "OnItemChanged";

        /// <summary>
        /// 仓库整体状态变化（版本号、占用格子、总重量），任何一次成功变更后广播。
        /// </summary>
        public const string OnWarehouseChanged = "OnWarehouseChanged";
    }

    /// <summary>
    /// 数量变更的操作类型
    /// </summary>
    public enum ItemChangeOp
    {
        /// <summary>增加</summary>
        Add = 0,
        /// <summary>减少</summary>
        Remove = 1,
        /// <summary>直接设置为指定数量</summary>
        Set = 2,
    }

    /// <summary>
    /// 数量变更的来源，便于统计与排查（不是写库的必需条件）
    /// </summary>
    public enum ItemChangeReason
    {
        None = 0,
        /// <summary>战斗掉落</summary>
        BattleDrop = 1,
        /// <summary>任务奖励</summary>
        QuestReward = 2,
        /// <summary>商店购买</summary>
        ShopBuy = 3,
        /// <summary>商店出售</summary>
        ShopSell = 4,
        /// <summary>使用道具</summary>
        UseItem = 5,
        /// <summary>兑换/合成</summary>
        Exchange = 6,
        /// <summary>读档</summary>
        Load = 7,
        /// <summary>调试指令</summary>
        Debug = 8,
    }

    /// <summary>
    /// 数量变更的执行结果
    /// </summary>
    public enum ItemChangeResult
    {
        /// <summary>尚未被权威数据库处理（说明监听者缺失）</summary>
        Pending = 0,
        /// <summary>成功</summary>
        Success = 1,
        /// <summary>item 表中不存在该道具</summary>
        UnknownItem = 2,
        /// <summary>数量不足，无法扣除</summary>
        NotEnough = 3,
        /// <summary>超出格子或负重上限</summary>
        Overflow = 4,
        /// <summary>数量非法（负数、零、越界）</summary>
        InvalidCount = 5,
        /// <summary>仓库数据尚未就绪</summary>
        NotReady = 6,
    }
}
