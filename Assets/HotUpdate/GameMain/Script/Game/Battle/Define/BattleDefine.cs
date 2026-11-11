namespace GameMain.Script.Battle
{
    /// <summary>
    /// 战斗阶段（全局流程状态，由 BattleFlow 驱动，不使用 GameState）。
    /// </summary>
    public enum BattlePhase
    {
        None = 0,
        /// <summary>加载战场与单位</summary>
        Loading = 1,
        /// <summary>战前准备：站位/物资分配/查看敌人</summary>
        Deploy = 2,
        /// <summary>倒计时与开场演出</summary>
        Countdown = 3,
        /// <summary>自动战斗进行中</summary>
        Fighting = 4,
        /// <summary>暂停：所有计时冻结，不接受指令</summary>
        Paused = 5,
        /// <summary>结算演出</summary>
        Settling = 6,
        /// <summary>已结束，等待退出</summary>
        Finished = 7,
    }

    /// <summary>
    /// 单位行为状态机（每个单位一个，由 UnitBrain 驱动）。
    /// </summary>
    public enum UnitBrainState
    {
        Idle = 0,
        Think = 1,
        Move = 2,
        Attack = 3,
        Reload = 4,
        CastSkill = 5,
        UseItem = 6,
        /// <summary>撤离中：朝最近边界移动，越界即离场</summary>
        Retreat = 7,
        /// <summary>被控制：不能行动，行动条与技能 CD 暂停</summary>
        Controlled = 8,
        /// <summary>重伤：倒计时中，可被药物救回</summary>
        Downed = 9,
        Dead = 10,
        /// <summary>癫狂（SAN 归零）：目标可为任意阵营</summary>
        Berserk = 11,
    }

    /// <summary>
    /// 行动条到点后执行的动作类型（注意：主动技能不在此列，技能由独立 CD 计时器触发）。
    /// </summary>
    public enum UnitActionType
    {
        Attack = 1,
        Move = 2,
        Reload = 3,
        CastSkill = 4,
        UseItem = 5,
        Retreat = 6,
        Standby = 7,
    }

    /// <summary>
    /// 战斗结果。
    /// </summary>
    public enum BattleOutcome
    {
        None = 0,
        Win = 1,
        Lose = 2,
        /// <summary>我方全员撤出边界</summary>
        Retreat = 3,
        /// <summary>玩家主动退出战斗</summary>
        Abort = 4,
    }

    /// <summary>
    /// 战斗单位类型（与 cfg.GameType.Faction 分开：Faction 是阵营，Kind 是来源）。
    /// </summary>
    public enum UnitKind
    {
        Role = 1,
        Enemy = 2,
        Summon = 3,
        Neutral = 4,
    }

    /// <summary>
    /// 战斗指令ID（与 command.xlsx 的 ID 一致）。
    /// </summary>
    public static class BattleCommandId
    {
        /// <summary>集火指定敌人</summary>
        public const int Focus = 80001;
        /// <summary>全体撤离</summary>
        public const int Retreat = 80002;
        /// <summary>自由行动（清除指令）</summary>
        public const int Free = 80003;
    }

    /// <summary>
    /// 指令响应结果（文档 1：指令是模糊的，角色可能不响应）。
    /// 判定顺序：硬性不可响应 → 自身状态优先 → 天赋拒绝 → 基础响应率。
    /// </summary>
    public enum CommandResponse
    {
        /// <summary>响应并执行</summary>
        Accepted = 0,
        /// <summary>硬性不可响应（重伤/死亡/被控制）</summary>
        RejectedDisabled = 1,
        /// <summary>角色自身状态判断优先，忽略指令（如正在撤离）</summary>
        RejectedSelfPriority = 2,
        /// <summary>天赋概率性拒绝</summary>
        RejectedTalent = 3,
        /// <summary>基础响应率未通过</summary>
        RejectedRoll = 4,
    }

    /// <summary>
    /// 战斗常量（后续接入 combatConst.xlsx 后以表为准，这里只是代码兜底默认值）。
    /// </summary>
    public static class BattleConst
    {
        /// <summary>
        /// 我方参战人数上限。必须与战前整备的槽位数（BattleSettingDefine.c_MaxTeamCount = 5）一致，
        /// 否则编队里选的人会在开战那一刻被悄悄截掉。
        /// </summary>
        public const int MaxAllyCount = 5;
        /// <summary>重伤倒计时（秒），只在战斗中走</summary>
        public const float DownedCountdown = 60f;
        /// <summary>基础移速（米/秒）</summary>
        public const float MoveSpeedBase = 2f;
        /// <summary>没有武器时的行动间隔（秒）</summary>
        public const float DefaultActionInterval = 1f;
        /// <summary>没有武器时的攻击距离（米，近战）</summary>
        public const float DefaultAttackRangeMax = 1.5f;
        /// <summary>攻击前摇（秒）</summary>
        public const float AttackWindUp = 0.35f;
        /// <summary>没有战场配置时的默认战场长度（米）</summary>
        public const float DefaultFieldLength = 40f;
        /// <summary>没有装备数据时的武器攻击加成（等价于一把基础武器）</summary>
        public const int DefaultWeaponAtk = 8;
        /// <summary>没有装备数据时的伤害倍率</summary>
        public const float DefaultDamageRate = 1f;
        /// <summary>伤害下限：命中后至少造成 1 点</summary>
        public const int MinDamage = 1;
        /// <summary>触发链最大深度（防递归）</summary>
        public const int MaxTriggerDepth = 8;
        /// <summary>闪避上限（万分比）</summary>
        public const int DodgeCapPerMille = 7500;
    }
}
