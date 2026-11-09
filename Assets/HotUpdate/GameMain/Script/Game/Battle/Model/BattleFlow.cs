using cfg.GameType;

namespace GameMain.Script.Battle
{
    /// <summary>
    /// 战斗流程判定：每帧检查结束条件，返回战斗结果类型。
    ///
    /// 判定优先级：
    ///   1) 双方都全灭   → Lose（同归于尽按失败处理）
    ///   2) 敌方全灭     → Win
    ///   3) 我方全灭     → Lose
    ///   4) 其余         → None（继续打）
    ///
    /// "撤离"（Retreat）不在这里判定：它由 #8 的指令系统驱动，单位越界离场后
    /// 我方存活数为 0 时自然落到 Lose 分支，届时再按"是否有单位成功撤离"改写结果。
    /// </summary>
    public class BattleFlow
    {
        private readonly BattleField m_Field;

        public BattleFlow(BattleField _field)
        {
            m_Field = _field;
        }

        public BattleOutcome CheckEnd()
        {
            if (m_Field == null)
            {
                return BattleOutcome.None;
            }

            int kEffectiveAllies = m_Field.CountEffective(Faction.ally);
            int kEffectiveEnemies = m_Field.CountEffective(Faction.enemy);
            int kLeftAllies = m_Field.CountLeftField(Faction.ally);

            if (kEffectiveAllies == 0 && kEffectiveEnemies == 0)
            {
                return BattleOutcome.Lose;
            }
            if (kEffectiveEnemies == 0)
            {
                return BattleOutcome.Win;
            }
            if (kEffectiveAllies == 0)
            {
                // 有人成功撤出边界 → 撤退；全员战死/重伤滞留 → 失败
                return kLeftAllies > 0 ? BattleOutcome.Retreat : BattleOutcome.Lose;
            }
            return BattleOutcome.None;
        }
    }
}
