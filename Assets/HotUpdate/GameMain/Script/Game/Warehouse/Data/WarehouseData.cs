using System.Collections.Generic;
using Framework.Script.Data;
using SimpleJSON;
using UnityEngine;

namespace GameMain.Script.Warehouse
{
    /// <summary>
    /// 仓库系统的权威数据库（唯一可信来源）。
    /// - 只存"数量 > 0"的道具，数量归零即移除，避免脏数据；
    /// - 唯一的写入口是监听 WarehouseEventDefine.OnItemUpdate，其它模块不允许直接改；
    /// - 道具ID必须能在 Luban 的 item 表中查到，表里没有的ID一律拒绝；
    /// - 每次成功变更后广播 OnItemChanged（单件）与 OnWarehouseChanged（整体）。
    /// 由 DataManager.LoadGameData() 注册，用 Game.Data.GetGameData&lt;WarehouseData&gt;() 取回。
    /// </summary>
    public class WarehouseData : GameData
    {
        /// <summary>道具ID → 数量，只保存数量 > 0 的记录</summary>
        private readonly Dictionary<int, int> m_ItemCounts = new Dictionary<int, int>();
        /// <summary>格子上限，0 表示不限制</summary>
        private int m_MaxSlots = 0;
        /// <summary>负重上限，0 表示不限制</summary>
        private int m_MaxWeight = 0;
        /// <summary>数据版本号，每次成功变更 +1，供 UI 判断是否需要重绘</summary>
        private long m_Version = 0;

        protected internal override void Init()
        {
            m_ItemCounts.Clear();
            m_MaxSlots = 0;
            m_MaxWeight = 0;
            m_Version = 0;

            // 权威数据库监听"变更请求"，这是全系统唯一的写库入口
            Game.Event.Subscribe(WarehouseEventDefine.OnItemUpdate, OnItemUpdate);

            Game.Log.LogInfo("[仓库] 权威数据库初始化完成，item 表道具数量：" + GetConfigCount());
        }

        protected internal override void Destroy()
        {
            Game.Event.UnSubscribe(WarehouseEventDefine.OnItemUpdate, OnItemUpdate);
            m_ItemCounts.Clear();
            m_Version = 0;
        }

        #region 事件监听（写库入口）

        /// <summary>
        /// 监听 OnItemUpdate：校验请求并写入权威数据，然后把结果回填到消息体上。
        /// </summary>
        private void OnItemUpdate(object _obj)
        {
            ItemUpdateMsg kMsg = _obj as ItemUpdateMsg;
            if (kMsg == null)
            {
                Game.Log.LogWarning("[仓库] OnItemUpdate 参数类型错误，期望 ItemUpdateMsg");
                return;
            }
            Apply(kMsg);
        }

        /// <summary>
        /// 执行一次数量变更（校验 → 写库 → 广播），结果写回 _msg。
        /// </summary>
        private void Apply(ItemUpdateMsg _msg)
        {
            _msg.m_OldCount = GetCount(_msg.m_ItemId);
            _msg.m_NewCount = _msg.m_OldCount;

            cfg.GameData_Item.ItemData kConfig = GetConfig(_msg.m_ItemId);
            if (kConfig == null)
            {
                Fail(_msg, ItemChangeResult.UnknownItem, "item 表中不存在道具：" + _msg.m_ItemId);
                return;
            }

            long kNewCount;
            switch (_msg.m_Op)
            {
                case ItemChangeOp.Add:
                    if (_msg.m_Count <= 0)
                    {
                        Fail(_msg, ItemChangeResult.InvalidCount, "增加数量必须大于 0");
                        return;
                    }
                    kNewCount = (long)_msg.m_OldCount + _msg.m_Count;
                    break;
                case ItemChangeOp.Remove:
                    if (_msg.m_Count <= 0)
                    {
                        Fail(_msg, ItemChangeResult.InvalidCount, "扣除数量必须大于 0");
                        return;
                    }
                    if (_msg.m_OldCount < _msg.m_Count)
                    {
                        Fail(_msg, ItemChangeResult.NotEnough, "数量不足，当前 " + _msg.m_OldCount + "，需要 " + _msg.m_Count);
                        return;
                    }
                    kNewCount = _msg.m_OldCount - _msg.m_Count;
                    break;
                case ItemChangeOp.Set:
                    if (_msg.m_Count < 0)
                    {
                        Fail(_msg, ItemChangeResult.InvalidCount, "目标数量不能为负数");
                        return;
                    }
                    kNewCount = _msg.m_Count;
                    break;
                default:
                    Fail(_msg, ItemChangeResult.InvalidCount, "未知的操作类型");
                    return;
            }

            if (kNewCount > int.MaxValue)
            {
                Fail(_msg, ItemChangeResult.Overflow, "数量超出 int 上限");
                return;
            }

            int kNew = (int)kNewCount;

            // 仅在数量增加时校验格子与负重上限
            if (kNew > _msg.m_OldCount)
            {
                int kAdd = kNew - _msg.m_OldCount;
                if (m_MaxSlots > 0 && _msg.m_OldCount == 0 && GetUsedSlots() + 1 > m_MaxSlots)
                {
                    Fail(_msg, ItemChangeResult.Overflow, "仓库格子已满（上限 " + m_MaxSlots + "）");
                    return;
                }
                if (m_MaxWeight > 0)
                {
                    long kWeight = (long)GetTotalWeight() + (long)kAdd * kConfig.Weight;
                    if (kWeight > m_MaxWeight)
                    {
                        Fail(_msg, ItemChangeResult.Overflow, "超出负重上限（" + kWeight + "/" + m_MaxWeight + "）");
                        return;
                    }
                }
            }

            if (kNew == 0)
            {
                m_ItemCounts.Remove(_msg.m_ItemId);
            }
            else
            {
                m_ItemCounts[_msg.m_ItemId] = kNew;
            }
            m_Version++;

            _msg.m_NewCount = kNew;
            _msg.m_Result = ItemChangeResult.Success;
            _msg.m_Error = string.Empty;

            Game.Event.SendMessage(
                WarehouseEventDefine.OnItemChanged,
                new ItemChangedMsg(_msg.m_ItemId, _msg.m_OldCount, kNew, _msg.m_Reason));
            Game.Event.SendMessage(
                WarehouseEventDefine.OnWarehouseChanged,
                new WarehouseChangedMsg(m_Version, GetUsedSlots(), GetTotalWeight(), _msg.m_Reason));
        }

        private static void Fail(ItemUpdateMsg _msg, ItemChangeResult _result, string _error)
        {
            _msg.m_Result = _result;
            _msg.m_Error = _error;
        }

        #endregion

        #region 查询

        /// <summary>
        /// 查询道具数量，不存在或未持有返回 0
        /// </summary>
        public int GetCount(int _itemId)
        {
            int kCount;
            return m_ItemCounts.TryGetValue(_itemId, out kCount) ? kCount : 0;
        }

        /// <summary>
        /// 是否持有指定数量
        /// </summary>
        public bool HasItem(int _itemId, int _count = 1)
        {
            if (_count <= 0)
            {
                return true;
            }
            return GetCount(_itemId) >= _count;
        }

        /// <summary>
        /// 已占用格子数（数量 > 0 的道具种类）
        /// </summary>
        public int GetUsedSlots()
        {
            return m_ItemCounts.Count;
        }

        /// <summary>
        /// 当前总重量
        /// </summary>
        public int GetTotalWeight()
        {
            long kWeight = 0;
            Dictionary<int, int>.Enumerator kE = m_ItemCounts.GetEnumerator();
            while (kE.MoveNext())
            {
                cfg.GameData_Item.ItemData kConfig = GetConfig(kE.Current.Key);
                if (kConfig != null)
                {
                    kWeight += (long)kConfig.Weight * kE.Current.Value;
                }
            }
            return kWeight > int.MaxValue ? int.MaxValue : (int)kWeight;
        }

        public int GetMaxSlots()
        {
            return m_MaxSlots;
        }

        public int GetMaxWeight()
        {
            return m_MaxWeight;
        }

        public long GetVersion()
        {
            return m_Version;
        }

        /// <summary>
        /// 导出快照（按 item 表顺序，只含数量 > 0 的项）
        /// </summary>
        public List<WarehouseItemStack> GetSnapshot()
        {
            List<WarehouseItemStack> kList = new List<WarehouseItemStack>();
            List<cfg.GameData_Item.ItemData> kConfigs = GetConfigList();
            for (int i = 0; i < kConfigs.Count; i++)
            {
                int kCount = GetCount(kConfigs[i].ID);
                if (kCount > 0)
                {
                    kList.Add(new WarehouseItemStack(kConfigs[i].ID, kCount));
                }
            }
            return kList;
        }

        /// <summary>
        /// 判断道具ID是否在 item 表中
        /// </summary>
        public static bool IsKnownItem(int _itemId)
        {
            return GetConfig(_itemId) != null;
        }

        /// <summary>
        /// 取 item 表里的配置，找不到返回 null
        /// </summary>
        public static cfg.GameData_Item.ItemData GetConfig(int _itemId)
        {
            cfg.Tables kTables = Game.Data.GetData;
            if (kTables == null || kTables.item == null)
            {
                return null;
            }
            return kTables.item.GetOrDefault(_itemId);
        }

        private static List<cfg.GameData_Item.ItemData> GetConfigList()
        {
            cfg.Tables kTables = Game.Data.GetData;
            if (kTables == null || kTables.item == null)
            {
                return new List<cfg.GameData_Item.ItemData>();
            }
            return kTables.item.DataList;
        }

        private static int GetConfigCount()
        {
            return GetConfigList().Count;
        }

        #endregion

        #region 维护 / 存档

        /// <summary>
        /// 设置容量上限，0 表示不限制
        /// </summary>
        public void SetCapacity(int _maxSlots, int _maxWeight, ItemChangeReason _reason = ItemChangeReason.None)
        {
            m_MaxSlots = _maxSlots < 0 ? 0 : _maxSlots;
            m_MaxWeight = _maxWeight < 0 ? 0 : _maxWeight;
            m_Version++;

            Game.Event.SendMessage(
                WarehouseEventDefine.OnWarehouseChanged,
                new WarehouseChangedMsg(m_Version, GetUsedSlots(), GetTotalWeight(), _reason));
        }

        /// <summary>
        /// 清空仓库（调试/新档用，不走 OnItemUpdate）
        /// </summary>
        public void ClearAll(ItemChangeReason _reason = ItemChangeReason.None)
        {
            m_ItemCounts.Clear();
            m_Version++;

            Game.Event.SendMessage(
                WarehouseEventDefine.OnWarehouseChanged,
                new WarehouseChangedMsg(m_Version, 0, 0, _reason));
        }

        /// <summary>
        /// 序列化为 JSON 字符串
        /// </summary>
        public string ToJson()
        {
            JSONObject kRoot = new JSONObject();
            kRoot.Add("version", m_Version);
            kRoot.Add("maxSlots", m_MaxSlots);
            kRoot.Add("maxWeight", m_MaxWeight);

            JSONArray kItems = new JSONArray();
            List<cfg.GameData_Item.ItemData> kConfigs = GetConfigList();
            for (int i = 0; i < kConfigs.Count; i++)
            {
                int kCount = GetCount(kConfigs[i].ID);
                if (kCount <= 0)
                {
                    continue;
                }
                JSONObject kNode = new JSONObject();
                kNode.Add("id", kConfigs[i].ID);
                kNode.Add("count", kCount);
                kItems.Add(kNode);
            }
            kRoot.Add("items", kItems);
            return kRoot.ToString(2);
        }

        /// <summary>
        /// 从 JSON 字符串还原（item 表中不存在的ID会被丢弃）
        /// </summary>
        public bool FromJson(string _json)
        {
            if (string.IsNullOrEmpty(_json))
            {
                return false;
            }

            JSONNode kRoot;
            try
            {
                kRoot = JSON.Parse(_json);
            }
            catch (System.Exception _e)
            {
                Game.Log.LogError("[仓库] 存档解析失败：" + _e.Message);
                return false;
            }

            if (kRoot == null)
            {
                return false;
            }

            Dictionary<int, int> kNewCounts = new Dictionary<int, int>();
            JSONArray kItems = kRoot["items"] != null ? kRoot["items"].AsArray : null;
            if (kItems != null)
            {
                for (int i = 0; i < kItems.Count; i++)
                {
                    int kId = kItems[i]["id"].AsInt;
                    int kCount = kItems[i]["count"].AsInt;
                    if (kCount > 0 && IsKnownItem(kId))
                    {
                        kNewCounts[kId] = kCount;
                    }
                }
            }

            m_ItemCounts.Clear();
            Dictionary<int, int>.Enumerator kE = kNewCounts.GetEnumerator();
            while (kE.MoveNext())
            {
                m_ItemCounts[kE.Current.Key] = kE.Current.Value;
            }

            m_MaxSlots = kRoot["maxSlots"] != null ? kRoot["maxSlots"].AsInt : 0;
            m_MaxWeight = kRoot["maxWeight"] != null ? kRoot["maxWeight"].AsInt : 0;
            if (m_MaxSlots < 0)
            {
                m_MaxSlots = 0;
            }
            if (m_MaxWeight < 0)
            {
                m_MaxWeight = 0;
            }
            m_Version = kRoot["version"] != null ? kRoot["version"].AsLong : 0;
            m_Version++;

            Game.Event.SendMessage(
                WarehouseEventDefine.OnWarehouseChanged,
                new WarehouseChangedMsg(m_Version, GetUsedSlots(), GetTotalWeight(), ItemChangeReason.Load));
            return true;
        }

        #endregion
    }
}
