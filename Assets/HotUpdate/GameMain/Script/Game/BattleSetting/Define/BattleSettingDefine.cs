namespace GameMain.Script.BattleSetting
{
    /// <summary>
    /// 战前整备数据变化的来源，用于事件消息里区分"是哪一类改动"。
    /// </summary>
    public enum BattleSettingChangeReason
    {
        None = 0,
        /// <summary>读档</summary>
        Load = 1,
        /// <summary>编队变化（增删/排序）</summary>
        Team = 2,
        /// <summary>站位变化</summary>
        Position = 3,
        /// <summary>物资分配变化（机械骡 / 支援）</summary>
        Supply = 4,
        /// <summary>遭遇战变化</summary>
        Encounter = 5,
        /// <summary>整份重置</summary>
        Reset = 6,
    }

    /// <summary>
    /// 物资槽位归属。
    /// </summary>
    public enum BattleSettingSupplySlot
    {
        /// <summary>机械骡货仓（3 格）</summary>
        Mule = 0,
        /// <summary>队伍支援栏（5 格）</summary>
        Support = 1,
    }

    /// <summary>
    /// 战前整备（BattleSetting）系统的常量。
    ///
    /// 职责边界：
    /// - 本系统只管"战前配置"：编队、站位、物资分配、当前遭遇战；
    /// - 真正的战斗过程归 BattleSystem；角色数据归 RoleSystem，物品归 InventorySystem；
    /// - 本系统不写角色/仓库的权威数据，只读取与校验。
    /// </summary>
    public static class BattleSettingDefine
    {
        /// <summary>出战人数上限（与界面 5 个槽位一致）</summary>
        public const int c_MaxTeamCount = 5;

        /// <summary>机械骡货仓格数</summary>
        public const int c_MuleItemCount = 3;

        /// <summary>队伍支援栏格数</summary>
        public const int c_SupportItemCount = 5;

        /// <summary>未配置站位时的默认归一化位置（战场正中间）</summary>
        public const float c_DefaultPosition = 0.5f;

        /// <summary>归一化站位下限（战场最左）</summary>
        public const float c_MinPosition = 0f;

        /// <summary>归一化站位上限（战场最右）</summary>
        public const float c_MaxPosition = 1f;

        /// <summary>空槽位的物品 ID</summary>
        public const int c_EmptyItemId = 0;

        /// <summary>日志前缀（带一个空格，拼日志时直接 c_LogTag + "内容" 即可）</summary>
        public const string c_LogTag = "[战前整备] ";
    }
}
