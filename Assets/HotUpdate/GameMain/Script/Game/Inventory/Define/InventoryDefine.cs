namespace GameMain.Script.Inventory
{
    /// <summary>
    /// 仓库（Inventory）的页签。页签顺序即界面 list_tab 的下标顺序。
    /// </summary>
    public enum InventoryTab
    {
        /// <summary>道具：item 表中已持有（数量 &gt; 0）的全部道具</summary>
        Item = 0,

        /// <summary>装备：军械库已合成的武器装备（数据在 ArmoryData）</summary>
        Equip = 1,
    }

    /// <summary>
    /// 道具数量变化的来源，只用于事件消息与日志（不是写库的必要条件）。
    /// </summary>
    public enum InventoryChangeReason
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
        /// <summary>兑换</summary>
        Exchange = 6,
        /// <summary>读档</summary>
        Load = 7,
        /// <summary>调试指令</summary>
        Debug = 8,
        /// <summary>军械库合成消耗</summary>
        Craft = 9,
    }

    /// <summary>
    /// 仓库（Inventory）系统的常量与页签文案。
    ///
    /// 职责边界：
    /// - 本系统是 item 表道具的权威数据（InventoryData）与唯一业务入口（InventorySystem），
    ///   军械库、战前整备等系统都通过 InventorySystem 增减道具；
    /// - 装备的持有数据仍归军械库（ArmoryData），仓库的"装备"页签只做只读展示；
    /// - 界面只调 InventorySystem，不直接读写 InventoryData。
    /// </summary>
    public static class InventoryDefine
    {
        /// <summary>空槽位的道具 ID</summary>
        public const int c_EmptyItemId = 0;

        /// <summary>界面中文标题</summary>
        public const string c_TitleChinese = "仓库";

        /// <summary>界面英文标题</summary>
        public const string c_TitleEnglish = "INVENTORY";

        /// <summary>日志前缀（带一个空格，拼日志时直接 c_LogTag + "内容" 即可）</summary>
        public const string c_LogTag = "[仓库] ";

        /// <summary>页签数量（与界面 list_tab 的按钮数一致）</summary>
        public static int GetTabCount()
        {
            return 2;
        }

        /// <summary>页签中文名</summary>
        public static string GetTabTitle(InventoryTab _tab)
        {
            switch (_tab)
            {
                case InventoryTab.Equip:
                    return "装备";
                default:
                    return "道具";
            }
        }

        /// <summary>页签英文名（暂时只用于日志，界面没有对应节点）</summary>
        public static string GetTabTitleEnglish(InventoryTab _tab)
        {
            switch (_tab)
            {
                case InventoryTab.Equip:
                    return "EQUIPMENT";
                default:
                    return "ITEMS";
            }
        }

        /// <summary>页签为空时的占位文案</summary>
        public static string GetEmptyText(InventoryTab _tab)
        {
            switch (_tab)
            {
                case InventoryTab.Equip:
                    return "还没有装备，先去军械库合成吧";
                default:
                    return "仓库里还没有道具";
            }
        }

        /// <summary>下标 → 页签（越界时回落到道具页）</summary>
        public static InventoryTab ToTab(int _index)
        {
            return _index == (int)InventoryTab.Equip ? InventoryTab.Equip : InventoryTab.Item;
        }
    }
}
