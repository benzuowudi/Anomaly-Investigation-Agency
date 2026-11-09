using System.Collections.Generic;
using GameMain.Script.Battle;
using UnityEngine;

namespace GameMain.Script.Skill
{
    /// <summary>
    /// 一次技能释放的完整流程（技能文档 §2.6）：
    /// 选目标 → 逐条效果 → 重置 CD / 行动条归零 → 广播。
    ///
    /// v1 没有前摇：skill.xlsx 目前没有 castTime 列，等列补上再把"前摇期"接进 UnitBrain。
    /// </summary>
    public class SkillCastFlow
    {
        private readonly SkillSystem m_System;
        private readonly SkillTargetSelector m_Selector;

        public SkillCastFlow(SkillSystem _system, SkillTargetSelector _selector)
        {
            m_System = _system;
            m_Selector = _selector;
        }

        /// <summary>对单位施放它的主动技能（调用前必须已过 SkillTriggerChecker）</summary>
        public SkillCastResult Cast(BattleUnit _caster)
        {
            if (_caster == null)
            {
                return SkillCastResult.Failed;
            }

            cfg.GameData_Skill.SkillData kSkill = SkillRule.GetSkill(_caster.m_ActiveSkillId);
            if (kSkill == null || kSkill.Effect == null || kSkill.Effect.Count == 0)
            {
                return SkillCastResult.Failed;
            }

            List<BattleUnit> kTargets = m_Selector.Select(_caster, kSkill);

            int kApplied = 0;
            for (int i = 0; i < kSkill.Effect.Count; i++)
            {
                cfg.GameData_Skill.EffectData kEffect = SkillRule.GetEffect(kSkill.Effect[i]);
                if (kEffect == null)
                {
                    Game.Log.LogWarning("[技能] 技能 " + kSkill.ID + " 引用的效果不存在：" + kSkill.Effect[i]);
                    continue;
                }
                kApplied += m_System.Resolver.Execute(_caster, kTargets, kEffect, kSkill.ID, EffectSource.Skill);
            }

            // CD 从施放瞬间起算；行动条归零，避免"技能+普攻"二连
            m_System.ResetSkillCooldown(_caster, kSkill);
            _caster.m_ActionGauge = 0f;
            _caster.m_SkillUseCount++;

            int[] kTargetIds = new int[kTargets.Count];
            for (int i = 0; i < kTargets.Count; i++)
            {
                kTargetIds[i] = kTargets[i].m_UnitId;
            }

            Game.Event.SendMessage(SkillEvent.OnSkillCast, new SkillCastMsg(_caster.m_UnitId, kSkill.ID, kTargetIds));
            Game.Log.LogInfo("[技能] " + _caster.m_UnitId + " 施放 " + kSkill.Name + "(" + kSkill.ID + ")"
                + " 目标:" + JoinIds(kTargetIds) + " 效果数:" + kApplied
                + " 下次CD:" + SkillRule.GetCd(kSkill).ToString("F1") + "s");

            return SkillCastResult.Cast;
        }

        private static string JoinIds(int[] _ids)
        {
            if (_ids == null || _ids.Length == 0)
            {
                return "无";
            }

            string kResult = string.Empty;
            for (int i = 0; i < _ids.Length; i++)
            {
                if (i > 0)
                {
                    kResult += ",";
                }
                kResult += _ids[i];
            }
            return kResult;
        }
    }
}
