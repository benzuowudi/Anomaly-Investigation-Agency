using System.Collections.Generic;
using SimpleJSON;

namespace GameMain.Script.Role
{
    /// <summary>
    /// 一个已获得角色的运行时记录（持久化对象）。
    ///
    /// 角色由 role.xlsx 这份"角色模板"随机生成：模板里每一行是一个属性，
    /// 生成时按该行的 min~max 掷点、再用 maxValue 封顶，结果存进 m_Attributes。
    /// 天赋（属性ID 6）比较特殊：值域就是 talent 表的 ID 区间，生成时随机挑一个天赋ID。
    ///
    /// 本类只做数据载体，不持有权威数据，也不做存档 IO：
    /// 权威数据在 RoleSystemData，读写入口是 RoleSystem。
    /// </summary>
    public class RoleRecord
    {
        /// <summary>角色唯一ID（同一存档内自增，不是配置ID）</summary>
        public int m_Id;
        /// <summary>角色名（为空时用 GetDisplayName 兜底）</summary>
        public string m_Name = string.Empty;
        /// <summary>等级</summary>
        public int m_Level = 1;
        /// <summary>获得时间（Unix 秒）</summary>
        public long m_CreateTime;
        /// <summary>天赋ID（talent 表的 ID），与 m_Attributes[6] 同步</summary>
        public int m_TalentId;
        /// <summary>属性ID → 属性值</summary>
        public Dictionary<int, int> m_Attributes = new Dictionary<int, int>();
        /// <summary>初始行为逻辑ID列表（生成时由 RoleBehaviorRule 推导并缓存）</summary>
        public List<int> m_Behaviors = new List<int>();
        /// <summary>持久效果（buff 表里跟着角色走的那些：伤残/瘫痪等，战斗临时效果不要写进来）</summary>
        public List<RoleEffect> m_Effects = new List<RoleEffect>();
        /// <summary>职称评分（0/10/50 对应初级/高级/资深）</summary>
        public int m_TitleScore;
        /// <summary>今日通过非任务途径（巡逻/培训）已获得的职称评分，每天重置</summary>
        public int m_DailyTitleScore;
        /// <summary>已穿戴装备ID（武器槽 1 件 + 护甲槽 1~3 件，按职称解锁）</summary>
        public List<int> m_EquipIds = new List<int>();
        /// <summary>已佩戴勋章ID（1~3 枚，按职称解锁）</summary>
        public List<int> m_MedalIds = new List<int>();
        /// <summary>主动技能ID（生成角色时从 skill 表随机分配，0 表示没有）</summary>
        public int m_ActiveSkillId;
        /// <summary>被动技能ID（0 表示没有）</summary>
        public int m_PassiveSkillId;
        /// <summary>异常适应性：异常类型 → 适应性等级</summary>
        public Dictionary<int, int> m_Adaptations = new Dictionary<int, int>();

        public RoleRecord()
        {
        }

        public RoleRecord(int _id)
        {
            m_Id = _id;
        }

        #region 属性

        /// <summary>
        /// 取属性值，未配置/未生成返回 0
        /// </summary>
        public int GetAttr(int _attrId)
        {
            int kValue;
            return m_Attributes.TryGetValue(_attrId, out kValue) ? kValue : 0;
        }

        public bool HasAttr(int _attrId)
        {
            return m_Attributes.ContainsKey(_attrId);
        }

        /// <summary>
        /// 写属性值。天赋属性（ID 6）会同步到 m_TalentId。
        /// </summary>
        public void SetAttr(int _attrId, int _value)
        {
            m_Attributes[_attrId] = _value;
            if (_attrId == RoleAttrId.Talent)
            {
                m_TalentId = _value;
            }
        }

        /// <summary>
        /// 属性值是否还在上限内（上限来自 role.xlsx 的 maxValue）
        /// </summary>
        public bool IsAttrFull(int _attrId, int _maxValue)
        {
            return _maxValue > 0 && GetAttr(_attrId) >= _maxValue;
        }

        #endregion

        #region 行为逻辑 / 显示

        /// <summary>
        /// 行为逻辑ID的副本（外部改动不会影响角色数据）
        /// </summary>
        public List<int> GetBehaviors()
        {
            return new List<int>(m_Behaviors);
        }

        /// <summary>
        /// 持械主手
        /// </summary>
        public RoleHand GetMainHand()
        {
            return RoleBehaviorRule.GetMainHand(m_Attributes);
        }

        /// <summary>
        /// 显示名：没有起名时用 "角色+ID" 兜底
        /// </summary>
        public string GetDisplayName()
        {
            return string.IsNullOrEmpty(m_Name) ? ("角色" + m_Id) : m_Name;
        }

        #endregion

        #region 职称

        /// <summary>
        /// 当前职称等级（由职称评分算出）
        /// </summary>
        public RoleTitleLevel GetTitleLevel()
        {
            return RoleTitleRule.GetLevel(m_TitleScore);
        }

        /// <summary>
        /// 当前职称名称
        /// </summary>
        public string GetTitleName()
        {
            return RoleTitleRule.GetName(GetTitleLevel());
        }

        #endregion

        #region 持久效果

        /// <summary>
        /// 某效果的层数，没有返回 0
        /// </summary>
        public int GetEffectStacks(int _effectId)
        {
            for (int i = 0; i < m_Effects.Count; i++)
            {
                if (m_Effects[i].m_Id == _effectId)
                {
                    return m_Effects[i].m_Stacks;
                }
            }
            return 0;
        }

        public bool HasEffect(int _effectId)
        {
            return GetEffectStacks(_effectId) > 0;
        }

        /// <summary>
        /// 设置效果层数，0 或负数表示移除
        /// </summary>
        public void SetEffectStacks(int _effectId, int _stacks)
        {
            if (_effectId <= 0)
            {
                return;
            }

            for (int i = 0; i < m_Effects.Count; i++)
            {
                if (m_Effects[i].m_Id == _effectId)
                {
                    if (_stacks <= 0)
                    {
                        m_Effects.RemoveAt(i);
                    }
                    else
                    {
                        RoleEffect kEffect = m_Effects[i];
                        kEffect.m_Stacks = _stacks;
                        m_Effects[i] = kEffect;
                    }
                    return;
                }
            }

            if (_stacks > 0)
            {
                RoleEffect kNew = new RoleEffect();
                kNew.m_Id = _effectId;
                kNew.m_Stacks = _stacks;
                m_Effects.Add(kNew);
            }
        }

        #endregion

        #region 克隆 / 序列化

        public RoleRecord Clone()
        {
            RoleRecord kCopy = new RoleRecord(m_Id);
            kCopy.m_Name = m_Name;
            kCopy.m_Level = m_Level;
            kCopy.m_CreateTime = m_CreateTime;
            kCopy.m_TalentId = m_TalentId;
            kCopy.m_Attributes = new Dictionary<int, int>(m_Attributes);
            kCopy.m_Behaviors = new List<int>(m_Behaviors);
            kCopy.m_Effects = new List<RoleEffect>(m_Effects);
            kCopy.m_TitleScore = m_TitleScore;
            kCopy.m_DailyTitleScore = m_DailyTitleScore;
            kCopy.m_EquipIds = new List<int>(m_EquipIds);
            kCopy.m_MedalIds = new List<int>(m_MedalIds);
            kCopy.m_ActiveSkillId = m_ActiveSkillId;
            kCopy.m_PassiveSkillId = m_PassiveSkillId;
            kCopy.m_Adaptations = new Dictionary<int, int>(m_Adaptations);
            return kCopy;
        }

        public JSONObject ToJson()
        {
            JSONObject kRoot = new JSONObject();
            kRoot.Add("id", m_Id);
            kRoot.Add("name", m_Name == null ? string.Empty : m_Name);
            kRoot.Add("level", m_Level);
            kRoot.Add("createTime", m_CreateTime);
            kRoot.Add("talentId", m_TalentId);

            JSONArray kAttrs = new JSONArray();
            Dictionary<int, int>.Enumerator kE = m_Attributes.GetEnumerator();
            while (kE.MoveNext())
            {
                JSONObject kNode = new JSONObject();
                kNode.Add("id", kE.Current.Key);
                kNode.Add("v", kE.Current.Value);
                kAttrs.Add(kNode);
            }
            kRoot.Add("attrs", kAttrs);

            JSONArray kBehaviors = new JSONArray();
            for (int i = 0; i < m_Behaviors.Count; i++)
            {
                kBehaviors.Add(m_Behaviors[i]);
            }
            kRoot.Add("behaviors", kBehaviors);

            JSONArray kEffects = new JSONArray();
            for (int i = 0; i < m_Effects.Count; i++)
            {
                JSONObject kNode = new JSONObject();
                kNode.Add("id", m_Effects[i].m_Id);
                kNode.Add("stacks", m_Effects[i].m_Stacks);
                kEffects.Add(kNode);
            }
            kRoot.Add("effects", kEffects);

            kRoot.Add("titleScore", m_TitleScore);
            kRoot.Add("dailyTitleScore", m_DailyTitleScore);

            JSONArray kEquips = new JSONArray();
            for (int i = 0; i < m_EquipIds.Count; i++)
            {
                kEquips.Add(m_EquipIds[i]);
            }
            kRoot.Add("equipIds", kEquips);

            JSONArray kMedals = new JSONArray();
            for (int i = 0; i < m_MedalIds.Count; i++)
            {
                kMedals.Add(m_MedalIds[i]);
            }
            kRoot.Add("medalIds", kMedals);

            kRoot.Add("activeSkillId", m_ActiveSkillId);
            kRoot.Add("passiveSkillId", m_PassiveSkillId);

            JSONArray kAdaptations = new JSONArray();
            Dictionary<int, int>.Enumerator kAE = m_Adaptations.GetEnumerator();
            while (kAE.MoveNext())
            {
                JSONObject kNode = new JSONObject();
                kNode.Add("id", kAE.Current.Key);
                kNode.Add("level", kAE.Current.Value);
                kAdaptations.Add(kNode);
            }
            kRoot.Add("adaptations", kAdaptations);
            return kRoot;
        }

        /// <summary>
        /// 从存档节点还原，字段缺失时用默认值，尽量不让一条坏数据毁掉整份存档
        /// </summary>
        public static RoleRecord FromJson(JSONNode _node)
        {
            if (_node == null)
            {
                return null;
            }

            RoleRecord kRole = new RoleRecord();
            kRole.m_Id = _node["id"] == null ? 0 : _node["id"].AsInt;
            kRole.m_Name = _node["name"] == null ? string.Empty : _node["name"].Value;
            kRole.m_Level = _node["level"] == null ? 1 : _node["level"].AsInt;
            kRole.m_CreateTime = _node["createTime"] == null ? 0 : _node["createTime"].AsLong;
            kRole.m_TalentId = _node["talentId"] == null ? 0 : _node["talentId"].AsInt;
            if (kRole.m_Level < 1)
            {
                kRole.m_Level = 1;
            }

            if (_node["attrs"] != null && _node["attrs"].IsArray)
            {
                JSONArray kAttrs = _node["attrs"].AsArray;
                for (int i = 0; i < kAttrs.Count; i++)
                {
                    JSONNode kItem = kAttrs[i];
                    if (kItem == null)
                    {
                        continue;
                    }
                    int kAttrId = kItem["id"] == null ? 0 : kItem["id"].AsInt;
                    if (kAttrId <= 0)
                    {
                        continue;
                    }
                    kRole.m_Attributes[kAttrId] = kItem["v"] == null ? 0 : kItem["v"].AsInt;
                }
            }

            if (_node["behaviors"] != null && _node["behaviors"].IsArray)
            {
                JSONArray kBehaviors = _node["behaviors"].AsArray;
                for (int i = 0; i < kBehaviors.Count; i++)
                {
                    int kBehaviorId = kBehaviors[i];
                    if (kBehaviorId > 0 && !kRole.m_Behaviors.Contains(kBehaviorId))
                    {
                        kRole.m_Behaviors.Add(kBehaviorId);
                    }
                }
            }

            if (_node["effects"] != null && _node["effects"].IsArray)
            {
                JSONArray kEffects = _node["effects"].AsArray;
                for (int i = 0; i < kEffects.Count; i++)
                {
                    JSONNode kItem = kEffects[i];
                    if (kItem == null)
                    {
                        continue;
                    }
                    int kEffectId = kItem["id"] == null ? 0 : kItem["id"].AsInt;
                    int kStacks = kItem["stacks"] == null ? 1 : kItem["stacks"].AsInt;
                    if (kEffectId > 0 && kStacks > 0)
                    {
                        kRole.SetEffectStacks(kEffectId, kStacks);
                    }
                }
            }

            kRole.m_TitleScore = _node["titleScore"] == null ? 0 : _node["titleScore"].AsInt;
            if (kRole.m_TitleScore < 0)
            {
                kRole.m_TitleScore = 0;
            }
            kRole.m_DailyTitleScore = _node["dailyTitleScore"] == null ? 0 : _node["dailyTitleScore"].AsInt;
            if (kRole.m_DailyTitleScore < 0)
            {
                kRole.m_DailyTitleScore = 0;
            }

            if (_node["equipIds"] != null && _node["equipIds"].IsArray)
            {
                JSONArray kEquips = _node["equipIds"].AsArray;
                for (int i = 0; i < kEquips.Count; i++)
                {
                    int kEquipId = kEquips[i];
                    if (kEquipId > 0 && !kRole.m_EquipIds.Contains(kEquipId))
                    {
                        kRole.m_EquipIds.Add(kEquipId);
                    }
                }
            }

            if (_node["medalIds"] != null && _node["medalIds"].IsArray)
            {
                JSONArray kMedals = _node["medalIds"].AsArray;
                for (int i = 0; i < kMedals.Count; i++)
                {
                    int kMedalId = kMedals[i];
                    if (kMedalId > 0 && !kRole.m_MedalIds.Contains(kMedalId))
                    {
                        kRole.m_MedalIds.Add(kMedalId);
                    }
                }
            }

            kRole.m_ActiveSkillId = _node["activeSkillId"] == null ? 0 : _node["activeSkillId"].AsInt;
            kRole.m_PassiveSkillId = _node["passiveSkillId"] == null ? 0 : _node["passiveSkillId"].AsInt;

            if (_node["adaptations"] != null && _node["adaptations"].IsArray)
            {
                JSONArray kAdaptations = _node["adaptations"].AsArray;
                for (int i = 0; i < kAdaptations.Count; i++)
                {
                    JSONNode kItem = kAdaptations[i];
                    if (kItem == null)
                    {
                        continue;
                    }
                    int kType = kItem["id"] == null ? 0 : kItem["id"].AsInt;
                    int kLevel = kItem["level"] == null ? 0 : kItem["level"].AsInt;
                    if (kType > 0 && kLevel > 0)
                    {
                        kRole.m_Adaptations[kType] = kLevel;
                    }
                }
            }

            // 老存档没有 behaviors 时，按属性补算一次
            if (kRole.m_Behaviors.Count == 0)
            {
                kRole.m_Behaviors = RoleBehaviorRule.Build(kRole.m_Attributes);
            }

            return kRole;
        }

        public override string ToString()
        {
            return "{ id:" + m_Id + ", name:" + GetDisplayName() + ", level:" + m_Level
                + ", talent:" + m_TalentId + ", attrs:" + m_Attributes.Count
                + ", behaviors:" + m_Behaviors.Count + " }";
        }

        #endregion
    }

    /// <summary>
    /// 角色身上的一个持久效果（对应 buff 表的一条）
    /// </summary>
    public struct RoleEffect
    {
        public int m_Id;
        public int m_Stacks;

        public override string ToString()
        {
            return "{ effectId:" + m_Id + ", stacks:" + m_Stacks + " }";
        }
    }

    /// <summary>
    /// 属性展示项：给 UI 直接用的扁平结构（名字/数值/上限/展示文本）。
    /// </summary>
    public class RoleAttributeView
    {
        public int m_AttrId;
        public string m_Name = string.Empty;
        public int m_Value;
        public int m_MaxValue;
        /// <summary>展示用文本：普通属性是数值，天赋是天赋名</summary>
        public string m_DisplayValue = string.Empty;
        public bool m_Show;

        public override string ToString()
        {
            return m_Name + "：" + m_DisplayValue + (m_MaxValue > 0 ? ("/" + m_MaxValue) : string.Empty);
        }
    }
}
