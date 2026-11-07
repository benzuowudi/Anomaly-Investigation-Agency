using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace GameMain.Script.Warehouse
{
    /// <summary>
    /// 仓库系统的对外接口（业务侧只用这个类，不直接碰 WarehouseData）。
    ///
    /// 数据流：
    ///   业务调用 AddItem/RemoveItem/SetItemCount
    ///     → 发送 WarehouseEventDefine.OnItemUpdate（ItemUpdateMsg）
    ///     → WarehouseData 监听并写入权威数据（校验 item 表、数量、格子、负重）
    ///     → 广播 OnItemChanged / OnWarehouseChanged 给 UI
    ///     → 结果回填到消息体，这里同步返回成功与否
    ///
    /// 查询类接口直接读权威数据库，不产生任何副作用。
    /// </summary>
    public static class WarehouseSystem
    {
        /// <summary>存档文件名（位于 Application.persistentDataPath/GameMain/Data 下）</summary>
        private const string c_SaveFileName = "warehouse.json";

        private static WarehouseData DB
        {
            get { return Game.Data.GetGameData<WarehouseData>(); }
        }

        #region 查询接口

        /// <summary>
        /// 查询道具数量（item 表中不存在时返回 0，不报错）
        /// </summary>
        public static int GetItemCount(int _itemId)
        {
            return DB.GetCount(_itemId);
        }

        /// <summary>
        /// 是否持有指定数量，默认判断是否至少有 1 个
        /// </summary>
        public static bool HasItem(int _itemId, int _count = 1)
        {
            return DB.HasItem(_itemId, _count);
        }

        /// <summary>
        /// 判断道具ID是否在 item 表中配置
        /// </summary>
        public static bool IsKnownItem(int _itemId)
        {
            return WarehouseData.IsKnownItem(_itemId);
        }

        /// <summary>
        /// 取 item 表配置，找不到返回 null
        /// </summary>
        public static cfg.GameData_Item.ItemData GetItemConfig(int _itemId)
        {
            return WarehouseData.GetConfig(_itemId);
        }

        /// <summary>
        /// 取道具中文名（找不到配置时返回 "未知道具(id)"）
        /// </summary>
        public static string GetItemName(int _itemId)
        {
            cfg.GameData_Item.ItemData kConfig = WarehouseData.GetConfig(_itemId);
            return kConfig == null ? "未知道具(" + _itemId + ")" : kConfig.NameCn;
        }

        /// <summary>
        /// 导出全部持有中的道具（数量 > 0，按 item 表顺序）
        /// </summary>
        public static List<WarehouseItemStack> GetAllItems()
        {
            return DB.GetSnapshot();
        }

        /// <summary>
        /// 按分组类型（item.type_group）筛选持有中的道具
        /// </summary>
        public static List<WarehouseItemStack> GetItemsByGroup(int _typeGroup)
        {
            return FilterItems(delegate (cfg.GameData_Item.ItemData _config)
            {
                return _config.TypeGroup == _typeGroup;
            });
        }

        /// <summary>
        /// 按展示类型（item.type_show）筛选持有中的道具
        /// </summary>
        public static List<WarehouseItemStack> GetItemsByShowType(int _typeShow)
        {
            return FilterItems(delegate (cfg.GameData_Item.ItemData _config)
            {
                return _config.TypeShow == _typeShow;
            });
        }

        private static List<WarehouseItemStack> FilterItems(System.Func<cfg.GameData_Item.ItemData, bool> _match)
        {
            List<WarehouseItemStack> kResult = new List<WarehouseItemStack>();
            List<WarehouseItemStack> kAll = DB.GetSnapshot();
            for (int i = 0; i < kAll.Count; i++)
            {
                cfg.GameData_Item.ItemData kConfig = WarehouseData.GetConfig(kAll[i].m_ItemId);
                if (kConfig != null && _match(kConfig))
                {
                    kResult.Add(kAll[i]);
                }
            }
            return kResult;
        }

        /// <summary>
        /// 持有道具的总数量（各道具数量之和）
        /// </summary>
        public static int GetTotalCount()
        {
            int kTotal = 0;
            List<WarehouseItemStack> kAll = DB.GetSnapshot();
            for (int i = 0; i < kAll.Count; i++)
            {
                kTotal += kAll[i].m_Count;
            }
            return kTotal;
        }

        /// <summary>
        /// 已占用格子数（道具种类数）
        /// </summary>
        public static int GetUsedSlots()
        {
            return DB.GetUsedSlots();
        }

        /// <summary>
        /// 当前总重量
        /// </summary>
        public static int GetTotalWeight()
        {
            return DB.GetTotalWeight();
        }

        /// <summary>
        /// 格子上限（0 表示不限制）
        /// </summary>
        public static int GetMaxSlots()
        {
            return DB.GetMaxSlots();
        }

        /// <summary>
        /// 负重上限（0 表示不限制）
        /// </summary>
        public static int GetMaxWeight()
        {
            return DB.GetMaxWeight();
        }

        /// <summary>
        /// 数据版本号（每次成功变更 +1）
        /// </summary>
        public static long GetVersion()
        {
            return DB.GetVersion();
        }

        /// <summary>
        /// 是否还能再放入指定数量（只做校验，不写库）
        /// </summary>
        public static bool CanAdd(int _itemId, int _count)
        {
            if (_count <= 0 || !WarehouseData.IsKnownItem(_itemId))
            {
                return false;
            }
            if (GetMaxSlots() > 0 && GetItemCount(_itemId) == 0 && GetUsedSlots() + 1 > GetMaxSlots())
            {
                return false;
            }
            if (GetMaxWeight() > 0)
            {
                cfg.GameData_Item.ItemData kConfig = WarehouseData.GetConfig(_itemId);
                long kWeight = (long)GetTotalWeight() + (long)kConfig.Weight * _count;
                if (kWeight > GetMaxWeight())
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// 是否扣得动指定数量（只做校验，不写库）
        /// </summary>
        public static bool CanRemove(int _itemId, int _count)
        {
            return _count > 0 && GetItemCount(_itemId) >= _count;
        }

        #endregion

        #region 变更接口

        /// <summary>
        /// 增加道具。成功返回 true；道具不存在 / 数量非法 / 超容量都返回 false 并打日志。
        /// </summary>
        public static bool AddItem(int _itemId, int _count, ItemChangeReason _reason = ItemChangeReason.None)
        {
            return SendUpdate(new ItemUpdateMsg(_itemId, ItemChangeOp.Add, _count, _reason));
        }

        /// <summary>
        /// 扣除道具。数量不足返回 false，权威数据保持不变。
        /// </summary>
        public static bool RemoveItem(int _itemId, int _count, ItemChangeReason _reason = ItemChangeReason.None)
        {
            return SendUpdate(new ItemUpdateMsg(_itemId, ItemChangeOp.Remove, _count, _reason));
        }

        /// <summary>
        /// 直接把数量设置为指定值（0 表示清空该道具）。
        /// </summary>
        public static bool SetItemCount(int _itemId, int _count, ItemChangeReason _reason = ItemChangeReason.None)
        {
            return SendUpdate(new ItemUpdateMsg(_itemId, ItemChangeOp.Set, _count, _reason));
        }

        /// <summary>
        /// 清空单个道具
        /// </summary>
        public static bool ClearItem(int _itemId, ItemChangeReason _reason = ItemChangeReason.None)
        {
            return SetItemCount(_itemId, 0, _reason);
        }

        /// <summary>
        /// 批量增加，返回成功的条数。单条失败不影响其它条。
        /// </summary>
        public static int AddItems(IList<WarehouseItemStack> _stacks, ItemChangeReason _reason = ItemChangeReason.None)
        {
            if (_stacks == null)
            {
                return 0;
            }
            int kSuccess = 0;
            for (int i = 0; i < _stacks.Count; i++)
            {
                if (_stacks[i] == null || _stacks[i].m_Count <= 0)
                {
                    continue;
                }
                if (AddItem(_stacks[i].m_ItemId, _stacks[i].m_Count, _reason))
                {
                    kSuccess++;
                }
            }
            return kSuccess;
        }

        /// <summary>
        /// 批量扣除，返回是否全部成功。任意一条失败时不做回滚，请用 TryExchange 做原子交易。
        /// </summary>
        public static bool RemoveItems(IList<WarehouseItemStack> _stacks, ItemChangeReason _reason = ItemChangeReason.None)
        {
            if (_stacks == null || _stacks.Count == 0)
            {
                return false;
            }
            for (int i = 0; i < _stacks.Count; i++)
            {
                if (_stacks[i] == null || _stacks[i].m_Count <= 0)
                {
                    continue;
                }
                if (!RemoveItem(_stacks[i].m_ItemId, _stacks[i].m_Count, _reason))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// 兑换：先扣 cost 再发 gain，任一步失败则整体回滚（原子操作）。
        /// </summary>
        public static bool TryExchange(
            IList<WarehouseItemStack> _cost,
            IList<WarehouseItemStack> _gain,
            ItemChangeReason _reason = ItemChangeReason.Exchange)
        {
            Dictionary<int, int> kCost = Aggregate(_cost);
            Dictionary<int, int> kGain = Aggregate(_gain);
            if (kCost.Count == 0 && kGain.Count == 0)
            {
                return false;
            }

            // 1. 先整体校验成本，避免扣到一半才发现不够
            Dictionary<int, int>.Enumerator kCheck = kCost.GetEnumerator();
            while (kCheck.MoveNext())
            {
                if (!CanRemove(kCheck.Current.Key, kCheck.Current.Value))
                {
                    Game.Log.LogWarning("[仓库] 兑换失败，道具不足：" + kCheck.Current.Key
                        + " 需要 " + kCheck.Current.Value + " 实际 " + GetItemCount(kCheck.Current.Key));
                    return false;
                }
            }

            // 2. 扣除
            List<WarehouseItemStack> kRemoved = new List<WarehouseItemStack>();
            Dictionary<int, int>.Enumerator kCostE = kCost.GetEnumerator();
            while (kCostE.MoveNext())
            {
                if (RemoveItem(kCostE.Current.Key, kCostE.Current.Value, _reason))
                {
                    kRemoved.Add(new WarehouseItemStack(kCostE.Current.Key, kCostE.Current.Value));
                }
                else
                {
                    RestoreItems(kRemoved, _reason);
                    return false;
                }
            }

            // 3. 发放
            List<WarehouseItemStack> kAdded = new List<WarehouseItemStack>();
            Dictionary<int, int>.Enumerator kGainE = kGain.GetEnumerator();
            while (kGainE.MoveNext())
            {
                if (AddItem(kGainE.Current.Key, kGainE.Current.Value, _reason))
                {
                    kAdded.Add(new WarehouseItemStack(kGainE.Current.Key, kGainE.Current.Value));
                }
                else
                {
                    // 发放失败：撤销已发放的，退回已扣除的
                    RemoveItems(kAdded, _reason);
                    RestoreItems(kRemoved, _reason);
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// 清空整个仓库（调试/新档用）
        /// </summary>
        public static void ClearAll(ItemChangeReason _reason = ItemChangeReason.Debug)
        {
            DB.ClearAll(_reason);
        }

        /// <summary>
        /// 设置容量上限，0 表示不限制
        /// </summary>
        public static void SetCapacity(int _maxSlots, int _maxWeight)
        {
            DB.SetCapacity(_maxSlots, _maxWeight);
        }

        private static bool SendUpdate(ItemUpdateMsg _msg)
        {
            Game.Event.SendMessage(WarehouseEventDefine.OnItemUpdate, _msg);

            if (_msg.m_Result == ItemChangeResult.Pending)
            {
                Game.Log.LogError("[仓库] OnItemUpdate 没有监听者，变更未生效：" + _msg);
                return false;
            }
            if (_msg.m_Result != ItemChangeResult.Success)
            {
                Game.Log.LogWarning("[仓库] 道具变更失败：" + _msg);
                return false;
            }
            return true;
        }

        private static Dictionary<int, int> Aggregate(IList<WarehouseItemStack> _stacks)
        {
            Dictionary<int, int> kResult = new Dictionary<int, int>();
            if (_stacks == null)
            {
                return kResult;
            }
            for (int i = 0; i < _stacks.Count; i++)
            {
                WarehouseItemStack kStack = _stacks[i];
                if (kStack == null || kStack.m_Count <= 0)
                {
                    continue;
                }
                int kOld;
                kResult.TryGetValue(kStack.m_ItemId, out kOld);
                kResult[kStack.m_ItemId] = kOld + kStack.m_Count;
            }
            return kResult;
        }

        private static void RestoreItems(List<WarehouseItemStack> _items, ItemChangeReason _reason)
        {
            for (int i = 0; i < _items.Count; i++)
            {
                AddItem(_items[i].m_ItemId, _items[i].m_Count, _reason);
            }
        }

        #endregion

        #region 存档

        /// <summary>
        /// 存档路径
        /// </summary>
        public static string GetSavePath()
        {
            return Application.persistentDataPath + "/GameMain/Data/" + c_SaveFileName;
        }

        /// <summary>
        /// 保存仓库到本地（JSON）。失败返回 false 并打日志。
        /// </summary>
        public static bool Save()
        {
            try
            {
                Game.Data.CreatPersistencePath();
                File.WriteAllText(GetSavePath(), DB.ToJson(), Encoding.UTF8);
                Game.Log.LogInfo("[仓库] 存档完成：" + GetSavePath());
                return true;
            }
            catch (System.Exception _e)
            {
                Game.Log.LogError("[仓库] 存档失败：" + _e.Message);
                return false;
            }
        }

        /// <summary>
        /// 从本地读档。文件不存在返回 false（不算错误）。
        /// </summary>
        public static bool Load()
        {
            string kPath = GetSavePath();
            if (!File.Exists(kPath))
            {
                Game.Log.LogInfo("[仓库] 没有找到存档：" + kPath);
                return false;
            }
            try
            {
                string kJson = File.ReadAllText(kPath, Encoding.UTF8);
                bool kOk = DB.FromJson(kJson);
                if (kOk)
                {
                    Game.Log.LogInfo("[仓库] 读档完成：" + kPath);
                }
                return kOk;
            }
            catch (System.Exception _e)
            {
                Game.Log.LogError("[仓库] 读档失败：" + _e.Message);
                return false;
            }
        }

        /// <summary>
        /// 删除本地存档
        /// </summary>
        public static bool DeleteSave()
        {
            string kPath = GetSavePath();
            if (!File.Exists(kPath))
            {
                return false;
            }
            try
            {
                File.Delete(kPath);
                return true;
            }
            catch (System.Exception _e)
            {
                Game.Log.LogError("[仓库] 删除存档失败：" + _e.Message);
                return false;
            }
        }

        #endregion

        #region 调试

        /// <summary>
        /// 生成一份可读的仓库快照，便于打日志排查
        /// </summary>
        public static string Dump()
        {
            StringBuilder kBuilder = new StringBuilder();
            kBuilder.Append("[仓库] version=").Append(DB.GetVersion())
                .Append(" slots=").Append(GetUsedSlots()).Append("/").Append(GetMaxSlots() > 0 ? GetMaxSlots().ToString() : "∞")
                .Append(" weight=").Append(GetTotalWeight()).Append("/").Append(GetMaxWeight() > 0 ? GetMaxWeight().ToString() : "∞");

            List<WarehouseItemStack> kAll = DB.GetSnapshot();
            if (kAll.Count == 0)
            {
                kBuilder.Append("（空）");
                return kBuilder.ToString();
            }
            for (int i = 0; i < kAll.Count; i++)
            {
                cfg.GameData_Item.ItemData kConfig = WarehouseData.GetConfig(kAll[i].m_ItemId);
                string kName = kConfig == null ? "?" : kConfig.NameCn;
                kBuilder.Append("\n  ").Append(kAll[i].m_ItemId).Append(" ").Append(kName)
                    .Append(" x").Append(kAll[i].m_Count);
            }
            return kBuilder.ToString();
        }

        #endregion
    }
}
