using System.Collections.Generic;
using cfg.GameType;
using Framework.Script.Model;
using GameMain.Script.Battle;
using UnityEngine;

namespace GameMain.Script.Skill
{
    /// <summary>
    /// 技能系统的主模型（M 层，技能域唯一对外入口）。
    ///
    /// 职责边界（技能文档 §9.3）：
    /// - 只管"技能/效果/状态的规则"，不持有战斗单位，不写 BattleData；
    /// - 冷却计时由战斗层每帧调 TickBattle(dt) 驱动（计时属于战斗状态，暂停时 dt=0）；
    /// - 伤害/治疗/位移通过 BattleSystem 的公开方法落地，技能层不自己改血量；
    /// - 不引用 Transform / GameObject / FairyGUI，表现层靠 SkillEvent + 每帧拉取。
    ///
    /// 本期实现：主动技能 CD 到点施放、效果实例生命周期、技能状态的条件触发与消耗。
    /// 被动/天赋（TalentService、TriggerBus、免疫/时长改写）留到下一项。
    /// </summary>
    public class SkillSystem : ModelBase
    {
        private readonly EffectService m_Effects;
        private readonly HashSet<int> m_WarnedNoEffect = new HashSet<int>();
        private readonly Dictionary<int, SkillDenyReason> m_LastDenyReason = new Dictionary<int, SkillDenyReason>();

        private EffectResolver m_Resolver;
        private SkillTargetSelector m_Selector;
        private SkillTriggerChecker m_Checker;
        private SkillCastFlow m_Flow;

        private int m_TriggerDepth;

        public SkillSystem()
        {
            m_Effects = new EffectService(this);
        }

        private SkillData DB
        {
            get { return Game.Data.GetGameData<SkillData>(); }
        }

        #region 对外访问

        /// <summary>效果实例容器（战斗/探索/表现层都从这里读）</summary>
        public EffectService Effects
        {
            get { return m_Effects; }
        }

        /// <summary>效果执行器（EffectService 的周期结算与状态 payload 会回调它）</summary>
        public EffectResolver Resolver
        {
            get { return m_Resolver; }
        }

        public SkillTargetSelector Selector
        {
            get { return m_Selector; }
        }

        #endregion

        #region 生命周期

        protected internal override void OnInit()
        {
            m_Resolver = new EffectResolver(this);
            m_Selector = new SkillTargetSelector(this);
            m_Checker = new SkillTriggerChecker(m_Selector);
            m_Flow = new SkillCastFlow(this, m_Selector);

            Game.Event.Subscribe(BattleEvent.OnUnitDamaged, OnUnitDamaged);
            Game.Event.Subscribe(BattleEvent.OnUnitDied, OnUnitDied);
            Game.Event.Subscribe(BattleEvent.OnBattleEnded, OnBattleEnded);
        }

        protected internal override void OnStart()
        {
            DB.Load();

            cfg.Tables kTables = Game.Data.GetData;
            int kSkillCount = kTables == null || kTables.skill == null ? 0 : kTables.skill.DataList.Count;
            int kEffectCount = kTables == null || kTables.effect == null ? 0 : kTables.effect.DataList.Count;
            Game.Log.LogInfo("[技能] 启动：skill " + kSkillCount + " 行，effect " + kEffectCount + " 行");
        }

        protected internal override void OnDestroy()
        {
            Game.Event.UnSubscribe(BattleEvent.OnUnitDamaged, OnUnitDamaged);
            Game.Event.UnSubscribe(BattleEvent.OnUnitDied, OnUnitDied);
            Game.Event.UnSubscribe(BattleEvent.OnBattleEnded, OnBattleEnded);

            m_Effects.ClearAll();
        }

        #endregion

        #region 每帧驱动（由 BattleSystem.Tick 调用）

        /// <summary>
        /// 战斗层每帧调用：先推进效果时长，再推进每个单位的技能冷却，到点即施放。
        /// 传入的是战斗时钟换算后的 dt（暂停=0、倍速放大）。
        /// </summary>
        public void TickBattle(float _dt)
        {
            BattleSystem kBattle = GetBattle();
            if (kBattle == null || kBattle.GetPhase() != BattlePhase.Fighting)
            {
                return;
            }

            m_Effects.Tick(_dt);

            IReadOnlyList<BattleUnit> kUnits = kBattle.GetUnits();
            for (int i = 0; i < kUnits.Count; i++)
            {
                TickUnitSkill(kUnits[i], _dt);
            }
        }

        /// <summary>单个单位的冷却推进 + 到点施放</summary>
        private void TickUnitSkill(BattleUnit _unit, float _dt)
        {
            if (_unit == null || _unit.m_ActiveSkillId <= 0)
            {
                return;
            }

            // 死亡/重伤/受控：CD 不递减（与行动条一致）
            if (!_unit.CanAct())
            {
                return;
            }

            float kCd = GetSkillCd(_unit);
            if (kCd > 0f)
            {
                kCd -= _dt;
                _unit.m_SkillCd[_unit.m_ActiveSkillId] = kCd > 0f ? kCd : 0f;
                if (kCd > 0f)
                {
                    return;
                }
            }

            SkillDenyReason kReason;
            cfg.GameData_Skill.SkillData kSkill;
            if (!m_Checker.Check(_unit, out kReason, out kSkill))
            {
                HandleDenied(_unit, kSkill, kReason);
                return;
            }

            m_LastDenyReason.Remove(_unit.m_UnitId);
            m_Flow.Cast(_unit);
        }

        /// <summary>
        /// 不能放时的处理：
        /// - 表没填效果 → CD 照走（否则每帧重试并刷日志）；
        /// - 其它原因（没目标/受控）→ CD 保持 0，下一帧重试，日志按"原因变化"限流。
        /// </summary>
        private void HandleDenied(BattleUnit _unit, cfg.GameData_Skill.SkillData _skill, SkillDenyReason _reason)
        {
            int kSkillId = _skill == null ? _unit.m_ActiveSkillId : _skill.ID;

            if (_reason == SkillDenyReason.NoEffect)
            {
                if (m_WarnedNoEffect.Add(kSkillId))
                {
                    Game.Log.LogWarning("[技能] 技能 " + kSkillId + " 的 effect 列为空，按空冷却处理（配置补齐后自动生效）");
                }
                ResetSkillCooldown(_unit, _skill, true);
                return;
            }

            SkillDenyReason kLast;
            if (m_LastDenyReason.TryGetValue(_unit.m_UnitId, out kLast) && kLast == _reason)
            {
                return;
            }

            m_LastDenyReason[_unit.m_UnitId] = _reason;
            Game.Log.LogInfo("[技能] " + _unit.m_UnitId + " 技能" + kSkillId
                + " 暂不施放：" + DescribeReason(_reason));
        }

        private static string DescribeReason(SkillDenyReason _reason)
        {
            switch (_reason)
            {
                case SkillDenyReason.NoSkill: return "没有配置技能";
                case SkillDenyReason.Dead: return "已死亡";
                case SkillDenyReason.Downed: return "重伤中";
                case SkillDenyReason.Controlled: return "被控制";
                case SkillDenyReason.NoTarget: return "选不出合法目标";
                case SkillDenyReason.UseLimit: return "本场次数用完";
                case SkillDenyReason.NoResource: return "资源不足";
                case SkillDenyReason.NoEffect: return "效果未配置";
                case SkillDenyReason.NotInBattle: return "不在战斗中";
                default: return _reason.ToString();
            }
        }

        #endregion

        #region 冷却

        /// <summary>剩余冷却（秒，0 = 就绪）</summary>
        public float GetSkillCd(BattleUnit _unit)
        {
            if (_unit == null || _unit.m_ActiveSkillId <= 0)
            {
                return 0f;
            }

            float kCd;
            return _unit.m_SkillCd.TryGetValue(_unit.m_ActiveSkillId, out kCd) ? kCd : 0f;
        }

        /// <summary>重置冷却为技能 cd（施放瞬间调用）</summary>
        public void ResetSkillCooldown(BattleUnit _unit, cfg.GameData_Skill.SkillData _skill,
            bool _evenIfNoEffects = false)
        {
            if (_unit == null || _unit.m_ActiveSkillId <= 0)
            {
                return;
            }

            if (_skill == null)
            {
                _skill = SkillRule.GetSkill(_unit.m_ActiveSkillId);
            }

            _unit.m_SkillCd[_unit.m_ActiveSkillId] = SkillRule.GetCd(_skill);
        }

        /// <summary>调试用：直接把冷却清零</summary>
        public void ClearSkillCooldown(BattleUnit _unit)
        {
            if (_unit == null || _unit.m_ActiveSkillId <= 0)
            {
                return;
            }
            _unit.m_SkillCd[_unit.m_ActiveSkillId] = 0f;
        }

        #endregion

        #region 战斗接口（EffectResolver 调这些落地）

        /// <summary>效果伤害（不过闪避判定，走防御/重伤/事件流程）</summary>
        public int ApplyDamage(BattleUnit _source, BattleUnit _target, int _value, bool _ignoreDef)
        {
            BattleSystem kBattle = GetBattle();
            if (kBattle == null)
            {
                return 0;
            }
            return kBattle.ApplyEffectDamage(_source, _target, _value, _ignoreDef);
        }

        /// <summary>效果治疗（超过上限按上限算），并触发"治疗队友"类技能状态</summary>
        public int ApplyHeal(BattleUnit _source, BattleUnit _target, int _value)
        {
            BattleSystem kBattle = GetBattle();
            if (kBattle == null)
            {
                return 0;
            }

            int kHeal = kBattle.ApplyEffectHeal(_target, _value);
            if (kHeal > 0)
            {
                NotifyTrigger(_source, StateTriggerCondition.onHealAlly, _target, kHeal);
            }
            return kHeal;
        }

        /// <summary>一维位移（正 = 朝敌/右，负 = 朝己/左），出界夹取</summary>
        public bool MoveBy(BattleUnit _unit, float _distance)
        {
            BattleField kField = GetField();
            if (kField == null || _unit == null)
            {
                return false;
            }

            float kOld = _unit.m_Pos;
            _unit.m_Pos = kField.ClampPosition(_unit.m_Pos + _distance);
            float kMoved = _unit.m_Pos - kOld;
            if (Mathf.Abs(kMoved) < 0.001f)
            {
                return false;
            }

            Game.Log.LogInfo("[技能] 位移 " + _unit.m_UnitId + " " + kOld.ToString("F2")
                + " → " + _unit.m_Pos.ToString("F2"));
            return true;
        }

        /// <summary>靠近最近的敌人（30001 的第一段）</summary>
        public bool MoveToNearestEnemy(BattleUnit _unit)
        {
            BattleField kField = GetField();
            if (kField == null || _unit == null)
            {
                return false;
            }

            BattleUnit kEnemy = kField.FindNearestEnemy(_unit);
            if (kEnemy == null)
            {
                return false;
            }

            float kDirection = BattleField.Direction(_unit, kEnemy);
            float kRange = _unit.m_AttackRangeMax > 0f ? _unit.m_AttackRangeMax : 1.5f;
            float kDest = kField.ClampPosition(kEnemy.m_Pos - kDirection * kRange * 0.9f);

            float kOld = _unit.m_Pos;
            _unit.m_Pos = kDest;
            Game.Log.LogInfo("[技能] 突进 " + _unit.m_UnitId + " " + kOld.ToString("F2")
                + " → " + kDest.ToString("F2") + "（贴近 " + kEnemy.m_UnitId + "）");
            return true;
        }

        /// <summary>穿到敌方最后排的外侧（30002 的第二段）</summary>
        public bool MoveBehindEnemyBackRow(BattleUnit _unit)
        {
            BattleField kField = GetField();
            if (kField == null || _unit == null)
            {
                return false;
            }

            BattleSystem kBattle = GetBattle();
            if (kBattle == null)
            {
                return false;
            }

            // "最后排"= 离己方最远的那个敌人
            BattleUnit kFarthest = null;
            float kBestDistance = -1f;
            IReadOnlyList<BattleUnit> kUnits = kBattle.GetUnits();
            for (int i = 0; i < kUnits.Count; i++)
            {
                BattleUnit kOther = kUnits[i];
                if (!BattleField.IsActive(kOther) || !BattleField.IsHostile(_unit.m_Faction, kOther.m_Faction))
                {
                    continue;
                }

                float kDistance = BattleField.Distance(_unit, kOther);
                if (kDistance > kBestDistance)
                {
                    kBestDistance = kDistance;
                    kFarthest = kOther;
                }
            }

            if (kFarthest == null)
            {
                return false;
            }

            float kDirection = BattleField.Direction(_unit, kFarthest);
            float kDest = kField.ClampPosition(kFarthest.m_Pos + kDirection * 1f);
            float kOld = _unit.m_Pos;
            _unit.m_Pos = kDest;
            _unit.m_TargetUnitId = kFarthest.m_UnitId;
            Game.Log.LogInfo("[技能] 穿阵 " + _unit.m_UnitId + " " + kOld.ToString("F2")
                + " → " + kDest.ToString("F2") + "（锁定 " + kFarthest.m_UnitId + "）");
            return true;
        }

        #endregion

        #region 触发分发

        /// <summary>
        /// 让某单位身上的技能状态检查是否命中。带递归深度保护（状态触发状态不能无限套）。
        /// </summary>
        public void NotifyTrigger(BattleUnit _owner, StateTriggerCondition _condition, BattleUnit _subject,
            int _param = 0)
        {
            if (_owner == null)
            {
                return;
            }

            if (m_TriggerDepth >= SkillConst.MaxTriggerDepth)
            {
                Game.Log.LogWarning("[技能] 触发递归超过上限，已丢弃：" + _condition + " owner:" + _owner.m_UnitId);
                return;
            }

            m_TriggerDepth++;
            try
            {
                m_Effects.NotifyTrigger(_owner.m_UnitId, _condition, _subject, _param);
            }
            finally
            {
                m_TriggerDepth--;
            }
        }

        /// <summary>受伤：攻击者触发"造成伤害/击杀"，目标触发"受到伤害"</summary>
        private void OnUnitDamaged(object _msg)
        {
            BattleUnitDamagedMsg kMsg = _msg as BattleUnitDamagedMsg;
            if (kMsg == null || !kMsg.m_IsHit || kMsg.m_Damage <= 0)
            {
                return;
            }

            BattleUnit kAttacker = GetUnit(kMsg.m_AttackerId);
            BattleUnit kTarget = GetUnit(kMsg.m_TargetId);

            if (kAttacker != null)
            {
                NotifyTrigger(kAttacker, StateTriggerCondition.onDealDamage, kTarget, kMsg.m_Damage);
            }
            if (kTarget != null)
            {
                NotifyTrigger(kTarget, StateTriggerCondition.onDamaged, kAttacker, kMsg.m_Damage);
            }
            if (kMsg.m_IsDowned && kAttacker != null)
            {
                NotifyTrigger(kAttacker, StateTriggerCondition.onKill, kTarget, 0);
            }
        }

        /// <summary>死亡：清掉该单位身上的效果</summary>
        private void OnUnitDied(object _msg)
        {
            BattleUnitDiedMsg kMsg = _msg as BattleUnitDiedMsg;
            if (kMsg == null)
            {
                return;
            }
            m_Effects.ClearUnit(kMsg.m_UnitId);
        }

        /// <summary>战斗结束：整场效果清空（CD 不跨战斗，下一场重新归零）</summary>
        private void OnBattleEnded(object _msg)
        {
            m_Effects.ClearAll();
            m_WarnedNoEffect.Clear();
            m_LastDenyReason.Clear();
            m_TriggerDepth = 0;
        }

        #endregion

        #region 查询

        public BattleSystem GetBattle()
        {
            return SkillRule.GetBattle();
        }

        public BattleField GetField()
        {
            BattleSystem kBattle = GetBattle();
            return kBattle == null ? null : kBattle.GetField();
        }

        public BattleUnit GetUnit(int _unitId)
        {
            return SkillRule.GetUnit(_unitId);
        }

        /// <summary>某单位的效果列表（只读）</summary>
        public IReadOnlyList<EffectInstance> GetEffects(int _unitId)
        {
            return m_Effects.GetByUnit(_unitId);
        }

        #endregion

        #region 调试支持

        /// <summary>调试用：把某阵营所有单位的技能换成指定技能（验证指定技能的表现）</summary>
        public int DebugAssignSkill(int _skillId, Faction _faction)
        {
            BattleSystem kBattle = GetBattle();
            if (kBattle == null)
            {
                return 0;
            }

            int kCount = 0;
            IReadOnlyList<BattleUnit> kUnits = kBattle.GetUnits();
            for (int i = 0; i < kUnits.Count; i++)
            {
                BattleUnit kUnit = kUnits[i];
                if (kUnit == null || kUnit.m_Faction != _faction)
                {
                    continue;
                }

                kUnit.m_ActiveSkillId = _skillId;
                kUnit.m_SkillCd[_skillId] = 0f;
                kCount++;
            }

            Game.Log.LogInfo("[技能] 调试：已把 " + kCount + " 个单位的技能设为 " + _skillId);
            return kCount;
        }

        /// <summary>调试用：立刻施放（跳过 CD 与校验）</summary>
        public bool DebugForceCast(int _unitId)
        {
            BattleUnit kUnit = GetUnit(_unitId);
            if (kUnit == null)
            {
                return false;
            }
            return m_Flow.Cast(kUnit) == SkillCastResult.Cast;
        }

        /// <summary>调试用：打印所有单位的效果</summary>
        public string DumpEffects()
        {
            BattleSystem kBattle = GetBattle();
            if (kBattle == null)
            {
                return "没有进行中的战斗";
            }

            string kText = "[技能] 效果实例共 " + m_Effects.Count + " 个";
            IReadOnlyList<BattleUnit> kUnits = kBattle.GetUnits();
            for (int i = 0; i < kUnits.Count; i++)
            {
                BattleUnit kUnit = kUnits[i];
                IReadOnlyList<EffectInstance> kList = m_Effects.GetByUnit(kUnit.m_UnitId);
                kText += "\n  unit " + kUnit.m_UnitId + " 效果 " + kList.Count + " 个";
                for (int e = 0; e < kList.Count; e++)
                {
                    kText += "\n    " + kList[e];
                }
            }
            return kText;
        }

        /// <summary>调试用：打印所有单位的技能冷却</summary>
        public string DumpSkillState()
        {
            BattleSystem kBattle = GetBattle();
            if (kBattle == null)
            {
                return "没有进行中的战斗";
            }

            string kText = "[技能] 冷却状态";
            IReadOnlyList<BattleUnit> kUnits = kBattle.GetUnits();
            for (int i = 0; i < kUnits.Count; i++)
            {
                BattleUnit kUnit = kUnits[i];
                cfg.GameData_Skill.SkillData kSkill = SkillRule.GetSkill(kUnit.m_ActiveSkillId);
                kText += "\n  unit " + kUnit.m_UnitId
                    + " 技能:" + (kSkill == null ? "无" : kSkill.Name + "(" + kSkill.ID + ")")
                    + " CD:" + GetSkillCd(kUnit).ToString("F1") + "s"
                    + " 已放:" + kUnit.m_SkillUseCount
                    + " 状态:" + m_Effects.CountStates(kUnit.m_UnitId);
            }
            return kText;
        }

        #endregion
    }
}
