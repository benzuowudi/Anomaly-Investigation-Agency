using System.Collections.Generic;
using Framework.Script.Model;
using GameMain.Script.Battle;
using GameMain.Script.Explore;
using GameMain.Script.Inventory;
using GameMain.Script.Role;

namespace GameMain.Script.BattleSetting
{
    /// <summary>
    /// 战前整备系统的主模型（ModelBase）。
    ///
    /// 职责：
    /// - 维护出战队伍（最多 5 人）与每名队员的归一化站位；
    /// - 维护机械骡货仓（3 格）与队伍支援栏（5 格）的物资分配；
    /// - 校验并确认开战：把配置交给 BattleSystem，成功后在战场单位生成的第一时间应用站位；
    /// - 所有写操作成功后广播 BattleSettingEvent.OnBattleSettingChanged 并存档。
    ///
    /// 边界（规范 §4）：
    /// - 只读写本系统的 BattleSettingData；角色/仓库数据只做只读查询与校验；
    /// - 不引用任何 FairyGUI / UIWindow 类型，界面通过 Snapshot + 事件刷新；
    /// - 开战本身归 BattleSystem，这里只负责"按配置发起"。
    /// </summary>
    public class BattleSettingSystem : ModelBase
    {
        private BattleSettingData DB
        {
            get { return Game.Data.GetGameData<BattleSettingData>(); }
        }

        private static BattleSystem Battle
        {
            get { return Game.Model.Get<BattleSystem>(); }
        }

        private static RoleSystem Role
        {
            get { return Game.Model.Get<RoleSystem>(); }
        }

        private static InventorySystem Inventory
        {
            get { return Game.Model.Get<InventorySystem>(); }
        }

        #region 生命周期

        protected internal override void OnStart()
        {
            Load();

            // 读档后做一次兜底：清掉已不存在的角色、没配站位就均分。
            // 注意：不自动补满队伍——战前整备默认是空队伍，由玩家点空槽位选人。
            RemoveMissingRoles();
            EnsureDefaultPositions();
        }

        #endregion

        #region 队伍

        public IReadOnlyList<int> GetTeamRoleIds()
        {
            return DB.GetTeamRoleIds();
        }

        public int GetTeamCount()
        {
            return DB.GetTeamCount();
        }

        public bool IsInTeam(int _roleId)
        {
            return DB.HasRole(_roleId);
        }

        /// <summary>
        /// 把一名角色加入出战队伍（放在最后一格）。
        /// </summary>
        public bool AddToTeam(int _roleId)
        {
            if (_roleId <= 0)
            {
                return false;
            }

            if (!Role.HasRole(_roleId))
            {
                Game.Log.LogWarning(BattleSettingDefine.c_LogTag + "角色不存在：" + _roleId);
                return false;
            }

            if (DB.HasRole(_roleId))
            {
                Game.Log.LogWarning(BattleSettingDefine.c_LogTag + "角色已在队伍里：" + _roleId);
                return false;
            }

            if (DB.GetTeamCount() >= BattleSettingDefine.c_MaxTeamCount)
            {
                Game.Log.LogWarning(BattleSettingDefine.c_LogTag + "出战人数已满（" +
                    BattleSettingDefine.c_MaxTeamCount + " 人）");
                return false;
            }

            if (!DB.AddRole(_roleId))
            {
                return false;
            }

            // 新队员默认落在自己格子的中心。
            // 不能直接用端点均分：添加时新队员永远在最后一格（index == count-1），
            // 端点均分算出来恒等于最右端，连加几个人会全部叠在战场右边。
            if (!DB.HasPosition(_roleId))
            {
                int kSlot = DB.IndexOfRole(_roleId);
                DB.SetPosition(_roleId, SlotCenterPosition(kSlot, DB.GetTeamCount()));
            }

            Broadcast(BattleSettingChangeReason.Team, _roleId);
            Save();
            return true;
        }

        /// <summary>把一名角色移出队伍（同时清掉它的站位）</summary>
        public bool RemoveFromTeam(int _roleId)
        {
            if (!DB.RemoveRole(_roleId))
            {
                return false;
            }

            Broadcast(BattleSettingChangeReason.Team, _roleId);
            Save();
            return true;
        }

        /// <summary>调整角色在队伍里的槽位顺序</summary>
        public bool MoveTeamSlot(int _roleId, int _slot)
        {
            if (!DB.MoveRole(_roleId, _slot))
            {
                return false;
            }

            Broadcast(BattleSettingChangeReason.Team, _roleId);
            Save();
            return true;
        }

        public void ClearTeam()
        {
            DB.ClearTeam();
            Broadcast(BattleSettingChangeReason.Team);
            Save();
        }

        /// <summary>
        /// 按名册顺序重建队伍（默认取前 N 名角色），调试菜单与"首次进入自动编队"共用。
        /// </summary>
        public void RebuildTeamFromRoster(int _count = BattleSettingDefine.c_MaxTeamCount)
        {
            List<RoleRecord> kRoles = Role.GetAllRoles();
            int kCount = _count;
            if (kCount > BattleSettingDefine.c_MaxTeamCount)
            {
                kCount = BattleSettingDefine.c_MaxTeamCount;
            }
            if (kCount > kRoles.Count)
            {
                kCount = kRoles.Count;
            }

            DB.ClearTeam();
            for (int i = 0; i < kCount; i++)
            {
                DB.AddRole(kRoles[i].m_Id);
            }

            ResetPositions(true);
            Broadcast(BattleSettingChangeReason.Team);
            Save();
        }

        private void RemoveMissingRoles()
        {
            IReadOnlyList<int> kRoleIds = DB.GetTeamRoleIds();
            List<int> kRemoved = new List<int>();
            for (int i = kRoleIds.Count - 1; i >= 0; i--)
            {
                int kRoleId = kRoleIds[i];
                if (!Role.HasRole(kRoleId))
                {
                    kRemoved.Add(kRoleId);
                    DB.RemoveRole(kRoleId);
                }
            }

            if (kRemoved.Count > 0)
            {
                Game.Log.LogWarning(BattleSettingDefine.c_LogTag + "从队伍里移除了 " + kRemoved.Count +
                    " 个已不存在的角色");
            }
        }

        #endregion

        #region 站位

        /// <summary>归一化站位（0 = 战场最左，1 = 战场最右）</summary>
        public float GetPosition(int _roleId)
        {
            return DB.GetPosition(_roleId);
        }

        /// <summary>设置归一化站位，越界会夹到 [0,1]</summary>
        public bool SetPosition(int _roleId, float _position)
        {
            if (!DB.HasRole(_roleId))
            {
                Game.Log.LogWarning(BattleSettingDefine.c_LogTag + "角色不在队伍里，无法设置站位：" + _roleId);
                return false;
            }

            float kPosition = ClampPosition(_position);
            if (DB.HasPosition(_roleId) && MathfApproximately(DB.GetPosition(_roleId), kPosition))
            {
                return false;
            }

            DB.SetPosition(_roleId, kPosition);
            Broadcast(BattleSettingChangeReason.Position, _roleId);
            Save();
            return true;
        }

        /// <summary>把队伍按槽位顺序在战场上均分（_notify=false 时由调用方自己广播）</summary>
        public void ResetPositions(bool _notify = true)
        {
            IReadOnlyList<int> kRoleIds = DB.GetTeamRoleIds();
            int kCount = kRoleIds.Count;
            for (int i = 0; i < kCount; i++)
            {
                DB.SetPosition(kRoleIds[i], SpreadPosition(i, kCount));
            }

            if (_notify)
            {
                Broadcast(BattleSettingChangeReason.Position);
                Save();
            }
        }

        private void EnsureDefaultPositions()
        {
            IReadOnlyList<int> kRoleIds = DB.GetTeamRoleIds();
            bool kNeedReset = false;
            for (int i = 0; i < kRoleIds.Count; i++)
            {
                if (!DB.HasPosition(kRoleIds[i]))
                {
                    kNeedReset = true;
                    break;
                }
            }

            if (kNeedReset)
            {
                ResetPositions(false);
            }
        }

        /// <summary>第 index 个人在 count 人里的默认归一化位置</summary>
        private static float SpreadPosition(int _index, int _count)
        {
            return BattleFactory.SpreadPosition(BattleSettingDefine.c_MinPosition,
                BattleSettingDefine.c_MaxPosition, _index, _count);
        }

        /// <summary>第 index 个槽位的中心（不贴左右边，按人数均分格子）</summary>
        private static float SlotCenterPosition(int _index, int _count)
        {
            if (_count <= 1)
            {
                return BattleSettingDefine.c_DefaultPosition;
            }

            return ClampPosition((_index + 0.5f) / _count);
        }

        /// <summary>归一化站位 → 战场坐标</summary>
        private static float ToFieldPosition(float _normalized, float _fieldLength)
        {
            return ClampPosition(_normalized) * _fieldLength;
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

        private static bool MathfApproximately(float _a, float _b)
        {
            return (_a > _b ? _a - _b : _b - _a) < 0.0001f;
        }

        #endregion

        #region 物资

        public IReadOnlyList<int> GetMuleItemIds()
        {
            return DB.GetMuleItemIds();
        }

        public IReadOnlyList<int> GetSupportItemIds()
        {
            return DB.GetSupportItemIds();
        }

        /// <summary>
        /// 给机械骡 / 支援栏的某个格子放置物品（_itemId = 0 表示清空）。
        /// 只校验"物品配置存在 + 仓库里有"，不扣仓库数量（真正消耗在战斗里）。
        /// </summary>
        public bool SetItem(BattleSettingSupplySlot _slotKind, int _slot, int _itemId)
        {
            if (!CheckSlot(_slotKind, _slot))
            {
                return false;
            }

            if (_itemId <= 0)
            {
                return ClearItem(_slotKind, _slot);
            }

            if (!Inventory.IsKnownItem(_itemId))
            {
                Game.Log.LogWarning(BattleSettingDefine.c_LogTag + "物品配置不存在：" + _itemId);
                return false;
            }

            if (!Inventory.HasItem(_itemId, 1))
            {
                Game.Log.LogWarning(BattleSettingDefine.c_LogTag + "仓库里没有这件物品：" + _itemId);
                return false;
            }

            if (IsItemInSlots(_slotKind, _itemId, _slot))
            {
                Game.Log.LogWarning(BattleSettingDefine.c_LogTag + "该物品已经放在别的格子里：" + _itemId);
                return false;
            }

            if (!DB.SetItem(_slotKind, _slot, _itemId))
            {
                return false;
            }

            Broadcast(BattleSettingChangeReason.Supply, 0, _itemId);
            Save();
            return true;
        }

        public bool ClearItem(BattleSettingSupplySlot _slotKind, int _slot)
        {
            if (!CheckSlot(_slotKind, _slot))
            {
                return false;
            }

            int kOldItemId = DB.GetItemId(_slotKind, _slot);
            if (!DB.SetItem(_slotKind, _slot, BattleSettingDefine.c_EmptyItemId))
            {
                return false;
            }

            Broadcast(BattleSettingChangeReason.Supply, 0, kOldItemId);
            Save();
            return true;
        }

        private bool CheckSlot(BattleSettingSupplySlot _slotKind, int _slot)
        {
            int kCount = _slotKind == BattleSettingSupplySlot.Mule
                ? BattleSettingDefine.c_MuleItemCount
                : BattleSettingDefine.c_SupportItemCount;
            if (_slot < 0 || _slot >= kCount)
            {
                Game.Log.LogWarning(BattleSettingDefine.c_LogTag + "槽位越界：" + _slotKind + "/" + _slot);
                return false;
            }

            return true;
        }

        private bool IsItemInSlots(BattleSettingSupplySlot _slotKind, int _itemId, int _excludeSlot)
        {
            IReadOnlyList<int> kSlots = _slotKind == BattleSettingSupplySlot.Mule
                ? DB.GetMuleItemIds()
                : DB.GetSupportItemIds();
            for (int i = 0; i < kSlots.Count; i++)
            {
                if (i != _excludeSlot && kSlots[i] == _itemId)
                {
                    return true;
                }
            }

            return false;
        }

        #endregion

        #region 遭遇战

        public int GetEncounterId()
        {
            return DB.GetEncounterId();
        }

        public bool SetEncounter(int _encounterId)
        {
            if (_encounterId > 0 && GetEncounterConfig(_encounterId) == null)
            {
                Game.Log.LogWarning(BattleSettingDefine.c_LogTag + "遭遇战配置不存在：" + _encounterId);
                return false;
            }

            if (DB.GetEncounterId() == _encounterId)
            {
                return false;
            }

            DB.SetEncounterId(_encounterId);
            Broadcast(BattleSettingChangeReason.Encounter);
            Save();
            return true;
        }

        public string GetEncounterName()
        {
            cfg.GameData_Battle.EncounterData kEncounter = GetEncounterConfig(DB.GetEncounterId());
            return kEncounter == null ? string.Empty : kEncounter.Name;
        }

        private static cfg.GameData_Battle.EncounterData GetEncounterConfig(int _encounterId)
        {
            cfg.Tables kTables = Game.Data.GetData;
            if (kTables == null || kTables.encounter == null || _encounterId <= 0)
            {
                return null;
            }

            return kTables.encounter.GetOrDefault(_encounterId);
        }

        #endregion

        #region 开战

        /// <summary>
        /// 是否满足出发条件。探索流程只要求"不在战斗中"；队伍为空也允许出发，
        /// 战斗节点会退回调试编成（方便在内容就绪前测通流程）。
        /// </summary>
        public bool CanDepart(out string _reason)
        {
            if (Battle.IsFighting())
            {
                _reason = "已经有一场战斗在进行";
                return false;
            }

            _reason = string.Empty;
            return true;
        }

        /// <summary>
        /// 确认出发：校验队伍 → 开始（或继续）一次异常区域调查。
        /// 探索界面由调用方负责打开；已经有进行中的调查时直接返回 true（继续原进度）。
        /// </summary>
        public bool ConfirmAndDepart(int _zoneId = 0)
        {
            string kReason;
            if (!CanDepart(out kReason))
            {
                Game.Log.LogWarning(BattleSettingDefine.c_LogTag + "无法出发：" + kReason);
                return false;
            }

            IReadOnlyList<int> kTeam = DB.GetTeamRoleIds();
            if (kTeam.Count == 0)
            {
                Game.Log.LogWarning(BattleSettingDefine.c_LogTag + "队伍为空，仍以调试编成出发（探索流程测试）");
            }

            ExploreSystem kExplore = Game.Model.Get<ExploreSystem>();
            if (kExplore.HasRun())
            {
                Game.Log.LogInfo(BattleSettingDefine.c_LogTag + "已有进行中的调查，继续原进度");
                return true;
            }

            int kZoneId = _zoneId > 0 ? _zoneId : ExploreConst.DefaultZoneId;
            if (!kExplore.StartRun(kZoneId, 0))
            {
                return false;
            }

            Save();
            Game.Log.LogInfo(BattleSettingDefine.c_LogTag + "出发前往异常区域：" + kZoneId
                + "，队员 " + kTeam.Count + " 人");
            return true;
        }

        /// <summary>
        /// 是否满足开战条件（给界面置灰按钮用）。
        /// </summary>
        public bool CanStart(out string _reason)
        {
            IReadOnlyList<int> kRoleIds = DB.GetTeamRoleIds();
            if (kRoleIds.Count == 0)
            {
                _reason = "请先安排至少 1 名队员";
                return false;
            }

            for (int i = 0; i < kRoleIds.Count; i++)
            {
                if (!Role.HasRole(kRoleIds[i]))
                {
                    _reason = "队员数据已丢失：" + kRoleIds[i];
                    return false;
                }
            }

            if (Battle.IsFighting())
            {
                _reason = "已经有一场战斗在进行";
                return false;
            }

            _reason = string.Empty;
            return true;
        }

        /// <summary>
        /// 确认开战：校验 → BattleSystem 开始战斗 → 应用站位 → 广播 → 存档。
        /// </summary>
        public bool ConfirmSetting()
        {
            string kReason;
            if (!CanStart(out kReason))
            {
                Game.Log.LogWarning(BattleSettingDefine.c_LogTag + "无法开战：" + kReason);
                return false;
            }

            IReadOnlyList<int> kTeam = DB.GetTeamRoleIds();
            List<int> kRoleIds = new List<int>(kTeam);
            int kEncounterId = DB.GetEncounterId();

            bool kStarted = false;
            if (kEncounterId > 0)
            {
                kStarted = Battle.StartBattle(kEncounterId, kRoleIds);
            }

            if (!kStarted)
            {
                // encounter.xlsx 还是空表 / 该遭遇战没有可用单位时，用调试编成把流程跑通，
                // 保证战前整备界面在内容就绪前也能实测。配好遭遇战后不会再走这里。
                Game.Log.LogWarning(BattleSettingDefine.c_LogTag + "遭遇战 " + kEncounterId +
                    " 无法开始，改用调试编成（" + kRoleIds.Count + " 人）");
                kStarted = Battle.StartDebugBattle(kRoleIds.Count, kRoleIds.Count);
            }

            if (!kStarted)
            {
                Game.Log.LogError(BattleSettingDefine.c_LogTag + "开战失败");
                return false;
            }

            ApplyPositions();
            Save();

            Game.Event.SendMessage(BattleSettingEvent.OnBattleSettingCommitted,
                new BattleSettingCommittedMsg(kEncounterId, kRoleIds.ToArray()));
            Game.Log.LogInfo(BattleSettingDefine.c_LogTag + "开战：遭遇战 " + kEncounterId +
                "，队员 " + kRoleIds.Count + " 人");
            return true;
        }

        /// <summary>
        /// 开战后把当前"布阵站位"应用到己方单位。
        /// 给"直接由战斗节点开战"的界面用（ConfirmSetting 内部也会调一次）。
        /// </summary>
        public void ApplyPositionsToBattle()
        {
            ApplyPositions();
        }

        /// <summary>
        /// 把配置里的归一化站位应用到刚生成的己方单位上。
        /// 放在开战之后、第一次 Tick 之前执行，场上还没人走动，改坐标是安全的。
        /// </summary>
        private void ApplyPositions()
        {
            IReadOnlyList<BattleUnit> kUnits = Battle.GetUnits();
            BattleField kField = Battle.GetField();
            if (kUnits == null || kField == null)
            {
                return;
            }

            for (int i = 0; i < kUnits.Count; i++)
            {
                BattleUnit kUnit = kUnits[i];
                if (kUnit == null || kUnit.m_Faction != cfg.GameType.Faction.ally)
                {
                    continue;
                }

                int kRoleId = kUnit.m_SourceId;
                if (!DB.HasRole(kRoleId) || !DB.HasPosition(kRoleId))
                {
                    continue;
                }

                kUnit.m_Pos = kField.ClampPosition(ToFieldPosition(DB.GetPosition(kRoleId), kField.Length));
            }
        }

        #endregion

        #region 快照

        /// <summary>
        /// 组装界面用的只读快照（一次性把队伍、站位、物资、遭遇战摊平）。
        /// </summary>
        public BattleSettingSnapshot BuildSnapshot()
        {
            BattleSettingSnapshot kSnapshot = new BattleSettingSnapshot();
            cfg.Tables kTables = Game.Data.GetData;

            int kEncounterId = DB.GetEncounterId();
            kSnapshot.m_EncounterId = kEncounterId;

            cfg.GameData_Battle.EncounterData kEncounter = GetEncounterConfig(kEncounterId);
            kSnapshot.m_EncounterName = kEncounter == null ? string.Empty : kEncounter.Name;

            cfg.GameData_Battle.BattlefieldData kField = null;
            if (kEncounter != null && kTables != null && kTables.battlefield != null)
            {
                kField = kTables.battlefield.GetOrDefault(kEncounter.BattlefieldID);
            }
            kSnapshot.m_FieldLength = kField != null && kField.Length > 0f
                ? kField.Length
                : BattleConst.DefaultFieldLength;

            IReadOnlyList<int> kTeam = DB.GetTeamRoleIds();
            for (int i = 0; i < kTeam.Count; i++)
            {
                int kRoleId = kTeam[i];
                RoleRecord kRecord = Role.GetRole(kRoleId);

                BattleSettingTeamEntry kEntry = new BattleSettingTeamEntry();
                kEntry.m_Slot = i;
                kEntry.m_RoleId = kRoleId;
                kEntry.m_Role = kRecord == null ? null : RoleSystem.BuildSnapshot(kRecord, kTables);
                kEntry.m_Position = DB.GetPosition(kRoleId);
                kEntry.m_FieldPosition = ToFieldPosition(kEntry.m_Position, kSnapshot.m_FieldLength);
                kSnapshot.m_Team.Add(kEntry);
            }

            BuildItemSlots(kSnapshot.m_MuleItems, DB.GetMuleItemIds());
            BuildItemSlots(kSnapshot.m_SupportItems, DB.GetSupportItemIds());

            string kBlockReason;
            kSnapshot.m_CanStart = CanStart(out kBlockReason);
            kSnapshot.m_BlockReason = kBlockReason;
            return kSnapshot;
        }

        private static void BuildItemSlots(List<BattleSettingItemSlot> _slots, IReadOnlyList<int> _itemIds)
        {
            for (int i = 0; i < _itemIds.Count; i++)
            {
                BattleSettingItemSlot kSlot = new BattleSettingItemSlot();
                kSlot.m_Slot = i;
                kSlot.m_ItemId = _itemIds[i];
                if (kSlot.m_ItemId > 0)
                {
                    kSlot.m_Item = Inventory.GetItemConfig(kSlot.m_ItemId);
                    kSlot.m_Count = Inventory.GetItemCount(kSlot.m_ItemId);
                }

                _slots.Add(kSlot);
            }
        }

        #endregion

        #region 存档 / 重置

        public bool Save()
        {
            return DB.Save();
        }

        public bool Load()
        {
            bool kOk = DB.Load();
            if (kOk)
            {
                Broadcast(BattleSettingChangeReason.Load);
            }

            return kOk;
        }

        /// <summary>清空整份配置（队伍保持为空，由玩家重新选人）</summary>
        public void Reset()
        {
            DB.Clear();
            ResetPositions(false);
            Broadcast(BattleSettingChangeReason.Reset);
            Save();
        }

        public int GetVersion()
        {
            return DB.GetVersion();
        }

        private void Broadcast(BattleSettingChangeReason _reason, int _roleId = 0, int _itemId = 0)
        {
            Game.Event.SendMessage(BattleSettingEvent.OnBattleSettingChanged,
                new BattleSettingChangedMsg(_reason, _roleId, _itemId));
        }

        #endregion
    }
}
