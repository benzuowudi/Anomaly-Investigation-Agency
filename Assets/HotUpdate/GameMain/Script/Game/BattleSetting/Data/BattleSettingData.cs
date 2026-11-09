using System.Collections.Generic;
using Framework.Script.Data;
using SimpleJSON;

namespace GameMain.Script.BattleSetting
{
    /// <summary>
    /// 战前整备系统的权威数据（SystemDataBase）。
    ///
    /// 存的是"下次进战前整备要还原"的配置：
    /// - 出战队伍（角色 ID，按槽位顺序）；
    /// - 每名队员的归一化站位（0 = 战场最左，1 = 战场最右）；
    /// - 机械骡货仓（3 格）与队伍支援栏（5 格）里放的东西（物品 ID）；
    /// - 当前遭遇战 ID。
    ///
    /// 约定（规范 §5）：
    /// - 写方法一律 internal，只有 BattleSettingSystem 能改；
    /// - 每次变化递增 m_Version，界面用版本号判断要不要重绘；
    /// - 不广播事件，事件由 Model 在写成功后统一发；
    /// - 文件 IO 全交给 SystemDataBase，这里不出现 File.*。
    /// </summary>
    public class BattleSettingData : SystemDataBase
    {
        public const string SaveFileName = "battle_setting.json";

        private const int c_SaveVersion = 1;

        private int m_Version = c_SaveVersion;
        private int m_EncounterId;
        private readonly List<int> m_TeamRoleIds = new List<int>();
        private readonly Dictionary<int, float> m_Positions = new Dictionary<int, float>();
        private readonly List<int> m_MuleItemIds = new List<int>();
        private readonly List<int> m_SupportItemIds = new List<int>();

        protected override string GetSaveFileName()
        {
            return SaveFileName;
        }

        protected override string GetLogTag()
        {
            return "战前整备";
        }

        public override void Clear()
        {
            m_Version = c_SaveVersion;
            m_EncounterId = 0;
            m_TeamRoleIds.Clear();
            m_Positions.Clear();
            FillEmptySlots(m_MuleItemIds, BattleSettingDefine.c_MuleItemCount);
            FillEmptySlots(m_SupportItemIds, BattleSettingDefine.c_SupportItemCount);
        }

        private static void FillEmptySlots(List<int> _slots, int _count)
        {
            _slots.Clear();
            for (int i = 0; i < _count; i++)
            {
                _slots.Add(BattleSettingDefine.c_EmptyItemId);
            }
        }

        #region 查询

        public int GetVersion()
        {
            return m_Version;
        }

        public int GetEncounterId()
        {
            return m_EncounterId;
        }

        public IReadOnlyList<int> GetTeamRoleIds()
        {
            return m_TeamRoleIds;
        }

        public int GetTeamCount()
        {
            return m_TeamRoleIds.Count;
        }

        public bool HasRole(int _roleId)
        {
            return m_TeamRoleIds.Contains(_roleId);
        }

        /// <summary>角色在队伍里的槽位下标；不在队里返回 -1</summary>
        public int IndexOfRole(int _roleId)
        {
            return m_TeamRoleIds.IndexOf(_roleId);
        }

        /// <summary>归一化站位；没配过返回默认值</summary>
        public float GetPosition(int _roleId)
        {
            float kPosition;
            if (m_Positions.TryGetValue(_roleId, out kPosition))
            {
                return kPosition;
            }

            return BattleSettingDefine.c_DefaultPosition;
        }

        /// <summary>是否给这名角色配过站位</summary>
        public bool HasPosition(int _roleId)
        {
            return m_Positions.ContainsKey(_roleId);
        }

        public IReadOnlyList<int> GetMuleItemIds()
        {
            return m_MuleItemIds;
        }

        public IReadOnlyList<int> GetSupportItemIds()
        {
            return m_SupportItemIds;
        }

        public int GetItemId(BattleSettingSupplySlot _slotKind, int _slot)
        {
            List<int> kSlots = _slotKind == BattleSettingSupplySlot.Mule ? m_MuleItemIds : m_SupportItemIds;
            if (_slot < 0 || _slot >= kSlots.Count)
            {
                return BattleSettingDefine.c_EmptyItemId;
            }

            return kSlots[_slot];
        }

        #endregion

        #region 写入（只给 BattleSettingSystem）

        internal void SetEncounterId(int _encounterId)
        {
            m_EncounterId = _encounterId;
            m_Version++;
        }

        internal bool AddRole(int _roleId)
        {
            if (_roleId <= 0 || m_TeamRoleIds.Contains(_roleId))
            {
                return false;
            }

            if (m_TeamRoleIds.Count >= BattleSettingDefine.c_MaxTeamCount)
            {
                return false;
            }

            m_TeamRoleIds.Add(_roleId);
            m_Version++;
            return true;
        }

        internal bool RemoveRole(int _roleId)
        {
            if (!m_TeamRoleIds.Remove(_roleId))
            {
                return false;
            }

            m_Positions.Remove(_roleId);
            m_Version++;
            return true;
        }

        internal bool MoveRole(int _roleId, int _slot)
        {
            int kIndex = m_TeamRoleIds.IndexOf(_roleId);
            if (kIndex < 0)
            {
                return false;
            }

            int kTarget = _slot;
            if (kTarget < 0)
            {
                kTarget = 0;
            }
            if (kTarget >= m_TeamRoleIds.Count)
            {
                kTarget = m_TeamRoleIds.Count - 1;
            }

            if (kTarget == kIndex)
            {
                return false;
            }

            m_TeamRoleIds.RemoveAt(kIndex);
            m_TeamRoleIds.Insert(kTarget, _roleId);
            m_Version++;
            return true;
        }

        internal void ClearTeam()
        {
            m_TeamRoleIds.Clear();
            m_Positions.Clear();
            m_Version++;
        }

        internal void SetPosition(int _roleId, float _position)
        {
            m_Positions[_roleId] = _position;
            m_Version++;
        }

        internal void ClearPositions()
        {
            m_Positions.Clear();
            m_Version++;
        }

        internal bool SetItem(BattleSettingSupplySlot _slotKind, int _slot, int _itemId)
        {
            List<int> kSlots = _slotKind == BattleSettingSupplySlot.Mule ? m_MuleItemIds : m_SupportItemIds;
            if (_slot < 0 || _slot >= kSlots.Count)
            {
                return false;
            }

            if (kSlots[_slot] == _itemId)
            {
                return false;
            }

            kSlots[_slot] = _itemId;
            m_Version++;
            return true;
        }

        internal void MarkDirty()
        {
            m_Version++;
        }

        #endregion

        #region 存档

        protected override string ToJson()
        {
            JSONObject kRoot = new JSONObject();
            kRoot.Add("version", m_Version);
            kRoot.Add("encounterId", m_EncounterId);

            JSONArray kTeam = new JSONArray();
            for (int i = 0; i < m_TeamRoleIds.Count; i++)
            {
                kTeam.Add(m_TeamRoleIds[i]);
            }
            kRoot.Add("team", kTeam);

            JSONArray kPositions = new JSONArray();
            Dictionary<int, float>.Enumerator kPE = m_Positions.GetEnumerator();
            while (kPE.MoveNext())
            {
                JSONObject kNode = new JSONObject();
                kNode.Add("roleId", kPE.Current.Key);
                kNode.Add("position", kPE.Current.Value);
                kPositions.Add(kNode);
            }
            kRoot.Add("positions", kPositions);

            kRoot.Add("mule", ToJsonArray(m_MuleItemIds));
            kRoot.Add("support", ToJsonArray(m_SupportItemIds));
            return kRoot.ToString();
        }

        private static JSONArray ToJsonArray(List<int> _values)
        {
            JSONArray kArray = new JSONArray();
            for (int i = 0; i < _values.Count; i++)
            {
                kArray.Add(_values[i]);
            }

            return kArray;
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
                kRoot = JSONNode.Parse(_json);
            }
            catch (System.Exception)
            {
                return false;
            }

            if (kRoot == null)
            {
                return false;
            }

            // 先解析到临时结构，全部成功后再一次性替换，坏档不破坏现有数据
            int kEncounterId = kRoot["encounterId"] == null ? 0 : kRoot["encounterId"].AsInt;

            List<int> kTeam = new List<int>();
            JSONNode kTeamNode = kRoot["team"];
            if (kTeamNode != null)
            {
                for (int i = 0; i < kTeamNode.Count && kTeam.Count < BattleSettingDefine.c_MaxTeamCount; i++)
                {
                    int kRoleId = kTeamNode[i].AsInt;
                    if (kRoleId > 0 && !kTeam.Contains(kRoleId))
                    {
                        kTeam.Add(kRoleId);
                    }
                }
            }

            Dictionary<int, float> kPositions = new Dictionary<int, float>();
            JSONNode kPositionNode = kRoot["positions"];
            if (kPositionNode != null)
            {
                for (int i = 0; i < kPositionNode.Count; i++)
                {
                    JSONNode kNode = kPositionNode[i];
                    if (kNode == null || kNode["roleId"] == null)
                    {
                        continue;
                    }

                    int kRoleId = kNode["roleId"].AsInt;
                    float kPosition = kNode["position"] == null
                        ? BattleSettingDefine.c_DefaultPosition
                        : kNode["position"].AsFloat;
                    if (kRoleId <= 0)
                    {
                        continue;
                    }

                    kPositions[kRoleId] = ClampPosition(kPosition);
                }
            }

            List<int> kMule = ParseSlots(kRoot["mule"], BattleSettingDefine.c_MuleItemCount);
            List<int> kSupport = ParseSlots(kRoot["support"], BattleSettingDefine.c_SupportItemCount);

            m_EncounterId = kEncounterId;
            m_TeamRoleIds.Clear();
            m_TeamRoleIds.AddRange(kTeam);
            m_Positions.Clear();
            Dictionary<int, float>.Enumerator kPE = kPositions.GetEnumerator();
            while (kPE.MoveNext())
            {
                m_Positions[kPE.Current.Key] = kPE.Current.Value;
            }

            m_MuleItemIds.Clear();
            m_MuleItemIds.AddRange(kMule);
            m_SupportItemIds.Clear();
            m_SupportItemIds.AddRange(kSupport);

            m_Version = kRoot["version"] == null ? c_SaveVersion : kRoot["version"].AsInt;
            return true;
        }

        private static List<int> ParseSlots(JSONNode _node, int _count)
        {
            List<int> kSlots = new List<int>(_count);
            for (int i = 0; i < _count; i++)
            {
                int kItemId = 0;
                if (_node != null && i < _node.Count)
                {
                    kItemId = _node[i].AsInt;
                }

                kSlots.Add(kItemId > 0 ? kItemId : BattleSettingDefine.c_EmptyItemId);
            }

            return kSlots;
        }

        private static float ClampPosition(float _position)
        {
            if (_position < BattleSettingDefine.c_MinPosition)
            {
                return BattleSettingDefine.c_MinPosition;
            }

            if (_position > BattleSettingDefine.c_MaxPosition)
            {
                return BattleSettingDefine.c_MaxPosition;
            }

            return _position;
        }

        #endregion
    }
}
