using System.Collections.Generic;
using UnityEngine;

namespace GameMain.Script.Role
{
    /// <summary>
    /// 角色生成器：严格按 role.xlsx 这份"角色模板"掷点，生成一个全新的随机角色。
    ///
    /// 模板语义（role.xlsx 每一行）：
    ///   min      初始随机区间下限
    ///   max      初始随机区间上限
    ///   maxValue 属性上限（生成结果不会超过它）
    ///   show     是否为展示属性（只影响 UI 展示，不影响生成）
    ///
    /// 特例：属性ID 6（天赋）的 min~max 是 talent 表的 ID 区间，
    /// 生成时从 talent 表里挑一个落在该区间的天赋，而不是掷出一个无意义的数字。
    ///
    /// 技能：生成时从 skill 表随机分配一个主动技能；被动技能留给后续培养流程。
    ///
    /// 生成只依赖传入的 cfg.Tables，不读写权威数据，方便在编辑器里直接预览。
    /// </summary>
    public static class RoleFactory
    {
        /// <summary>
        /// 按模板生成一个角色（使用 Unity 的随机数）
        /// </summary>
        public static RoleRecord Roll(cfg.Tables _tables, int _roleId)
        {
            return Roll(_tables, _roleId, null);
        }

        /// <summary>
        /// 按模板生成一个角色。_random 传 null 时使用 UnityEngine.Random，
        /// 传 System.Random 可用于复现同一套属性（测试/回放）。
        /// </summary>
        public static RoleRecord Roll(cfg.Tables _tables, int _roleId, System.Random _random)
        {
            if (_tables == null || _tables.role == null)
            {
                Game.Log.LogError("[角色] 生成失败：role 表未加载");
                return null;
            }

            List<cfg.GameData_Role.RoleData> kTemplate = _tables.role.DataList;
            if (kTemplate == null || kTemplate.Count == 0)
            {
                Game.Log.LogError("[角色] 生成失败：role 表为空，请检查 role.xlsx");
                return null;
            }

            RoleRecord kRole = new RoleRecord(_roleId);
            kRole.m_CreateTime = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            kRole.m_Name = "调查员" + _roleId.ToString("D4");
            kRole.m_Level = 1;

            for (int i = 0; i < kTemplate.Count; i++)
            {
                cfg.GameData_Role.RoleData kAttr = kTemplate[i];
                if (kAttr == null)
                {
                    continue;
                }
                kRole.SetAttr(kAttr.ID, RollAttribute(_tables, kAttr, _random));
            }

            kRole.m_TalentId = kRole.GetAttr(RoleAttrId.Talent);
            kRole.m_Behaviors = RoleBehaviorRule.Build(kRole.m_Attributes);
            // 每个新角色随机一个主动技能（skill 表），被动技能后续培养再解锁
            kRole.m_ActiveSkillId = PickActiveSkill(_tables, _random);
            // 新人入职默认初级专员（职称评分 0），勋章/适应性由后续培养流程填；
            // 所有角色获得时默认穿戴"赤手空拳"（装备 5000）
            kRole.m_TitleScore = 0;
            kRole.m_EquipIds.Add(RoleTitleRule.DefaultEquipId);
            return kRole;
        }

        /// <summary>
        /// 掷单个属性
        /// </summary>
        public static int RollAttribute(cfg.Tables _tables, cfg.GameData_Role.RoleData _cfg, System.Random _random)
        {
            if (_cfg == null)
            {
                return 0;
            }

            // 天赋：从 talent 表里挑一个落在 [min,max] 区间的天赋ID
            if (_cfg.ID == RoleAttrId.Talent)
            {
                int kTalentId = PickTalent(_tables, _cfg, _random);
                if (kTalentId > 0)
                {
                    return kTalentId;
                }
            }

            int kLo = Mathf.RoundToInt(_cfg.Min);
            int kHi = Mathf.RoundToInt(_cfg.Max);
            if (kHi < kLo)
            {
                int kTmp = kLo;
                kLo = kHi;
                kHi = kTmp;
            }

            int kValue = NextInt(_random, kLo, kHi + 1);

            // 用 maxValue 封顶
            int kCap = Mathf.RoundToInt(_cfg.MaxValue);
            if (kCap > 0 && kValue > kCap)
            {
                kValue = kCap;
            }
            if (kValue < 0)
            {
                kValue = 0;
            }
            return kValue;
        }

        /// <summary>
        /// 从 talent 表里按 probability（万分比）随机挑一个天赋ID。
        /// 优先取落在角色模板区间内的天赋；区间内没有候选时退回全表。
        /// </summary>
        public static int PickTalent(cfg.Tables _tables, cfg.GameData_Role.RoleData _cfg, System.Random _random)
        {
            if (_tables == null || _tables.talent == null)
            {
                return 0;
            }

            List<cfg.GameData_Skill.TalentData> kAll = _tables.talent.DataList;
            if (kAll == null || kAll.Count == 0)
            {
                return 0;
            }

            int kLo = _cfg == null ? 0 : Mathf.RoundToInt(_cfg.Min);
            int kHi = _cfg == null ? 0 : Mathf.RoundToInt(_cfg.Max);

            List<cfg.GameData_Skill.TalentData> kCandidates =
                new List<cfg.GameData_Skill.TalentData>();
            for (int i = 0; i < kAll.Count; i++)
            {
                if (kAll[i] != null && kAll[i].ID >= kLo && kAll[i].ID <= kHi)
                {
                    kCandidates.Add(kAll[i]);
                }
            }

            if (kCandidates.Count == 0)
            {
                for (int i = 0; i < kAll.Count; i++)
                {
                    if (kAll[i] != null)
                    {
                        kCandidates.Add(kAll[i]);
                    }
                }
            }

            if (kCandidates.Count == 0)
            {
                return 0;
            }

            int kTotalWeight = 0;
            for (int i = 0; i < kCandidates.Count; i++)
            {
                kTotalWeight += Mathf.Max(0, kCandidates[i].Probability);
            }

            // 概率全为 0 时退回均匀随机，避免完全抽不出天赋
            if (kTotalWeight <= 0)
            {
                return kCandidates[NextInt(_random, 0, kCandidates.Count)].ID;
            }

            int kRoll = NextInt(_random, 0, kTotalWeight);
            int kAccum = 0;
            for (int i = 0; i < kCandidates.Count; i++)
            {
                kAccum += Mathf.Max(0, kCandidates[i].Probability);
                if (kRoll < kAccum)
                {
                    return kCandidates[i].ID;
                }
            }

            return kCandidates[kCandidates.Count - 1].ID;
        }

        /// <summary>
        /// 从 skill 表里等概率随机一个主动技能ID，表未加载/为空时返回 0。
        /// 目前 skill 表里的记录都是主动技能，范围由策划改表控制。
        /// </summary>
        public static int PickActiveSkill(cfg.Tables _tables, System.Random _random)
        {
            if (_tables == null || _tables.skill == null)
            {
                return 0;
            }

            List<cfg.GameData_Skill.SkillData> kAll = _tables.skill.DataList;
            if (kAll == null || kAll.Count == 0)
            {
                return 0;
            }

            List<cfg.GameData_Skill.SkillData> kCandidates =
                new List<cfg.GameData_Skill.SkillData>();
            for (int i = 0; i < kAll.Count; i++)
            {
                if (kAll[i] != null)
                {
                    kCandidates.Add(kAll[i]);
                }
            }

            if (kCandidates.Count == 0)
            {
                return 0;
            }

            return kCandidates[NextInt(_random, 0, kCandidates.Count)].ID;
        }

        private static int NextInt(System.Random _random, int _minInclusive, int _maxExclusive)
        {
            if (_maxExclusive <= _minInclusive)
            {
                return _minInclusive;
            }
            return _random == null
                ? UnityEngine.Random.Range(_minInclusive, _maxExclusive)
                : _random.Next(_minInclusive, _maxExclusive);
        }
    }
}
