using System.Collections.Generic;
using Framework.Script.Data;
using SimpleJSON;

namespace GameMain.Script.Inventory
{
    /// <summary>
    /// 仓库系统的权威数据（SystemDataBase）：item 表道具的持有数量 + 容量上限。
    ///
    /// 约定（规范 §5）：
    /// - 唯一写入口是 InventorySystem，写方法一律 internal，别的系统想改道具必须走模型；
    /// - 只存"数量 &gt; 0"的道具，数量归零即移除，避免脏数据；
    /// - 每次变化递增 m_Version，界面用版本号判断是否需要重绘；
    /// - 不广播事件（事件由 InventorySystem 在写成功后统一发），文件 IO 全部交给 SystemDataBase。
    ///
    /// 存档：Application.persistentDataPath/GameMain/Data/inventory.json
    /// 兼容：旧版本的 warehouse.json 与之同构，第一次读档时基类会自动迁移过来。
    ///
    /// 数据来源变化：item.xlsx 改版后没有 weight 列，重量暂时按"每件 1"计算，
    /// 以后接回重量表只需要改 GetTotalWeight。
    /// </summary>
    public class InventoryData : SystemDataBase
    {
        /// <summary>存档文件名</summary>
        public const string SaveFileName = "inventory.json";

        /// <summary>旧版仓库存档（迁移用，读取成功后另存为 inventory.json）</summary>
        private const string c_LegacySaveFileName = "warehouse.json";

        private const long c_SaveVersion = 1;

        /// <summary>道具 ID → 数量（只保留数量 &gt; 0 的记录）</summary>
        private readonly Dictionary<int, int> m_ItemCounts = new Dictionary<int, int>();

        /// <summary>格子上限，0 表示不限制</summary>
        private int m_MaxSlots;

        /// <summary>负重上限，0 表示不限制</summary>
        private int m_MaxWeight;

        /// <summary>数据版本号，每次成功变更 +1，供 UI 判断是否需要重绘</summary>
        private long m_Version = c_SaveVersion;

        protected override string GetSaveFileName()
        {
            return SaveFileName;
        }

        protected override string GetLegacySaveFileName()
        {
            return c_LegacySaveFileName;
        }

        protected override string GetLogTag()
        {
            return "仓库";
        }

        public override void Clear()
        {
            m_ItemCounts.Clear();
            m_MaxSlots = 0;
            m_MaxWeight = 0;
            m_Version = c_SaveVersion;
        }

        #region 查询

        /// <summary>查询道具数量，未持有返回 0</summary>
        public int GetCount(int _itemId)
        {
            int kCount;
            return m_ItemCounts.TryGetValue(_itemId, out kCount) ? kCount : 0;
        }

        /// <summary>是否持有指定数量（_count &lt;= 0 视为满足）</summary>
        public bool HasItem(int _itemId, int _count = 1)
        {
            if (_count <= 0)
            {
                return true;
            }

            return GetCount(_itemId) >= _count;
        }

        /// <summary>全部持有中的道具（按道具 ID 升序）</summary>
        public List<InventoryItemStack> GetItems()
        {
            List<int> kIds = new List<int>(m_ItemCounts.Keys);
            kIds.Sort();

            List<InventoryItemStack> kResult = new List<InventoryItemStack>(kIds.Count);
            for (int i = 0; i < kIds.Count; i++)
            {
                kResult.Add(new InventoryItemStack(kIds[i], m_ItemCounts[kIds[i]]));
            }

            return kResult;
        }

        /// <summary>已占用格子数（数量 &gt; 0 的道具种类）</summary>
        public int GetUsedSlots()
        {
            return m_ItemCounts.Count;
        }

        /// <summary>各道具数量之和</summary>
        public int GetTotalCount()
        {
            long kTotal = 0;
            Dictionary<int, int>.Enumerator kE = m_ItemCounts.GetEnumerator();
            while (kE.MoveNext())
            {
                kTotal += kE.Current.Value;
            }

            return kTotal > int.MaxValue ? int.MaxValue : (int)kTotal;
        }

        /// <summary>
        /// 当前总重量。item 表暂时没有重量列，按"每件 1"计算；
        /// 以后接回重量表时在这里换成"数量 × 配置重量"。
        /// </summary>
        public int GetTotalWeight()
        {
            return GetTotalCount();
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

        #endregion

        #region 写入（只给 InventorySystem）

        /// <summary>
        /// 把某件道具的数量设为指定值（0 表示移除）。数量没变化时返回 false。
        /// 业务校验（配置是否存在、够不够扣、超没超容量）由 InventorySystem 负责。
        /// </summary>
        internal bool SetCount(int _itemId, int _count)
        {
            if (_itemId <= 0 || _count < 0)
            {
                return false;
            }

            int kOld = GetCount(_itemId);
            if (kOld == _count)
            {
                return false;
            }

            if (_count == 0)
            {
                m_ItemCounts.Remove(_itemId);
            }
            else
            {
                m_ItemCounts[_itemId] = _count;
            }

            m_Version++;
            return true;
        }

        /// <summary>设置容量上限，0 表示不限制</summary>
        internal void SetCapacity(int _maxSlots, int _maxWeight)
        {
            m_MaxSlots = _maxSlots < 0 ? 0 : _maxSlots;
            m_MaxWeight = _maxWeight < 0 ? 0 : _maxWeight;
            m_Version++;
        }

        /// <summary>清空全部道具（容量保持不变）</summary>
        internal void ClearItems()
        {
            m_ItemCounts.Clear();
            m_Version++;
        }

        /// <summary>只递增版本号（容量等不落盘字段变化时用）</summary>
        internal void MarkDirty()
        {
            m_Version++;
        }

        #endregion

        #region 序列化

        protected override string ToJson()
        {
            JSONObject kRoot = new JSONObject();
            kRoot.Add("version", m_Version);
            kRoot.Add("maxSlots", m_MaxSlots);
            kRoot.Add("maxWeight", m_MaxWeight);

            // 固定按 ID 升序，保证同一份数据每次写出的文本一致
            List<int> kIds = new List<int>(m_ItemCounts.Keys);
            kIds.Sort();

            JSONArray kItems = new JSONArray();
            for (int i = 0; i < kIds.Count; i++)
            {
                JSONObject kNode = new JSONObject();
                kNode.Add("id", kIds[i]);
                kNode.Add("count", m_ItemCounts[kIds[i]]);
                kItems.Add(kNode);
            }

            kRoot.Add("items", kItems);
            return kRoot.ToString(2);
        }

        protected override bool FromJson(string _json)
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
            catch (System.Exception)
            {
                return false;
            }

            if (kRoot == null || !kRoot.IsObject)
            {
                return false;
            }

            // 先解析到临时结构，全部成功后再一次性替换，坏档不破坏现有数据
            Dictionary<int, int> kCounts = new Dictionary<int, int>();
            JSONArray kItems = kRoot["items"] == null ? null : kRoot["items"].AsArray;
            if (kItems != null)
            {
                for (int i = 0; i < kItems.Count; i++)
                {
                    JSONNode kNode = kItems[i];
                    if (kNode == null || kNode["id"] == null)
                    {
                        continue;
                    }

                    int kId = kNode["id"].AsInt;
                    int kCount = kNode["count"] == null ? 0 : kNode["count"].AsInt;
                    if (kId > 0 && kCount > 0)
                    {
                        kCounts[kId] = kCount;
                    }
                }
            }

            m_ItemCounts.Clear();
            Dictionary<int, int>.Enumerator kE = kCounts.GetEnumerator();
            while (kE.MoveNext())
            {
                m_ItemCounts[kE.Current.Key] = kE.Current.Value;
            }

            int kMaxSlots = kRoot["maxSlots"] == null ? 0 : kRoot["maxSlots"].AsInt;
            int kMaxWeight = kRoot["maxWeight"] == null ? 0 : kRoot["maxWeight"].AsInt;
            m_MaxSlots = kMaxSlots < 0 ? 0 : kMaxSlots;
            m_MaxWeight = kMaxWeight < 0 ? 0 : kMaxWeight;

            m_Version = kRoot["version"] == null ? c_SaveVersion : kRoot["version"].AsLong;
            m_Version++;
            return true;
        }

        #endregion
    }
}
