using cfg.GameType;
using GameMain.Script.Battle;

namespace GameMain.Script.Skill
{
    /// <summary>
    /// 施放前的"能不能放"硬性校验（技能文档 §2.1）。
    ///
    /// 注意这是校验，不是决策：技能没有"要不要放"的判断，CD 到点就放，
    /// 只有放不了（死了/被控/没目标/次数用完）才跳过，并且 CD 保持 0、下一帧重试。
    /// </summary>
    public class SkillTriggerChecker
    {
        private readonly SkillTargetSelector m_Selector;

        public SkillTriggerChecker(SkillTargetSelector _selector)
        {
            m_Selector = _selector;
        }

        /// <summary>校验；不通过时通过 _reason 说明原因（只进日志与 UI 提示）</summary>
        public bool Check(BattleUnit _caster, out SkillDenyReason _reason, out cfg.GameData_Skill.SkillData _skill)
        {
            _reason = SkillDenyReason.None;
            _skill = null;

            if (_caster == null)
            {
                _reason = SkillDenyReason.NoSkill;
                return false;
            }

            if (_caster.m_State == UnitBrainState.Dead)
            {
                _reason = SkillDenyReason.Dead;
                return false;
            }
            if (_caster.m_State == UnitBrainState.Downed)
            {
                _reason = SkillDenyReason.Downed;
                return false;
            }
            if (_caster.m_State == UnitBrainState.Controlled)
            {
                _reason = SkillDenyReason.Controlled;
                return false;
            }
            if (_caster.m_LeftField)
            {
                _reason = SkillDenyReason.NotInBattle;
                return false;
            }

            if (_caster.m_ActiveSkillId <= 0)
            {
                _reason = SkillDenyReason.NoSkill;
                return false;
            }

            _skill = SkillRule.GetSkill(_caster.m_ActiveSkillId);
            if (_skill == null)
            {
                _reason = SkillDenyReason.NoSkill;
                return false;
            }

            // 表还没填效果：不算"能放"，但也不该每帧刷日志（CD 照走，见 SkillSystem）
            if (_skill.Effect == null || _skill.Effect.Count == 0)
            {
                _reason = SkillDenyReason.NoEffect;
                return false;
            }

            if (SkillTargetSelector.NeedTarget(_skill))
            {
                if (m_Selector.Select(_caster, _skill).Count == 0)
                {
                    _reason = SkillDenyReason.NoTarget;
                    return false;
                }
            }

            return true;
        }
    }
}
