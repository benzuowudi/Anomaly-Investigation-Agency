namespace GameMain.Script.BattleSetting
{
    /// <summary>
    /// 战前整备系统的事件名（Event 辅助模块）。
    ///
    /// 只广播"已经发生变化"的通知；配置的读接口由 BattleSettingSystem 提供。
    /// </summary>
    public static class BattleSettingEvent
    {
        /// <summary>战前配置发生变化（编队/站位/物资/遭遇战）。参数：BattleSettingChangedMsg</summary>
        public const string OnBattleSettingChanged = "OnBattleSettingChanged";

        /// <summary>确认开战（校验通过、BattleSystem 已开始）。参数：BattleSettingCommittedMsg</summary>
        public const string OnBattleSettingCommitted = "OnBattleSettingCommitted";
    }
}
