namespace GameMain.Script.Role
{
    /// <summary>
    /// 角色系统的事件名（Event 辅助模块）。
    ///
    /// EventManager 以字符串作为契约，本系统所有事件主题统一收敛在这里，
    /// 业务代码只允许使用常量，禁止直接写字符串字面量。
    /// 事件名必须全局唯一，命名规则：On + 系统 + 动作/对象 + 变化。
    /// </summary>
    public static class RoleEvent
    {
        /// <summary>
        /// 获得了一个新角色（权威数据已写入、存档已完成）。参数：RoleAcquiredMsg。
        /// </summary>
        public const string OnRoleAcquired = "OnRoleAcquired";

        /// <summary>
        /// 单个角色发生变化（属性/等级/天赋/改名/职称/装备/勋章/技能）。参数：RoleChangedMsg。
        /// </summary>
        public const string OnRoleChanged = "OnRoleChanged";

        /// <summary>
        /// 角色列表整体变化（新增/移除/读档），参数：RolesChangedMsg，UI 可用版本号判断是否重绘。
        /// </summary>
        public const string OnRolesChanged = "OnRolesChanged";

        /// <summary>
        /// 当前出战（主）角色变化。参数：MainRoleChangedMsg。
        /// </summary>
        public const string OnMainRoleChanged = "OnMainRoleChanged";
    }
}
