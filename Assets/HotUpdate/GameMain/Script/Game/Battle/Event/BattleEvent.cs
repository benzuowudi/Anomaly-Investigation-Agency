namespace GameMain.Script.Battle
{
    /// <summary>
    /// 战斗系统的事件名（Event 辅助模块）。
    ///
    /// 只放"已经发生变化"的离散通知；位置/血条这类高频数据由 View 每帧向 BattleSystem 拉取，
    /// 不走事件总线（EventManager 是同步遍历，高频事件会拖帧）。
    /// </summary>
    public static class BattleEvent
    {
        /// <summary>战斗阶段变化。参数：BattlePhaseChangedMsg</summary>
        public const string OnBattlePhaseChanged = "OnBattlePhaseChanged";

        /// <summary>单位生成。参数：BattleUnitSpawnedMsg</summary>
        public const string OnUnitSpawned = "OnUnitSpawned";

        /// <summary>单位状态机切换。参数：BattleUnitStateChangedMsg</summary>
        public const string OnUnitStateChanged = "OnUnitStateChanged";

        /// <summary>单位受到一次攻击（伤害已结算并写入）。参数：BattleUnitDamagedMsg</summary>
        public const string OnUnitDamaged = "OnUnitDamaged";

        /// <summary>单位死亡。参数：BattleUnitDiedMsg</summary>
        public const string OnUnitDied = "OnUnitDied";

        /// <summary>战斗结束（结果已产出）。参数：BattleEndedMsg</summary>
        public const string OnBattleEnded = "OnBattleEnded";
    }
}
