using cfg.GameType;

namespace GameMain.Script.Skill
{
    /// <summary>
    /// 一个效果实例（运行时值对象，不落盘、不进 Data）。
    ///
    /// 两种形态：
    /// - <b>持续修正</b>（attrMod / addBuff）：挂上去时改数值，到期或被驱散时改回来；
    /// - <b>技能状态</b>（type=addState）：不改数值，只等 stateTrigger 命中后执行一次 payload
    ///   （payload = 该 effect 行的 tickEffectID，见技能文档 §5.1 的落地约定）。
    ///
    /// 归属关系：实例挂在"拥有者"身上（owner = 被加状态的那个单位），
    /// 执行 payload 时伤害来源用 caster，与 owner 可能不同（例如队友给的护盾）。
    /// </summary>
    public class EffectInstance
    {
        /// <summary>效果配置ID（effect.xlsx）</summary>
        public int m_EffectId;
        /// <summary>来源配置ID（技能ID / 天赋ID / 装备ID）</summary>
        public int m_SourceId;
        public EffectSource m_Source = EffectSource.Skill;
        public EffectScopeType m_Scope = EffectScopeType.Battle;

        /// <summary>挂在谁身上</summary>
        public int m_OwnerUnitId;
        /// <summary>是谁放的（伤害/治疗来源）</summary>
        public int m_CasterUnitId;

        public int m_BuffId;
        public int m_Stacks = 1;
        /// <summary>剩余次数（0 = 不按次数消耗）</summary>
        public int m_Charges;
        /// <summary>剩余时长（秒；&lt;0 = 永久，直到被消耗或按作用域清理）</summary>
        public float m_Remain = -1f;
        /// <summary>是否永久（创建时确定；到期倒计时会把 m_Remain 减到负数，不能靠它判断）</summary>
        public bool m_Permanent;

        #region 技能状态（addState）

        public StateTriggerCondition m_Trigger = StateTriggerCondition.immediate;
        /// <summary>条件参数（阈值 / itemType / buffID）</summary>
        public int m_TriggerParam;
        public ConditionLogic m_Logic = ConditionLogic.all;
        /// <summary>触发后执行的效果ID（0 = 只广播事件，不结算）</summary>
        public int m_PayloadEffectId;

        #endregion

        #region 属性修正（attrMod）

        public int m_AttrId;
        /// <summary>挂载时实际施加的增量，移除时按它反算回去</summary>
        public int m_AttrDelta;

        #endregion

        #region 周期效果

        public float m_TickInterval;
        public int m_TickEffectId;
        public float m_TickRemain;

        #endregion

        /// <summary>是否已经失效（等待清理）</summary>
        public bool m_Expired;

        /// <summary>设置时长（&lt;0 记为永久），同时维护 m_Permanent</summary>
        public void SetDuration(float _duration)
        {
            m_Permanent = _duration < 0f;
            m_Remain = m_Permanent ? -1f : _duration;
        }

        public bool IsState()
        {
            return m_Trigger != StateTriggerCondition.immediate;
        }

        public bool IsPermanent()
        {
            return m_Permanent;
        }

        /// <summary>是否由"技能状态"消耗驱动（有次数）</summary>
        public bool HasCharges()
        {
            return m_Charges > 0;
        }

        public float GetDurationOrZero()
        {
            return m_Permanent ? 0f : m_Remain;
        }

        public override string ToString()
        {
            return "{ eff:" + m_EffectId + " src:" + m_SourceId + "/" + m_Source
                + " owner:" + m_OwnerUnitId
                + (m_BuffId > 0 ? " buff:" + m_BuffId : string.Empty)
                + (m_Stacks > 1 ? " x" + m_Stacks : string.Empty)
                + (m_Charges > 0 ? " charges:" + m_Charges : string.Empty)
                + (IsPermanent() ? " 永久" : " remain:" + m_Remain.ToString("F1"))
                + (IsState() ? " trigger:" + m_Trigger : string.Empty)
                + " }";
        }
    }
}
