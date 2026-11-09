namespace GameMain.Script.Explore
{
    /// <summary>节点状态</summary>
    public enum ExploreNodeState
    {
        /// <summary>不可达（隔层的节点）</summary>
        Locked = 0,
        /// <summary>可从当前节点走到的相邻节点</summary>
        Available = 1,
        /// <summary>当前所在节点</summary>
        Current = 2,
        /// <summary>已探索（可以走回去，但不再触发事件）</summary>
        Explored = 3,
    }

    /// <summary>
    /// 探索系统常量（配表缺失时的兜底默认值；有 exploreZone / zoneNodeWeight 时以表为准）。
    /// </summary>
    public static class ExploreConst
    {
        /// <summary>默认层数</summary>
        public const int DefaultLayerCount = 7;
        /// <summary>每层最少节点</summary>
        public const int DefaultNodesMin = 2;
        /// <summary>每层最多节点</summary>
        public const int DefaultNodesMax = 3;
        /// <summary>每 N 层保底一个露营地</summary>
        public const int DefaultCampInterval = 3;
        /// <summary>每次移动扣除的 SAN</summary>
        public const int DefaultSanCostPerMove = 5;
        /// <summary>节点ID起始值</summary>
        public const int NodeIdBase = 1;

        /// <summary>默认节点权重（按 GameType.NodeType 1~7 下标：battle/rescue/supply/investigate/recover/scene/camp）</summary>
        public static readonly int[] DefaultWeights = { 40, 10, 15, 10, 5, 10, 10 };
    }
}
