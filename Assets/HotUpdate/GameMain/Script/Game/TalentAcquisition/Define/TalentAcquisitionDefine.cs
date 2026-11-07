namespace GameMain.Script.TalentAcquisition
{
    /// <summary>
    /// 人才招募系统的事件主题。
    /// EventManager 以字符串作为契约，所有主题统一收敛到这里，避免字面量散落在业务代码中。
    /// </summary>
    public static class TalentAcquisitionEventDefine
    {
        /// <summary>
        /// 欧元 / 黄金变化（招募消费、调试加钱、重置），参数：CurrencyChangedMsg。
        /// </summary>
        public const string OnCurrencyChanged = "OnCurrencyChanged";
    }

    /// <summary>
    /// 货币变化的事件体（OnCurrencyChanged），只读快照。
    /// </summary>
    public class CurrencyChangedMsg
    {
        public int m_Euro;
        public int m_Gold;

        public CurrencyChangedMsg(int _euro, int _gold)
        {
            m_Euro = _euro;
            m_Gold = _gold;
        }

        public override string ToString()
        {
            return "{ euro:" + m_Euro + ", gold:" + m_Gold + " }";
        }
    }
}
