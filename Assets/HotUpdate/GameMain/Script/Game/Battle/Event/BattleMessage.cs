namespace GameMain.Script.Battle
{
    /// <summary>战斗阶段变化的消息体</summary>
    public class BattlePhaseChangedMsg
    {
        public BattlePhase m_OldPhase;
        public BattlePhase m_NewPhase;

        public BattlePhaseChangedMsg(BattlePhase _oldPhase, BattlePhase _newPhase)
        {
            m_OldPhase = _oldPhase;
            m_NewPhase = _newPhase;
        }

        public override string ToString()
        {
            return "[战斗阶段] " + m_OldPhase + " -> " + m_NewPhase;
        }
    }

    /// <summary>单位生成的消息体</summary>
    public class BattleUnitSpawnedMsg
    {
        public BattleUnit m_Unit;

        public BattleUnitSpawnedMsg(BattleUnit _unit)
        {
            m_Unit = _unit;
        }

        public override string ToString()
        {
            return "[单位入场] " + m_Unit;
        }
    }

    /// <summary>单位状态机切换的消息体</summary>
    public class BattleUnitStateChangedMsg
    {
        public int m_UnitId;
        public UnitBrainState m_OldState;
        public UnitBrainState m_NewState;

        public BattleUnitStateChangedMsg(int _unitId, UnitBrainState _oldState, UnitBrainState _newState)
        {
            m_UnitId = _unitId;
            m_OldState = _oldState;
            m_NewState = _newState;
        }

        public override string ToString()
        {
            return "[单位状态] " + m_UnitId + " " + m_OldState + " -> " + m_NewState;
        }
    }

    /// <summary>单位受到伤害的消息体</summary>
    public class BattleUnitDamagedMsg
    {
        public int m_AttackerId;
        public int m_TargetId;
        public int m_Damage;
        public bool m_IsHit;
        public int m_TargetHpAfter;
        public bool m_IsDowned;

        public BattleUnitDamagedMsg(int _attackerId, int _targetId, int _damage, bool _isHit,
            int _targetHpAfter, bool _isDowned)
        {
            m_AttackerId = _attackerId;
            m_TargetId = _targetId;
            m_Damage = _damage;
            m_IsHit = _isHit;
            m_TargetHpAfter = _targetHpAfter;
            m_IsDowned = _isDowned;
        }

        public override string ToString()
        {
            return "[伤害] " + m_AttackerId + " -> " + m_TargetId
                + " hit:" + m_IsHit + " dmg:" + m_Damage + " hpAfter:" + m_TargetHpAfter
                + " downed:" + m_IsDowned;
        }
    }

    /// <summary>单位死亡的消息体</summary>
    public class BattleUnitDiedMsg
    {
        public int m_UnitId;
        public int m_SourceId;

        public BattleUnitDiedMsg(int _unitId, int _sourceId)
        {
            m_UnitId = _unitId;
            m_SourceId = _sourceId;
        }

        public override string ToString()
        {
            return "[单位死亡] unit:" + m_UnitId + " src:" + m_SourceId;
        }
    }

    /// <summary>战斗结束的消息体</summary>
    public class BattleEndedMsg
    {
        public BattleResult m_Result;

        public BattleEndedMsg(BattleResult _result)
        {
            m_Result = _result;
        }

        public override string ToString()
        {
            return "[战斗结束] " + m_Result;
        }
    }
}
