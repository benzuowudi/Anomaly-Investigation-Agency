using System.Collections.Generic;
using cfg.GameType;

namespace GameMain.Script.Battle
{
    /// <summary>
    /// 一维战场：单位集合 + 空间查询。
    ///
    /// 约定：
    /// - 位置单位是"米"，取值 [0, Length]；
    /// - 双方单位可以出现在任意位置（允许互相穿插、被包夹），不按阵营划分半场；
    /// - 不做碰撞与阻挡，只提供距离/顺序查询。
    /// </summary>
    public class BattleField
    {
        private readonly IReadOnlyList<BattleUnit> m_Units;

        public BattleField(IReadOnlyList<BattleUnit> _units, float _length)
        {
            m_Units = _units;
            Length = _length > 0f ? _length : 40f;
        }

        /// <summary>战场总长（米）</summary>
        public float Length { get; private set; }

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

        #region 距离与方向

        public static float Distance(BattleUnit _a, BattleUnit _b)
        {
            if (_a == null || _b == null)
            {
                return float.MaxValue;
            }
            return UnityEngine.Mathf.Abs(_a.m_Pos - _b.m_Pos);
        }

        /// <summary>从 self 指向 target 的方向：+1 向右，-1 向左，0 重合</summary>
        public static float Direction(BattleUnit _self, BattleUnit _target)
        {
            if (_self == null || _target == null)
            {
                return 0f;
            }
            float kDelta = _target.m_Pos - _self.m_Pos;
            if (UnityEngine.Mathf.Abs(kDelta) < 0.001f)
            {
                return 0f;
            }
            return kDelta > 0f ? 1f : -1f;
        }

        /// <summary>按单位的武器射程判断目标是否在射程内</summary>
        public bool IsInRange(BattleUnit _self, BattleUnit _target)
        {
            if (_self == null || _target == null)
            {
                return false;
            }

            float kDistance = Distance(_self, _target);
            return kDistance >= _self.m_AttackRangeMin && kDistance <= _self.m_AttackRangeMax;
        }

        /// <summary>把坐标夹到战场范围内</summary>
        public float ClampPosition(float _pos)
        {
            if (_pos < 0f)
            {
                return 0f;
            }
            if (_pos > Length)
            {
                return Length;
            }
            return _pos;
        }

        #endregion

        #region 阵营查询

        public static bool IsHostile(Faction _a, Faction _b)
        {
            if (_a == Faction.neutral || _b == Faction.neutral)
            {
                return false;
            }
            return _a != _b;
        }

        /// <summary>还在场上（存活且未离场）</summary>
        public static bool IsActive(BattleUnit _unit)
        {
            return _unit != null && _unit.IsAlive() && !_unit.m_LeftField;
        }

        /// <summary>离 _self 最近的敌对存活单位；没有返回 null</summary>
        public BattleUnit FindNearestEnemy(BattleUnit _self)
        {
            if (_self == null)
            {
                return null;
            }

            BattleUnit kBest = null;
            float kBestDistance = float.MaxValue;
            for (int i = 0; i < m_Units.Count; i++)
            {
                BattleUnit kOther = m_Units[i];
                if (kOther == null || kOther == _self || !IsActive(kOther))
                {
                    continue;
                }
                if (!IsHostile(_self.m_Faction, kOther.m_Faction))
                {
                    continue;
                }

                float kDistance = Distance(_self, kOther);
                if (kDistance < kBestDistance)
                {
                    kBestDistance = kDistance;
                    kBest = kOther;
                }
            }
            return kBest;
        }

        /// <summary>某阵营在场上的单位数（存活且未离场）</summary>
        public int CountAlive(Faction _faction)
        {
            int kCount = 0;
            for (int i = 0; i < m_Units.Count; i++)
            {
                BattleUnit kUnit = m_Units[i];
                if (IsActive(kUnit) && kUnit.m_Faction == _faction)
                {
                    kCount++;
                }
            }
            return kCount;
        }

        /// <summary>某阵营已越界离场且未死亡的单位数（撤离判定用）</summary>
        public int CountLeftField(Faction _faction)
        {
            int kCount = 0;
            for (int i = 0; i < m_Units.Count; i++)
            {
                BattleUnit kUnit = m_Units[i];
                if (kUnit != null && kUnit.m_LeftField && kUnit.IsAlive() && kUnit.m_Faction == _faction)
                {
                    kCount++;
                }
            }
            return kCount;
        }

        /// <summary>
        /// 仍有作战能力的单位数：排除重伤、死亡、已离场。
        /// 胜负判定用这个，而不是 CountAlive（重伤也打不动了，不能算"还在打"）。
        /// </summary>
        public int CountEffective(Faction _faction)
        {
            int kCount = 0;
            for (int i = 0; i < m_Units.Count; i++)
            {
                BattleUnit kUnit = m_Units[i];
                if (kUnit == null || kUnit.m_LeftField || kUnit.m_Faction != _faction)
                {
                    continue;
                }
                if (kUnit.m_State == UnitBrainState.Dead || kUnit.m_State == UnitBrainState.Downed)
                {
                    continue;
                }
                kCount++;
            }
            return kCount;
        }

        /// <summary>
        /// 阵营内的名次（按 pos 升序，0 = 最靠左）。
        /// 前排/后排的具体方向由阵营的推进方向决定，技能接入时按"离敌方重心更近"再解释。
        /// </summary>
        public int GetRowIndex(BattleUnit _unit)
        {
            if (_unit == null)
            {
                return -1;
            }

            int kIndex = 0;
            for (int i = 0; i < m_Units.Count; i++)
            {
                BattleUnit kOther = m_Units[i];
                if (!IsActive(kOther) || kOther.m_Faction != _unit.m_Faction)
                {
                    continue;
                }
                if (kOther.m_Pos < _unit.m_Pos)
                {
                    kIndex++;
                }
            }
            return kIndex;
        }

        /// <summary>某阵营内所有存活单位</summary>
        public List<BattleUnit> GetAllies(Faction _faction, bool _includeSelf = true)
        {
            List<BattleUnit> kResult = new List<BattleUnit>();
            for (int i = 0; i < m_Units.Count; i++)
            {
                BattleUnit kUnit = m_Units[i];
                if (!IsActive(kUnit) || kUnit.m_Faction != _faction)
                {
                    continue;
                }
                kResult.Add(kUnit);
            }
            return kResult;
        }

        /// <summary>
        /// 一维范围查询：以 _center 为中心、_radius 为半径内、指定阵营的存活单位。
        /// _faction 传 null 表示不限阵营。
        /// </summary>
        public List<BattleUnit> GetUnitsInRadius(float _center, float _radius, Faction? _faction = null)
        {
            List<BattleUnit> kResult = new List<BattleUnit>();
            for (int i = 0; i < m_Units.Count; i++)
            {
                BattleUnit kUnit = m_Units[i];
                if (!IsActive(kUnit))
                {
                    continue;
                }
                if (_faction.HasValue && kUnit.m_Faction != _faction.Value)
                {
                    continue;
                }
                if (UnityEngine.Mathf.Abs(kUnit.m_Pos - _center) <= _radius)
                {
                    kResult.Add(kUnit);
                }
            }
            return kResult;
        }

        #endregion
    }
}
