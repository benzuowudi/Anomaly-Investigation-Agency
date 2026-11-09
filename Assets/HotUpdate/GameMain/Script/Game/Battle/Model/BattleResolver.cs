using GameMain.Script.Role;
using UnityEngine;

namespace GameMain.Script.Battle
{
    /// <summary>一次攻击的结算结果</summary>
    public struct BattleDamage
    {
        /// <summary>是否命中（false = 被闪避）</summary>
        public bool m_IsHit;
        /// <summary>实际伤害</summary>
        public int m_Damage;
        /// <summary>结算后目标血量</summary>
        public int m_TargetHpAfter;
        /// <summary>目标是否被打到 0 血（进入重伤，不等于死亡）</summary>
        public bool m_IsDowned;
        /// <summary>本次的闪避率（日志用）</summary>
        public float m_DodgeRate;

        public override string ToString()
        {
            return "{ hit:" + m_IsHit + " dmg:" + m_Damage + " hpAfter:" + m_TargetHpAfter
                + " downed:" + m_IsDowned + " dodge:" + m_DodgeRate.ToString("F2") + " }";
        }
    }

    /// <summary>
    /// 战斗结算器：命中/闪避/伤害。
    ///
    /// 公式（对应文档 8 / 文档 5）：
    ///   闪避率 = min(dodgeCap, 目标敏捷 × dodgePerAgi)        —— 敏捷 10~50、1%/点、上限 75%
    ///   命中率 = clamp01(1 − 闪避率 + 攻击者命中修正)
    ///   攻击来源 = 角色攻击 + 武器攻击（枪械按配置只取武器攻击，等 equip 扩列后接表）
    ///   基础伤害 = 攻击来源 × 伤害倍率
    ///   最终伤害 = max(1, 基础伤害 − 目标防御)
    ///
    /// 随机数由外部注入固定种子，保证同一局面可复现（调试与回放要用）。
    /// </summary>
    public class BattleResolver
    {
        private readonly BattleConfig m_Config;
        private readonly System.Random m_Random;

        public BattleResolver(BattleConfig _config, System.Random _random)
        {
            m_Config = _config;
            m_Random = _random;
        }

        /// <summary>目标当前闪避率（0~dodgeCap）</summary>
        public float GetDodgeRate(BattleUnit _target)
        {
            if (_target == null)
            {
                return 0f;
            }

            float kDodge = _target.GetAttr(RoleAttrId.Agi) * m_Config.DodgePerAgi;
            return Mathf.Min(m_Config.DodgeCap, Mathf.Max(0f, kDodge));
        }

        /// <summary>攻击者对该目标的命中率（0~1）</summary>
        public float GetHitChance(BattleUnit _attacker, BattleUnit _target)
        {
            float kDodge = GetDodgeRate(_target);
            float kBonus = _attacker == null ? 0f : _attacker.m_HitBonus;
            return Mathf.Clamp01(1f - kDodge + kBonus);
        }

        /// <summary>
        /// 结算一次普通攻击。不修改目标血量，只返回结果，由调用方写入并广播事件。
        /// </summary>
        public BattleDamage ResolveAttack(BattleUnit _attacker, BattleUnit _target)
        {
            BattleDamage kResult = new BattleDamage();
            if (_attacker == null || _target == null || !_target.IsAlive())
            {
                return kResult;
            }

            float kDodge = GetDodgeRate(_target);
            kResult.m_DodgeRate = kDodge;
            kResult.m_TargetHpAfter = _target.m_Hp;

            float kHitChance = GetHitChance(_attacker, _target);
            if ((float)m_Random.NextDouble() > kHitChance)
            {
                kResult.m_IsHit = false;
                return kResult;
            }

            float kAttackSource = _attacker.GetAttr(RoleAttrId.Atk) + _attacker.m_WeaponAtk;
            float kRaw = kAttackSource * _attacker.m_DamageRate;
            float kAfterDef = kRaw - _target.m_Defense;

            int kDamage = Mathf.RoundToInt(kAfterDef);
            if (kDamage < BattleConst.MinDamage)
            {
                kDamage = BattleConst.MinDamage;
            }

            int kHpAfter = _target.m_Hp - kDamage;
            if (kHpAfter < 0)
            {
                kHpAfter = 0;
            }

            kResult.m_IsHit = true;
            kResult.m_Damage = kDamage;
            kResult.m_TargetHpAfter = kHpAfter;
            kResult.m_IsDowned = kHpAfter <= 0;
            return kResult;
        }
    }
}
