using System.Collections.Generic;
using cfg.GameType;
using GameMain.Script.Battle;
using UnityEngine;

namespace GameMain.Script.Skill
{
    /// <summary>
    /// 效果实例的容器与生命周期（挂在单位上，按单位ID索引）。
    ///
    /// 为什么不把 List&lt;EffectInstance&gt; 直接放进 BattleUnit：
    /// 那样 Battle/Data 就要引用 Skill 域的类型，方向反了（技能读战斗，战斗不读技能）。
    /// 这里用 unitId 做键，战斗层完全不需要知道"效果"这个概念。
    ///
    /// 三件事：挂载/叠层、每帧到期与周期结算、按条件消耗（技能状态）。
    /// </summary>
    public class EffectService
    {
        private readonly Dictionary<int, List<EffectInstance>> m_ByUnit = new Dictionary<int, List<EffectInstance>>();
        private readonly SkillSystem m_System;

        public EffectService(SkillSystem _system)
        {
            m_System = _system;
        }

        /// <summary>当前所有存活的效果实例数</summary>
        public int Count
        {
            get
            {
                int kCount = 0;
                foreach (KeyValuePair<int, List<EffectInstance>> kPair in m_ByUnit)
                {
                    kCount += kPair.Value.Count;
                }
                return kCount;
            }
        }

        #region 挂载

        /// <summary>
        /// 挂一个效果实例。同 ID 已存在时：刷新时长、叠层数、补属性增量（stackPolicy 精细区分等 buff 表扩列后再做）。
        /// </summary>
        public EffectInstance Attach(EffectInstance _inst)
        {
            if (_inst == null || _inst.m_OwnerUnitId <= 0)
            {
                return null;
            }

            List<EffectInstance> kList = GetOrCreateList(_inst.m_OwnerUnitId);
            EffectInstance kExist = Find(kList, _inst.m_EffectId);
            if (kExist != null && !kExist.m_Expired)
            {
                kExist.m_Remain = _inst.m_Remain;
                kExist.m_Permanent = _inst.m_Permanent;
                kExist.m_TickRemain = _inst.m_TickInterval;
                if (_inst.m_Stacks > 0)
                {
                    kExist.m_Stacks += _inst.m_Stacks;
                }
                if (_inst.m_Charges > kExist.m_Charges)
                {
                    kExist.m_Charges = _inst.m_Charges;
                }
                ApplyAttrDelta(kExist.m_OwnerUnitId, kExist.m_AttrId, _inst.m_AttrDelta);
                kExist.m_AttrDelta += _inst.m_AttrDelta;

                SendApplied(kExist);
                return kExist;
            }

            ApplyAttrDelta(_inst.m_OwnerUnitId, _inst.m_AttrId, _inst.m_AttrDelta);
            kList.Add(_inst);
            SendApplied(_inst);
            return _inst;
        }

        private void SendApplied(EffectInstance _inst)
        {
            Game.Event.SendMessage(SkillEvent.OnEffectApplied,
                new EffectAppliedMsg(_inst.m_OwnerUnitId, _inst.m_EffectId, _inst.m_Source,
                    _inst.m_Stacks, _inst.GetDurationOrZero(), _inst.IsState()));
            Game.Log.LogInfo("[技能] 挂载 " + _inst);
        }

        #endregion

        #region 每帧

        /// <summary>
        /// 每帧推进：剩余时长、周期效果。传进来的是战斗时钟换算后的 dt（暂停=0）。
        /// </summary>
        public void Tick(float _dt)
        {
            if (_dt <= 0f)
            {
                return;
            }

            foreach (KeyValuePair<int, List<EffectInstance>> kPair in m_ByUnit)
            {
                List<EffectInstance> kList = kPair.Value;
                for (int i = kList.Count - 1; i >= 0; i--)
                {
                    EffectInstance kInst = kList[i];
                    if (kInst == null || kInst.m_Expired)
                    {
                        kList.RemoveAt(i);
                        continue;
                    }

                    // 时长
                    if (kInst.m_Remain >= 0f)
                    {
                        kInst.m_Remain -= _dt;
                        if (kInst.m_Remain <= 0f)
                        {
                            RemoveAt(kList, i, "到期");
                            continue;
                        }
                    }

                    // 周期结算（tickInterval + tickEffectID）
                    if (kInst.m_TickInterval > 0f && kInst.m_TickEffectId > 0 && !kInst.IsState())
                    {
                        kInst.m_TickRemain -= _dt;
                        if (kInst.m_TickRemain <= 0f)
                        {
                            kInst.m_TickRemain = kInst.m_TickInterval;
                            TickEffect(kInst);
                        }
                    }
                }
            }
        }

        private void TickEffect(EffectInstance _inst)
        {
            cfg.GameData_Skill.EffectData kEffect = SkillRule.GetEffect(_inst.m_TickEffectId);
            if (kEffect == null)
            {
                return;
            }

            BattleUnit kOwner = SkillRule.GetUnit(_inst.m_OwnerUnitId);
            BattleUnit kCaster = SkillRule.GetUnit(_inst.m_CasterUnitId);
            if (kOwner == null)
            {
                return;
            }

            m_System.Resolver.ExecuteOne(kCaster ?? kOwner, kOwner, kEffect, _inst.m_SourceId, _inst.m_Source);
        }

        #endregion

        #region 触发（技能状态）

        /// <summary>
        /// 有事件发生时，让"挂在这些单位身上的技能状态"检查是否命中。
        /// 命中就执行 payload 并扣次数，扣到 0 移除。
        /// </summary>
        public void NotifyTrigger(int _ownerUnitId, StateTriggerCondition _condition, BattleUnit _subject, int _param)
        {
            List<EffectInstance> kList;
            if (!m_ByUnit.TryGetValue(_ownerUnitId, out kList) || kList.Count == 0)
            {
                return;
            }

            for (int i = kList.Count - 1; i >= 0; i--)
            {
                EffectInstance kInst = kList[i];
                if (kInst == null || kInst.m_Expired || !kInst.IsState())
                {
                    continue;
                }
                if (kInst.m_Trigger != _condition)
                {
                    continue;
                }
                if (!MatchParam(kInst, _subject, _param))
                {
                    continue;
                }

                TriggerOne(kInst, _subject);
                if (kInst.m_Expired)
                {
                    kList.RemoveAt(i);
                }
            }
        }

        private void TriggerOne(EffectInstance _inst, BattleUnit _subject)
        {
            BattleUnit kOwner = SkillRule.GetUnit(_inst.m_OwnerUnitId);
            BattleUnit kCaster = SkillRule.GetUnit(_inst.m_CasterUnitId) ?? kOwner;
            BattleUnit kPayloadTarget = _subject ?? kOwner;

            Game.Event.SendMessage(SkillEvent.OnEffectTriggered,
                new EffectTriggeredMsg(_inst.m_OwnerUnitId, _inst.m_EffectId, _inst.m_Trigger,
                    kPayloadTarget == null ? 0 : kPayloadTarget.m_UnitId));

            if (_inst.m_PayloadEffectId > 0 && kPayloadTarget != null)
            {
                cfg.GameData_Skill.EffectData kPayload = SkillRule.GetEffect(_inst.m_PayloadEffectId);
                if (kPayload != null)
                {
                    m_System.Resolver.ExecuteOne(kCaster, kPayloadTarget, kPayload, _inst.m_SourceId, _inst.m_Source);
                }
                else
                {
                    Game.Log.LogWarning("[技能] 状态 payload 效果不存在：" + _inst.m_PayloadEffectId);
                }
            }

            if (_inst.HasCharges())
            {
                _inst.m_Charges--;
                if (_inst.m_Charges <= 0)
                {
                    MarkExpired(_inst, "消耗");
                }
            }
        }

        /// <summary>条件参数校验：阈值类条件比较参数，其它条件只要事件类型对上</summary>
        private static bool MatchParam(EffectInstance _inst, BattleUnit _subject, int _param)
        {
            switch (_inst.m_Trigger)
            {
                case StateTriggerCondition.selfHpBelow:
                case StateTriggerCondition.allyHpBelow:
                {
                    if (_subject == null)
                    {
                        return false;
                    }
                    float kThreshold = _inst.m_TriggerParam <= 0 ? 0f : _inst.m_TriggerParam / 100f;
                    return _subject.GetHpPercent() < kThreshold;
                }
                case StateTriggerCondition.inRow:
                    return true;
                case StateTriggerCondition.onUseItem:
                    return _inst.m_TriggerParam <= 0 || _inst.m_TriggerParam == _param;
                default:
                    return true;
            }
        }

        #endregion

        #region 移除

        /// <summary>按效果ID移除（驱散 / removeBuff）</summary>
        public bool RemoveByEffectId(int _unitId, int _effectId, string _reason)
        {
            List<EffectInstance> kList = GetOrCreateList(_unitId);
            bool kRemoved = false;
            for (int i = kList.Count - 1; i >= 0; i--)
            {
                if (kList[i].m_EffectId != _effectId)
                {
                    continue;
                }
                RemoveAt(kList, i, _reason);
                kRemoved = true;
            }
            return kRemoved;
        }

        /// <summary>按 buffID 移除</summary>
        public bool RemoveByBuffId(int _unitId, int _buffId, string _reason)
        {
            List<EffectInstance> kList = GetOrCreateList(_unitId);
            bool kRemoved = false;
            for (int i = kList.Count - 1; i >= 0; i--)
            {
                if (kList[i].m_BuffId != _buffId)
                {
                    continue;
                }
                RemoveAt(kList, i, _reason);
                kRemoved = true;
            }
            return kRemoved;
        }

        /// <summary>按来源清理（装备卸下 / 天赋失效）</summary>
        public int RemoveBySource(EffectSource _source)
        {
            int kCount = 0;
            foreach (KeyValuePair<int, List<EffectInstance>> kPair in m_ByUnit)
            {
                List<EffectInstance> kList = kPair.Value;
                for (int i = kList.Count - 1; i >= 0; i--)
                {
                    if (kList[i].m_Source != _source)
                    {
                        continue;
                    }
                    RemoveAt(kList, i, "按来源清理");
                    kCount++;
                }
            }
            return kCount;
        }

        /// <summary>按作用域清理（离开探索区域）</summary>
        public int RemoveByScope(EffectScopeType _scope)
        {
            int kCount = 0;
            foreach (KeyValuePair<int, List<EffectInstance>> kPair in m_ByUnit)
            {
                List<EffectInstance> kList = kPair.Value;
                for (int i = kList.Count - 1; i >= 0; i--)
                {
                    if (kList[i].m_Scope != _scope)
                    {
                        continue;
                    }
                    RemoveAt(kList, i, "作用域关闭");
                    kCount++;
                }
            }
            return kCount;
        }

        /// <summary>清掉某个单位的全部效果（单位死亡）</summary>
        public void ClearUnit(int _unitId)
        {
            List<EffectInstance> kList = GetOrCreateList(_unitId);
            for (int i = kList.Count - 1; i >= 0; i--)
            {
                RemoveAt(kList, i, "单位离场");
            }
        }

        /// <summary>清空全部（战斗结束）</summary>
        public void ClearAll()
        {
            foreach (KeyValuePair<int, List<EffectInstance>> kPair in m_ByUnit)
            {
                List<EffectInstance> kList = kPair.Value;
                for (int i = kList.Count - 1; i >= 0; i--)
                {
                    RemoveAt(kList, i, "战斗结束");
                }
            }
            m_ByUnit.Clear();
        }

        private void MarkExpired(EffectInstance _inst, string _reason)
        {
            RevertAttrDelta(_inst);
            _inst.m_Expired = true;
            Game.Event.SendMessage(SkillEvent.OnEffectRemoved,
                new EffectRemovedMsg(_inst.m_OwnerUnitId, _inst.m_EffectId, _inst.m_Source, _reason));
            Game.Log.LogInfo("[技能] 移除 " + _inst + " 原因：" + _reason);
        }

        private void RemoveAt(List<EffectInstance> _list, int _index, string _reason)
        {
            EffectInstance kInst = _list[_index];
            _list.RemoveAt(_index);
            if (kInst != null)
            {
                MarkExpired(kInst, _reason);
            }
        }

        #endregion

        #region 查询

        /// <summary>某单位的全部效果（只读，没有返回空列表）</summary>
        public IReadOnlyList<EffectInstance> GetByUnit(int _unitId)
        {
            List<EffectInstance> kList;
            if (m_ByUnit.TryGetValue(_unitId, out kList))
            {
                return kList;
            }
            return s_Empty;
        }

        public bool Has(int _unitId, int _effectId)
        {
            return Find(GetOrCreateList(_unitId), _effectId) != null;
        }

        /// <summary>某单位身上"还没被消耗的技能状态"数量（调试/UI 用）</summary>
        public int CountStates(int _unitId)
        {
            IReadOnlyList<EffectInstance> kList = GetByUnit(_unitId);
            int kCount = 0;
            for (int i = 0; i < kList.Count; i++)
            {
                if (kList[i].IsState())
                {
                    kCount++;
                }
            }
            return kCount;
        }

        private static readonly List<EffectInstance> s_Empty = new List<EffectInstance>();

        private List<EffectInstance> GetOrCreateList(int _unitId)
        {
            List<EffectInstance> kList;
            if (!m_ByUnit.TryGetValue(_unitId, out kList))
            {
                kList = new List<EffectInstance>();
                m_ByUnit[_unitId] = kList;
            }
            return kList;
        }

        private static EffectInstance Find(List<EffectInstance> _list, int _effectId)
        {
            for (int i = 0; i < _list.Count; i++)
            {
                if (_list[i].m_EffectId == _effectId && !_list[i].m_Expired)
                {
                    return _list[i];
                }
            }
            return null;
        }

        #endregion

        #region 属性增量

        private static void ApplyAttrDelta(int _unitId, int _attrId, int _delta)
        {
            if (_attrId <= 0 || _delta == 0)
            {
                return;
            }

            BattleUnit kUnit = SkillRule.GetUnit(_unitId);
            if (kUnit == null)
            {
                return;
            }
            kUnit.SetAttr(_attrId, kUnit.GetAttr(_attrId) + _delta);
        }

        private static void RevertAttrDelta(EffectInstance _inst)
        {
            if (_inst == null || _inst.m_AttrId <= 0 || _inst.m_AttrDelta == 0)
            {
                return;
            }

            BattleUnit kUnit = SkillRule.GetUnit(_inst.m_OwnerUnitId);
            if (kUnit == null)
            {
                return;
            }
            kUnit.SetAttr(_inst.m_AttrId, kUnit.GetAttr(_inst.m_AttrId) - _inst.m_AttrDelta);
        }

        #endregion
    }
}
