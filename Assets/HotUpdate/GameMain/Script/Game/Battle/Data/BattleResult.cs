using System.Collections.Generic;

namespace GameMain.Script.Battle
{
    /// <summary>
    /// 单个参战单位的战斗结果（战斗层产物，交给 RoleSystem / ExploreSystem 写回）。
    /// </summary>
    public class UnitResult
    {
        public int m_UnitId;
        public int m_SourceId;
        public int m_Hp;
        public int m_San;
        public bool m_Downed;
        public bool m_Dead;
        /// <summary>战斗结束时重伤倒计时的剩余秒数（未死亡时有效）</summary>
        public float m_DownedRemain;

        public override string ToString()
        {
            return "{ unit:" + m_UnitId + " src:" + m_SourceId + " hp:" + m_Hp
                + " san:" + m_San + " downed:" + m_Downed + " dead:" + m_Dead
                + " remain:" + m_DownedRemain.ToString("F1") + " }";
        }
    }

    /// <summary>
    /// 一场战斗的结算产物。战斗层只产出它，不直接写任何别的系统的 Data。
    /// </summary>
    public class BattleResult
    {
        public BattleOutcome m_Outcome = BattleOutcome.None;
        public float m_Duration;
        public int m_EncounterId;
        public int m_BattlefieldId;
        public readonly List<UnitResult> m_Units = new List<UnitResult>();

        public void AddUnit(UnitResult _unit)
        {
            if (_unit != null)
            {
                m_Units.Add(_unit);
            }
        }

        public override string ToString()
        {
            return "{ outcome:" + m_Outcome + " duration:" + m_Duration.ToString("F1")
                + " units:" + m_Units.Count + " }";
        }
    }
}
