using System.Collections.Generic;
using Framework.Script.Data;
using SimpleJSON;

namespace GameMain.Script.Armory
{
    /// <summary>
    /// 军械库 Data（SystemDataBase）：已合成的装备背包。
    ///
    /// 装备 ID 来自 equip 表（5000+），不和 item 表的道具混在一起。
    /// 存档：Application/persistentDataPath/GameMain/Data/armory.json
    /// </summary>
    public class ArmoryData : SystemDataBase
    {
        public const string SaveFileName = "armory.json";

        private readonly Dictionary<int, int> m_EquipCounts = new Dictionary<int, int>();

        protected override string GetSaveFileName()
        {
            return SaveFileName;
        }

        protected override string GetLogTag()
        {
            return "军械库";
        }

        public override void Clear()
        {
            m_EquipCounts.Clear();
        }

        #region 查询 / 写入

        public int GetCount(int _equipId)
        {
            int kCount;
            return m_EquipCounts.TryGetValue(_equipId, out kCount) ? kCount : 0;
        }

        public bool HasEquip(int _equipId)
        {
            return GetCount(_equipId) > 0;
        }

        /// <summary>已拥有的装备 ID（升序）</summary>
        public List<int> GetEquipIds()
        {
            List<int> kResult = new List<int>(m_EquipCounts.Keys);
            kResult.Sort();
            return kResult;
        }

        internal void AddEquip(int _equipId, int _count)
        {
            if (_equipId <= 0 || _count <= 0)
            {
                return;
            }

            int kOld;
            m_EquipCounts.TryGetValue(_equipId, out kOld);
            m_EquipCounts[_equipId] = kOld + _count;
        }

        #endregion

        #region 序列化

        protected override string ToJson()
        {
            JSONObject kRoot = new JSONObject();
            JSONArray kEquipments = new JSONArray();

            foreach (KeyValuePair<int, int> kPair in m_EquipCounts)
            {
                JSONObject kItem = new JSONObject();
                kItem.Add("id", kPair.Key);
                kItem.Add("count", kPair.Value);
                kEquipments.Add(kItem);
            }

            kRoot.Add("equipments", kEquipments);
            return kRoot.ToString();
        }

        protected override bool FromJson(string _json)
        {
            JSONNode kRoot = JSON.Parse(_json);
            if (kRoot == null || !kRoot.IsObject)
            {
                return false;
            }

            m_EquipCounts.Clear();

            JSONNode kEquipments = kRoot["equipments"];
            if (kEquipments != null && kEquipments.IsArray)
            {
                for (int i = 0; i < kEquipments.Count; i++)
                {
                    JSONNode kItem = kEquipments[i];
                    if (kItem == null)
                    {
                        continue;
                    }

                    int kId = kItem["id"] == null ? 0 : kItem["id"].AsInt;
                    int kCount = kItem["count"] == null ? 1 : kItem["count"].AsInt;
                    if (kId > 0 && kCount > 0)
                    {
                        m_EquipCounts[kId] = kCount;
                    }
                }
            }

            return true;
        }

        #endregion
    }
}
