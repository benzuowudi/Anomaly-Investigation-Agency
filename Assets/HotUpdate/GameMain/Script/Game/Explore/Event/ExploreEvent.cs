namespace GameMain.Script.Explore
{
    /// <summary>
    /// 探索系统的事件名（Event 辅助模块）。
    /// </summary>
    public static class ExploreEvent
    {
        /// <summary>一次调查开始。参数：ExploreRunStartedMsg</summary>
        public const string OnRunStarted = "OnRunStarted";

        /// <summary>进入了某个节点（状态已更新）。参数：ExploreNodeEnteredMsg</summary>
        public const string OnNodeEntered = "OnNodeEntered";

        /// <summary>进入了一个战斗节点，等界面开战前整备。参数：ExploreBattleNodeMsg</summary>
        public const string OnBattleNodeEntered = "OnBattleNodeEntered";

        /// <summary>进入了一个非战斗节点（事件内容后续接）。参数：ExploreNodeEventMsg</summary>
        public const string OnNodeEventTriggered = "OnNodeEventTriggered";

        /// <summary>调查结束（完成或撤离）。参数：ExploreRunEndedMsg</summary>
        public const string OnRunEnded = "OnRunEnded";

        /// <summary>节点网/进度发生任何变化（UI 可据此重绘）。参数：ExploreRunChangedMsg</summary>
        public const string OnRunChanged = "OnRunChanged";

        /// <summary>深入后地图追加了新层。参数：ExploreRunChangedMsg</summary>
        public const string OnRunMapExtended = "OnRunMapExtended";
    }
}
