namespace GameMain.Script.Battle
{
    /// <summary>
    /// 战斗结算：把战场状态汇总成 BattleResult。
    ///
    /// 只做"数据汇总"，不写别的系统的 Data：
    /// 角色血量/重伤剩余/死亡标记的落库，由上层（ExploreSystem / RoleSystem）拿到
    /// BattleResult 后调用各自的 Model 公开接口完成。
    /// </summary>
    public static class BattleSettlement
    {
        public static BattleResult Build(BattleField _field, BattleOutcome _outcome, float _duration, int _encounterId)
        {
            BattleResult kResult = new BattleResult();
            kResult.m_Outcome = _outcome;
            kResult.m_Duration = _duration;
            kResult.m_EncounterId = _encounterId;

            if (_field == null || _field.GetUnits() == null)
            {
                return kResult;
            }

            for (int i = 0; i < _field.GetUnits().Count; i++)
            {
                BattleUnit kUnit = _field.GetUnits()[i];
                if (kUnit == null)
                {
                    continue;
                }

                UnitResult kUnitResult = new UnitResult();
                kUnitResult.m_UnitId = kUnit.m_UnitId;
                kUnitResult.m_SourceId = kUnit.m_SourceId;
                kUnitResult.m_Hp = kUnit.m_Hp;
                kUnitResult.m_San = kUnit.m_San;
                kUnitResult.m_Dead = kUnit.m_State == UnitBrainState.Dead;
                kUnitResult.m_Downed = kUnit.m_State == UnitBrainState.Downed;
                kUnitResult.m_DownedRemain = kUnit.m_DownedRemain;
                kResult.AddUnit(kUnitResult);
            }

            return kResult;
        }
    }
}
