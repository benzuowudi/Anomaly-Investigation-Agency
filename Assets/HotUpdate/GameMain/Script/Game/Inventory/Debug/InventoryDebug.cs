#if UNITY_EDITOR

using Framework.Script.UI;
using GameMain.Script.Armory;
using UnityEditor;
using UnityEngine;

namespace GameMain.Script.Inventory
{
    /// <summary>
    /// 仓库（Inventory）的编辑器调试入口（整文件只在编辑器编译，不进包体）。
    ///
    /// 菜单：Framework/仓库/…
    /// 所有操作都要求运行中（要访问 ModelManager 里的模型实例）。
    /// </summary>
    public static class InventoryDebug
    {
        private const string c_Menu = "Framework/仓库/";

        /// <summary>测试道具：材料（1005~1023）+ 蓝图（2000~2005）</summary>
        private static readonly int[] c_TestItemIds =
        {
            1005, 1006, 1007, 1008, 1009, 1010, 1011, 1012, 1013, 1014,
            1015, 1016, 1017, 1018, 1019, 1020, 1021, 1022, 1023,
            2000, 2001, 2002, 2003, 2004, 2005,
        };

        /// <summary>测试装备（军械库合成产物同款）</summary>
        private static readonly int[] c_TestEquipIds = { 5001, 5003, 5007 };

        [MenuItem(c_Menu + "打开界面（运行中）", false, 100)]
        public static void OpenView()
        {
            if (!CheckPlaying())
            {
                return;
            }

            Game.UI.ShowWindow(UIDefine.Inventory);
        }

        [MenuItem(c_Menu + "发放测试道具（材料/蓝图各 5）", false, 101)]
        public static void GrantTestItems()
        {
            if (!CheckPlaying())
            {
                return;
            }

            InventorySystem kInventory = Game.Model.Get<InventorySystem>();
            int kSuccess = 0;
            for (int i = 0; i < c_TestItemIds.Length; i++)
            {
                if (kInventory.AddItem(c_TestItemIds[i], 5, InventoryChangeReason.Debug))
                {
                    kSuccess++;
                }
            }

            Debug.Log(InventoryDefine.c_LogTag + "已发放测试道具 " + kSuccess + "/" + c_TestItemIds.Length + " 种（每种 5 个）");
        }

        [MenuItem(c_Menu + "发放测试装备（3 件）", false, 102)]
        public static void GrantTestEquips()
        {
            if (!CheckPlaying())
            {
                return;
            }

            ArmorySystem kArmory = Game.Model.Get<ArmorySystem>();
            for (int i = 0; i < c_TestEquipIds.Length; i++)
            {
                kArmory.GrantEquip(c_TestEquipIds[i], 1);
            }

            Debug.Log(InventoryDefine.c_LogTag + "已发放测试装备 " + c_TestEquipIds.Length + " 件");
        }

        [MenuItem(c_Menu + "打印仓库快照", false, 103)]
        public static void PrintSnapshot()
        {
            if (!CheckPlaying())
            {
                return;
            }

            InventorySystem kInventory = Game.Model.Get<InventorySystem>();
            Debug.Log(kInventory.Dump());

            InventorySnapshot kSnapshot = kInventory.BuildSnapshot(InventoryTab.Item);
            Debug.Log(InventoryDefine.c_LogTag + "道具页签：" + kSnapshot);
            for (int i = 0; i < kSnapshot.m_Items.Count; i++)
            {
                InventoryEntry kEntry = kSnapshot.m_Items[i];
                Debug.Log("  道具 " + (i + 1) + "：" + kEntry.m_Name + "（" + kEntry.m_Id + "）x" + kEntry.m_Count
                    + "，品质 " + kEntry.m_Quality);
            }

            InventorySnapshot kEquipSnapshot = kInventory.BuildSnapshot(InventoryTab.Equip);
            Debug.Log(InventoryDefine.c_LogTag + "装备页签：" + kEquipSnapshot);
            for (int i = 0; i < kEquipSnapshot.m_Items.Count; i++)
            {
                InventoryEntry kEntry = kEquipSnapshot.m_Items[i];
                Debug.Log("  装备 " + (i + 1) + "：" + kEntry.m_Name + "（" + kEntry.m_Id + "）x" + kEntry.m_Count
                    + "，品质 " + kEntry.m_Quality);
            }
        }

        [MenuItem(c_Menu + "清空仓库", false, 104)]
        public static void ClearInventory()
        {
            if (!CheckPlaying())
            {
                return;
            }

            Game.Model.Get<InventorySystem>().ClearAll(InventoryChangeReason.Debug);
            Debug.Log(InventoryDefine.c_LogTag + "已清空仓库（容量保持不变）");
        }

        [MenuItem(c_Menu + "删除仓库存档", false, 105)]
        public static void DeleteSave()
        {
            if (!CheckPlaying())
            {
                return;
            }

            bool kOk = Game.Model.Get<InventorySystem>().DeleteSave();
            Debug.Log(InventoryDefine.c_LogTag + "删除存档：" + (kOk ? "成功" : "没有存档"));
        }

        private static bool CheckPlaying()
        {
            if (Application.isPlaying)
            {
                return true;
            }

            Debug.LogWarning(InventoryDefine.c_LogTag + "该操作需要先运行游戏（模型只在运行时创建）");
            return false;
        }
    }
}

#endif
