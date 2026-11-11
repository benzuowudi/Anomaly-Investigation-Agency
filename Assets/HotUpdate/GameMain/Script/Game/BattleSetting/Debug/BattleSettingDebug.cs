#if UNITY_EDITOR

using Framework.Script.UI;
using UnityEditor;
using UnityEngine;

namespace GameMain.Script.BattleSetting
{
    /// <summary>
    /// 战前整备系统的编辑器调试入口（整文件只在编辑器编译，不进包体）。
    ///
    /// 菜单：Framework/战前整备/…
    /// 所有操作都要求运行中（要访问 ModelManager 里的模型实例）。
    /// </summary>
    public static class BattleSettingDebug
    {
        private const string c_Menu = "Framework/战前整备/";

        [MenuItem(c_Menu + "打开界面（运行中）", false, 100)]
        public static void OpenView()
        {
            if (!CheckPlaying())
            {
                return;
            }

            Game.UI.ShowWindow(UIDefine.BattleSetting);
        }

        [MenuItem(c_Menu + "打印战前配置快照", false, 101)]
        public static void PrintSnapshot()
        {
            if (!CheckPlaying())
            {
                return;
            }

            BattleSettingSnapshot kSnapshot = Game.Model.Get<BattleSettingSystem>().BuildSnapshot();
            Debug.Log("[战前整备] 快照：" + kSnapshot);
            for (int i = 0; i < kSnapshot.m_Team.Count; i++)
            {
                BattleSettingTeamEntry kEntry = kSnapshot.m_Team[i];
                string kName = kEntry.m_Role == null ? "<角色缺失>" : kEntry.m_Role.m_Role.GetDisplayName();
                Debug.Log("  队员 " + (i + 1) + "：" + kName + "（" + kEntry.m_RoleId + "），站位 "
                    + kEntry.m_Position.ToString("F2") + " → 战场坐标 " + kEntry.m_FieldPosition.ToString("F1"));
            }

            for (int i = 0; i < kSnapshot.m_MuleItems.Count; i++)
            {
                BattleSettingItemSlot kSlot = kSnapshot.m_MuleItems[i];
                Debug.Log("  机械骡 " + (i + 1) + "：" + (kSlot.IsEmpty ? "空" : kSlot.m_ItemId + " ×" + kSlot.m_Count));
            }

            for (int i = 0; i < kSnapshot.m_SupportItems.Count; i++)
            {
                BattleSettingItemSlot kSlot = kSnapshot.m_SupportItems[i];
                Debug.Log("  支援 " + (i + 1) + "：" + (kSlot.IsEmpty ? "空" : kSlot.m_ItemId + " ×" + kSlot.m_Count));
            }

            Debug.Log("[战前整备] 可否开战：" + kSnapshot.m_CanStart +
                (kSnapshot.m_CanStart ? string.Empty : "（" + kSnapshot.m_BlockReason + "）"));
        }

        [MenuItem(c_Menu + "按名册重建队伍（前 5 人）", false, 102)]
        public static void RebuildTeam()
        {
            if (!CheckPlaying())
            {
                return;
            }

            Game.Model.Get<BattleSettingSystem>().RebuildTeamFromRoster();
            Debug.Log("[战前整备] 已按名册重建队伍");
        }

        [MenuItem(c_Menu + "把第一件物资放进机械骡", false, 103)]
        public static void FillFirstMuleSlot()
        {
            if (!CheckPlaying())
            {
                return;
            }

            System.Collections.Generic.List<Inventory.InventoryItemStack> kItems =
                Game.Model.Get<Inventory.InventorySystem>().GetAllItems();
            if (kItems == null || kItems.Count == 0)
            {
                Debug.LogWarning("[战前整备] 仓库里没有物品");
                return;
            }

            bool kOk = Game.Model.Get<BattleSettingSystem>()
                .SetItem(BattleSettingSupplySlot.Mule, 0, kItems[0].m_ItemId);
            Debug.Log("[战前整备] 放入机械骡 1 号格：" + (kOk ? "成功" : "失败") + "（物品 " + kItems[0].m_ItemId + "）");
        }

        [MenuItem(c_Menu + "直接开战（运行中）", false, 104)]
        public static void ConfirmBattle()
        {
            if (!CheckPlaying())
            {
                return;
            }

            bool kOk = Game.Model.Get<BattleSettingSystem>().ConfirmSetting();
            Debug.Log("[战前整备] 直接开战：" + (kOk ? "成功" : "失败"));
        }

        [MenuItem(c_Menu + "重置战前配置（删档）", false, 105)]
        public static void ResetSetting()
        {
            if (!CheckPlaying())
            {
                return;
            }

            Game.Model.Get<BattleSettingSystem>().Reset();
            Debug.Log("[战前整备] 已重置战前配置");
        }

        private static bool CheckPlaying()
        {
            if (Application.isPlaying)
            {
                return true;
            }

            Debug.LogWarning("[战前整备] 该操作需要先运行游戏（模型只在运行时创建）");
            return false;
        }
    }
}

#endif
