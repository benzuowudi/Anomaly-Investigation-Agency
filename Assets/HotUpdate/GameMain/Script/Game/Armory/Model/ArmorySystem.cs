using System.Collections.Generic;
using Framework.Script.Model;
using GameMain.Script.Inventory;

namespace GameMain.Script.Armory
{
    /// <summary>
    /// 军械库系统（M 层）：装备合成的唯一业务入口。
    ///
    /// 职责：
    /// - 校验合成材料（1 张蓝图 + 1~3 份材料，且仓库里都有）；
    /// - 扣材料、把产物装备写进 ArmoryData 并存档；
    /// - 给界面提供已拥有装备查询。
    /// </summary>
    public class ArmorySystem : ModelBase
    {
        public const string c_LogTag = "[军械库]";

        private ArmoryData DB
        {
            get { return Game.Data.GetGameData<ArmoryData>(); }
        }

        /// <summary>仓库模型（材料的持有/扣除都走它，军械库自己不存材料）</summary>
        private static InventorySystem Inventory
        {
            get { return Game.Model.Get<InventorySystem>(); }
        }

        #region 生命周期

        protected internal override void OnInit()
        {
        }

        protected internal override void OnStart()
        {
            DB.Load();
        }

        protected internal override void OnDestroy()
        {
        }

        #endregion

        #region 查询

        public List<int> GetEquipIds()
        {
            return DB.GetEquipIds();
        }

        public int GetEquipCount(int _equipId)
        {
            return DB.GetCount(_equipId);
        }

        public bool HasEquip(int _equipId)
        {
            return DB.HasEquip(_equipId);
        }

        #endregion

        #region 合成

        /// <summary>
        /// 用选中的材料合成一件装备。
        /// 规则（临时）：必须正好 1 张蓝图，其余为材料；蓝图名字去掉"蓝图"对应 equip.name。
        /// </summary>
        public bool Craft(IReadOnlyList<int> _itemIds, out int _equipId, out string _error)
        {
            _equipId = 0;
            _error = string.Empty;

            if (_itemIds == null || _itemIds.Count < 2)
            {
                _error = "至少要放入 1 张蓝图和 1 份材料";
                return false;
            }

            cfg.Tables kTables = Game.Data.GetData;
            if (kTables == null || kTables.item == null || kTables.equip == null)
            {
                _error = "配置表未加载";
                return false;
            }

            int kBlueprintId = 0;
            for (int i = 0; i < _itemIds.Count; i++)
            {
                cfg.GameData_Item.ItemData kItem = kTables.item.GetOrDefault(_itemIds[i]);
                if (kItem == null)
                {
                    _error = "材料不存在：" + _itemIds[i];
                    return false;
                }

                if (ArmoryRecipeRule.IsBlueprint(kItem))
                {
                    if (kBlueprintId > 0)
                    {
                        _error = "只能放 1 张蓝图";
                        return false;
                    }
                    kBlueprintId = kItem.ID;
                }

                if (!Inventory.HasItem(kItem.ID))
                {
                    _error = "仓库里没有：" + kItem.Name;
                    return false;
                }
            }

            if (kBlueprintId <= 0)
            {
                _error = "没有放入蓝图";
                return false;
            }

            int kEquipId = ArmoryRecipeRule.ResolveEquipId(kTables, kBlueprintId);
            if (kEquipId <= 0)
            {
                _error = "蓝图没有对应装备：" + kBlueprintId;
                return false;
            }

            // 先全部校验再扣，避免扣到一半失败
            for (int i = 0; i < _itemIds.Count; i++)
            {
                if (!Inventory.RemoveItem(_itemIds[i], 1, InventoryChangeReason.Craft))
                {
                    _error = "扣除材料失败：" + _itemIds[i];
                    return false;
                }
            }

            DB.AddEquip(kEquipId, 1);
            DB.Save();

            _equipId = kEquipId;
            Game.Log.LogInfo(c_LogTag + "合成成功：装备 " + kEquipId + "，消耗材料 " + _itemIds.Count + " 份");
            return true;
        }

        /// <summary>调试/发放用</summary>
        public bool GrantEquip(int _equipId, int _count = 1)
        {
            cfg.Tables kTables = Game.Data.GetData;
            if (kTables == null || kTables.equip == null || kTables.equip.GetOrDefault(_equipId) == null)
            {
                Game.Log.LogWarning(c_LogTag + "发放失败，equip 表没有：" + _equipId);
                return false;
            }

            DB.AddEquip(_equipId, _count);
            DB.Save();
            Game.Log.LogInfo(c_LogTag + "获得装备：" + _equipId + " x" + _count);
            return true;
        }

        #endregion
    }
}
