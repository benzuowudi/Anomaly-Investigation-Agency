using System.Collections.Generic;

namespace GameMain.Script.Battle
{
    /// <summary>
    /// 战斗常量读取器：把 combatConst.xlsx 的 KV 表转成强类型读取。
    ///
    /// 表为空 / 缺 key 时一律回落到 BattleConst 里的默认值，保证"配置没填也能跑"，
    /// 但会在日志里提示一次，避免数值不对时找不到原因。
    /// </summary>
    public class BattleConfig
    {
        private readonly Dictionary<string, float> m_Values = new Dictionary<string, float>();
        private bool m_Loaded;

        public bool IsLoaded()
        {
            return m_Loaded;
        }

        public int GetCount()
        {
            return m_Values.Count;
        }

        /// <summary>
        /// 从配置表装载。重复调用会先清空。
        /// </summary>
        public void Load(cfg.Tables _tables)
        {
            m_Values.Clear();
            m_Loaded = true;

            if (_tables == null || _tables.combatConst == null)
            {
                Game.Log.LogWarning("[战斗] combatConst 表不可用，全部使用代码默认值");
                return;
            }

            List<cfg.GameData_Battle.CombatConstData> kList = _tables.combatConst.DataList;
            for (int i = 0; i < kList.Count; i++)
            {
                cfg.GameData_Battle.CombatConstData kRow = kList[i];
                if (kRow == null || string.IsNullOrEmpty(kRow.Key))
                {
                    continue;
                }
                m_Values[kRow.Key] = kRow.Value;
            }

            if (m_Values.Count == 0)
            {
                Game.Log.LogWarning("[战斗] combatConst 表里没有数据，全部使用代码默认值");
            }
        }

        public float Get(string _key, float _default)
        {
            float kValue;
            return m_Values.TryGetValue(_key, out kValue) ? kValue : _default;
        }

        public int GetInt(string _key, int _default)
        {
            return UnityEngine.Mathf.RoundToInt(Get(_key, _default));
        }

        #region 强类型读取

        /// <summary>每点敏捷的闪避率（默认 1%）</summary>
        public float DodgePerAgi
        {
            get { return Get("dodgePerAgi", 0.01f); }
        }

        /// <summary>闪避上限（默认 75%）</summary>
        public float DodgeCap
        {
            get { return Get("dodgeCap", BattleConst.DodgeCapPerMille / 10000f); }
        }

        /// <summary>重伤倒计时（秒）</summary>
        public float DownedCountdown
        {
            get { return Get("downedCountdown", BattleConst.DownedCountdown); }
        }

        /// <summary>基础移速（米/秒）</summary>
        public float MoveSpeedBase
        {
            get { return Get("moveSpeedBase", BattleConst.MoveSpeedBase); }
        }

        /// <summary>ATB 基础填充系数（1 = 按武器间隔填满）</summary>
        public float AtbFillBase
        {
            get { return Get("atbFillBase", 1f); }
        }

        /// <summary>指令基础响应率（0~1）</summary>
        public float CommandBaseSuccess
        {
            get { return Get("commandBaseSuccess", 1f); }
        }

        /// <summary>决策随机扰动幅度（0~1）</summary>
        public float AiJitter
        {
            get { return Get("aiJitter", 0.05f); }
        }

        /// <summary>集火目标在行动评分里的加成</summary>
        public float FocusScoreBonus
        {
            get { return Get("focusScoreBonus", 200f); }
        }

        #endregion

        public override string ToString()
        {
            return "{ consts:" + m_Values.Count + " }";
        }
    }
}
