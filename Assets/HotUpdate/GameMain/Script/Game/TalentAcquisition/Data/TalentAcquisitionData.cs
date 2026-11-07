using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using FairyGUI;
using Framework.Script.Data;
using GameMain.Script.UI;
using SimpleJSON;
using UnityEngine;

namespace GameMain.Script.Data
{
    /// <summary>
    /// 人才招募数据：UI 类注册 + 货币 + 猎头倒计时落盘。
    ///
    /// 货币和猎头结束时间都放在 talent_acquisition.json：
    ///   euro / gold 前端临时货币，等经济系统接入后替换；
    ///   headhunterEndTime 是预计结束的 Unix 时间戳，0 表示没有进行中的猎头。
    /// </summary>
    public class TalentAcquisitionData : GameData
    {
        private const string c_SaveFileName = "talent_acquisition.json";

        // 临时初始货币，等正式经济系统接入后改为从玩家数据读取
        private const int c_DefaultEuro = 18000;
        private const int c_DefaultGold = 100;

        private int m_Euro = c_DefaultEuro;
        private int m_Gold = c_DefaultGold;
        private long m_HeadhunterEndTime;
        private readonly List<string> m_HeadhunterConditions = new List<string>();

        protected internal override void Init()
        {
            base.Init();

            UIObjectFactory.SetPackageItemExtension("ui://TalentAcquisition/TACom", typeof(TACom));
            UIObjectFactory.SetPackageItemExtension("ui://TalentAcquisition/TADescCom", typeof(TADescCom));
            UIObjectFactory.SetPackageItemExtension("ui://TalentAcquisition/TAConditionCom", typeof(TAConditionCom));
            UIObjectFactory.SetPackageItemExtension("ui://TalentAcquisition/TASelectBtn", typeof(TASelectBtn));
            UIObjectFactory.SetPackageItemExtension("ui://TalentAcquisition/TABtn_1", typeof(TABtnCom));
            UIObjectFactory.SetPackageItemExtension("ui://TalentAcquisition/TABtn_2", typeof(TABtn2Com));

            Load();
        }

        #region 货币

        public int GetEuro()
        {
            return m_Euro;
        }

        public int GetGold()
        {
            return m_Gold;
        }

        public bool SpendEuro(int _amount)
        {
            if (_amount <= 0 || m_Euro < _amount)
            {
                return false;
            }
            m_Euro -= _amount;
            Save();
            return true;
        }

        public bool SpendGold(int _amount)
        {
            if (_amount <= 0 || m_Gold < _amount)
            {
                return false;
            }
            m_Gold -= _amount;
            Save();
            return true;
        }

        public void AddEuro(int _amount)
        {
            if (_amount <= 0)
            {
                return;
            }
            m_Euro += _amount;
            Save();
        }

        public void AddGold(int _amount)
        {
            if (_amount <= 0)
            {
                return;
            }
            m_Gold += _amount;
            Save();
        }

        public void SetCurrency(int _euro, int _gold)
        {
            m_Euro = Math.Max(0, _euro);
            m_Gold = Math.Max(0, _gold);
            Save();
        }

        public void ResetCurrency()
        {
            SetCurrency(c_DefaultEuro, c_DefaultGold);
        }

        #endregion

        #region 猎头倒计时

        public bool IsHeadhunterActive()
        {
            return m_HeadhunterEndTime > DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }

        public long GetHeadhunterEndTime()
        {
            return m_HeadhunterEndTime;
        }

        public int GetHeadhunterRemainingSeconds()
        {
            long kNow = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            long kRemaining = m_HeadhunterEndTime - kNow;
            if (kRemaining <= 0)
            {
                return 0;
            }
            return kRemaining > int.MaxValue ? int.MaxValue : (int)kRemaining;
        }

        public void StartHeadhunter(int _seconds, IList<string> _conditions)
        {
            if (_seconds <= 0)
            {
                Game.Log.LogWarning("[人才招募] 猎头时间必须大于 0");
                return;
            }

            m_HeadhunterEndTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + _seconds;
            m_HeadhunterConditions.Clear();
            if (_conditions != null)
            {
                for (int i = 0; i < _conditions.Count; i++)
                {
                    m_HeadhunterConditions.Add(_conditions[i]);
                }
            }
            Save();
            Game.Log.LogInfo("[人才招募] 开始猎头，结束时间：" + m_HeadhunterEndTime);
        }

        public List<string> GetHeadhunterConditions()
        {
            return new List<string>(m_HeadhunterConditions);
        }

        public void ClearHeadhunter()
        {
            m_HeadhunterEndTime = 0;
            m_HeadhunterConditions.Clear();
            Save();
        }

        public string GetSavePath()
        {
            return Application.persistentDataPath + "/GameMain/Data/" + c_SaveFileName;
        }

        private void Save()
        {
            try
            {
                Game.Data.CreatPersistencePath();
                JSONObject kRoot = new JSONObject();
                kRoot.Add("euro", m_Euro);
                kRoot.Add("gold", m_Gold);
                kRoot.Add("headhunterEndTime", m_HeadhunterEndTime);
                JSONArray kConditions = new JSONArray();
                for (int i = 0; i < m_HeadhunterConditions.Count; i++)
                {
                    kConditions.Add(m_HeadhunterConditions[i]);
                }
                kRoot.Add("headhunterConditions", kConditions);
                File.WriteAllText(GetSavePath(), kRoot.ToString(2), Encoding.UTF8);
            }
            catch (Exception _e)
            {
                Game.Log.LogError("[人才招募] 存档失败：" + _e.Message);
            }
        }

        private void Load()
        {
            m_Euro = c_DefaultEuro;
            m_Gold = c_DefaultGold;
            m_HeadhunterEndTime = 0;

            string kPath = GetSavePath();
            if (!File.Exists(kPath))
            {
                return;
            }

            try
            {
                JSONNode kRoot = JSON.Parse(File.ReadAllText(kPath, Encoding.UTF8));
                m_Euro = kRoot["euro"] == null ? c_DefaultEuro : kRoot["euro"].AsInt;
                m_Gold = kRoot["gold"] == null ? c_DefaultGold : kRoot["gold"].AsInt;
                m_HeadhunterEndTime = kRoot["headhunterEndTime"] == null
                    ? 0
                    : kRoot["headhunterEndTime"].AsLong;

                m_HeadhunterConditions.Clear();
                JSONNode kConditions = kRoot["headhunterConditions"];
                if (kConditions != null)
                {
                    for (int i = 0; i < kConditions.Count; i++)
                    {
                        m_HeadhunterConditions.Add(kConditions[i]);
                    }
                }

                if (m_HeadhunterEndTime > 0 && !IsHeadhunterActive())
                {
                    // 保留过期时间戳，等招募界面打开时结算猎头结果
                    Game.Log.LogInfo("[人才招募] 发现待结算的猎头：" + m_HeadhunterEndTime);
                }
                else if (m_HeadhunterEndTime > 0)
                {
                    Game.Log.LogInfo("[人才招募] 读档猎头结束时间：" + m_HeadhunterEndTime);
                }
            }
            catch (Exception _e)
            {
                Game.Log.LogError("[人才招募] 存档解析失败：" + _e.Message);
            }
        }

        #endregion
    }
}
