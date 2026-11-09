using System.Collections.Generic;
using cfg.GameType;
using GameMain.Script.Battle;
using GameMain.Script.Role;
using UnityEngine;

namespace GameMain.Script.Skill
{
    /// <summary>
    /// 单条 effect 的执行器：把"配置一行"翻译成"对某个单位做了什么"。
    ///
    /// 目标决策（两条规则，写在 SkillRule.IsSelfEffect 里）：
    /// - 自身类效果（属性/状态/位移/给道具/换弹）→ 施法者；
    /// - 目标类效果（伤害/治疗/加buff）→ 技能选中的目标，没选中就退回施法者。
    /// 多目标技能（scope=MultiTarget）属性类效果打到全体选中目标（振奋人心）。
    ///
    /// 这里不碰冷却、不碰次数，那些在 SkillCastFlow。
    /// </summary>
    public class EffectResolver
    {
        private readonly SkillSystem m_System;

        public EffectResolver(SkillSystem _system)
        {
            m_System = _system;
        }

        #region 批量入口

        /// <summary>对一批目标执行同一条效果</summary>
        public int Execute(BattleUnit _caster, List<BattleUnit> _targets,
            cfg.GameData_Skill.EffectData _effect, int _sourceId, EffectSource _source)
        {
            if (_effect == null || _caster == null)
            {
                return 0;
            }

            List<BattleUnit> kRecipients = DecideTargets(_caster, _targets, _effect);
            int kApplied = 0;
            for (int i = 0; i < kRecipients.Count; i++)
            {
                if (ExecuteOne(_caster, kRecipients[i], _effect, _sourceId, _source))
                {
                    kApplied++;
                }
            }
            return kApplied;
        }

        /// <summary>对单个目标执行一条效果</summary>
        public bool ExecuteOne(BattleUnit _caster, BattleUnit _target,
            cfg.GameData_Skill.EffectData _effect, int _sourceId, EffectSource _source)
        {
            if (_effect == null || _caster == null || _target == null)
            {
                return false;
            }

            switch (_effect.Type)
            {
                case EffectType.damage:
                    return ExecuteDamage(_caster, _target, _effect);
                case EffectType.heal:
                    return ExecuteHeal(_caster, _target, _effect);
                case EffectType.attrMod:
                    return ExecuteAttrMod(_caster, _target, _effect, _sourceId, _source);
                case EffectType.addBuff:
                    return ExecuteAddBuff(_caster, _target, _effect, _sourceId, _source);
                case EffectType.removeBuff:
                    return m_System.Effects.RemoveByBuffId(_target.m_UnitId, _effect.BuffID, "被驱散");
                case EffectType.addState:
                    return ExecuteAddState(_caster, _target, _effect, _sourceId, _source);
                case EffectType.move:
                    return ExecuteMove(_caster, _target, _effect);
                case EffectType.addItem:
                    return ExecuteAddItem(_target, _effect);
                case EffectType.reload:
                    return ExecuteReload(_target);
                case EffectType.clearAmmo:
                    _target.m_AmmoInMag = 0;
                    Game.Log.LogInfo("[技能] 清空弹匣 " + _target.m_UnitId);
                    return true;
                case EffectType.aggro:
                    // 一维战场没有仇恨表：清目标即可（30002 暗影强袭的第一段）
                    _target.m_TargetUnitId = 0;
                    _target.m_FocusTargetUnitId = 0;
                    Game.Log.LogInfo("[技能] 清除仇恨 " + _target.m_UnitId);
                    return true;
                case EffectType.flag:
                    return ExecuteFlag(_caster, _target, _effect, _sourceId, _source);
                default:
                    Game.Log.LogWarning("[技能] 效果类型暂未实现：" + _effect.Type + "（effectID " + _effect.ID + "）");
                    return false;
            }
        }

        #endregion

        #region 目标决策

        /// <summary>算这次效果该打在谁身上</summary>
        private static List<BattleUnit> DecideTargets(BattleUnit _caster, List<BattleUnit> _targets,
            cfg.GameData_Skill.EffectData _effect)
        {
            List<BattleUnit> kResult = new List<BattleUnit>();
            if (_caster == null)
            {
                return kResult;
            }

            // 目标类效果：技能选中了谁就打谁；没选中就退回施法者
            if (!SkillRule.IsSelfEffect(_effect.Type))
            {
                if (_targets != null && _targets.Count > 0)
                {
                    kResult.AddRange(_targets);
                    return kResult;
                }
                kResult.Add(_caster);
                return kResult;
            }

            // 自身类效果：多目标技能的属性类效果打全体选中目标（30012 振奋人心）
            if (_effect.Type == EffectType.attrMod
                && _targets != null && _targets.Count > 0 && _targets.Count > 1)
            {
                kResult.AddRange(_targets);
                return kResult;
            }

            kResult.Add(_caster);
            return kResult;
        }

        #endregion

        #region 原子效果

        private bool ExecuteDamage(BattleUnit _caster, BattleUnit _target, cfg.GameData_Skill.EffectData _effect)
        {
            if (!BattleField.IsActive(_target))
            {
                return false;
            }

            int kValue = SkillRule.ResolveValue(_effect, _target.m_MaxHp);
            if (kValue <= 0)
            {
                return false;
            }

            int kDamage = m_System.ApplyDamage(_caster, _target, kValue, _effect.IgnoreDef);
            return kDamage > 0;
        }

        private bool ExecuteHeal(BattleUnit _caster, BattleUnit _target, cfg.GameData_Skill.EffectData _effect)
        {
            if (!BattleField.IsActive(_target))
            {
                return false;
            }

            int kValue = SkillRule.ResolveValue(_effect, _target.m_MaxHp);
            if (kValue <= 0)
            {
                return false;
            }

            int kHeal = m_System.ApplyHeal(_caster, _target, kValue);
            return kHeal > 0;
        }

        private bool ExecuteAttrMod(BattleUnit _caster, BattleUnit _target,
            cfg.GameData_Skill.EffectData _effect, int _sourceId, EffectSource _source)
        {
            if (_effect.AttrID <= 0 || !BattleField.IsActive(_target))
            {
                Game.Log.LogWarning("[技能] attrMod 缺 attrID 或目标已离场：" + _effect.ID);
                return false;
            }

            int kCurrent = _target.GetAttr(_effect.AttrID);
            int kNext = SkillRule.ResolveAttrValue(kCurrent, _effect);
            int kDelta = kNext - kCurrent;

            EffectInstance kInst = new EffectInstance();
            kInst.m_EffectId = _effect.ID;
            kInst.m_SourceId = _sourceId;
            kInst.m_Source = _source;
            kInst.m_Scope = EffectScopeType.Battle;
            kInst.m_OwnerUnitId = _target.m_UnitId;
            kInst.m_CasterUnitId = _caster.m_UnitId;
            kInst.m_Stacks = _effect.Stacks <= 0 ? 1 : _effect.Stacks;
            kInst.SetDuration(SkillRule.ResolveDuration(_effect));
            kInst.m_AttrId = _effect.AttrID;
            kInst.m_AttrDelta = kDelta;

            m_System.Effects.Attach(kInst);
            Game.Log.LogInfo("[技能] 属性 " + _target.m_UnitId + " 属性" + _effect.AttrID
                + " " + kCurrent + " → " + kNext + "（" + (kInst.m_Remain < 0f ? "永久" : kInst.m_Remain + "秒") + "）");
            return true;
        }

        private bool ExecuteAddBuff(BattleUnit _caster, BattleUnit _target,
            cfg.GameData_Skill.EffectData _effect, int _sourceId, EffectSource _source)
        {
            if (_effect.BuffID <= 0 || !BattleField.IsActive(_target))
            {
                Game.Log.LogWarning("[技能] addBuff 缺 buffID：" + _effect.ID);
                return false;
            }

            cfg.GameData_Skill.buffData kBuff = SkillRule.GetBuff(_effect.BuffID);

            EffectInstance kInst = new EffectInstance();
            kInst.m_EffectId = _effect.ID;
            kInst.m_SourceId = _sourceId;
            kInst.m_Source = _source;
            kInst.m_Scope = EffectScopeType.Battle;
            kInst.m_OwnerUnitId = _target.m_UnitId;
            kInst.m_CasterUnitId = _caster.m_UnitId;
            kInst.m_BuffId = _effect.BuffID;
            kInst.m_Stacks = _effect.Stacks <= 0 ? 1 : _effect.Stacks;
            kInst.SetDuration(SkillRule.ResolveDuration(_effect));

            m_System.Effects.Attach(kInst);
            Game.Log.LogInfo("[技能] 加buff " + (_target == null ? 0 : _target.m_UnitId)
                + " ← " + (kBuff == null ? ("buff" + _effect.BuffID) : kBuff.Name)
                + " x" + kInst.m_Stacks);
            return true;
        }

        private bool ExecuteAddState(BattleUnit _caster, BattleUnit _target,
            cfg.GameData_Skill.EffectData _effect, int _sourceId, EffectSource _source)
        {
            if (_effect.StateTrigger == StateTriggerCondition.immediate)
            {
                // 没配触发条件就不是状态，按即时效果处理（避免挂一个永远不会响的实例）
                Game.Log.LogWarning("[技能] addState 没配 stateTrigger：" + _effect.ID);
                return false;
            }

            EffectInstance kInst = new EffectInstance();
            kInst.m_EffectId = _effect.ID;
            kInst.m_SourceId = _sourceId;
            kInst.m_Source = _source;
            kInst.m_Scope = EffectScopeType.Battle;
            kInst.m_OwnerUnitId = _target.m_UnitId;
            kInst.m_CasterUnitId = _caster.m_UnitId;
            kInst.m_Trigger = _effect.StateTrigger;
            kInst.m_TriggerParam = _effect.StateConditionParam;
            kInst.m_Logic = _effect.StateConditionLogic;
            kInst.m_Charges = _effect.Charges <= 0 ? 1 : _effect.Charges;
            kInst.SetDuration(SkillRule.ResolveDuration(_effect));
            // 约定：addState 的 payload 用 tickEffectID 列（表里没有单独的 payload 列）
            kInst.m_PayloadEffectId = _effect.TickEffectID;

            m_System.Effects.Attach(kInst);
            Game.Log.LogInfo("[技能] 挂状态 " + _target.m_UnitId + " effect:" + _effect.ID
                + " 条件:" + _effect.StateTrigger + " payload:" + kInst.m_PayloadEffectId);
            return true;
        }

        private bool ExecuteMove(BattleUnit _caster, BattleUnit _target, cfg.GameData_Skill.EffectData _effect)
        {
            // move 永远作用在施法者身上：_target 只是"朝谁去"的参考
            BattleUnit kMover = _caster;
            if (kMover == null || !BattleField.IsActive(kMover))
            {
                return false;
            }

            if (_effect.MoveToRelation == 1)
            {
                return m_System.MoveToNearestEnemy(kMover);
            }
            if (_effect.MoveToRelation == 8)
            {
                return m_System.MoveBehindEnemyBackRow(kMover);
            }
            if (Mathf.Abs(_effect.MoveDistance) < 0.001f)
            {
                return false;
            }
            return m_System.MoveBy(kMover, _effect.MoveDistance);
        }

        private bool ExecuteAddItem(BattleUnit _target, cfg.GameData_Skill.EffectData _effect)
        {
            int kItemId = _effect.ValueSourceParam;
            int kCount = Mathf.RoundToInt(_effect.Value);
            if (kItemId <= 0 || kCount <= 0)
            {
                Game.Log.LogWarning("[技能] addItem 缺参数（valueSourceParam=itemID, value=数量）：" + _effect.ID);
                return false;
            }

            int kOld = 0;
            _target.m_CarriedItems.TryGetValue(kItemId, out kOld);
            _target.m_CarriedItems[kItemId] = kOld + kCount;
            Game.Log.LogInfo("[技能] 给补给 " + _target.m_UnitId + " 补给" + kItemId
                + " +" + kCount + "（现有 " + (kOld + kCount) + "）");
            return true;
        }

        private bool ExecuteReload(BattleUnit _target)
        {
            if (_target.m_MagazineSize <= 0)
            {
                Game.Log.LogInfo("[技能] 换弹请求被忽略（该单位没有弹匣概念）" + _target.m_UnitId);
                return false;
            }

            _target.m_AmmoInMag = _target.m_MagazineSize;
            Game.Log.LogInfo("[技能] 立刻换弹 " + _target.m_UnitId + " → " + _target.m_AmmoInMag);
            return true;
        }

        private bool ExecuteFlag(BattleUnit _caster, BattleUnit _target,
            cfg.GameData_Skill.EffectData _effect, int _sourceId, EffectSource _source)
        {
            // 控制标记等 buff 表扩列（controlFlags）后接 UnitBrain；现在只挂实例做记录
            EffectInstance kInst = new EffectInstance();
            kInst.m_EffectId = _effect.ID;
            kInst.m_SourceId = _sourceId;
            kInst.m_Source = _source;
            kInst.m_Scope = EffectScopeType.Battle;
            kInst.m_OwnerUnitId = _target.m_UnitId;
            kInst.m_CasterUnitId = _caster.m_UnitId;
            kInst.m_Stacks = 1;
            kInst.SetDuration(SkillRule.ResolveDuration(_effect));

            m_System.Effects.Attach(kInst);
            Game.Log.LogInfo("[技能] 标记 " + _target.m_UnitId + " flag:" + _effect.FlagID
                + "（行为影响等 buff.controlFlags 落地后接）");
            return true;
        }

        #endregion
    }
}
