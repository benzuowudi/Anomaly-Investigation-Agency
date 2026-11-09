using cfg.GameType;

namespace GameMain.Script.Skill
{
    /// <summary>技能施放成功</summary>
    public class SkillCastMsg
    {
        public int m_UnitId;
        public int m_SkillId;
        /// <summary>本次施放选中的目标（可能为空）</summary>
        public int[] m_TargetUnitIds;

        public SkillCastMsg(int _unitId, int _skillId, int[] _targetUnitIds)
        {
            m_UnitId = _unitId;
            m_SkillId = _skillId;
            m_TargetUnitIds = _targetUnitIds;
        }
    }

    /// <summary>技能没能施放</summary>
    public class SkillDeniedMsg
    {
        public int m_UnitId;
        public int m_SkillId;
        public SkillDenyReason m_Reason;

        public SkillDeniedMsg(int _unitId, int _skillId, SkillDenyReason _reason)
        {
            m_UnitId = _unitId;
            m_SkillId = _skillId;
            m_Reason = _reason;
        }
    }

    /// <summary>效果挂载</summary>
    public class EffectAppliedMsg
    {
        public int m_UnitId;
        public int m_EffectId;
        public EffectSource m_Source;
        public int m_Stacks;
        public float m_Duration;
        public bool m_IsState;

        public EffectAppliedMsg(int _unitId, int _effectId, EffectSource _source,
            int _stacks, float _duration, bool _isState)
        {
            m_UnitId = _unitId;
            m_EffectId = _effectId;
            m_Source = _source;
            m_Stacks = _stacks;
            m_Duration = _duration;
            m_IsState = _isState;
        }
    }

    /// <summary>效果移除</summary>
    public class EffectRemovedMsg
    {
        public int m_UnitId;
        public int m_EffectId;
        public EffectSource m_Source;
        /// <summary>移除原因（到期 / 消耗 / 驱散 / 清场）</summary>
        public string m_Reason;

        public EffectRemovedMsg(int _unitId, int _effectId, EffectSource _source, string _reason)
        {
            m_UnitId = _unitId;
            m_EffectId = _effectId;
            m_Source = _source;
            m_Reason = _reason;
        }
    }

    /// <summary>技能状态被条件命中</summary>
    public class EffectTriggeredMsg
    {
        public int m_UnitId;
        public int m_EffectId;
        public StateTriggerCondition m_Trigger;
        /// <summary>事件当事人（被治疗者 / 攻击者 / 目标）</summary>
        public int m_SubjectUnitId;

        public EffectTriggeredMsg(int _unitId, int _effectId, StateTriggerCondition _trigger, int _subjectUnitId)
        {
            m_UnitId = _unitId;
            m_EffectId = _effectId;
            m_Trigger = _trigger;
            m_SubjectUnitId = _subjectUnitId;
        }
    }
}
