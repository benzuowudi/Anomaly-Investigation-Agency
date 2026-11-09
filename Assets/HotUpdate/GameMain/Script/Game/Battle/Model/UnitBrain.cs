using System;
using UnityEngine;

namespace GameMain.Script.Battle
{
    /// <summary>
    /// 单位行为状态机：每个单位一个，负责"选目标 → 靠近 → 到射程后攒行动条 → 攻击"。
    ///
    /// 本条目（#4）只做移动与攻击前摇，伤害结算在 #5 的 BattleResolver 里补。
    /// 状态流转：
    ///   Idle ──目标在射程外──▶ Move ──进入射程──▶ Idle
    ///   Idle ──行动条满且目标在射程内──▶ Attack（前摇）──▶ Idle
    /// 控制/重伤/死亡由 CanAct() 挡住，不进入本状态机。
    /// </summary>
    public class UnitBrain
    {
        private readonly BattleField m_Field;
        private readonly BattleConfig m_Config;
        private readonly Action<BattleUnit, UnitBrainState, UnitBrainState> m_OnStateChanged;
        private readonly Action<BattleUnit, int> m_OnAttackResolved;

        public UnitBrain(BattleField _field, BattleConfig _config,
            Action<BattleUnit, UnitBrainState, UnitBrainState> _onStateChanged,
            Action<BattleUnit, int> _onAttackResolved)
        {
            m_Field = _field;
            m_Config = _config;
            m_OnStateChanged = _onStateChanged;
            m_OnAttackResolved = _onAttackResolved;
        }

        /// <summary>
        /// 每帧驱动一个单位。dt 已经过战斗时钟。
        /// </summary>
        public void Tick(BattleUnit _unit, float _dt)
        {
            if (_unit == null || _dt <= 0f || !_unit.CanAct())
            {
                return;
            }

            // 已越界离场：不再行动
            if (_unit.m_LeftField)
            {
                return;
            }

            // 撤离优先于一切
            if (_unit.m_Retreating)
            {
                TickRetreat(_unit, _dt);
                return;
            }

            // 1) 有前摇/换弹的状态先推进完
            if (TickOngoing(_unit, _dt))
            {
                return;
            }

            // 2) 有弹匣概念的武器：打空先换弹
            if (NeedsReload(_unit))
            {
                StartReload(_unit);
                return;
            }

            // 3) 保证有目标
            if (!EnsureTarget(_unit))
            {
                return;
            }

            BattleUnit kTarget = m_Field.GetUnit(_unit.m_TargetUnitId);
            if (kTarget == null)
            {
                SetState(_unit, UnitBrainState.Idle);
                return;
            }

            // 3) 射程内：待机，行动条满则攻击
            if (m_Field.IsInRange(_unit, kTarget))
            {
                SetState(_unit, UnitBrainState.Idle);
                if (_unit.m_ActionGauge >= 1f)
                {
                    StartAttack(_unit, kTarget);
                }
                return;
            }

            // 4) 射程外：靠近
            MoveToward(_unit, kTarget, _dt);
            SetState(_unit, UnitBrainState.Move);
        }

        #region 状态

        /// <summary>推进攻击前摇 / 换弹；返回 true 表示本帧已被占用</summary>
        private bool TickOngoing(BattleUnit _unit, float _dt)
        {
            if (_unit.m_State == UnitBrainState.Reload)
            {
                _unit.m_StateTimer -= _dt;
                if (_unit.m_StateTimer <= 0f)
                {
                    _unit.m_StateTimer = 0f;
                    _unit.m_AmmoInMag = _unit.m_MagazineSize;
                    SetState(_unit, UnitBrainState.Idle);
                }
                return true;
            }

            if (_unit.m_State != UnitBrainState.Attack)
            {
                return false;
            }

            _unit.m_StateTimer -= _dt;
            if (_unit.m_StateTimer <= 0f)
            {
                _unit.m_StateTimer = 0f;
                SetState(_unit, UnitBrainState.Idle);
                if (m_OnAttackResolved != null)
                {
                    m_OnAttackResolved(_unit, _unit.m_AttackTargetUnitId);
                }
            }
            return true;
        }

        private bool NeedsReload(BattleUnit _unit)
        {
            return _unit.m_MagazineSize > 0 && _unit.m_AmmoInMag <= 0;
        }

        private void StartReload(BattleUnit _unit)
        {
            _unit.m_StateTimer = Mathf.Max(0.1f, _unit.m_ReloadTime);
            SetState(_unit, UnitBrainState.Reload);
        }

        private void StartAttack(BattleUnit _unit, BattleUnit _target)
        {
            _unit.m_ActionGauge -= 1f;
            if (_unit.m_ActionGauge < 0f)
            {
                _unit.m_ActionGauge = 0f;
            }
            _unit.m_ActionCount++;
            _unit.m_StateTimer = BattleConst.AttackWindUp;
            _unit.m_AttackTargetUnitId = _target.m_UnitId;

            // 有弹匣概念的武器：出手即消耗一发（打空后下次 Tick 会进换弹）
            if (_unit.m_MagazineSize > 0 && _unit.m_AmmoInMag > 0)
            {
                _unit.m_AmmoInMag--;
            }

            SetState(_unit, UnitBrainState.Attack);
        }

        private void SetState(BattleUnit _unit, UnitBrainState _state)
        {
            if (_unit.m_State == _state)
            {
                return;
            }

            UnitBrainState kOld = _unit.m_State;
            _unit.m_State = _state;
            if (m_OnStateChanged != null)
            {
                m_OnStateChanged(_unit, kOld, _state);
            }
        }

        #endregion

        #region 目标与移动

        private bool EnsureTarget(BattleUnit _unit)
        {
            // 指令优先：集火目标有效时一直打它
            if (_unit.m_FocusTargetUnitId > 0)
            {
                BattleUnit kFocus = m_Field.GetUnit(_unit.m_FocusTargetUnitId);
                if (kFocus != null && BattleField.IsActive(kFocus))
                {
                    _unit.m_TargetUnitId = kFocus.m_UnitId;
                    return true;
                }
                _unit.m_FocusTargetUnitId = 0;
            }

            BattleUnit kTarget = m_Field.GetUnit(_unit.m_TargetUnitId);
            if (kTarget != null && kTarget.IsAlive())
            {
                return true;
            }

            kTarget = m_Field.FindNearestEnemy(_unit);
            _unit.m_TargetUnitId = kTarget == null ? 0 : kTarget.m_UnitId;

            if (_unit.m_TargetUnitId == 0)
            {
                SetState(_unit, UnitBrainState.Idle);
                return false;
            }
            return true;
        }

        /// <summary>撤离：朝最近的地图边界移动，越界即离场（立刻计入撤离判定）</summary>
        private void TickRetreat(BattleUnit _unit, float _dt)
        {
            SetState(_unit, UnitBrainState.Retreat);

            float kHalf = m_Field.Length * 0.5f;
            float kDir = _unit.m_Pos <= kHalf ? -1f : 1f;
            _unit.m_Pos += kDir * _unit.m_MoveSpeed * _dt;

            if (_unit.m_Pos <= 0f || _unit.m_Pos >= m_Field.Length)
            {
                _unit.m_Pos = _unit.m_Pos <= 0f ? 0f : m_Field.Length;
                _unit.m_LeftField = true;
                _unit.m_TargetUnitId = 0;
                _unit.m_AttackTargetUnitId = 0;
                SetState(_unit, UnitBrainState.Idle);
                Game.Log.LogWarning("[战斗] 撤离离场：unit " + _unit.m_UnitId);
            }
        }

        private void MoveToward(BattleUnit _unit, BattleUnit _target, float _dt)
        {
            float kDir = BattleField.Direction(_unit, _target);
            if (kDir == 0f)
            {
                return;
            }

            float kDistance = BattleField.Distance(_unit, _target);
            float kStep = _unit.m_MoveSpeed * _dt;
            float kStopDistance = Mathf.Max(0.1f, _unit.m_AttackRangeMax * 0.9f);

            if (kDistance - kStep <= kStopDistance)
            {
                // 这一步就能进射程：直接停在射程边缘，避免贴脸抖动
                _unit.m_Pos = m_Field.ClampPosition(_target.m_Pos - kDir * kStopDistance);
                return;
            }

            _unit.m_Pos = m_Field.ClampPosition(_unit.m_Pos + kDir * kStep);
        }

        #endregion
    }
}
