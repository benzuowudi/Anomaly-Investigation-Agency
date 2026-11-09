namespace GameMain.Script.Battle
{
    /// <summary>
    /// 战斗时钟：把真实时间换算成战斗时间（暂停时返回 0，倍速时放大）。
    ///
    /// 所有计时（ATB 行动条、技能 CD、效果时长、指令 CD）都只认它吐出来的 dt，
    /// 这样"暂停"只需要一处开关，不会漏掉某个计时器。
    /// </summary>
    public class BattleClock
    {
        private float m_Scale = 1f;
        private bool m_Paused;

        public bool IsPaused()
        {
            return m_Paused;
        }

        public float GetScale()
        {
            return m_Scale;
        }

        public void SetScale(float _scale)
        {
            float kValue = _scale <= 0f ? 1f : _scale;
            if (kValue > 3f)
            {
                kValue = 3f;
            }
            m_Scale = kValue;
        }

        public void Pause()
        {
            m_Paused = true;
        }

        public void Resume()
        {
            m_Paused = false;
        }

        public void Reset()
        {
            m_Paused = false;
        }

        /// <summary>
        /// 传入真实 dt，返回本帧可用的战斗 dt（暂停时为 0）。
        /// </summary>
        public float Tick(float _realDt)
        {
            if (m_Paused || _realDt <= 0f)
            {
                return 0f;
            }
            return _realDt * m_Scale;
        }
    }
}
