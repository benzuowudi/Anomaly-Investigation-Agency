using System.Collections.Generic;
using cfg.GameType;

namespace GameMain.Script.Battle
{
    /// <summary>
    /// 战斗中的一个单位（角色 / 敌人 / 召唤物 / 中立）。
    ///
    /// 只是一份运行时值对象：
    /// - 不进 Data、不落盘，一场战斗结束即丢弃；
    /// - 角色侧的数据来自 RoleSnapshot，敌人侧来自 enemy.xlsx；
    /// - 属性修改一律叠加在快照之上，不写回 RoleRecord。
    /// </summary>
    public class BattleUnit
    {
        #region 身份

        /// <summary>战斗内唯一ID</summary>
        public int m_UnitId;
        /// <summary>来源ID：角色ID / 敌人ID / 召唤物ID</summary>
        public int m_SourceId;
        public UnitKind m_Kind = UnitKind.Role;
        public Faction m_Faction = Faction.ally;
        /// <summary>同阵营内的序号（决定站位与前后排名次）</summary>
        public int m_TeamIndex;

        #endregion

        #region 数值

        public int m_Hp;
        public int m_MaxHp;
        public int m_San = 100;
        /// <summary>属性ID → 当前值（快照 + 修正后的结果）</summary>
        public readonly Dictionary<int, int> m_Attrs = new Dictionary<int, int>();
        /// <summary>弹匣内剩余弹药</summary>
        public int m_AmmoInMag;
        public int m_MagazineSize;

        #endregion

        #region 位置与移动

        /// <summary>一维坐标（米）</summary>
        public float m_Pos;
        /// <summary>移速（米/秒）</summary>
        public float m_MoveSpeed = BattleConst.MoveSpeedBase;

        #endregion

        #region 状态

        public UnitBrainState m_State = UnitBrainState.Idle;
        /// <summary>本场主动技能ID（0 = 没有），来源 RoleSnapshot 的 m_ActiveSkill</summary>
        public int m_ActiveSkillId;
        /// <summary>本场已施放技能次数（调试验收用）</summary>
        public int m_SkillUseCount;
        /// <summary>技能ID → 剩余冷却（秒，独立计时）</summary>
        public readonly Dictionary<int, float> m_SkillCd = new Dictionary<int, float>();
        /// <summary>战斗携带的补给品/弹药：itemID → 数量</summary>
        public readonly Dictionary<int, int> m_CarriedItems = new Dictionary<int, int>();

        /// <summary>当前目标单位ID（0 = 无目标）</summary>
        public int m_TargetUnitId;

        /// <summary>ATB 行动条 0~1</summary>
        public float m_ActionGauge;
        /// <summary>填满行动条所需秒数（= 武器攻击间隔 / 攻速修正）</summary>
        public float m_ActionInterval = BattleConst.DefaultActionInterval;
        /// <summary>已完成的行动次数（调试验收用）</summary>
        public int m_ActionCount;
        /// <summary>当前状态剩余时间（攻击前摇/换弹等）</summary>
        public float m_StateTimer;

        /// <summary>武器最小射程（米）</summary>
        public float m_AttackRangeMin;
        /// <summary>武器最大射程（米）；没有武器配置时用近战默认值</summary>
        public float m_AttackRangeMax = BattleConst.DefaultAttackRangeMax;

        /// <summary>武器攻击加成（刀具/枪械的加成值）</summary>
        public int m_WeaponAtk;
        /// <summary>伤害倍率（1 = 100%）</summary>
        public float m_DamageRate = 1f;
        /// <summary>防御（减伤，直接扣减）</summary>
        public int m_Defense;
        /// <summary>命中修正（+0.1 = +10%）</summary>
        public float m_HitBonus;
        /// <summary>换弹时间（秒，0 = 无弹药概念）</summary>
        public float m_ReloadTime = 1.5f;
        /// <summary>本次攻击的目标ID（前摇结束时结算用）</summary>
        public int m_AttackTargetUnitId;

        /// <summary>集火指令指定的目标（0 = 无，按最近敌人自动选）</summary>
        public int m_FocusTargetUnitId;
        /// <summary>是否正在执行撤离</summary>
        public bool m_Retreating;
        /// <summary>是否已经越界离场（离场不算死亡，不计入存活）</summary>
        public bool m_LeftField;

        /// <summary>重伤剩余秒数（跨战斗保留，由结算写回 RoleRecord）</summary>
        public float m_DownedRemain;

        #endregion

        #region 查询

        public bool IsAlive()
        {
            return m_State != UnitBrainState.Dead;
        }

        public bool CanAct()
        {
            return m_State != UnitBrainState.Dead
                && m_State != UnitBrainState.Downed
                && m_State != UnitBrainState.Controlled;
        }

        public int GetAttr(int _attrId)
        {
            int kValue;
            return m_Attrs.TryGetValue(_attrId, out kValue) ? kValue : 0;
        }

        public void SetAttr(int _attrId, int _value)
        {
            m_Attrs[_attrId] = _value;
        }

        /// <summary>血量百分比（0~1），上限为 0 时返回 0</summary>
        public float GetHpPercent()
        {
            return m_MaxHp <= 0 ? 0f : (float)m_Hp / m_MaxHp;
        }

        #endregion

        public override string ToString()
        {
            return "{ unit:" + m_UnitId + " src:" + m_SourceId + " faction:" + m_Faction
                + " hp:" + m_Hp + "/" + m_MaxHp + " pos:" + m_Pos.ToString("F2")
                + " state:" + m_State + " gauge:" + m_ActionGauge.ToString("F2")
                + " act:" + m_ActionCount + " }";
        }
    }
}
