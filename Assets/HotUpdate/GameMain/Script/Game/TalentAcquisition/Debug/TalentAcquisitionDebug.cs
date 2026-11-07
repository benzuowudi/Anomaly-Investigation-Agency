#if UNITY_EDITOR
using Framework.Script.Constant;
using GameMain.Script.Data;
using UnityEditor;
using UnityEngine;

namespace GameMain.Script.TalentAcquisition
{
    /// <summary>
    /// 人才招募调试工具：运行游戏后从菜单获得欧元/黄金，方便测试招募和猎头。
    /// 菜单：Framework/人才招募/...
    /// </summary>
    public static class TalentAcquisitionDebug
    {
        [MenuItem("Framework/人才招募/获得 10000 欧元", false, 200)]
        public static void AddEuro()
        {
            if (!CheckRunning())
            {
                return;
            }

            TalentAcquisitionData kData = Game.Data.GetGameData<TalentAcquisitionData>();
            kData.AddEuro(10000);
            Debug.Log("[人才招募] 获得 10000 欧元，当前：" + kData.GetEuro());
        }

        [MenuItem("Framework/人才招募/获得 100 黄金", false, 201)]
        public static void AddGold()
        {
            if (!CheckRunning())
            {
                return;
            }

            TalentAcquisitionData kData = Game.Data.GetGameData<TalentAcquisitionData>();
            kData.AddGold(100);
            Debug.Log("[人才招募] 获得 100 黄金，当前：" + kData.GetGold());
        }

        [MenuItem("Framework/人才招募/重置货币为初始值", false, 202)]
        public static void ResetCurrency()
        {
            if (!CheckRunning())
            {
                return;
            }

            TalentAcquisitionData kData = Game.Data.GetGameData<TalentAcquisitionData>();
            kData.ResetCurrency();
            Debug.Log("[人才招募] 货币已重置：欧元 " + kData.GetEuro() + "，黄金 " + kData.GetGold());
        }

        [MenuItem("Framework/人才招募/打印当前货币", false, 203)]
        public static void LogCurrency()
        {
            if (!CheckRunning())
            {
                return;
            }

            TalentAcquisitionData kData = Game.Data.GetGameData<TalentAcquisitionData>();
            Debug.Log("[人才招募] 欧元：" + kData.GetEuro()
                + "，黄金：" + kData.GetGold()
                + "，猎头剩余：" + kData.GetHeadhunterRemainingSeconds() + "s"
                + "，存档：" + kData.GetSavePath());
        }

        [MenuItem("Framework/人才招募/清除猎头倒计时", false, 204)]
        public static void ClearHeadhunter()
        {
            if (!CheckRunning())
            {
                return;
            }

            TalentAcquisitionData kData = Game.Data.GetGameData<TalentAcquisitionData>();
            kData.ClearHeadhunter();
            Debug.Log("[人才招募] 已清除猎头倒计时");
        }

        private static bool CheckRunning()
        {
            if (!GameConst.m_GameStart)
            {
                Debug.LogError("[人才招募] 请先运行游戏再点这个菜单（GameConst.m_GameStart = false）");
                return false;
            }
            return true;
        }
    }
}
#endif
