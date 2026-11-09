using System.Collections.Generic;
using Framework.Script.Data;
using SimpleJSON;

namespace GameMain.Script.Battle
{
    /// <summary>
    /// 战斗系统的 Data（SystemDataBase）。
    ///
    /// 只存"跨战斗要保留"的东西：战斗偏好（倍速）、最近一场战报摘要。
    /// 战斗内的单位、位置、效果、CD 全部属于 BattleSystem 的运行时状态，不进 Data。
    /// </summary>
    public class BattleData : SystemDataBase
    {
        public const string SaveFileName = "battle.json";

        private const int c_Version = 1;

        private int m_Version = c_Version;
        private float m_TimeScale = 1f;
        private int m_LastOutcome;
        private float m_LastDuration;

        protected override string GetSaveFileName()
        {
            return SaveFileName;
        }

        protected override string GetLogTag()
        {
            return "战斗";
        }

        public override void Clear()
        {
            m_Version = c_Version;
            m_TimeScale = 1f;
            m_LastOutcome = 0;
            m_LastDuration = 0f;
        }

        #region 查询 / 写入

        public int GetVersion()
        {
            return m_Version;
        }

        /// <summary>战斗倍速（1/2/3）</summary>
        public float GetTimeScale()
        {
            return m_TimeScale <= 0f ? 1f : m_TimeScale;
        }

        internal void SetTimeScale(float _scale)
        {
            float kValue = _scale <= 0f ? 1f : _scale;
            if (kValue > 3f)
            {
                kValue = 3f;
            }
            m_TimeScale = kValue;
            m_Version++;
        }

        public int GetLastOutcome()
        {
            return m_LastOutcome;
        }

        public float GetLastDuration()
        {
            return m_LastDuration;
        }

        internal void SetLastBattle(int _outcome, float _duration)
        {
            m_LastOutcome = _outcome;
            m_LastDuration = _duration;
            m_Version++;
        }

        #endregion

        #region 序列化

        protected override string ToJson()
        {
            JSONObject kRoot = new JSONObject();
            kRoot.Add("version", m_Version);
            kRoot.Add("timeScale", m_TimeScale);
            kRoot.Add("lastOutcome", m_LastOutcome);
            kRoot.Add("lastDuration", m_LastDuration);
            return kRoot.ToString();
        }

        protected override bool FromJson(string _json)
        {
            JSONNode kRoot = JSON.Parse(_json);
            if (kRoot == null || !kRoot.IsObject)
            {
                return false;
            }

            m_Version = kRoot["version"] == null ? c_Version : kRoot["version"].AsInt;
            m_TimeScale = kRoot["timeScale"] == null ? 1f : kRoot["timeScale"].AsFloat;
            m_LastOutcome = kRoot["lastOutcome"] == null ? 0 : kRoot["lastOutcome"].AsInt;
            m_LastDuration = kRoot["lastDuration"] == null ? 0f : kRoot["lastDuration"].AsFloat;

            if (m_TimeScale <= 0f)
            {
                m_TimeScale = 1f;
            }
            return true;
        }

        #endregion
    }
}
