using System.Collections.Generic;
using cfg.GameType;
using GameMain.Script.Battle;
using UnityEngine;

namespace GameMain.Script.Skill
{
    /// <summary>
    /// 一维战场下的目标选取。
    ///
    /// skill.xlsx 用的是两列：
    /// - target：可以多选（自己 / 敌人 / 队友 / 中立 / 召唤物），30012 就是 [自己, 队友]；
    /// - scope ：单目标 / 多目标 / 无目标。
    ///
    /// 选取规则（先关系、后数量）：
    ///   1. 按 target 里的每个关系收集候选；
    ///   2. scope=SingleTarget 时在候选里取【离施法者最近】的一个；
    ///   3. scope=MultiTarget 时全取（半径字段表里还没有，先按"全体该关系"处理）。
    /// </summary>
    public class SkillTargetSelector
    {
        private readonly SkillSystem m_System;

        public SkillTargetSelector(SkillSystem _system)
        {
            m_System = _system;
        }

        /// <summary>这个技能是否必须有目标才施放（配置里只有 None 或空 → 不需要）</summary>
        public static bool NeedTarget(cfg.GameData_Skill.SkillData _skill)
        {
            if (_skill == null || _skill.Target == null || _skill.Target.Count == 0)
            {
                return false;
            }

            for (int i = 0; i < _skill.Target.Count; i++)
            {
                if (_skill.Target[i] != SkillTarget.None)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>是否单目标</summary>
        public static bool IsSingleTarget(cfg.GameData_Skill.SkillData _skill)
        {
            if (_skill == null || _skill.Scope == null || _skill.Scope.Count == 0)
            {
                return true;
            }
            return _skill.Scope[0] == SkillScope.SingleTarget;
        }

        /// <summary>选目标。选不出返回空列表（不是 null）</summary>
        public List<BattleUnit> Select(BattleUnit _caster, cfg.GameData_Skill.SkillData _skill)
        {
            List<BattleUnit> kResult = new List<BattleUnit>();
            if (_caster == null || _skill == null || _skill.Target == null)
            {
                return kResult;
            }

            // 1. 收集候选（去重）
            List<BattleUnit> kCandidates = new List<BattleUnit>();
            for (int i = 0; i < _skill.Target.Count; i++)
            {
                CollectByRelation(_caster, _skill.Target[i], kCandidates);
            }

            if (kCandidates.Count == 0)
            {
                return kResult;
            }

            // 2. 单体：取最近
            if (IsSingleTarget(_skill))
            {
                BattleUnit kBest = null;
                float kBestDistance = float.MaxValue;
                for (int i = 0; i < kCandidates.Count; i++)
                {
                    float kDistance = BattleField.Distance(_caster, kCandidates[i]);
                    if (kDistance < kBestDistance)
                    {
                        kBestDistance = kDistance;
                        kBest = kCandidates[i];
                    }
                }

                if (kBest != null)
                {
                    kResult.Add(kBest);
                }
                return kResult;
            }

            kResult.AddRange(kCandidates);
            return kResult;
        }

        /// <summary>按关系收集候选并去重</summary>
        private void CollectByRelation(BattleUnit _caster, SkillTarget _relation, List<BattleUnit> _result)
        {
            switch (_relation)
            {
                case SkillTarget.Self:
                    AddUnique(_result, _caster);
                    return;
                case SkillTarget.Enemy:
                    CollectFaction(_caster, true, _result);
                    return;
                case SkillTarget.Teammate:
                    CollectFaction(_caster, false, _result);
                    return;
                case SkillTarget.Neutral:
                    CollectNeutral(_caster, _result);
                    return;
                case SkillTarget.Summon:
                    CollectSummon(_caster, _result);
                    return;
                default:
                    return;   // None：无目标技能
            }
        }

        private void CollectFaction(BattleUnit _caster, bool _hostile, List<BattleUnit> _result)
        {
            BattleSystem kBattle = m_System.GetBattle();
            if (kBattle == null)
            {
                return;
            }

            IReadOnlyList<BattleUnit> kUnits = kBattle.GetUnits();
            for (int i = 0; i < kUnits.Count; i++)
            {
                BattleUnit kUnit = kUnits[i];
                if (!BattleField.IsActive(kUnit))
                {
                    continue;
                }

                bool kIsHostile = BattleField.IsHostile(_caster.m_Faction, kUnit.m_Faction);
                if (kIsHostile != _hostile)
                {
                    continue;
                }
                // 队友不含自己（自己由 target=Self 单独表达）
                if (!_hostile && kUnit == _caster)
                {
                    continue;
                }
                AddUnique(_result, kUnit);
            }
        }

        private void CollectNeutral(BattleUnit _caster, List<BattleUnit> _result)
        {
            BattleSystem kBattle = m_System.GetBattle();
            if (kBattle == null)
            {
                return;
            }

            IReadOnlyList<BattleUnit> kUnits = kBattle.GetUnits();
            for (int i = 0; i < kUnits.Count; i++)
            {
                BattleUnit kUnit = kUnits[i];
                if (!BattleField.IsActive(kUnit) || kUnit.m_Faction != Faction.neutral)
                {
                    continue;
                }
                AddUnique(_result, kUnit);
            }
        }

        private void CollectSummon(BattleUnit _caster, List<BattleUnit> _result)
        {
            BattleSystem kBattle = m_System.GetBattle();
            if (kBattle == null)
            {
                return;
            }

            IReadOnlyList<BattleUnit> kUnits = kBattle.GetUnits();
            for (int i = 0; i < kUnits.Count; i++)
            {
                BattleUnit kUnit = kUnits[i];
                if (!BattleField.IsActive(kUnit) || kUnit.m_Kind != UnitKind.Summon)
                {
                    continue;
                }
                AddUnique(_result, kUnit);
            }
        }

        private static void AddUnique(List<BattleUnit> _list, BattleUnit _unit)
        {
            if (_unit == null || _list.Contains(_unit))
            {
                return;
            }
            _list.Add(_unit);
        }
    }
}
