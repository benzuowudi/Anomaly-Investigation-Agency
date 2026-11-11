using UnityEditor;
using UnityEngine;
using GameMain.Script.Armory;
using GameMain.Script.Inventory;

namespace GameMain.Script.Armory
{
    /// <summary>军械库调试菜单（只在 Play 模式下操作存档）</summary>
    public static class ArmoryDebug
    {
        private static readonly int[] c_TestItemIds =
        {
            2000, 2001, 2002, 2003, 2004, 2005,
            1007, 1008, 1009, 1010, 1012, 1013, 1015, 1016,
        };

        [MenuItem("Framework/军械库/发放测试材料")]
        public static void GrantTestMaterials()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[军械库] 先进入 Play 模式");
                return;
            }

            InventorySystem kInventory = Game.Model.Get<InventorySystem>();
            for (int i = 0; i < c_TestItemIds.Length; i++)
            {
                kInventory.AddItem(c_TestItemIds[i], 5, InventoryChangeReason.Debug);
            }

            Debug.Log("[军械库] 已发放测试材料 x5");
        }

        [MenuItem("Framework/军械库/打印已拥有装备")]
        public static void DumpEquipments()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[军械库] 先进入 Play 模式");
                return;
            }

            ArmorySystem kArmory = Game.Model.Get<ArmorySystem>();
            var kIds = kArmory.GetEquipIds();
            Debug.Log("[军械库] 已拥有装备 " + kIds.Count + " 种："
                + string.Join(", ", kIds));
        }
    }
}
