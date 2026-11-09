using System.Collections.Generic;
using cfg.GameType;
using GameMain.Script.Role;
using UnityEngine;

namespace GameMain.Script.Battle
{
    /// <summary>
    /// 战斗单位生成器（纯函数，允许 static：只依赖入参与配置表，不读存档、不发事件）。
    ///
    /// 两个入口：
    /// - BuildFromEncounter：正式路径，encounter + encounterEnemy + battlefield + enemy.xlsx → 单位列表；
    /// - BuildDebug：调试路径，没配遭遇也能凑出一队 4v4（敌人用合成模板）。
    ///
    /// 空表降级：encounter 不存在 / 没有敌人编成时返回空列表并打日志，由调用方决定是否走调试路径。
    /// </summary>
    public static class BattleFactory
    {
        /// <summary>没有配置时敌人的默认模板</summary>
        public const int DefaultEnemyHp = 100;
        public const int DefaultEnemyAtk = 10;
        public const int DefaultEnemyAgi = 20;
        public const int DefaultEnemyPer = 50;

        /// <summary>我方默认落点（没有 battlefield 配置时）</summary>
        public const float DefaultAllyMin = 2f;
        public const float DefaultAllyMax = 8f;

        /// <summary>敌人默认落点（没有 battlefield 配置时）</summary>
        public const float DefaultEnemyPos = 22f;

        #region 正式路径

        /// <summary>
        /// 按遭遇配置生成一场战斗的全部单位。数据不全时返回 null。
        /// </summary>
        public static List<BattleUnit> BuildFromEncounter(cfg.Tables _tables, int _encounterId,
            IReadOnlyList<RoleSnapshot> _allies, ref int _nextUnitId)
        {
            if (_tables == null)
            {
                Game.Log.LogError("[战斗] 配置表未加载，无法生成遭遇");
                return null;
            }

            cfg.GameData_Battle.EncounterData kEncounter =
                _tables.encounter == null ? null : _tables.encounter.GetOrDefault(_encounterId);
            if (kEncounter == null)
            {
                Game.Log.LogWarning("[战斗] 遭遇不存在：" + _encounterId);
                return null;
            }

            cfg.GameData_Battle.BattlefieldData kField =
                _tables.battlefield == null ? null : _tables.battlefield.GetOrDefault(kEncounter.BattlefieldID);

            float kLength = kField != null && kField.Length > 0f ? kField.Length : 40f;
            float kAllyMin = kField != null ? kField.AllyDefaultMin : DefaultAllyMin;
            float kAllyMax = kField != null ? kField.AllyDefaultMax : DefaultAllyMax;

            List<BattleUnit> kUnits = new List<BattleUnit>();

            // 我方
            int kAllyCount = _allies == null ? 0 : _allies.Count;
            for (int i = 0; i < kAllyCount; i++)
            {
                float kPos = SpreadPosition(kAllyMin, kAllyMax, i, kAllyCount);
                BattleUnit kUnit = CreateAlly(_allies[i], i, kPos, _nextUnitId++);
                if (kUnit != null)
                {
                    kUnits.Add(kUnit);
                }
            }

            // 敌方
            int kEnemyIndex = 0;
            if (_tables.encounterEnemy != null)
            {
                List<cfg.GameData_Battle.EncounterEnemyData> kRows = _tables.encounterEnemy.DataList;
                for (int i = 0; i < kRows.Count; i++)
                {
                    cfg.GameData_Battle.EncounterEnemyData kRow = kRows[i];
                    if (kRow == null || kRow.EncounterID != _encounterId)
                    {
                        continue;
                    }

                    cfg.GameData_Enemy.EnemyData kEnemyCfg =
                        _tables.enemy == null ? null : _tables.enemy.GetOrDefault(kRow.EnemyID);

                    int kCount = kRow.Count <= 0 ? 1 : kRow.Count;
                    float kBasePos = Mathf.Clamp01(kRow.PositionPercent) * kLength;

                    for (int c = 0; c < kCount; c++)
                    {
                        // 同组多只稍微错开，避免完全重叠
                        float kPos = Mathf.Clamp(kBasePos + c * 0.8f, 0f, kLength);
                        BattleUnit kUnit = CreateEnemy(kEnemyCfg, kEnemyIndex, kPos, _nextUnitId++, kRow.LevelScale);
                        if (kUnit != null)
                        {
                            kUnits.Add(kUnit);
                        }
                        kEnemyIndex++;
                    }
                }
            }

            if (kEnemyIndex == 0)
            {
                Game.Log.LogWarning("[战斗] 遭遇没有敌人编成：" + _encounterId);
            }

            return kUnits;
        }

        #endregion

        #region 调试路径

        /// <summary>
        /// 调试用：凑出 allyCount + enemyCount 个单位，不依赖 encounter 数据。
        /// 敌人配置缺失时用合成模板。
        /// </summary>
        public static List<BattleUnit> BuildDebug(cfg.Tables _tables, IReadOnlyList<RoleSnapshot> _allies,
            int _allyCount, int _enemyCount, ref int _nextUnitId)
        {
            List<BattleUnit> kUnits = new List<BattleUnit>();

            int kAllyReal = _allies == null ? 0 : _allies.Count;
            int kAllyTarget = _allyCount > 0 ? _allyCount : kAllyReal;
            for (int i = 0; i < kAllyTarget; i++)
            {
                float kPos = SpreadPosition(DefaultAllyMin, DefaultAllyMax, i, kAllyTarget);
                BattleUnit kUnit = i < kAllyReal
                    ? CreateAlly(_allies[i], i, kPos, _nextUnitId++)
                    : CreateSyntheticAlly(i, kPos, _nextUnitId++);
                if (kUnit != null)
                {
                    kUnits.Add(kUnit);
                }
            }

            for (int i = 0; i < _enemyCount; i++)
            {
                float kPos = DefaultEnemyPos + i * 1.5f;
                kUnits.Add(CreateSyntheticEnemy(i, kPos, _nextUnitId++));
            }

            return kUnits;
        }

        #endregion

        #region 单位构造

        private static BattleUnit CreateAlly(RoleSnapshot _snapshot, int _teamIndex, float _pos, int _unitId)
        {
            if (_snapshot == null)
            {
                return null;
            }

            BattleUnit kUnit = new BattleUnit();
            kUnit.m_UnitId = _unitId;
            kUnit.m_SourceId = _snapshot.m_Role == null ? 0 : _snapshot.m_Role.m_Id;
            kUnit.m_Kind = UnitKind.Role;
            kUnit.m_Faction = Faction.ally;
            kUnit.m_TeamIndex = _teamIndex;

            int kHp = _snapshot.GetFinalAttr(RoleAttrId.Hp);
            kUnit.m_MaxHp = kHp > 0 ? kHp : DefaultEnemyHp;
            kUnit.m_Hp = kUnit.m_MaxHp;

            int kSan = _snapshot.GetFinalAttr(RoleAttrId.San);
            kUnit.m_San = kSan > 0 ? kSan : 100;

            kUnit.SetAttr(RoleAttrId.Hp, kUnit.m_MaxHp);
            kUnit.SetAttr(RoleAttrId.Atk, _snapshot.GetFinalAttr(RoleAttrId.Atk));
            kUnit.SetAttr(RoleAttrId.Agi, _snapshot.GetFinalAttr(RoleAttrId.Agi));
            kUnit.SetAttr(RoleAttrId.Per, _snapshot.GetFinalAttr(RoleAttrId.Per));
            kUnit.SetAttr(RoleAttrId.San, kUnit.m_San);

            kUnit.m_Pos = _pos;
            kUnit.m_MoveSpeed = BattleConst.MoveSpeedBase;
            kUnit.m_ActionInterval = BattleConst.DefaultActionInterval;
            kUnit.m_WeaponAtk = BattleConst.DefaultWeaponAtk;
            kUnit.m_DamageRate = BattleConst.DefaultDamageRate;
            kUnit.m_Defense = _snapshot.m_Defense;
            kUnit.m_HitBonus = 0f;
            kUnit.m_MagazineSize = 0;
            kUnit.m_State = UnitBrainState.Idle;

            // 主动技能：技能系统读这个 ID 去查 skill.xlsx；CD 每场归零（已拍板）
            kUnit.m_ActiveSkillId = _snapshot.m_ActiveSkill == null ? 0 : _snapshot.m_ActiveSkill.ID;
            if (kUnit.m_ActiveSkillId > 0)
            {
                kUnit.m_SkillCd[kUnit.m_ActiveSkillId] = 0f;
            }
            return kUnit;
        }

        private static BattleUnit CreateEnemy(cfg.GameData_Enemy.EnemyData _cfg, int _teamIndex, float _pos,
            int _unitId, float _levelScale)
        {
            if (_cfg == null)
            {
                return CreateSyntheticEnemy(_teamIndex, _pos, _unitId);
            }

            float kScale = _levelScale <= 0f ? 1f : _levelScale;

            BattleUnit kUnit = new BattleUnit();
            kUnit.m_UnitId = _unitId;
            kUnit.m_SourceId = _cfg.ID;
            kUnit.m_Kind = UnitKind.Enemy;
            kUnit.m_Faction = Faction.enemy;
            kUnit.m_TeamIndex = _teamIndex;

            kUnit.m_MaxHp = Mathf.Max(1, Mathf.RoundToInt(_cfg.EnemyHp * kScale));
            kUnit.m_Hp = kUnit.m_MaxHp;
            kUnit.m_San = 100;

            kUnit.SetAttr(RoleAttrId.Hp, kUnit.m_MaxHp);
            kUnit.SetAttr(RoleAttrId.Atk, Mathf.RoundToInt(_cfg.EnemyAtk * kScale));
            kUnit.SetAttr(RoleAttrId.Agi, _cfg.EnemyAgi);
            kUnit.SetAttr(RoleAttrId.Per, _cfg.EnemyPer);
            kUnit.SetAttr(RoleAttrId.San, kUnit.m_San);

            kUnit.m_Pos = _pos;
            kUnit.m_MoveSpeed = BattleConst.MoveSpeedBase;
            kUnit.m_ActionInterval = BattleConst.DefaultActionInterval;
            kUnit.m_WeaponAtk = BattleConst.DefaultWeaponAtk;
            kUnit.m_DamageRate = BattleConst.DefaultDamageRate;
            kUnit.m_Defense = _cfg.EnemyDef;
            kUnit.m_MagazineSize = 0;
            kUnit.m_State = UnitBrainState.Idle;
            return kUnit;
        }

        private static BattleUnit CreateSyntheticEnemy(int _teamIndex, float _pos, int _unitId)
        {
            BattleUnit kUnit = new BattleUnit();
            kUnit.m_UnitId = _unitId;
            kUnit.m_SourceId = 0;
            kUnit.m_Kind = UnitKind.Enemy;
            kUnit.m_Faction = Faction.enemy;
            kUnit.m_TeamIndex = _teamIndex;
            kUnit.m_MaxHp = DefaultEnemyHp;
            kUnit.m_Hp = kUnit.m_MaxHp;
            kUnit.m_San = 100;

            kUnit.SetAttr(RoleAttrId.Hp, kUnit.m_MaxHp);
            kUnit.SetAttr(RoleAttrId.Atk, DefaultEnemyAtk);
            kUnit.SetAttr(RoleAttrId.Agi, DefaultEnemyAgi);
            kUnit.SetAttr(RoleAttrId.Per, DefaultEnemyPer);
            kUnit.SetAttr(RoleAttrId.San, kUnit.m_San);

            kUnit.m_Pos = _pos;
            kUnit.m_MoveSpeed = BattleConst.MoveSpeedBase;
            kUnit.m_ActionInterval = BattleConst.DefaultActionInterval;
            kUnit.m_WeaponAtk = BattleConst.DefaultWeaponAtk;
            kUnit.m_DamageRate = BattleConst.DefaultDamageRate;
            kUnit.m_Defense = 0;
            kUnit.m_MagazineSize = 0;
            kUnit.m_State = UnitBrainState.Idle;
            return kUnit;
        }

        private static BattleUnit CreateSyntheticAlly(int _teamIndex, float _pos, int _unitId)
        {
            BattleUnit kUnit = CreateSyntheticEnemy(_teamIndex, _pos, _unitId);
            kUnit.m_Kind = UnitKind.Role;
            kUnit.m_Faction = Faction.ally;
            return kUnit;
        }

        #endregion

        #region 工具

        /// <summary>把 count 个单位均匀铺在 [min,max] 上，单个时取中点</summary>
        public static float SpreadPosition(float _min, float _max, int _index, int _count)
        {
            if (_count <= 1)
            {
                return (_min + _max) * 0.5f;
            }
            float kStep = (_max - _min) / (_count - 1);
            return _min + kStep * _index;
        }

        #endregion
    }
}
