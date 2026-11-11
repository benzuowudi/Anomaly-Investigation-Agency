using System.Collections.Generic;
using System.Text;
using Framework.Script.Model;
using GameMain.Script.Armory;

namespace GameMain.Script.Inventory
{
    /// <summary>
    /// 仓库系统的主模型（ModelBase）：item 表道具的权威业务入口。
    ///
    /// 职责：
    /// - 校验并执行道具增减（配置是否存在、数量是否合法、够不够扣、格子/负重是否够）；
    /// - 写 InventoryData 并在成功后广播 InventoryEvent.OnItemChanged / OnInventoryChanged；
    /// - 给界面组装 InventorySnapshot（"道具"页签读 item 表，"装备"页签只读军械库的持有装备）。
    ///
    /// 边界（规范 §4）：
    /// - 只读写本系统的 InventoryData；装备持有数据归 ArmorySystem，这里只做只读查询；
    /// - 不引用任何 FairyGUI / UIWindow 类型；
    /// - 军械库、战前整备等系统通过 Game.Model.Get&lt;InventorySystem&gt;() 增减道具，
    ///   不要再出现第二个直接改数量的入口。
    /// </summary>
    public class InventorySystem : ModelBase
    {
        private InventoryData DB
        {
            get { return Game.Data.GetGameData<InventoryData>(); }
        }

        private static ArmorySystem Armory
        {
            get { return Game.Model.Get<ArmorySystem>(); }
        }

        #region 生命周期

        protected internal override void OnStart()
        {
            Load();
        }

        #endregion

        #region 查询

        /// <summary>查询持有数量，未持有返回 0</summary>
        public int GetItemCount(int _itemId)
        {
            return DB.GetCount(_itemId);
        }

        /// <summary>是否持有指定数量（默认至少 1 个）</summary>
        public bool HasItem(int _itemId, int _count = 1)
        {
            return DB.HasItem(_itemId, _count);
        }

        /// <summary>道具 ID 是否在 item 表中配置</summary>
        public bool IsKnownItem(int _itemId)
        {
            return GetItemConfig(_itemId) != null;
        }

        /// <summary>取 item 表配置，找不到返回 null</summary>
        public cfg.GameData_Item.ItemData GetItemConfig(int _itemId)
        {
            cfg.Tables kTables = Game.Data.GetData;
            if (kTables == null || kTables.item == null || _itemId <= 0)
            {
                return null;
            }

            return kTables.item.GetOrDefault(_itemId);
        }

        /// <summary>取 equip 表配置，找不到返回 null</summary>
        public cfg.GameData_Item.EquipData GetEquipConfig(int _equipId)
        {
            cfg.Tables kTables = Game.Data.GetData;
            if (kTables == null || kTables.equip == null || _equipId <= 0)
            {
                return null;
            }

            return kTables.equip.GetOrDefault(_equipId);
        }

        /// <summary>取道具中文名（没有配置时返回可读占位）</summary>
        public string GetItemName(int _itemId)
        {
            cfg.GameData_Item.ItemData kConfig = GetItemConfig(_itemId);
            return kConfig == null ? "未知道具(" + _itemId + ")" : kConfig.Name;
        }

        /// <summary>全部持有中的道具（按道具 ID 升序）</summary>
        public List<InventoryItemStack> GetAllItems()
        {
            return DB.GetItems();
        }

        /// <summary>按展示类型（item.type_show）筛选持有中的道具</summary>
        public List<InventoryItemStack> GetItemsByShowType(int _typeShow)
        {
            List<InventoryItemStack> kAll = DB.GetItems();
            List<InventoryItemStack> kResult = new List<InventoryItemStack>();
            for (int i = 0; i < kAll.Count; i++)
            {
                cfg.GameData_Item.ItemData kConfig = GetItemConfig(kAll[i].m_ItemId);
                if (kConfig != null && kConfig.TypeShow == _typeShow)
                {
                    kResult.Add(kAll[i]);
                }
            }

            return kResult;
        }

        /// <summary>持有道具的总数量（各道具数量之和）</summary>
        public int GetTotalCount()
        {
            return DB.GetTotalCount();
        }

        /// <summary>已占用格子数（道具种类数）</summary>
        public int GetUsedSlots()
        {
            return DB.GetUsedSlots();
        }

        /// <summary>当前总重量（item 表暂无重量列，每件按 1 计）</summary>
        public int GetTotalWeight()
        {
            return DB.GetTotalWeight();
        }

        /// <summary>格子上限（0 表示不限制）</summary>
        public int GetMaxSlots()
        {
            return DB.GetMaxSlots();
        }

        /// <summary>负重上限（0 表示不限制）</summary>
        public int GetMaxWeight()
        {
            return DB.GetMaxWeight();
        }

        /// <summary>数据版本号（每次成功变更 +1）</summary>
        public long GetVersion()
        {
            return DB.GetVersion();
        }

        /// <summary>是否还能再放入指定数量（只校验，不写库）</summary>
        public bool CanAdd(int _itemId, int _count)
        {
            if (_count <= 0 || !IsKnownItem(_itemId))
            {
                return false;
            }

            if (DB.GetMaxSlots() > 0 && DB.GetCount(_itemId) == 0 && DB.GetUsedSlots() + 1 > DB.GetMaxSlots())
            {
                return false;
            }

            if (DB.GetMaxWeight() > 0)
            {
                long kWeight = (long)DB.GetTotalWeight() + _count;
                if (kWeight > DB.GetMaxWeight())
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>是否扣得动指定数量（只校验，不写库）</summary>
        public bool CanRemove(int _itemId, int _count)
        {
            return _count > 0 && DB.GetCount(_itemId) >= _count;
        }

        #endregion

        #region 快照

        /// <summary>
        /// 组装界面用的只读快照：当前页签的条目 + 容量信息，条目按品质降序、同品质 ID 升序。
        /// </summary>
        public InventorySnapshot BuildSnapshot(InventoryTab _tab)
        {
            InventorySnapshot kSnapshot = new InventorySnapshot();
            kSnapshot.m_Tab = _tab;
            kSnapshot.m_Version = DB.GetVersion();
            kSnapshot.m_UsedSlots = DB.GetUsedSlots();
            kSnapshot.m_MaxSlots = DB.GetMaxSlots();
            kSnapshot.m_TotalWeight = DB.GetTotalWeight();
            kSnapshot.m_MaxWeight = DB.GetMaxWeight();

            if (_tab == InventoryTab.Equip)
            {
                BuildEquipEntries(kSnapshot);
            }
            else
            {
                BuildItemEntries(kSnapshot);
            }

            SortEntries(kSnapshot.m_Items);
            return kSnapshot;
        }

        /// <summary>"道具"页签：item 表中所有持有中的道具（含货币类，暂不单独分页）</summary>
        private void BuildItemEntries(InventorySnapshot _snapshot)
        {
            List<InventoryItemStack> kStacks = DB.GetItems();
            int kMissing = 0;
            for (int i = 0; i < kStacks.Count; i++)
            {
                cfg.GameData_Item.ItemData kConfig = GetItemConfig(kStacks[i].m_ItemId);
                if (kConfig == null)
                {
                    kMissing++;
                    continue;
                }

                InventoryEntry kEntry = new InventoryEntry();
                kEntry.m_Id = kConfig.ID;
                kEntry.m_IsEquip = false;
                kEntry.m_Count = kStacks[i].m_Count;
                kEntry.m_Name = kConfig.Name;
                kEntry.m_Icon = kConfig.Icon;
                kEntry.m_Quality = kConfig.Quality;
                _snapshot.m_Items.Add(kEntry);
            }

            if (kMissing > 0)
            {
                Game.Log.LogWarning(InventoryDefine.c_LogTag + "有 " + kMissing + " 件存档道具在 item 表中不存在，界面已跳过");
            }
        }

        /// <summary>"装备"页签：军械库已合成的装备（持有数据在 ArmoryData）</summary>
        private void BuildEquipEntries(InventorySnapshot _snapshot)
        {
            List<int> kEquipIds = Armory.GetEquipIds();
            int kMissing = 0;
            for (int i = 0; i < kEquipIds.Count; i++)
            {
                int kEquipId = kEquipIds[i];
                int kCount = Armory.GetEquipCount(kEquipId);
                if (kCount <= 0)
                {
                    continue;
                }

                cfg.GameData_Item.EquipData kConfig = GetEquipConfig(kEquipId);
                if (kConfig == null)
                {
                    kMissing++;
                    continue;
                }

                InventoryEntry kEntry = new InventoryEntry();
                kEntry.m_Id = kConfig.ID;
                kEntry.m_IsEquip = true;
                kEntry.m_Count = kCount;
                kEntry.m_Name = kConfig.Name;
                kEntry.m_Icon = kConfig.Icon;
                kEntry.m_Quality = kConfig.Quality;
                _snapshot.m_Items.Add(kEntry);
            }

            if (kMissing > 0)
            {
                Game.Log.LogWarning(InventoryDefine.c_LogTag + "有 " + kMissing + " 件装备在 equip 表中不存在，界面已跳过");
            }
        }

        private static void SortEntries(List<InventoryEntry> _entries)
        {
            _entries.Sort(delegate (InventoryEntry _a, InventoryEntry _b)
            {
                if (_a.m_Quality != _b.m_Quality)
                {
                    return _b.m_Quality.CompareTo(_a.m_Quality);
                }

                return _a.m_Id.CompareTo(_b.m_Id);
            });
        }

        #endregion

        #region 变更

        /// <summary>增加道具。道具不存在 / 数量非法 / 超容量都返回 false 并打日志。</summary>
        public bool AddItem(int _itemId, int _count, InventoryChangeReason _reason = InventoryChangeReason.None)
        {
            if (_count <= 0)
            {
                Game.Log.LogWarning(InventoryDefine.c_LogTag + "增加数量必须大于 0：" + _count);
                return false;
            }

            if (!CheckItemConfig(_itemId))
            {
                return false;
            }

            int kOldCount = DB.GetCount(_itemId);
            long kNewCount = (long)kOldCount + _count;
            if (kNewCount > int.MaxValue)
            {
                Game.Log.LogWarning(InventoryDefine.c_LogTag + "数量超出 int 上限：" + _itemId);
                return false;
            }

            if (!CanAdd(_itemId, _count))
            {
                Game.Log.LogWarning(InventoryDefine.c_LogTag + "仓库放不下：" + GetItemName(_itemId)
                    + "，格子 " + DB.GetUsedSlots() + "/" + DB.GetMaxSlots()
                    + "，负重 " + DB.GetTotalWeight() + "/" + DB.GetMaxWeight());
                return false;
            }

            return ApplyItem(_itemId, (int)kNewCount, kOldCount, _reason);
        }

        /// <summary>扣除道具。数量不足返回 false，权威数据保持不变。</summary>
        public bool RemoveItem(int _itemId, int _count, InventoryChangeReason _reason = InventoryChangeReason.None)
        {
            if (_count <= 0)
            {
                Game.Log.LogWarning(InventoryDefine.c_LogTag + "扣除数量必须大于 0：" + _count);
                return false;
            }

            if (!CheckItemConfig(_itemId))
            {
                return false;
            }

            int kOldCount = DB.GetCount(_itemId);
            if (kOldCount < _count)
            {
                Game.Log.LogWarning(InventoryDefine.c_LogTag + "道具数量不足：" + GetItemName(_itemId)
                    + " 需要 " + _count + "，实际 " + kOldCount);
                return false;
            }

            return ApplyItem(_itemId, kOldCount - _count, kOldCount, _reason);
        }

        /// <summary>直接把数量设为指定值（0 表示清空该道具）。</summary>
        public bool SetItemCount(int _itemId, int _count, InventoryChangeReason _reason = InventoryChangeReason.None)
        {
            if (!CheckItemConfig(_itemId))
            {
                return false;
            }

            if (_count < 0)
            {
                Game.Log.LogWarning(InventoryDefine.c_LogTag + "目标数量不能为负数：" + _count);
                return false;
            }

            int kOldCount = DB.GetCount(_itemId);
            if (kOldCount == _count)
            {
                return false;
            }

            if (_count > kOldCount && !CanAdd(_itemId, _count - kOldCount))
            {
                Game.Log.LogWarning(InventoryDefine.c_LogTag + "仓库放不下：" + GetItemName(_itemId)
                    + "，目标数量 " + _count);
                return false;
            }

            return ApplyItem(_itemId, _count, kOldCount, _reason);
        }

        /// <summary>清空单个道具</summary>
        public bool ClearItem(int _itemId, InventoryChangeReason _reason = InventoryChangeReason.None)
        {
            return SetItemCount(_itemId, 0, _reason);
        }

        /// <summary>批量增加，返回成功的条数。单条失败不影响其它条。</summary>
        public int AddItems(IList<InventoryItemStack> _stacks, InventoryChangeReason _reason = InventoryChangeReason.None)
        {
            if (_stacks == null || _stacks.Count == 0)
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

                if (AddItemInternal(_stacks[i].m_ItemId, _stacks[i].m_Count, _reason))
                {
                    kSuccess++;
                }
            }

            if (kSuccess > 0)
            {
                DB.Save();
            }

            return kSuccess;
        }

        /// <summary>批量扣除，返回是否全部成功。任意一条失败时不做回滚，请用 TryExchange 做原子交易。</summary>
        public bool RemoveItems(IList<InventoryItemStack> _stacks, InventoryChangeReason _reason = InventoryChangeReason.None)
        {
            if (_stacks == null || _stacks.Count == 0)
            {
                return false;
            }

            bool kAllOk = true;
            bool kAnyChanged = false;
            for (int i = 0; i < _stacks.Count; i++)
            {
                if (_stacks[i] == null || _stacks[i].m_Count <= 0)
                {
                    continue;
                }

                int kOldCount = DB.GetCount(_stacks[i].m_ItemId);
                if (!CheckItemConfig(_stacks[i].m_ItemId))
                {
                    kAllOk = false;
                    continue;
                }

                if (kOldCount < _stacks[i].m_Count)
                {
                    Game.Log.LogWarning(InventoryDefine.c_LogTag + "批量扣除失败，数量不足：" + GetItemName(_stacks[i].m_ItemId)
                        + " 需要 " + _stacks[i].m_Count + "，实际 " + kOldCount);
                    kAllOk = false;
                    continue;
                }

                if (ApplyItem(_stacks[i].m_ItemId, kOldCount - _stacks[i].m_Count, kOldCount, _reason, false))
                {
                    kAnyChanged = true;
                }
            }

            if (kAnyChanged)
            {
                DB.Save();
            }

            return kAllOk;
        }

        /// <summary>
        /// 兑换：先扣 cost 再发 gain，任一步失败则整体回滚（原子操作）。
        /// </summary>
        public bool TryExchange(
            IList<InventoryItemStack> _cost,
            IList<InventoryItemStack> _gain,
            InventoryChangeReason _reason = InventoryChangeReason.Exchange)
        {
            Dictionary<int, int> kCost = Aggregate(_cost);
            Dictionary<int, int> kGain = Aggregate(_gain);
            if (kCost.Count == 0 && kGain.Count == 0)
            {
                return false;
            }

            // 1. 先整体校验成本，避免扣到一半才发现不够
            foreach (KeyValuePair<int, int> kPair in kCost)
            {
                if (!CanRemove(kPair.Key, kPair.Value))
                {
                    Game.Log.LogWarning(InventoryDefine.c_LogTag + "兑换失败，道具不足：" + GetItemName(kPair.Key)
                        + " 需要 " + kPair.Value + " 实际 " + DB.GetCount(kPair.Key));
                    return false;
                }
            }

            // 2. 扣除
            List<InventoryItemStack> kRemoved = new List<InventoryItemStack>();
            foreach (KeyValuePair<int, int> kPair in kCost)
            {
                if (RemoveItem(kPair.Key, kPair.Value, _reason))
                {
                    kRemoved.Add(new InventoryItemStack(kPair.Key, kPair.Value));
                }
                else
                {
                    RestoreItems(kRemoved, _reason);
                    return false;
                }
            }

            // 3. 发放
            List<InventoryItemStack> kAdded = new List<InventoryItemStack>();
            foreach (KeyValuePair<int, int> kPair in kGain)
            {
                if (AddItem(kPair.Key, kPair.Value, _reason))
                {
                    kAdded.Add(new InventoryItemStack(kPair.Key, kPair.Value));
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

        /// <summary>清空整个仓库（调试/新档用，容量保持不变）</summary>
        public void ClearAll(InventoryChangeReason _reason = InventoryChangeReason.Debug)
        {
            DB.ClearItems();
            BroadcastChanged(_reason);
            DB.Save();
        }

        /// <summary>设置容量上限，0 表示不限制</summary>
        public void SetCapacity(int _maxSlots, int _maxWeight)
        {
            DB.SetCapacity(_maxSlots, _maxWeight);
            BroadcastChanged(InventoryChangeReason.None);
            DB.Save();
        }

        /// <summary>AddItems 用：跳过"先校验再写"的重复日志，失败仍按单条规则处理</summary>
        private bool AddItemInternal(int _itemId, int _count, InventoryChangeReason _reason)
        {
            if (!CheckItemConfig(_itemId))
            {
                return false;
            }

            int kOldCount = DB.GetCount(_itemId);
            long kNewCount = (long)kOldCount + _count;
            if (kNewCount > int.MaxValue || !CanAdd(_itemId, _count))
            {
                Game.Log.LogWarning(InventoryDefine.c_LogTag + "批量发放失败：" + GetItemName(_itemId)
                    + " x" + _count + "（容量不足或数量溢出）");
                return false;
            }

            return ApplyItem(_itemId, (int)kNewCount, kOldCount, _reason, false);
        }

        /// <summary>
        /// 写入一次数量并广播。写成功才广播，事件里读到的一定是新数据。
        /// </summary>
        private bool ApplyItem(int _itemId, int _newCount, int _oldCount, InventoryChangeReason _reason, bool _save = true)
        {
            if (!DB.SetCount(_itemId, _newCount))
            {
                return false;
            }

            Game.Event.SendMessage(InventoryEvent.OnItemChanged,
                new ItemChangedMsg(_itemId, _oldCount, _newCount, _reason));
            BroadcastChanged(_reason);

            if (_save)
            {
                DB.Save();
            }

            return true;
        }

        private void BroadcastChanged(InventoryChangeReason _reason)
        {
            Game.Event.SendMessage(InventoryEvent.OnInventoryChanged,
                new InventoryChangedMsg(DB.GetVersion(), DB.GetUsedSlots(), DB.GetTotalWeight(), _reason));
        }

        private static Dictionary<int, int> Aggregate(IList<InventoryItemStack> _stacks)
        {
            Dictionary<int, int> kResult = new Dictionary<int, int>();
            if (_stacks == null)
            {
                return kResult;
            }

            for (int i = 0; i < _stacks.Count; i++)
            {
                InventoryItemStack kStack = _stacks[i];
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

        private void RestoreItems(List<InventoryItemStack> _items, InventoryChangeReason _reason)
        {
            for (int i = 0; i < _items.Count; i++)
            {
                AddItem(_items[i].m_ItemId, _items[i].m_Count, _reason);
            }
        }

        private bool CheckItemConfig(int _itemId)
        {
            if (IsKnownItem(_itemId))
            {
                return true;
            }

            Game.Log.LogWarning(InventoryDefine.c_LogTag + "item 表中不存在道具：" + _itemId);
            return false;
        }

        #endregion

        #region 存档

        /// <summary>保存仓库（变更成功后由模型调用）</summary>
        public bool Save()
        {
            return DB.Save();
        }

        /// <summary>从本地读档（OnStart 调一次；调试菜单可重复调用）</summary>
        public bool Load()
        {
            bool kOk = DB.Load();
            if (kOk)
            {
                BroadcastChanged(InventoryChangeReason.Load);
            }

            return kOk;
        }

        /// <summary>删除本地存档（调试/新档用，不会清空内存）</summary>
        public bool DeleteSave()
        {
            return DB.DeleteSave();
        }

        #endregion

        #region 调试

        /// <summary>生成一份可读的仓库快照，便于打日志排查</summary>
        public string Dump()
        {
            StringBuilder kBuilder = new StringBuilder();
            kBuilder.Append(InventoryDefine.c_LogTag).Append("version=").Append(DB.GetVersion())
                .Append(" slots=").Append(DB.GetUsedSlots())
                .Append("/").Append(DB.GetMaxSlots() > 0 ? DB.GetMaxSlots().ToString() : "∞")
                .Append(" weight=").Append(DB.GetTotalWeight())
                .Append("/").Append(DB.GetMaxWeight() > 0 ? DB.GetMaxWeight().ToString() : "∞");

            List<InventoryItemStack> kAll = DB.GetItems();
            if (kAll.Count == 0)
            {
                kBuilder.Append("（空）");
                return kBuilder.ToString();
            }

            for (int i = 0; i < kAll.Count; i++)
            {
                kBuilder.Append("\n  ").Append(kAll[i].m_ItemId).Append(" ")
                    .Append(GetItemName(kAll[i].m_ItemId)).Append(" x").Append(kAll[i].m_Count);
            }

            return kBuilder.ToString();
        }

        #endregion
    }
}
