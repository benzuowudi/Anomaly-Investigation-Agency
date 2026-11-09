namespace GameMain.Script.Skill
{
    /// <summary>
    /// 效果实例的来源。用于"按来源清理"（技能到期 / 天赋失效 / 装备卸下）。
    ///
    /// 只放代码内部用的枚举；策划要填的（EffectType / StateTriggerCondition / ConditionLogic 等）
    /// 都在 __enums__.xlsx 里，生成到 cfg.GameType。
    /// </summary>
    public enum EffectSource
    {
        None = 0,
        /// <summary>玩家主动技能（skill.xlsx）</summary>
        Skill = 1,
        /// <summary>天赋/被动（talent.xlsx）</summary>
        Talent = 2,
        /// <summary>装备特效（equip / medal）</summary>
        Equip = 3,
        /// <summary>道具/补给品</summary>
        Item = 4,
        /// <summary>buff 内部展开的效果</summary>
        Buff = 5,
        /// <summary>敌人技能（skill_enemy.xlsx）</summary>
        EnemySkill = 6,
    }

    /// <summary>
    /// 效果实例的作用域：决定什么时候批量清理。
    /// </summary>
    public enum EffectScopeType
    {
        None = 0,
        /// <summary>战斗域：战斗结束即清</summary>
        Battle = 1,
        /// <summary>探索域：离开区域清（20006/20009 这类）</summary>
        Explore = 2,
        /// <summary>持久：跨战斗保留（走存档，本期不落盘）</summary>
        Persistent = 3,
    }

    /// <summary>
    /// 技能"不能放"的原因。只用于调试日志与 UI 提示，
    /// 规则是"能不能放"而不是"要不要放"（技能没有决策，见技能文档 §2.1）。
    /// </summary>
    public enum SkillDenyReason
    {
        None = 0,
        /// <summary>没有配置技能</summary>
        NoSkill = 1,
        /// <summary>已死亡</summary>
        Dead = 2,
        /// <summary>重伤倒计时中</summary>
        Downed = 3,
        /// <summary>被控制（无法行动）</summary>
        Controlled = 4,
        /// <summary>需要目标但选不出合法目标</summary>
        NoTarget = 5,
        /// <summary>本场次数用完</summary>
        UseLimit = 6,
        /// <summary>资源不足（补给品/弹药）</summary>
        NoResource = 7,
        /// <summary>技能没有任何效果配置（表没填），按"冷却照走但不施放"处理，避免每帧刷</summary>
        NoEffect = 8,
        /// <summary>不在战斗中</summary>
        NotInBattle = 9,
    }

    /// <summary>一次施放的结果</summary>
    public enum SkillCastResult
    {
        Failed = 0,
        Cast = 1,
    }

    /// <summary>
    /// 技能域的代码内常量。配置能表达的不要放这里。
    /// </summary>
    public static class SkillConst
    {
        /// <summary>事件分发最大递归深度（防"状态触发状态"无限套娃）</summary>
        public const int MaxTriggerDepth = 8;

        /// <summary>冷却下限，避免 cd=0 的技能每帧施放</summary>
        public const float MinCd = 0.1f;

        /// <summary>补给品ID（supplies 表未落地前的临时代码值，落地后改成表ID）</summary>
        public static class SupplyId
        {
            /// <summary>医疗补给品</summary>
            public const int Medical = 1;
            /// <summary>弹匣补给品</summary>
            public const int Magazine = 2;
        }
    }
}
