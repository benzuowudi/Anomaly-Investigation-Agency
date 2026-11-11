using System.Collections.Generic;

namespace GameMain.Script.Armory
{
    /// <summary>
    /// 合成配方规则（临时）：没有独立配方表，先用"蓝图名字去掉'蓝图'"去 equip 表里找同名装备。
    /// 例：匕首蓝图(2000) → 匕首(5001)、日本刀蓝图(2001) → 日本刀(5002)。
    /// 以后接 armoryRecipe 表时只需要替换这个类。
    /// </summary>
    public static class ArmoryRecipeRule
    {
        public const string c_BlueprintSuffix = "蓝图";

        public static bool IsBlueprint(cfg.GameData_Item.ItemData _item)
        {
            return _item != null && !string.IsNullOrEmpty(_item.Name) && _item.Name.EndsWith(c_BlueprintSuffix);
        }

        /// <summary>蓝图道具 ID → 装备 ID（找不到返回 0）</summary>
        public static int ResolveEquipId(cfg.Tables _tables, int _blueprintItemId)
        {
            if (_tables == null || _tables.item == null || _tables.equip == null || _blueprintItemId <= 0)
            {
                return 0;
            }

            cfg.GameData_Item.ItemData kItem = _tables.item.GetOrDefault(_blueprintItemId);
            if (!IsBlueprint(kItem))
            {
                return 0;
            }

            string kEquipName = kItem.Name.Substring(0, kItem.Name.Length - c_BlueprintSuffix.Length);
            List<cfg.GameData_Item.EquipData> kEquips = _tables.equip.DataList;
            for (int i = 0; i < kEquips.Count; i++)
            {
                if (kEquips[i] != null && kEquips[i].Name == kEquipName)
                {
                    return kEquips[i].ID;
                }
            }

            return 0;
        }
    }
}
