using cfg.GameType;
using GameMain.Script.Battle;
using UnityEngine;

namespace GameMain.Script.Skill
{
    /// <summary>
    /// 技能域的配置查询与公式（static 纯函数，规范 §4.5 允许）。
    ///
    /// 为什么要这一层：战斗层不直接读 skill/talent/effect 表（战斗文档 §8.2），
    /// 所有配置查询一律从这里走，换表/换列只改这一个文件。
    ///
    /// 注意命名冲突：本文件所在的命名空间里也有一个 SkillData（系统的 Data），
    /// 所以配置行类型一律写全 cfg.GameData_Skill.SkillData。
    /// </summary>
    public static class SkillRule
    {
        private static cfg.Tables Tables
        {
            get { return Game.Data.GetData; }
        }

        #region 配置查询

        public static cfg.GameData_Skill.SkillData GetSkill(int _skillId)
        {
            cfg.Tables kTables = Tables;
            if (kTables == null || kTables.skill == null || _skillId <= 0)
            {
                return null;
            }
            return kTables.skill.GetOrDefault(_skillId);
        }

        public static cfg.GameData_Skill.EffectData GetEffect(int _effectId)
        {
            cfg.Tables kTables = Tables;
            if (kTables == null || kTables.effect == null || _effectId <= 0)
            {
                return null;
            }
            return kTables.effect.GetOrDefault(_effectId);
        }

        public static cfg.GameData_Skill.buffData GetBuff(int _buffId)
        {
            cfg.Tables kTables = Tables;
            if (kTables == null || kTables.buff == null || _buffId <= 0)
            {
                return null;
            }
            return kTables.buff.GetOrDefault(_buffId);
        }

        public static cfg.GameData_Skill.TalentData GetTalent(int _talentId)
        {
            cfg.Tables kTables = Tables;
            if (kTables == null || kTables.talent == null || _talentId <= 0)
            {
                return null;
            }
            return kTables.talent.GetOrDefault(_talentId);
        }

        public static cfg.GameData_Skill.SkillEnemyData GetSkillEnemy(int _skillId)
        {
            cfg.Tables kTables = Tables;
            if (kTables == null || kTables.skill_enemy == null || _skillId <= 0)
            {
                return null;
            }
            return kTables.skill_enemy.GetOrDefault(_skillId);
        }

        /// <summary>技能冷却（秒）。表里 cd 是 int，做了下限保护</summary>
        public static float GetCd(cfg.GameData_Skill.SkillData _skill)
        {
            if (_skill == null)
            {
                return SkillConst.MinCd;
            }
            return _skill.Cd < SkillConst.MinCd ? SkillConst.MinCd : _skill.Cd;
        }

        #endregion

        #region 效果判定

        /// <summary>
        /// 这条效果是"作用于自身"还是"作用于选中目标"。
        ///
        /// 约定（表里没有 target 列，所以由类型决定）：
        /// - 自身类：attrMod / addState / move / addItem / reload / clearAmmo / flag；
        /// - 目标类：damage / heal / addBuff / removeBuff / aggro / summon。
        ///
        /// 多目标（skill.scope=MultiTarget）时，属性类效果打到全体选中目标（30012 振奋人心），
        /// 单体（SingleTarget）时属性类只打自己（30001 攻击力+2）。
        /// </summary>
        public static bool IsSelfEffect(EffectType _type)
        {
            switch (_type)
            {
                case EffectType.attrMod:
                case EffectType.addState:
                case EffectType.move:
                case EffectType.addItem:
                case EffectType.reload:
                case EffectType.clearAmmo:
                case EffectType.flag:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>效果时长语义换算：duration&lt;0 或 durationType=permanent → 永久（返回 -1）</summary>
        public static float ResolveDuration(cfg.GameData_Skill.EffectData _effect)
        {
            if (_effect == null)
            {
                return 0f;
            }
            if (_effect.DurationType == DurationType.permanent)
            {
                return -1f;
            }
            if (_effect.Duration < 0f)
            {
                return -1f;
            }
            return _effect.Duration;
        }

        /// <summary>把 value / valueType 折算成实际数值（百分比按 base 取）</summary>
        public static int ResolveValue(cfg.GameData_Skill.EffectData _effect, int _baseValue)
        {
            if (_effect == null)
            {
                return 0;
            }

            switch (_effect.ValueType)
            {
                case AttrModType.percent:
                    return Mathf.RoundToInt(_baseValue * _effect.Value / 100f);
                case AttrModType.setTo:
                    return Mathf.RoundToInt(_effect.Value);
                default:
                    return Mathf.RoundToInt(_effect.Value);
            }
        }

        /// <summary>该属性改完之后的值</summary>
        public static int ResolveAttrValue(int _current, cfg.GameData_Skill.EffectData _effect)
        {
            if (_effect == null)
            {
                return _current;
            }

            switch (_effect.ValueType)
            {
                case AttrModType.percent:
                    return _current + Mathf.RoundToInt(_current * _effect.Value / 100f);
                case AttrModType.setTo:
                    return Mathf.RoundToInt(_effect.Value);
                default:
                    return _current + Mathf.RoundToInt(_effect.Value);
            }
        }

        #endregion

        #region 战斗引用

        /// <summary>当前战斗（没有战斗时为 null）</summary>
        public static BattleSystem GetBattle()
        {
            if (!Game.Model.Has<BattleSystem>())
            {
                return null;
            }
            return Game.Model.Get<BattleSystem>();
        }

        /// <summary>取单位（跨战斗/已清场时返回 null）</summary>
        public static BattleUnit GetUnit(int _unitId)
        {
            BattleSystem kBattle = GetBattle();
            return kBattle == null ? null : kBattle.GetUnit(_unitId);
        }

        #endregion
    }
}
