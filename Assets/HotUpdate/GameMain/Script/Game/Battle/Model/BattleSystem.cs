using System.Collections.Generic;
using Framework.Script.Model;
using GameMain.Script.Role;
using GameMain.Script.Skill;

namespace GameMain.Script.Battle
{
    /// <summary>
    /// 战斗系统的主模型（M 层，战斗域唯一对外入口）。
    ///
    /// 职责边界：
    /// - 持有本场战斗的运行时状态（单位、阶段、时钟），一场战斗结束即丢弃；
    /// - 通过 BattleData 读写"战斗偏好/最近战报"这类跨战斗持久数据；
    /// - 不引用 Transform / GameObject / FairyGUI，表现层通过 BattleEvent + 每帧拉取 GetUnits() 获得数据；
    /// - 每帧由 BattleOperate.Update() 调用 Tick(dt)，模型自身不继承 MonoBehaviour。
    ///
    /// 当前进度（实现计划 #2）：单位生成/BattleConfig 已接入；ATB/决策/伤害在后续条目补。
    /// </summary>
    public class BattleSystem : ModelBase
    {
        private readonly List<BattleUnit> m_Units = new List<BattleUnit>();
        private readonly BattleConfig m_Config = new BattleConfig();
        private readonly BattleClock m_Clock = new BattleClock();

        private BattleField m_Field;
        private UnitBrain m_Brain;
        private BattleResolver m_Resolver;
        private BattleFlow m_Flow;
        private BattleCommandService m_Command;
        private readonly System.Random m_Random = new System.Random(12345);

        private BattlePhase m_Phase = BattlePhase.None;
        private float m_Elapsed;
        private float m_TimeScale = 1f;
        private int m_NextUnitId = 1;
        private int m_EncounterId;
        private BattleResult m_LastResult;

        private BattleData DB
        {
            get { return Game.Data.GetGameData<BattleData>(); }
        }

        #region 生命周期

        protected internal override void OnStart()
        {
            DB.Load();
            m_TimeScale = DB.GetTimeScale();
            m_Clock.SetScale(m_TimeScale);
            m_Config.Load(Game.Data.GetData);
        }

        protected internal override void OnDestroy()
        {
            ClearBattle();
        }

        #endregion

        #region 查询

        public BattlePhase GetPhase()
        {
            return m_Phase;
        }

        public bool IsFighting()
        {
            return m_Phase == BattlePhase.Fighting;
        }

        public IReadOnlyList<BattleUnit> GetUnits()
        {
            return m_Units;
        }

        public BattleUnit GetUnit(int _unitId)
        {
            for (int i = 0; i < m_Units.Count; i++)
            {
                if (m_Units[i].m_UnitId == _unitId)
                {
                    return m_Units[i];
                }
            }
            return null;
        }

        public BattleResult GetLastResult()
        {
            return m_LastResult;
        }

        public float GetElapsed()
        {
            return m_Elapsed;
        }

        public BattleConfig GetConfig()
        {
            return m_Config;
        }

        public BattleClock GetClock()
        {
            return m_Clock;
        }

        public BattleField GetField()
        {
            return m_Field;
        }

        public int GetEncounterId()
        {
            return m_EncounterId;
        }

        #endregion

        #region 战斗控制

        /// <summary>
        /// 按遭遇开始一场战斗。roleIds 为空时自动取当前角色列表；
        /// 人数上限见 BattleConst.MaxAllyCount（与战前整备的 5 个槽位一致）。
        /// encounter / 敌人编成为空时返回 false，由调用方决定是否走调试路径。
        /// </summary>
        public bool StartBattle(int _encounterId, IReadOnlyList<int> _roleIds = null)
        {
            if (!CanStart())
            {
                return false;
            }

            m_Config.Load(Game.Data.GetData);
            List<RoleSnapshot> kAllies = CollectAllySnapshots(_roleIds, BattleConst.MaxAllyCount);

            List<BattleUnit> kUnits = BattleFactory.BuildFromEncounter(
                Game.Data.GetData, _encounterId, kAllies, ref m_NextUnitId);

            if (kUnits == null || kUnits.Count == 0)
            {
                Game.Log.LogWarning("[战斗] 遭遇无可用单位，转入调试编成：" + _encounterId);
                return false;
            }

            m_EncounterId = _encounterId;
            return BeginBattle(kUnits, ResolveFieldLength(_encounterId));
        }

        /// <summary>
        /// 调试用：不依赖遭遇表，凑 allyCount + enemyCount 个单位开打。
        /// _roleIds 传队伍角色 ID 时优先用真实角色（否则按全部角色取前 N 个），
        /// 这样布阵界面保存的站位（按角色 ID）才能应用上。
        /// </summary>
        public bool StartDebugBattle(int _allyCount = 4, int _enemyCount = 4, IReadOnlyList<int> _roleIds = null)
        {
            if (!CanStart())
            {
                return false;
            }

            m_Config.Load(Game.Data.GetData);
            List<RoleSnapshot> kAllies = CollectAllySnapshots(_roleIds, _allyCount);

            List<BattleUnit> kUnits = BattleFactory.BuildDebug(
                Game.Data.GetData, kAllies, _allyCount, _enemyCount, ref m_NextUnitId);

            m_EncounterId = 0;
            return BeginBattle(kUnits, BattleConst.DefaultFieldLength);
        }

        /// <summary>
        /// 每帧推进。传入真实 dt，内部经 BattleClock 换算（暂停=0，倍速放大）。
        /// </summary>
        public void Tick(float _realDt)
        {
            if (m_Phase != BattlePhase.Fighting)
            {
                return;
            }

            float kDt = m_Clock.Tick(_realDt);
            if (kDt <= 0f)
            {
                return;
            }

            m_Elapsed += kDt;
            TickActionGauge(kDt);
            TickSkills(kDt);
            TickBrains(kDt);
            TickDowned(kDt);
            if (m_Command != null)
            {
                m_Command.Tick(kDt);
            }
            CheckFlow();
        }

        /// <summary>结束条件判定：敌方全灭 Win / 我方全灭 Lose</summary>
        private void CheckFlow()
        {
            if (m_Flow == null || m_Phase != BattlePhase.Fighting)
            {
                return;
            }

            BattleOutcome kOutcome = m_Flow.CheckEnd();
            if (kOutcome != BattleOutcome.None)
            {
                FinishBattle(kOutcome);
            }
        }

        /// <summary>
        /// ATB 行动条：每帧按 (dt / 行动间隔) 累积，封顶 1。
        /// 消耗与"执行行动"由 UnitBrain 负责（射程内才消耗，射程外保持满条蓄力）。
        /// </summary>
        private void TickActionGauge(float _dt)
        {
            float kFillRate = m_Config.AtbFillBase;
            for (int i = 0; i < m_Units.Count; i++)
            {
                BattleUnit kUnit = m_Units[i];
                if (!kUnit.CanAct())
                {
                    continue;
                }

                float kInterval = kUnit.m_ActionInterval <= 0.05f ? 0.05f : kUnit.m_ActionInterval;
                kUnit.m_ActionGauge += _dt / kInterval * kFillRate;
                if (kUnit.m_ActionGauge > 1f)
                {
                    kUnit.m_ActionGauge = 1f;
                }
            }
        }

        /// <summary>
        /// 技能计时与自动施放：交给技能系统（战斗层只管"每帧给它 dt"）。
        /// 放在行动条之后、Brain 之前 —— 技能施放会把行动条清零，
        /// 这样同帧不会出现"技能 + 普攻"二连（技能文档 §1.4）。
        /// </summary>
        private void TickSkills(float _dt)
        {
            if (!Game.Model.Has<SkillSystem>())
            {
                return;
            }
            Game.Model.Get<SkillSystem>().TickBattle(_dt);
        }

        /// <summary>驱动所有单位的状态机</summary>
        private void TickBrains(float _dt)
        {
            if (m_Brain == null)
            {
                return;
            }

            for (int i = 0; i < m_Units.Count; i++)
            {
                m_Brain.Tick(m_Units[i], _dt);
            }
        }

        /// <summary>重伤倒计时：只在战斗中走，归零即死亡</summary>
        private void TickDowned(float _dt)
        {
            for (int i = 0; i < m_Units.Count; i++)
            {
                BattleUnit kUnit = m_Units[i];
                if (kUnit == null || kUnit.m_State != UnitBrainState.Downed)
                {
                    continue;
                }

                kUnit.m_DownedRemain -= _dt;
                if (kUnit.m_DownedRemain <= 0f)
                {
                    kUnit.m_DownedRemain = 0f;
                    KillUnit(kUnit);
                }
            }
        }

        /// <summary>设置倍速（1/2/3），并写入 BattleData</summary>
        public void SetTimeScale(float _scale)
        {
            m_Clock.SetScale(_scale);
            m_TimeScale = m_Clock.GetScale();
            DB.SetTimeScale(m_TimeScale);
            DB.Save();
        }

        public void Pause()
        {
            if (m_Phase == BattlePhase.Fighting)
            {
                m_Clock.Pause();
                SetPhase(BattlePhase.Paused);
            }
        }

        public void Resume()
        {
            if (m_Phase == BattlePhase.Paused)
            {
                m_Clock.Resume();
                SetPhase(BattlePhase.Fighting);
            }
        }

        /// <summary>
        /// 主动退出战斗：按撤退处理，结果由结算层产出。
        /// </summary>
        public void Quit()
        {
            FinishBattle(BattleOutcome.Abort);
        }

        /// <summary>
        /// 结束战斗：产出结果、落最近战报、广播事件、进入 Finished。
        /// 重复调用会被忽略（阶段守卫）。
        /// </summary>
        private void FinishBattle(BattleOutcome _outcome)
        {
            if (m_Phase != BattlePhase.Fighting && m_Phase != BattlePhase.Paused)
            {
                return;
            }

            SetPhase(BattlePhase.Settling);
            m_LastResult = BattleSettlement.Build(m_Field, _outcome, m_Elapsed, m_EncounterId);

            DB.SetLastBattle((int)_outcome, m_Elapsed);
            DB.Save();

            Game.Log.LogInfo("[战斗] 结束：" + m_LastResult);
            Game.Event.SendMessage(BattleEvent.OnBattleEnded, new BattleEndedMsg(m_LastResult));
            SetPhase(BattlePhase.Finished);
        }

        public bool IsFinished()
        {
            return m_Phase == BattlePhase.Finished;
        }

        #region 指令

        /// <summary>
        /// 下达指令（仅战斗进行中可用）。paramUnitId：集火时是目标敌人ID，其它传 0。
        /// 返回每个接收者的响应结果；无法下达时返回 null。
        /// </summary>
        public BattleCommandResult IssueCommand(int _commandId, int _paramUnitId = 0)
        {
            if (m_Phase != BattlePhase.Fighting)
            {
                Game.Log.LogWarning("[战斗] 当前阶段不能下达指令：" + m_Phase);
                return null;
            }
            if (m_Command == null)
            {
                return null;
            }
            return m_Command.Issue(_commandId, _paramUnitId);
        }

        /// <summary>把天赋拒绝率接进指令系统（技能系统接入后调用）</summary>
        public void SetTalentRefuseProvider(System.Func<BattleUnit, int, float> _provider)
        {
            if (m_Command != null)
            {
                m_Command.SetTalentRefuseProvider(_provider);
            }
        }

        #endregion

        #region 效果落地（技能系统调这些）

        /// <summary>
        /// 效果伤害：不过闪避判定（技能命中是必然的），但仍按防御减伤、
        /// 走同一个重伤/死亡/事件流程，保证血条与结算一致。
        /// </summary>
        public int ApplyEffectDamage(BattleUnit _source, BattleUnit _target, int _value, bool _ignoreDef)
        {
            if (_target == null || !_target.IsAlive() || _value <= 0)
            {
                return 0;
            }

            int kDamage = _ignoreDef ? _value : _value - _target.m_Defense;
            if (kDamage < BattleConst.MinDamage)
            {
                kDamage = BattleConst.MinDamage;
            }

            int kHpAfter = _target.m_Hp - kDamage;
            if (kHpAfter < 0)
            {
                kHpAfter = 0;
            }
            _target.m_Hp = kHpAfter;

            bool kDowned = kHpAfter <= 0;
            if (kDowned)
            {
                EnterDowned(_target);
            }

            int kSourceId = _source == null ? 0 : _source.m_UnitId;
            Game.Event.SendMessage(BattleEvent.OnUnitDamaged,
                new BattleUnitDamagedMsg(kSourceId, _target.m_UnitId, kDamage, true, kHpAfter, kDowned));

            Game.Log.LogInfo("[战斗] 效果伤害 " + kSourceId + " -> " + _target.m_UnitId
                + " " + kDamage + "，剩余 " + kHpAfter);
            return kDamage;
        }

        /// <summary>效果治疗：超过上限按上限算，返回实际治疗量</summary>
        public int ApplyEffectHeal(BattleUnit _target, int _value)
        {
            if (_target == null || !_target.IsAlive() || _value <= 0)
            {
                return 0;
            }

            int kBefore = _target.m_Hp;
            int kAfter = kBefore + _value;
            if (kAfter > _target.m_MaxHp)
            {
                kAfter = _target.m_MaxHp;
            }
            _target.m_Hp = kAfter;

            int kHeal = kAfter - kBefore;
            if (kHeal > 0)
            {
                Game.Log.LogInfo("[战斗] 效果治疗 " + _target.m_UnitId + " +" + kHeal + "，剩余 " + kAfter);
            }
            return kHeal;
        }

        #endregion

        /// <summary>结束并清场，下次 StartBattle 从干净状态开始</summary>
        public void ClearBattle()
        {
            m_Units.Clear();
            m_Elapsed = 0f;
            m_NextUnitId = 1;
            m_EncounterId = 0;
            m_Field = null;
            m_Brain = null;
            m_Flow = null;
            m_Command = null;
            m_Clock.Reset();
            m_Phase = BattlePhase.None;
        }

        #endregion

        #region 内部

        private bool CanStart()
        {
            if (m_Phase != BattlePhase.None && m_Phase != BattlePhase.Finished)
            {
                Game.Log.LogWarning("[战斗] 已有战斗在进行，忽略本次开始：" + m_Phase);
                return false;
            }
            return true;
        }

        private bool BeginBattle(List<BattleUnit> _units, float _fieldLength)
        {
            if (_units == null || _units.Count == 0)
            {
                Game.Log.LogError("[战斗] 没有生成任何单位，战斗无法开始");
                return false;
            }

            m_Units.Clear();
            m_Units.AddRange(_units);
            m_Elapsed = 0f;
            m_LastResult = null;
            m_TimeScale = DB.GetTimeScale();
            m_Clock.Reset();
            m_Clock.SetScale(m_TimeScale);

            m_Field = new BattleField(m_Units, _fieldLength);
            m_Resolver = new BattleResolver(m_Config, m_Random);
            m_Flow = new BattleFlow(m_Field);
            m_Command = new BattleCommandService(m_Field, m_Config, m_Random);
            m_Brain = new UnitBrain(m_Field, m_Config, OnUnitStateChanged, OnAttackResolved);

            SetPhase(BattlePhase.Loading);
            SetPhase(BattlePhase.Countdown);

            for (int i = 0; i < m_Units.Count; i++)
            {
                Game.Event.SendMessage(BattleEvent.OnUnitSpawned, new BattleUnitSpawnedMsg(m_Units[i]));
            }

            SetPhase(BattlePhase.Fighting);
            Game.Log.LogInfo("[战斗] 开始，单位数：" + m_Units.Count
                + " 战场长度：" + m_Field.Length.ToString("F1"));
            DumpUnits();
            return true;
        }

        /// <summary>
        /// 由 encounter → battlefield 解析战场长度；缺表时用默认值。
        /// </summary>
        private float ResolveFieldLength(int _encounterId)
        {
            cfg.Tables kTables = Game.Data.GetData;
            if (kTables == null || kTables.encounter == null || kTables.battlefield == null)
            {
                return BattleConst.DefaultFieldLength;
            }

            cfg.GameData_Battle.EncounterData kEncounter = kTables.encounter.GetOrDefault(_encounterId);
            if (kEncounter == null)
            {
                return BattleConst.DefaultFieldLength;
            }

            cfg.GameData_Battle.BattlefieldData kField = kTables.battlefield.GetOrDefault(kEncounter.BattlefieldID);
            if (kField == null || kField.Length <= 0f)
            {
                return BattleConst.DefaultFieldLength;
            }
            return kField.Length;
        }

        private void OnUnitStateChanged(BattleUnit _unit, UnitBrainState _oldState, UnitBrainState _newState)
        {
            Game.Event.SendMessage(BattleEvent.OnUnitStateChanged,
                new BattleUnitStateChangedMsg(_unit.m_UnitId, _oldState, _newState));
        }

        /// <summary>攻击前摇结束：结算伤害、写血、广播</summary>
        private void OnAttackResolved(BattleUnit _attacker, int _targetUnitId)
        {
            BattleUnit kTarget = GetUnit(_targetUnitId);
            if (kTarget == null || !kTarget.IsAlive())
            {
                return;
            }

            BattleDamage kDamage = m_Resolver.ResolveAttack(_attacker, kTarget);
            if (kDamage.m_IsHit)
            {
                kTarget.m_Hp = kDamage.m_TargetHpAfter;
                if (kDamage.m_IsDowned)
                {
                    EnterDowned(kTarget);
                }
            }

            Game.Event.SendMessage(BattleEvent.OnUnitDamaged,
                new BattleUnitDamagedMsg(_attacker.m_UnitId, kTarget.m_UnitId, kDamage.m_Damage,
                    kDamage.m_IsHit, kDamage.m_TargetHpAfter, kDamage.m_IsDowned));

            Game.Log.LogInfo("[战斗] " + _attacker.m_UnitId + " -> " + kTarget.m_UnitId + " "
                + (kDamage.m_IsHit
                    ? ("伤害 " + kDamage.m_Damage + "，剩余 " + kDamage.m_TargetHpAfter)
                    : "被闪避"));
        }

        /// <summary>血量归零 → 重伤（不是死亡），开始倒计时</summary>
        private void EnterDowned(BattleUnit _unit)
        {
            if (_unit == null || _unit.m_State == UnitBrainState.Downed || _unit.m_State == UnitBrainState.Dead)
            {
                return;
            }

            _unit.m_DownedRemain = m_Config.DownedCountdown;
            _unit.m_ActionGauge = 0f;
            _unit.m_StateTimer = 0f;
            _unit.m_TargetUnitId = 0;
            SetUnitState(_unit, UnitBrainState.Downed);

            Game.Log.LogInfo("[战斗] 重伤：" + _unit.m_UnitId
                + " 倒计时 " + _unit.m_DownedRemain.ToString("F0") + " 秒");
        }

        private void KillUnit(BattleUnit _unit)
        {
            if (_unit == null || _unit.m_State == UnitBrainState.Dead)
            {
                return;
            }

            SetUnitState(_unit, UnitBrainState.Dead);
            Game.Event.SendMessage(BattleEvent.OnUnitDied,
                new BattleUnitDiedMsg(_unit.m_UnitId, _unit.m_SourceId));
            Game.Log.LogInfo("[战斗] 死亡：" + _unit.m_UnitId);
        }

        private void SetUnitState(BattleUnit _unit, UnitBrainState _state)
        {
            if (_unit.m_State == _state)
            {
                return;
            }

            UnitBrainState kOld = _unit.m_State;
            _unit.m_State = _state;
            Game.Event.SendMessage(BattleEvent.OnUnitStateChanged,
                new BattleUnitStateChangedMsg(_unit.m_UnitId, kOld, _state));
        }

        /// <summary>取参战角色的快照；roleIds 为空时取全部角色（最多 _max 个）</summary>
        private List<RoleSnapshot> CollectAllySnapshots(IReadOnlyList<int> _roleIds, int _max)
        {
            List<RoleSnapshot> kResult = new List<RoleSnapshot>();
            RoleSystem kRoleSystem = Game.Model.Get<RoleSystem>();
            if (kRoleSystem == null)
            {
                return kResult;
            }

            if (_roleIds != null && _roleIds.Count > 0)
            {
                for (int i = 0; i < _roleIds.Count; i++)
                {
                    if (_max > 0 && kResult.Count >= _max)
                    {
                        break;
                    }
                    RoleRecord kRole = kRoleSystem.GetRole(_roleIds[i]);
                    RoleSnapshot kSnapshot = RoleSystem.BuildSnapshot(kRole);
                    if (kSnapshot != null)
                    {
                        kResult.Add(kSnapshot);
                    }
                }
                return kResult;
            }

            List<RoleRecord> kRoles = kRoleSystem.GetAllRoles();
            for (int i = 0; i < kRoles.Count; i++)
            {
                if (_max > 0 && kResult.Count >= _max)
                {
                    break;
                }
                RoleSnapshot kSnapshot = RoleSystem.BuildSnapshot(kRoles[i]);
                if (kSnapshot != null)
                {
                    kResult.Add(kSnapshot);
                }
            }
            return kResult;
        }

        /// <summary>把本场单位打到日志（调试验收用）</summary>
        public void DumpUnits()
        {
            for (int i = 0; i < m_Units.Count; i++)
            {
                Game.Log.LogInfo("[战斗] " + m_Units[i]);
            }
        }

        private void SetPhase(BattlePhase _phase)
        {
            if (m_Phase == _phase)
            {
                return;
            }

            BattlePhase kOld = m_Phase;
            m_Phase = _phase;
            Game.Event.SendMessage(BattleEvent.OnBattlePhaseChanged, new BattlePhaseChangedMsg(kOld, _phase));
        }

        #endregion
    }
}
