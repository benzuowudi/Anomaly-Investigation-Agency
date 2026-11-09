using System.Collections.Generic;

namespace GameMain.Script.Role
{
    /// <summary>
    /// 获得角色的来源，便于统计与排查（不是生成的必需条件）
    /// </summary>
    public enum RoleAcquireReason
    {
        None = 0,
        /// <summary>抽卡/招募</summary>
        Lottery = 1,
        /// <summary>剧情奖励</summary>
        StoryReward = 2,
        /// <summary>商店购买</summary>
        ShopBuy = 3,
        /// <summary>调试指令</summary>
        Debug = 4,
        /// <summary>读档时的批量还原</summary>
        Load = 5,
    }

    /// <summary>
    /// 单个角色发生的变化类型
    /// </summary>
    public enum RoleChangeOp
    {
        None = 0,
        /// <summary>获得</summary>
        Acquired = 1,
        /// <summary>移除</summary>
        Removed = 2,
        /// <summary>属性变化（训练/成长/受伤）</summary>
        Attribute = 3,
        /// <summary>等级变化</summary>
        Level = 4,
        /// <summary>天赋变化</summary>
        Talent = 5,
        /// <summary>改名</summary>
        Name = 6,
        /// <summary>持久效果变化（受伤/致残/治疗）</summary>
        Effect = 7,
        /// <summary>职称评分/职称等级变化</summary>
        Title = 8,
        /// <summary>装备穿脱</summary>
        Equip = 9,
        /// <summary>勋章佩戴/卸下</summary>
        Medal = 10,
        /// <summary>技能变化</summary>
        Skill = 11,
        /// <summary>异常适应性变化</summary>
        Adaptation = 12,
    }

    /// <summary>
    /// 角色职称（培养系统文档：初级专员 / 高级专员 / 资深专员）
    /// </summary>
    public enum RoleTitleLevel
    {
        /// <summary>初级专员（入职默认）</summary>
        Junior = 0,
        /// <summary>高级专员（职称评分 10）</summary>
        Senior = 1,
        /// <summary>资深专员（职称评分 50）</summary>
        Veteran = 2,
    }

    /// <summary>
    /// 职称规则（初版先写在代码里，等策划出 title.xlsx 后再改为读表）：
    ///   · 评分 0/10/50 对应 初级/高级/资深；
    ///   · 参与行动获得 1~5 分，非任务途径（巡逻/培训）每天最多 5 分；
    ///   · 职称越高可穿戴的护甲件数与勋章槽位越多：护甲 1/2/3（对应两件套/三件套）、勋章 1/2/3；
    ///   · 武器/支援类（type_1 = 1,2,3,5,6,7,8）始终只能穿 1 件；
    ///   · 职称对属性的加成：敏捷 +5/级，感知 +10 点/级（再用 role.xlsx 的 maxValue 封顶）。
    /// 这些数值都来自策划文档，改规则只需要动这个类。
    /// </summary>
    public static class RoleTitleRule
    {
        /// <summary>高级专员的评分门槛</summary>
        public const int SeniorScore = 10;
        /// <summary>资深专员的评分门槛</summary>
        public const int VeteranScore = 50;

        /// <summary>护甲类型（equip.type_1 = 4，头盔/躯干/鞋子三件套）</summary>
        public const int ArmorType1 = 4;

        /// <summary>根据评分算职称等级</summary>
        public static RoleTitleLevel GetLevel(int _score)
        {
            if (_score >= VeteranScore)
            {
                return RoleTitleLevel.Veteran;
            }
            if (_score >= SeniorScore)
            {
                return RoleTitleLevel.Senior;
            }
            return RoleTitleLevel.Junior;
        }

        /// <summary>职称名称</summary>
        public static string GetName(RoleTitleLevel _level)
        {
            switch (_level)
            {
                case RoleTitleLevel.Senior:
                    return "高级专员";
                case RoleTitleLevel.Veteran:
                    return "资深专员";
                default:
                    return "初级专员";
            }
        }

        /// <summary>职称英文名（角色界面竖排英文副标题用）</summary>
        public static string GetNameEn(RoleTitleLevel _level)
        {
            switch (_level)
            {
                case RoleTitleLevel.Senior:
                    return "SENIOR";
                case RoleTitleLevel.Veteran:
                    return "VETERAN";
                default:
                    return "JUNIOR";
            }
        }

        /// <summary>下一级职称需要的评分，已满级返回 -1</summary>
        public static int GetNextScore(RoleTitleLevel _level)
        {
            switch (_level)
            {
                case RoleTitleLevel.Junior:
                    return SeniorScore;
                case RoleTitleLevel.Senior:
                    return VeteranScore;
                default:
                    return -1;
            }
        }

        /// <summary>护甲槽位数：初级 1 / 高级 2 / 资深 3</summary>
        public static int GetArmorSlots(RoleTitleLevel _level)
        {
            return (int)_level + 1;
        }

        /// <summary>勋章槽位数：初级 1 / 高级 2 / 资深 3（文档上限 3 枚）</summary>
        public static int GetMedalSlots(RoleTitleLevel _level)
        {
            return (int)_level + 1;
        }

        /// <summary>武器/支援槽位：始终 1 件</summary>
        public static int GetWeaponSlots()
        {
            return 1;
        }

        /// <summary>某件装备占用的槽位是否属于护甲（否则算武器槽）</summary>
        public static bool IsArmor(int _type1)
        {
            return _type1 == ArmorType1;
        }

        /// <summary>
        /// 职称带来的属性加成（不包含装备/勋章，它们由 RoleSnapshot 单独汇总）
        /// </summary>
        public static int GetAttrBonus(int _attrId, RoleTitleLevel _level, int _baseValue)
        {
            int kLevel = (int)_level;
            if (kLevel <= 0)
            {
                return 0;
            }

            switch (_attrId)
            {
                case RoleAttrId.Agi:
                    return 5 * kLevel;
                case RoleAttrId.Per:
                    // 文档：感知基础 100，职称提升 +10%/级、上限 130。
                    // role.xlsx 目前是 0~100 勺点，按百分比算加成会接近 0，所以按文档取固定 +10/级；
                    // 若策划把感知改成“基础 100”的写法，把这里换回 Mathf.RoundToInt(_baseValue * 0.1f * kLevel) 即可。
                    return 10 * kLevel;
                default:
                    return 0;
            }
        }
    }

    /// <summary>
    /// 角色持械手偏好
    /// </summary>
    public enum RoleHand
    {
        Right = 0,
        Left = 1,
        /// <summary>左右手都偏好（双持）</summary>
        Both = 2,
    }

    /// <summary>
    /// role.xlsx 的属性ID。
    /// 配置表是"角色模板"：每一行就是一个属性的初始随机区间（min~max）、属性上限（maxValue）和是否展示（show）。
    /// 这里的常量必须与 role.xlsx 的 ID 列保持一致，策划改表时同步改这里。
    /// </summary>
    public static class RoleAttrId
    {
        /// <summary>生命</summary>
        public const int Hp = 1;
        /// <summary>攻击</summary>
        public const int Atk = 2;
        /// <summary>敏捷</summary>
        public const int Agi = 3;
        /// <summary>感知</summary>
        public const int Per = 4;
        /// <summary>理智</summary>
        public const int San = 5;
        /// <summary>天赋（值域即 talent 表的 ID 区间，生成时随机取一个天赋ID）</summary>
        public const int Talent = 6;
        /// <summary>攻击距离倾向：0 近身 / 1 远程</summary>
        public const int AttackRangePrefer = 8;
        /// <summary>自我治疗倾向：0 自我疗伤 / 1 寻求治疗</summary>
        public const int SelfHealPrefer = 9;
        /// <summary>使用装备倾向：0 普通武器 / 1 炼金武器 / 2 两者都偏好</summary>
        public const int EquipPrefer = 10;
        /// <summary>使用道具倾向：0 不用 / 1 异常道具 / 2 异常道具+投掷物</summary>
        public const int ItemPrefer = 11;
        /// <summary>左手偏好：0 否 / 1 是</summary>
        public const int LeftHandPrefer = 12;
        /// <summary>右手偏好：0 否 / 1 是</summary>
        public const int RightHandPrefer = 13;
    }

    /// <summary>
    /// behaviorLogic.xlsx 的行为逻辑ID，与配置表的 ID 列保持一致。
    /// </summary>
    public static class RoleBehaviorId
    {
        /// <summary>倾向近身战斗</summary>
        public const int MeleeCombat = 40001;
        /// <summary>倾向远程战斗</summary>
        public const int RangedCombat = 40002;
        /// <summary>倾向自我疗伤</summary>
        public const int SelfHeal = 40003;
        /// <summary>倾向寻求治疗</summary>
        public const int SeekHeal = 40004;
        /// <summary>倾向治疗他人</summary>
        public const int HealOthers = 40005;
        /// <summary>倾向退出战斗</summary>
        public const int Retreat = 40006;
        /// <summary>倾向使用普通武器</summary>
        public const int NormalWeapon = 40007;
        /// <summary>倾向使用炼金武器</summary>
        public const int AlchemyWeapon = 40008;
        /// <summary>倾向使用异常道具</summary>
        public const int AnomalyItem = 40009;
        /// <summary>倾向使用投掷物</summary>
        public const int Throwable = 40010;
    }

    /// <summary>
    /// 角色初始化时的行为逻辑推导规则（初版）。
    ///
    /// 说明：behaviorLogic 表只定义了"有哪些行为倾向"，并没有规定 role 的倾向数值怎么映射到行为逻辑，
    /// 因此这里用一张显式的映射规则把两者接起来，策划调整规则时只改这一个类：
    ///   8  攻击距离倾向  0 → 40001 近身       1 → 40002 远程
    ///   9  自我治疗倾向  0 → 40003 自我疗伤   1 → 40004 寻求治疗
    ///   10 使用装备倾向  0 → 40007 普通武器   1 → 40008 炼金武器   2 → 40007 + 40008
    ///   11 使用道具倾向  0 → 无               1 → 40009 异常道具   2 → 40009 + 40010 投掷物
    ///   12/13 左右手偏好 只决定主手，不产生行为逻辑
    /// 40005（治疗他人）与 40006（退出战斗）属于战斗内的动态判断（例如身上有【伤愈】/【沮丧】时才追加），
    /// 不在角色生成期固化，避免写死。
    /// </summary>
    public static class RoleBehaviorRule
    {
        /// <summary>
        /// 根据角色属性推导初始行为逻辑ID列表（去重、按配置顺序）
        /// </summary>
        public static List<int> Build(Dictionary<int, int> _attributes)
        {
            List<int> kResult = new List<int>();
            if (_attributes == null)
            {
                return kResult;
            }

            // 攻击距离倾向
            if (Get(_attributes, RoleAttrId.AttackRangePrefer) >= 1)
            {
                Add(kResult, RoleBehaviorId.RangedCombat);
            }
            else
            {
                Add(kResult, RoleBehaviorId.MeleeCombat);
            }

            // 自我治疗倾向
            if (Get(_attributes, RoleAttrId.SelfHealPrefer) >= 1)
            {
                Add(kResult, RoleBehaviorId.SeekHeal);
            }
            else
            {
                Add(kResult, RoleBehaviorId.SelfHeal);
            }

            // 使用装备倾向
            int kEquip = Get(_attributes, RoleAttrId.EquipPrefer);
            if (kEquip >= 2)
            {
                Add(kResult, RoleBehaviorId.NormalWeapon);
                Add(kResult, RoleBehaviorId.AlchemyWeapon);
            }
            else if (kEquip == 1)
            {
                Add(kResult, RoleBehaviorId.AlchemyWeapon);
            }
            else
            {
                Add(kResult, RoleBehaviorId.NormalWeapon);
            }

            // 使用道具倾向
            int kItem = Get(_attributes, RoleAttrId.ItemPrefer);
            if (kItem >= 2)
            {
                Add(kResult, RoleBehaviorId.AnomalyItem);
                Add(kResult, RoleBehaviorId.Throwable);
            }
            else if (kItem == 1)
            {
                Add(kResult, RoleBehaviorId.AnomalyItem);
            }

            return kResult;
        }

        /// <summary>
        /// 根据左右手偏好推导主手
        /// </summary>
        public static RoleHand GetMainHand(Dictionary<int, int> _attributes)
        {
            int kLeft = Get(_attributes, RoleAttrId.LeftHandPrefer);
            int kRight = Get(_attributes, RoleAttrId.RightHandPrefer);
            if (kLeft >= 1 && kRight >= 1)
            {
                return RoleHand.Both;
            }
            if (kLeft >= 1)
            {
                return RoleHand.Left;
            }
            return RoleHand.Right;
        }

        private static int Get(Dictionary<int, int> _attributes, int _attrId)
        {
            int kValue;
            return _attributes.TryGetValue(_attrId, out kValue) ? kValue : 0;
        }

        private static void Add(List<int> _list, int _behaviorId)
        {
            if (!_list.Contains(_behaviorId))
            {
                _list.Add(_behaviorId);
            }
        }
    }
}
