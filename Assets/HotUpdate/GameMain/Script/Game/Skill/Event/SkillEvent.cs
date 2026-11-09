namespace GameMain.Script.Skill
{
    /// <summary>
    /// 技能系统的事件名（Event 辅助模块）。
    ///
    /// 技能是"战斗中每帧高频发生"的东西（CD 每帧减），所以事件只发离散变化：
    /// 施放成功 / 被拒绝 / 效果挂载 / 效果移除 / 状态触发。
    /// CD 剩余、效果层数这类连续值由 View 每帧向 SkillSystem 拉取。
    /// </summary>
    public static class SkillEvent
    {
        /// <summary>技能施放成功。参数：SkillCastMsg</summary>
        public const string OnSkillCast = "OnSkillCast";

        /// <summary>技能施放被拒绝（无目标/次数用完/受控等）。参数：SkillDeniedMsg</summary>
        public const string OnSkillDenied = "OnSkillDenied";

        /// <summary>效果挂载到单位。参数：EffectAppliedMsg</summary>
        public const string OnEffectApplied = "OnEffectApplied";

        /// <summary>效果移除（到期/被消耗/被驱散）。参数：EffectRemovedMsg</summary>
        public const string OnEffectRemoved = "OnEffectRemoved";

        /// <summary>技能状态被条件命中并执行。参数：EffectTriggeredMsg</summary>
        public const string OnEffectTriggered = "OnEffectTriggered";
    }
}
