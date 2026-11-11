namespace GameMain.Script.Battle
{
    /// <summary>
    /// 战斗表现的"抢占"开关。
    ///
    /// 当前有两种战斗画面：
    /// - 正式表现：NodeSettingView 里的布阵界面切到战斗模式（FairyGUI + RoleAnimCom）；
    /// - 调试表现：BattleDebugView（编辑器专用 IMGUI 覆盖层）。
    ///
    /// FGUI 战斗画面激活时把 s_FguiActive 置 true，IMGUI 层只负责 Tick、不再绘制，
    /// 避免两套画面叠在一起。
    /// </summary>
    public static class BattlePresentation
    {
        /// <summary>FGUI 战斗画面是否正在显示</summary>
        public static bool s_FguiActive;
    }
}
