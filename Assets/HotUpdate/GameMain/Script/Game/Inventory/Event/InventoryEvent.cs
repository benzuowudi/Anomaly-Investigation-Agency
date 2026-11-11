namespace GameMain.Script.Inventory
{
    /// <summary>
    /// 仓库（Inventory）系统的事件名（Event 辅助模块）。
    ///
    /// 事件只在 InventorySystem 写入 InventoryData 成功后广播；
    /// Data 层不发事件，界面的读接口一律走 InventorySystem。
    /// </summary>
    public static class InventoryEvent
    {
        /// <summary>单个道具数量发生变化（已写入权威数据）。参数：ItemChangedMsg</summary>
        public const string OnItemChanged = "OnItemChanged";

        /// <summary>仓库整体状态变化（版本 / 占用格子 / 总重量）。参数：InventoryChangedMsg</summary>
        public const string OnInventoryChanged = "OnInventoryChanged";
    }
}
