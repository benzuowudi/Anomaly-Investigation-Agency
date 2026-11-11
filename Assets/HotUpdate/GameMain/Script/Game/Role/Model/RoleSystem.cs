using System.Collections.Generic;
using System.Text;
using Framework.Script.Constant;
using Framework.Script.Model;
using GameMain.Script.Battle;
using GameMain.Script.BattleSetting;
using UnityEngine;

namespace GameMain.Script.Role
{
    /// <summary>
    /// 角色系统的主模型（M 层，Role 系统的唯一对外入口）。
    ///
    /// 数据流：
    ///   业务调用 AcquireRole()（抽卡/剧情/商店发放）
    ///     → RoleFactory 按 role.xlsx 模板掷出一份属性（含随机天赋、随机主动技能、推导行为逻辑）
    ///     → RoleData 写入权威角色库
    ///     → 广播 OnRoleAcquired / OnRoleChanged / OnRolesChanged
    ///     → RoleData.Save() 存档到本地（role.json），下次启动由 OnStart → Load() 还原
    ///
    /// 由 ModelManager.LoadModels() 注册，业务侧用 Game.Model.Get&lt;RoleSystem&gt;() 取用；
    /// 查询类接口不产生副作用；talent / behaviorLogic / buff 三张配套表也在这里统一取，
    /// 避免业务侧到处拼 cfg 命名空间。
    /// </summary>
    public class RoleSystem : ModelBase
    {
        /// <summary>非任务途径（巡逻/培训）每天可获得的职称评分上限</summary>
        private const int c_DailyTitleScoreCap = 5;

        private RoleData DB
        {
            get { return Game.Data.GetGameData<RoleData>(); }
        }

        #region 生命周期

        protected internal override void OnInit()
        {
            Game.Event.Subscribe(BattleEvent.OnBattleEnded, OnBattleEnded);
        }

        /// <summary>
        /// 全部模型注册完成后读本系统存档；新档/没有存档不算错误。
        /// </summary>
        protected internal override void OnStart()
        {
            Load();
            EnsureDefaultEquip();
        }

        protected internal override void OnDestroy()
        {
            Game.Event.UnSubscribe(BattleEvent.OnBattleEnded, OnBattleEnded);
        }

        #endregion

        #region 获得 / 移除

        /// <summary>
        /// 按 role.xlsx 模板随机获得一个新角色：生成 → 入库 → 广播 → 立即存档。
        /// 生成失败（表没加载/表为空）返回 null。
        /// </summary>
        public RoleRecord AcquireRole(RoleAcquireReason _reason = RoleAcquireReason.None)
        {
            cfg.Tables kTables = GetRuntimeTables();
            if (kTables == null)
            {
                Game.Log.LogError("[角色] 获得角色失败：配置表尚未加载");
                return null;
            }

            RoleData kDB = DB;
            RoleRecord kRole = RoleFactory.Roll(kTables, kDB.GetNextRoleId());
            if (kRole == null || !kDB.AddRole(kRole))
            {
                return null;
            }

            Game.Event.SendMessage(RoleEvent.OnRoleAcquired, new RoleAcquiredMsg(kRole, _reason));
            Game.Event.SendMessage(RoleEvent.OnRoleChanged, new RoleChangedMsg(kRole.m_Id, RoleChangeOp.Acquired, _reason));
            Game.Event.SendMessage(RoleEvent.OnRolesChanged, new RolesChangedMsg(kDB.GetVersion(), kDB.GetRoleCount(), _reason));

            Save();
            Game.Log.LogInfo("[角色] 获得新角色：" + Describe(kRole));
            return kRole;
        }

        /// <summary>
        /// 批量获得，返回实际成功的数量
        /// </summary>
        public int AcquireRoles(int _count, RoleAcquireReason _reason = RoleAcquireReason.None)
        {
            if (_count <= 0)
            {
                return 0;
            }
            int kSuccess = 0;
            for (int i = 0; i < _count; i++)
            {
                if (AcquireRole(_reason) != null)
                {
                    kSuccess++;
                }
            }
            return kSuccess;
        }

        /// <summary>
        /// 旧存档里没有默认装备的角色补上"赤手空拳"（只补空装备列表的角色）。
        /// </summary>
        private void EnsureDefaultEquip()
        {
            RoleData kDB = DB;
            List<RoleRecord> kRoles = kDB.GetAllRoles();
            bool kChanged = false;
            for (int i = 0; i < kRoles.Count; i++)
            {
                RoleRecord kRole = kRoles[i];
                if (kRole != null && kRole.m_EquipIds.Count == 0)
                {
                    kRole.m_EquipIds.Add(RoleTitleRule.DefaultEquipId);
                    kChanged = true;
                }
            }

            if (kChanged)
            {
                kDB.MarkDirty();
                Save();
                Game.Log.LogInfo("[角色] 已为没有装备的角色补上默认装备：" + RoleTitleRule.DefaultEquipId);
            }
        }

        /// <summary>
        /// 战斗结束：永久死亡（m_Dead）的己方角色从名册移除，不再出现在角色列表。
        /// 重伤倒计时没走完的（m_Downed）仍然保留。
        /// </summary>
        private void OnBattleEnded(object _msg)
        {
            BattleEndedMsg kMsg = _msg as BattleEndedMsg;
            if (kMsg == null || kMsg.m_Result == null)
            {
                return;
            }

            List<int> kDeadRoleIds = new List<int>();
            List<UnitResult> kUnits = kMsg.m_Result.m_Units;
            for (int i = 0; i < kUnits.Count; i++)
            {
                UnitResult kUnit = kUnits[i];
                if (kUnit != null && kUnit.m_Dead && kUnit.m_SourceId > 0
                    && !kDeadRoleIds.Contains(kUnit.m_SourceId))
                {
                    kDeadRoleIds.Add(kUnit.m_SourceId);
                }
            }

            for (int i = 0; i < kDeadRoleIds.Count; i++)
            {
                RemoveDeadRole(kDeadRoleIds[i]);
            }
        }

        private void RemoveDeadRole(int _roleId)
        {
            RoleRecord kRole = DB.GetRole(_roleId);
            if (kRole == null)
            {
                return;
            }

            Game.Log.LogWarning("[角色] " + kRole.GetDisplayName() + "（" + _roleId
                + "）在战斗中永久死亡，从名册移除");

            // 先移出出战队伍，避免后续整备界面还引用已删除的角色
            BattleSettingSystem kSetting = Game.Model.Get<BattleSettingSystem>();
            if (kSetting != null)
            {
                kSetting.RemoveFromTeam(_roleId);
            }

            RemoveRole(_roleId, RoleAcquireReason.Death);
        }

        /// <summary>
        /// 移除一个角色
        /// </summary>
        public bool RemoveRole(int _roleId, RoleAcquireReason _reason = RoleAcquireReason.None)
        {
            RoleData kDB = DB;
            if (!kDB.RemoveRole(_roleId))
            {
                Game.Log.LogWarning("[角色] 移除失败，角色不存在：" + _roleId);
                return false;
            }

            Game.Event.SendMessage(RoleEvent.OnRoleChanged, new RoleChangedMsg(_roleId, RoleChangeOp.Removed, _reason));
            Game.Event.SendMessage(RoleEvent.OnRolesChanged, new RolesChangedMsg(kDB.GetVersion(), kDB.GetRoleCount(), _reason));
            Save();
            return true;
        }

        /// <summary>
        /// 清空全部角色（调试/新档用）
        /// </summary>
        public void ClearAllRoles(RoleAcquireReason _reason = RoleAcquireReason.Debug)
        {
            RoleData kDB = DB;
            kDB.ClearAll();
            Game.Event.SendMessage(RoleEvent.OnRolesChanged, new RolesChangedMsg(kDB.GetVersion(), 0, _reason));
            Save();
        }

        #endregion

        #region 查询

        public RoleRecord GetRole(int _roleId)
        {
            return DB.GetRole(_roleId);
        }

        public bool HasRole(int _roleId)
        {
            return DB.HasRole(_roleId);
        }

        public int GetRoleCount()
        {
            return DB.GetRoleCount();
        }

        public long GetVersion()
        {
            return DB.GetVersion();
        }

        public int GetNextRoleId()
        {
            return DB.GetNextRoleId();
        }

        /// <summary>
        /// 把外部已经生成好的角色（例如招募候选人）写入权威库并广播。
        /// </summary>
        public bool AddGeneratedRole(RoleRecord _role, RoleAcquireReason _reason = RoleAcquireReason.None)
        {
            if (_role == null)
            {
                return false;
            }

            RoleData kDB = DB;
            if (!kDB.AddRole(_role))
            {
                return false;
            }

            Game.Event.SendMessage(RoleEvent.OnRoleAcquired, new RoleAcquiredMsg(_role, _reason));
            Game.Event.SendMessage(RoleEvent.OnRoleChanged, new RoleChangedMsg(_role.m_Id, RoleChangeOp.Acquired, _reason));
            Game.Event.SendMessage(RoleEvent.OnRolesChanged, new RolesChangedMsg(kDB.GetVersion(), kDB.GetRoleCount(), _reason));
            Save();
            return true;
        }

        /// <summary>
        /// 全部角色（按角色ID升序）
        /// </summary>
        public List<RoleRecord> GetAllRoles()
        {
            return DB.GetAllRoles();
        }

        /// <summary>
        /// 当前出战（主）角色
        /// </summary>
        public RoleRecord GetMainRole()
        {
            return DB.GetMainRole();
        }

        public int GetMainRoleId()
        {
            return DB.GetMainRoleId();
        }

        public bool SetMainRole(int _roleId)
        {
            RoleData kDB = DB;
            int kOld = kDB.GetMainRoleId();
            if (!kDB.SetMainRole(_roleId))
            {
                Game.Log.LogWarning("[角色] 设置主角色失败，角色不存在：" + _roleId);
                return false;
            }
            if (kOld != kDB.GetMainRoleId())
            {
                Game.Event.SendMessage(RoleEvent.OnMainRoleChanged, new MainRoleChangedMsg(kOld, kDB.GetMainRoleId()));
                Save();
            }
            return true;
        }

        #endregion

        #region 养成（改名 / 等级 / 属性）

        /// <summary>
        /// 改角色名（空字符串表示恢复默认显示名）
        /// </summary>
        public bool SetRoleName(int _roleId, string _name)
        {
            RoleData kDB = DB;
            RoleRecord kRole = kDB.GetRole(_roleId);
            if (kRole == null)
            {
                return false;
            }

            string kOld = kRole.m_Name;
            string kNew = _name == null ? string.Empty : _name.Trim();
            if (kOld == kNew)
            {
                return true;
            }

            kRole.m_Name = kNew;
            kDB.MarkDirty();
            Game.Event.SendMessage(RoleEvent.OnRoleChanged,
                new RoleChangedMsg(_roleId, RoleChangeOp.Name, "name", kOld, kNew));
            Save();
            return true;
        }

        /// <summary>
        /// 设置等级（最小 1）
        /// </summary>
        public bool SetLevel(int _roleId, int _level)
        {
            RoleData kDB = DB;
            RoleRecord kRole = kDB.GetRole(_roleId);
            if (kRole == null)
            {
                return false;
            }

            if (_level < 1)
            {
                _level = 1;
            }
            int kOld = kRole.m_Level;
            if (kOld == _level)
            {
                return true;
            }

            kRole.m_Level = _level;
            kDB.MarkDirty();
            Game.Event.SendMessage(RoleEvent.OnRoleChanged,
                new RoleChangedMsg(_roleId, RoleChangeOp.Level, "level", kOld, _level));
            Save();
            return true;
        }

        /// <summary>
        /// 加等级，返回新等级（角色不存在返回 -1）
        /// </summary>
        public int AddLevel(int _roleId, int _delta = 1)
        {
            RoleRecord kRole = DB.GetRole(_roleId);
            if (kRole == null)
            {
                return -1;
            }
            int kNew = kRole.m_Level + _delta;
            SetLevel(_roleId, kNew);
            return kNew < 1 ? 1 : kNew;
        }

        /// <summary>
        /// 写单个属性（自动按 role.xlsx 的 maxValue 封顶、不小于 0），成功返回 true
        /// </summary>
        public bool SetAttribute(int _roleId, int _attrId, int _value, bool _saveImmediately = true)
        {
            RoleData kDB = DB;
            RoleRecord kRole = kDB.GetRole(_roleId);
            if (kRole == null)
            {
                Game.Log.LogWarning("[角色] 改属性失败，角色不存在：" + _roleId);
                return false;
            }

            cfg.GameData_Role.RoleData kConfig = GetAttrConfig(_attrId);
            if (kConfig == null)
            {
                Game.Log.LogWarning("[角色] 改属性失败，role.xlsx 里没有该属性ID：" + _attrId);
                return false;
            }

            int kMax = Mathf.RoundToInt(kConfig.MaxValue);
            if (kMax > 0 && _value > kMax)
            {
                _value = kMax;
            }
            if (_value < 0)
            {
                _value = 0;
            }

            int kOld = kRole.GetAttr(_attrId);
            if (kOld == _value)
            {
                return true;
            }

            kRole.SetAttr(_attrId, _value);
            kDB.MarkDirty();
            Game.Event.SendMessage(RoleEvent.OnRoleChanged,
                new RoleChangedMsg(_roleId, RoleChangeOp.Attribute, "attr:" + _attrId, kOld, _value));
            if (_saveImmediately)
            {
                Save();
            }
            return true;
        }

        /// <summary>
        /// 加属性（负数表示扣），返回写入后的值
        /// </summary>
        public int AddAttribute(int _roleId, int _attrId, int _delta, bool _saveImmediately = true)
        {
            RoleRecord kRole = DB.GetRole(_roleId);
            if (kRole == null)
            {
                return 0;
            }
            int kNew = kRole.GetAttr(_attrId) + _delta;
            SetAttribute(_roleId, _attrId, kNew, _saveImmediately);
            return DB.GetRole(_roleId).GetAttr(_attrId);
        }

        public int GetAttribute(int _roleId, int _attrId)
        {
            RoleRecord kRole = DB.GetRole(_roleId);
            return kRole == null ? 0 : kRole.GetAttr(_attrId);
        }

        /// <summary>
        /// 属性上限（role.xlsx 的 maxValue），取不到返回 0
        /// </summary>
        public static int GetAttributeMax(int _attrId)
        {
            cfg.GameData_Role.RoleData kConfig = GetAttrConfig(_attrId);
            return kConfig == null ? 0 : Mathf.RoundToInt(kConfig.MaxValue);
        }

        /// <summary>
        /// 属性显示名（role.xlsx 的 name）
        /// </summary>
        public static string GetAttributeName(int _attrId)
        {
            cfg.GameData_Role.RoleData kConfig = GetAttrConfig(_attrId);
            return kConfig == null ? ("属性" + _attrId) : kConfig.Name;
        }

        /// <summary>
        /// role.xlsx 里配置的属性定义，找不到返回 null
        /// </summary>
        public static cfg.GameData_Role.RoleData GetAttrConfig(int _attrId)
        {
            cfg.Tables kTables = GetRuntimeTables();
            if (kTables == null || kTables.role == null)
            {
                return null;
            }
            return kTables.role.GetOrDefault(_attrId);
        }

        #endregion

        #region 展示数据

        /// <summary>
        /// 只取 show = true 的属性，按 role.xlsx 顺序，给 UI 直接渲染
        /// </summary>
        public static List<RoleAttributeView> GetDisplayAttributes(RoleRecord _role)
        {
            return BuildAttributeViews(_role, true);
        }

        /// <summary>
        /// 取全部属性（含隐藏倾向），_onlyShow 为 true 时只返回展示属性
        /// </summary>
        public static List<RoleAttributeView> BuildAttributeViews(RoleRecord _role, bool _onlyShow)
        {
            List<RoleAttributeView> kResult = new List<RoleAttributeView>();
            cfg.Tables kTables = GetRuntimeTables();
            if (_role == null || kTables == null || kTables.role == null)
            {
                return kResult;
            }

            List<cfg.GameData_Role.RoleData> kTemplate = kTables.role.DataList;
            for (int i = 0; i < kTemplate.Count; i++)
            {
                cfg.GameData_Role.RoleData kAttr = kTemplate[i];
                if (kAttr == null || (_onlyShow && !kAttr.Show))
                {
                    continue;
                }

                int kValue = _role.GetAttr(kAttr.ID);
                RoleAttributeView kView = new RoleAttributeView();
                kView.m_AttrId = kAttr.ID;
                kView.m_Name = kAttr.Name;
                kView.m_Value = kValue;
                kView.m_MaxValue = kAttr.ID == RoleAttrId.Talent ? 0 : Mathf.RoundToInt(kAttr.MaxValue);
                kView.m_Show = kAttr.Show;
                kView.m_DisplayValue = kAttr.ID == RoleAttrId.Talent ? GetTalentName(kValue) : kValue.ToString();
                kResult.Add(kView);
            }
            return kResult;
        }

        /// <summary>
        /// 主手（左右手偏好推导）
        /// </summary>
        public static RoleHand GetMainHand(RoleRecord _role)
        {
            return _role == null ? RoleHand.Right : _role.GetMainHand();
        }

        #endregion

        #region 职称

        /// <summary>
        /// 增加职称评分。_isTask = true（参与行动/任务）不受每日上限限制；
        /// 非任务途径（巡逻/培训）每天最多 5 分，跨天由 ResetDailyTitleScore 重置。
        /// 返回是否真的加上了分。
        /// </summary>
        public bool AddTitleScore(int _roleId, int _score, bool _isTask = false)
        {
            if (_score <= 0)
            {
                return false;
            }

            RoleData kDB = DB;
            RoleRecord kRole = kDB.GetRole(_roleId);
            if (kRole == null)
            {
                Game.Log.LogWarning("[角色] 加职称评分失败，角色不存在：" + _roleId);
                return false;
            }

            if (!_isTask)
            {
                int kLeft = c_DailyTitleScoreCap - kRole.m_DailyTitleScore;
                if (kLeft <= 0)
                {
                    Game.Log.LogInfo("[角色] " + kRole.GetDisplayName() + " 今日非任务职称评分已达上限 " + c_DailyTitleScoreCap);
                    return false;
                }
                if (_score > kLeft)
                {
                    _score = kLeft;
                }
                kRole.m_DailyTitleScore += _score;
            }

            int kOldScore = kRole.m_TitleScore;
            RoleTitleLevel kOldLevel = kRole.GetTitleLevel();
            kRole.m_TitleScore += _score;
            RoleTitleLevel kNewLevel = kRole.GetTitleLevel();

            kDB.MarkDirty();
            NotifyChanged(_roleId, RoleChangeOp.Title, "titleScore", kOldScore, kRole.m_TitleScore);
            if (kNewLevel != kOldLevel)
            {
                Game.Log.LogInfo("[角色] 职称晋升：" + kRole.GetDisplayName() + " "
                    + RoleTitleRule.GetName(kOldLevel) + " → " + kRole.GetTitleName());
                NotifyChanged(_roleId, RoleChangeOp.Title, "titleLevel", kOldLevel, kNewLevel);
            }
            Save();
            return true;
        }

        /// <summary>
        /// 跨天重置"非任务途径职称评分"计数，建议由游戏内时间系统每日切换时调一次
        /// </summary>
        public void ResetDailyTitleScore()
        {
            RoleData kDB = DB;
            List<RoleRecord> kAll = kDB.GetAllRoles();
            for (int i = 0; i < kAll.Count; i++)
            {
                kAll[i].m_DailyTitleScore = 0;
            }
            kDB.MarkDirty();
            Save();
        }

        public RoleTitleLevel GetTitleLevel(int _roleId)
        {
            RoleRecord kRole = DB.GetRole(_roleId);
            return kRole == null ? RoleTitleLevel.Junior : kRole.GetTitleLevel();
        }

        public string GetTitleName(int _roleId)
        {
            return RoleTitleRule.GetName(GetTitleLevel(_roleId));
        }

        public int GetTitleScore(int _roleId)
        {
            RoleRecord kRole = DB.GetRole(_roleId);
            return kRole == null ? 0 : kRole.m_TitleScore;
        }

        public int GetMedalSlots(int _roleId)
        {
            return RoleTitleRule.GetMedalSlots(GetTitleLevel(_roleId));
        }

        public int GetArmorSlots(int _roleId)
        {
            return RoleTitleRule.GetArmorSlots(GetTitleLevel(_roleId));
        }

        #endregion

        #region 装备

        /// <summary>
        /// 穿装备。校验：装备存在、没有重复穿、对应槽位未满
        /// （武器/支援 1 件；护甲按职称 1~3 件，对应装备套装的两件套/三件套）。
        /// </summary>
        public bool Equip(int _roleId, int _equipId)
        {
            cfg.Tables kTables = GetRuntimeTables();
            if (kTables == null || kTables.equip == null)
            {
                Game.Log.LogWarning("[角色] 穿装备失败：equip 表未加载");
                return false;
            }

            cfg.GameData_Item.EquipData kEquip = kTables.equip.GetOrDefault(_equipId);
            if (kEquip == null)
            {
                Game.Log.LogWarning("[角色] 穿装备失败，equip 表中没有该装备：" + _equipId);
                return false;
            }

            RoleData kDB = DB;
            RoleRecord kRole = kDB.GetRole(_roleId);
            if (kRole == null)
            {
                Game.Log.LogWarning("[角色] 穿装备失败，角色不存在：" + _roleId);
                return false;
            }
            if (kRole.m_EquipIds.Contains(_equipId))
            {
                Game.Log.LogWarning("[角色] 该装备已经穿在身上：" + _equipId);
                return false;
            }

            bool kArmor = RoleTitleRule.IsArmor(kEquip.Type1);

            // 默认"赤手空拳"不占武器槽：穿真实武器时先把它摘掉
            bool kRemovedDefault = false;
            if (!kArmor && _equipId != RoleTitleRule.DefaultEquipId)
            {
                kRemovedDefault = kRole.m_EquipIds.Remove(RoleTitleRule.DefaultEquipId);
            }

            int kUsed = CountEquipByGroup(kRole, kArmor, kTables);
            int kSlots = kArmor
                ? RoleTitleRule.GetArmorSlots(kRole.GetTitleLevel())
                : RoleTitleRule.GetWeaponSlots();
            if (kUsed >= kSlots)
            {
                // 校验失败把默认装备放回去，避免只改内存不存档
                if (kRemovedDefault)
                {
                    kRole.m_EquipIds.Add(RoleTitleRule.DefaultEquipId);
                }

                Game.Log.LogWarning("[角色] 穿装备失败：" + (kArmor ? "护甲" : "武器") + "槽位已满（"
                    + kUsed + "/" + kSlots + "），当前职称 " + kRole.GetTitleName());
                return false;
            }

            kRole.m_EquipIds.Add(_equipId);
            kDB.MarkDirty();
            NotifyChanged(_roleId, RoleChangeOp.Equip, "equip:" + _equipId, 0, _equipId);
            Save();
            return true;
        }

        /// <summary>
        /// 卸下指定装备
        /// </summary>
        public bool Unequip(int _roleId, int _equipId)
        {
            RoleData kDB = DB;
            RoleRecord kRole = kDB.GetRole(_roleId);
            if (kRole == null)
            {
                return false;
            }
            if (!kRole.m_EquipIds.Remove(_equipId))
            {
                return false;
            }

            kDB.MarkDirty();
            NotifyChanged(_roleId, RoleChangeOp.Equip, "equip:" + _equipId, _equipId, 0);
            Save();
            return true;
        }

        /// <summary>
        /// 卸下全部装备
        /// </summary>
        public bool UnequipAll(int _roleId)
        {
            RoleData kDB = DB;
            RoleRecord kRole = kDB.GetRole(_roleId);
            if (kRole == null)
            {
                return false;
            }
            if (kRole.m_EquipIds.Count == 0)
            {
                return true;
            }

            kRole.m_EquipIds.Clear();
            kDB.MarkDirty();
            NotifyChanged(_roleId, RoleChangeOp.Equip, "equip:*", null, null);
            Save();
            return true;
        }

        /// <summary>
        /// 角色已穿戴的装备配置
        /// </summary>
        public static List<cfg.GameData_Item.EquipData> GetEquips(RoleRecord _role)
        {
            List<cfg.GameData_Item.EquipData> kResult = new List<cfg.GameData_Item.EquipData>();
            cfg.Tables kTables = GetRuntimeTables();
            if (_role == null || kTables == null || kTables.equip == null)
            {
                return kResult;
            }
            for (int i = 0; i < _role.m_EquipIds.Count; i++)
            {
                cfg.GameData_Item.EquipData kEquip = kTables.equip.GetOrDefault(_role.m_EquipIds[i]);
                if (kEquip != null)
                {
                    kResult.Add(kEquip);
                }
            }
            return kResult;
        }

        private static int CountEquipByGroup(RoleRecord _role, bool _armor, cfg.Tables _tables)
        {
            int kCount = 0;
            for (int i = 0; i < _role.m_EquipIds.Count; i++)
            {
                cfg.GameData_Item.EquipData kEquip = _tables.equip.GetOrDefault(_role.m_EquipIds[i]);
                if (kEquip != null && RoleTitleRule.IsArmor(kEquip.Type1) == _armor)
                {
                    kCount++;
                }
            }
            return kCount;
        }

        #endregion

        #region 勋章

        /// <summary>
        /// 佩戴勋章，数量受职称限制（初级 1 / 高级 2 / 资深 3）
        /// </summary>
        public bool AddMedal(int _roleId, int _medalId)
        {
            cfg.Tables kTables = GetRuntimeTables();
            if (kTables == null || kTables.medal == null)
            {
                Game.Log.LogWarning("[角色] 佩戴勋章失败：medal 表未加载");
                return false;
            }
            if (kTables.medal.GetOrDefault(_medalId) == null)
            {
                Game.Log.LogWarning("[角色] 佩戴勋章失败，medal 表中没有该勋章：" + _medalId);
                return false;
            }

            RoleData kDB = DB;
            RoleRecord kRole = kDB.GetRole(_roleId);
            if (kRole == null)
            {
                return false;
            }
            if (kRole.m_MedalIds.Contains(_medalId))
            {
                return false;
            }

            int kSlots = RoleTitleRule.GetMedalSlots(kRole.GetTitleLevel());
            if (kRole.m_MedalIds.Count >= kSlots)
            {
                Game.Log.LogWarning("[角色] 佩戴勋章失败：勋章槽已满（" + kRole.m_MedalIds.Count + "/" + kSlots
                    + "），当前职称 " + kRole.GetTitleName());
                return false;
            }

            kRole.m_MedalIds.Add(_medalId);
            kDB.MarkDirty();
            NotifyChanged(_roleId, RoleChangeOp.Medal, "medal:" + _medalId, 0, _medalId);
            Save();
            return true;
        }

        public bool RemoveMedal(int _roleId, int _medalId)
        {
            RoleData kDB = DB;
            RoleRecord kRole = kDB.GetRole(_roleId);
            if (kRole == null)
            {
                return false;
            }
            if (!kRole.m_MedalIds.Remove(_medalId))
            {
                return false;
            }

            kDB.MarkDirty();
            NotifyChanged(_roleId, RoleChangeOp.Medal, "medal:" + _medalId, _medalId, 0);
            Save();
            return true;
        }

        public static List<cfg.GameData_Item.MedalData> GetMedals(RoleRecord _role)
        {
            List<cfg.GameData_Item.MedalData> kResult = new List<cfg.GameData_Item.MedalData>();
            cfg.Tables kTables = GetRuntimeTables();
            if (_role == null || kTables == null || kTables.medal == null)
            {
                return kResult;
            }
            for (int i = 0; i < _role.m_MedalIds.Count; i++)
            {
                cfg.GameData_Item.MedalData kMedal = kTables.medal.GetOrDefault(_role.m_MedalIds[i]);
                if (kMedal != null)
                {
                    kResult.Add(kMedal);
                }
            }
            return kResult;
        }

        #endregion

        #region 技能 / 异常适应性

        /// <summary>
        /// 设置主动（_isActive = true）或被动技能。_skillId 传 0 表示清空；
        /// 非 0 时必须是 skill 表里配置过的技能。
        /// </summary>
        public bool SetSkill(int _roleId, int _skillId, bool _isActive)
        {
            RoleData kDB = DB;
            RoleRecord kRole = kDB.GetRole(_roleId);
            if (kRole == null)
            {
                return false;
            }

            if (_skillId > 0)
            {
                cfg.GameData_Skill.SkillData kSkill = GetSkill(_skillId);
                if (kSkill == null)
                {
                    Game.Log.LogWarning("[角色] 设置技能失败，skill 表中没有该技能：" + _skillId);
                    return false;
                }
            }
            else
            {
                _skillId = 0;
            }

            int kOld = _isActive ? kRole.m_ActiveSkillId : kRole.m_PassiveSkillId;
            if (kOld == _skillId)
            {
                return true;
            }

            if (_isActive)
            {
                kRole.m_ActiveSkillId = _skillId;
            }
            else
            {
                kRole.m_PassiveSkillId = _skillId;
            }
            kDB.MarkDirty();
            NotifyChanged(_roleId, RoleChangeOp.Skill, (_isActive ? "activeSkill:" : "passiveSkill:") + _skillId, kOld, _skillId);
            Save();
            return true;
        }

        public static cfg.GameData_Skill.SkillData GetSkill(int _skillId)
        {
            cfg.Tables kTables = GetRuntimeTables();
            if (_skillId <= 0 || kTables == null || kTables.skill == null)
            {
                return null;
            }
            return kTables.skill.GetOrDefault(_skillId);
        }

        public static string GetSkillName(int _skillId)
        {
            cfg.GameData_Skill.SkillData kSkill = GetSkill(_skillId);
            return kSkill == null ? "无" : kSkill.Name;
        }

        /// <summary>
        /// 设置异常适应性等级（0 表示清除）。异常类型由异常系统定义，这里只存数据。
        /// </summary>
        public bool SetAdaptation(int _roleId, int _anomalyType, int _level)
        {
            if (_anomalyType <= 0)
            {
                return false;
            }

            RoleData kDB = DB;
            RoleRecord kRole = kDB.GetRole(_roleId);
            if (kRole == null)
            {
                return false;
            }

            int kOld = GetAdaptation(_roleId, _anomalyType);
            if (_level < 0)
            {
                _level = 0;
            }
            if (kOld == _level)
            {
                return true;
            }

            if (_level == 0)
            {
                kRole.m_Adaptations.Remove(_anomalyType);
            }
            else
            {
                kRole.m_Adaptations[_anomalyType] = _level;
            }
            kDB.MarkDirty();
            NotifyChanged(_roleId, RoleChangeOp.Adaptation, "adaptation:" + _anomalyType, kOld, _level);
            Save();
            return true;
        }

        public int GetAdaptation(int _roleId, int _anomalyType)
        {
            RoleRecord kRole = DB.GetRole(_roleId);
            if (kRole == null)
            {
                return 0;
            }
            int kLevel;
            return kRole.m_Adaptations.TryGetValue(_anomalyType, out kLevel) ? kLevel : 0;
        }

        #endregion

        #region 持久效果（buff 表里跟着角色走的那些）

        /// <summary>
        /// 叠加一个持久效果（层数按 buff 表的 stacks 封顶）。
        /// 战斗内的临时效果由战斗系统自己管，不要写到这里。
        /// </summary>
        public bool AddEffect(int _roleId, int _effectId, int _stacks = 1)
        {
            if (_stacks <= 0)
            {
                return false;
            }
            if (GetBuff(_effectId) == null)
            {
                Game.Log.LogWarning("[角色] 加效果失败，buff 表中没有该效果：" + _effectId);
                return false;
            }

            RoleData kDB = DB;
            RoleRecord kRole = kDB.GetRole(_roleId);
            if (kRole == null)
            {
                Game.Log.LogWarning("[角色] 加效果失败，角色不存在：" + _roleId);
                return false;
            }

            int kOld = kRole.GetEffectStacks(_effectId);
            int kNew = kOld + _stacks;
            int kMax = GetBuffMaxStacks(_effectId);
            if (kMax > 0 && kNew > kMax)
            {
                kNew = kMax;
            }
            if (kNew == kOld)
            {
                return true;
            }

            kRole.SetEffectStacks(_effectId, kNew);
            kDB.MarkDirty();
            Game.Event.SendMessage(RoleEvent.OnRoleChanged,
                new RoleChangedMsg(_roleId, RoleChangeOp.Effect, "effect:" + _effectId, kOld, kNew));
            Save();
            return true;
        }

        /// <summary>
        /// 扣减持久效果层数，扣到 0 即移除
        /// </summary>
        public bool RemoveEffect(int _roleId, int _effectId, int _stacks = 1)
        {
            RoleData kDB = DB;
            RoleRecord kRole = kDB.GetRole(_roleId);
            if (kRole == null)
            {
                return false;
            }

            int kOld = kRole.GetEffectStacks(_effectId);
            if (kOld <= 0)
            {
                return true;
            }

            int kNew = kOld - _stacks;
            if (kNew < 0)
            {
                kNew = 0;
            }
            kRole.SetEffectStacks(_effectId, kNew);
            kDB.MarkDirty();
            Game.Event.SendMessage(RoleEvent.OnRoleChanged,
                new RoleChangedMsg(_roleId, RoleChangeOp.Effect, "effect:" + _effectId, kOld, kNew));
            Save();
            return true;
        }

        /// <summary>
        /// 清空某个角色的全部持久效果
        /// </summary>
        public bool ClearEffects(int _roleId)
        {
            RoleData kDB = DB;
            RoleRecord kRole = kDB.GetRole(_roleId);
            if (kRole == null)
            {
                return false;
            }
            if (kRole.m_Effects.Count == 0)
            {
                return true;
            }

            kRole.m_Effects.Clear();
            kDB.MarkDirty();
            Game.Event.SendMessage(RoleEvent.OnRoleChanged,
                new RoleChangedMsg(_roleId, RoleChangeOp.Effect, "effect:*", null, null));
            Save();
            return true;
        }

        /// <summary>
        /// 角色身上的持久效果列表（副本）
        /// </summary>
        public static List<RoleEffect> GetEffects(RoleRecord _role)
        {
            return _role == null ? new List<RoleEffect>() : new List<RoleEffect>(_role.m_Effects);
        }

        public int GetEffectStacks(int _roleId, int _effectId)
        {
            RoleRecord kRole = DB.GetRole(_roleId);
            return kRole == null ? 0 : kRole.GetEffectStacks(_effectId);
        }

        public bool HasEffect(int _roleId, int _effectId)
        {
            return GetEffectStacks(_roleId, _effectId) > 0;
        }

        #endregion

        #region 配套表：talent / behaviorLogic / buff

        /// <summary>
        /// talent 表配置，找不到返回 null
        /// </summary>
        public static cfg.GameData_Skill.TalentData GetTalent(int _talentId)
        {
            cfg.Tables kTables = GetRuntimeTables();
            if (kTables == null || kTables.talent == null)
            {
                return null;
            }
            return kTables.talent.GetOrDefault(_talentId);
        }

        /// <summary>
        /// 角色身上的天赋配置，没有天赋返回 null
        /// </summary>
        public static cfg.GameData_Skill.TalentData GetTalent(RoleRecord _role)
        {
            return _role == null ? null : GetTalent(_role.m_TalentId);
        }

        /// <summary>
        /// 天赋名（找不到时返回 "未知天赋(id)"）
        /// </summary>
        public static string GetTalentName(int _talentId)
        {
            if (_talentId <= 0)
            {
                return "无天赋";
            }
            cfg.GameData_Skill.TalentData kConfig = GetTalent(_talentId);
            return kConfig == null ? ("未知天赋(" + _talentId + ")") : kConfig.Name;
        }

        /// <summary>
        /// behaviorLogic 表配置，找不到返回 null
        /// </summary>
        public static cfg.GameData_Skill.behaviorLogicData GetBehavior(int _behaviorId)
        {
            cfg.Tables kTables = GetRuntimeTables();
            if (kTables == null || kTables.behaviorLogic == null)
            {
                return null;
            }
            return kTables.behaviorLogic.GetOrDefault(_behaviorId);
        }

        public static string GetBehaviorName(int _behaviorId)
        {
            cfg.GameData_Skill.behaviorLogicData kConfig = GetBehavior(_behaviorId);
            return kConfig == null ? ("未知行为(" + _behaviorId + ")") : kConfig.Desc;
        }

        /// <summary>
        /// 角色当前的初始行为逻辑配置列表
        /// </summary>
        public static List<cfg.GameData_Skill.behaviorLogicData> GetBehaviors(RoleRecord _role)
        {
            List<cfg.GameData_Skill.behaviorLogicData> kResult = new List<cfg.GameData_Skill.behaviorLogicData>();
            if (_role == null)
            {
                return kResult;
            }
            for (int i = 0; i < _role.m_Behaviors.Count; i++)
            {
                cfg.GameData_Skill.behaviorLogicData kConfig = GetBehavior(_role.m_Behaviors[i]);
                if (kConfig != null)
                {
                    kResult.Add(kConfig);
                }
            }
            return kResult;
        }

        /// <summary>
        /// buff 表配置，找不到返回 null
        /// </summary>
        public static cfg.GameData_Skill.buffData GetBuff(int _buffId)
        {
            cfg.Tables kTables = GetRuntimeTables();
            if (kTables == null || kTables.buff == null)
            {
                return null;
            }
            return kTables.buff.GetOrDefault(_buffId);
        }

        public static string GetBuffName(int _buffId)
        {
            cfg.GameData_Skill.buffData kConfig = GetBuff(_buffId);
            return kConfig == null ? ("未知效果(" + _buffId + ")") : kConfig.Name;
        }

        /// <summary>
        /// 该效果是否是正面效果（buff 表 isBuff），配置缺失时返回 false
        /// </summary>
        public static bool IsPositiveBuff(int _buffId)
        {
            cfg.GameData_Skill.buffData kConfig = GetBuff(_buffId);
            return kConfig != null && kConfig.IsBuff;
        }

        /// <summary>
        /// 该效果的最大堆叠层数，配置缺失时返回 0
        /// </summary>
        public static int GetBuffMaxStacks(int _buffId)
        {
            cfg.GameData_Skill.buffData kConfig = GetBuff(_buffId);
            return kConfig == null ? 0 : kConfig.Stacks;
        }

        #endregion

        #region 整体数据

        /// <summary>
        /// 取角色的整体数据快照（属性/职称/装备/勋章/技能/天赋/行为逻辑/持久效果）。
        /// 业务侧拿这一个对象就够了，不需要自己再去拼配置表和加成。
        /// </summary>
        public RoleSnapshot GetRoleData(int _roleId)
        {
            return BuildSnapshot(DB.GetRole(_roleId));
        }

        /// <summary>
        /// 取当前出战（主）角色的整体数据
        /// </summary>
        public RoleSnapshot GetMainRoleData()
        {
            return BuildSnapshot(DB.GetMainRole());
        }

        /// <summary>
        /// 组装角色快照。_tables 为空时用运行时配置表（编辑器预览可自己传一份）。
        /// </summary>
        public static RoleSnapshot BuildSnapshot(RoleRecord _role, cfg.Tables _tables = null)
        {
            if (_role == null)
            {
                return null;
            }

            cfg.Tables kTables = _tables == null ? GetRuntimeTables() : _tables;
            RoleSnapshot kSnapshot = new RoleSnapshot();
            kSnapshot.m_Role = _role;

            RoleTitleLevel kTitle = _role.GetTitleLevel();
            kSnapshot.m_TitleLevel = kTitle;
            kSnapshot.m_TitleName = _role.GetTitleName();
            kSnapshot.m_TitleScore = _role.m_TitleScore;
            kSnapshot.m_NextTitleScore = RoleTitleRule.GetNextScore(kTitle);
            kSnapshot.m_MedalSlots = RoleTitleRule.GetMedalSlots(kTitle);
            kSnapshot.m_ArmorSlots = RoleTitleRule.GetArmorSlots(kTitle);
            kSnapshot.m_MainHand = _role.GetMainHand();
            kSnapshot.m_Effects = GetEffects(_role);

            Dictionary<int, int>.Enumerator kAttrE = _role.m_Attributes.GetEnumerator();
            while (kAttrE.MoveNext())
            {
                kSnapshot.m_BaseAttributes[kAttrE.Current.Key] = kAttrE.Current.Value;
            }

            int kEquipAtk = 0;
            if (kTables != null)
            {
                // 装备（新装备表没有套装字段，套装效果暂时不参与计算）
                for (int i = 0; i < _role.m_EquipIds.Count; i++)
                {
                    cfg.GameData_Item.EquipData kEquip = kTables.equip == null ? null : kTables.equip.GetOrDefault(_role.m_EquipIds[i]);
                    if (kEquip == null)
                    {
                        continue;
                    }
                    kSnapshot.m_Equips.Add(kEquip);
                    kEquipAtk += kEquip.ATK;
                    kSnapshot.m_Defense += kEquip.DEF;
                }

                // 勋章（含套装效果）
                Dictionary<int, int> kMedalSetCount = new Dictionary<int, int>();
                for (int i = 0; i < _role.m_MedalIds.Count; i++)
                {
                    cfg.GameData_Item.MedalData kMedal = kTables.medal == null ? null : kTables.medal.GetOrDefault(_role.m_MedalIds[i]);
                    if (kMedal == null)
                    {
                        continue;
                    }
                    kSnapshot.m_Medals.Add(kMedal);
                    AppendBuffIds(kSnapshot.m_BuffIds, kMedal.Effect);

                    if (kMedal.MedalSet > 0)
                    {
                        int kCount;
                        kMedalSetCount.TryGetValue(kMedal.MedalSet, out kCount);
                        kMedalSetCount[kMedal.MedalSet] = kCount + 1;
                    }
                }

                if (kTables.medal_set != null)
                {
                    Dictionary<int, int>.Enumerator kE = kMedalSetCount.GetEnumerator();
                    while (kE.MoveNext())
                    {
                        cfg.GameData_Item.MedalSetData kSet = kTables.medal_set.GetOrDefault(kE.Current.Key);
                        if (kSet == null)
                        {
                            continue;
                        }
                        if (kE.Current.Value >= 2)
                        {
                            AppendBuffIds(kSnapshot.m_BuffIds, kSet.Effect2);
                        }
                        if (kE.Current.Value >= 3)
                        {
                            AppendBuffIds(kSnapshot.m_BuffIds, kSet.Effect3);
                        }
                    }
                }

                kSnapshot.m_Talent = kTables.talent == null ? null : kTables.talent.GetOrDefault(_role.m_TalentId);
                kSnapshot.m_ActiveSkill = _role.m_ActiveSkillId > 0 ? GetSkill(_role.m_ActiveSkillId) : null;
                kSnapshot.m_PassiveSkill = _role.m_PassiveSkillId > 0 ? GetSkill(_role.m_PassiveSkillId) : null;
                kSnapshot.m_Behaviors = GetBehaviors(_role);
            }

            // 最终属性 = 基础 + 职称加成 + 装备攻击，再用 role.xlsx 的 maxValue 封顶
            if (kTables != null && kTables.role != null)
            {
                List<cfg.GameData_Role.RoleData> kTemplate = kTables.role.DataList;
                for (int i = 0; i < kTemplate.Count; i++)
                {
                    cfg.GameData_Role.RoleData kConfig = kTemplate[i];
                    if (kConfig == null)
                    {
                        continue;
                    }

                    int kBase = _role.GetAttr(kConfig.ID);
                    int kBonus = RoleTitleRule.GetAttrBonus(kConfig.ID, kTitle, kBase);
                    if (kConfig.ID == RoleAttrId.Atk)
                    {
                        kBonus += kEquipAtk;
                    }

                    int kFinal = kBase + kBonus;
                    int kCap = Mathf.RoundToInt(kConfig.MaxValue);
                    if (kCap > 0 && kFinal > kCap)
                    {
                        kFinal = kCap;
                    }
                    if (kFinal < 0)
                    {
                        kFinal = 0;
                    }

                    kSnapshot.m_FinalAttributes[kConfig.ID] = kFinal;
                    kSnapshot.m_AttributeBonus[kConfig.ID] = kFinal - kBase;
                }
            }
            else
            {
                Dictionary<int, int>.Enumerator kE = kSnapshot.m_BaseAttributes.GetEnumerator();
                while (kE.MoveNext())
                {
                    kSnapshot.m_FinalAttributes[kE.Current.Key] = kE.Current.Value;
                }
            }

            return kSnapshot;
        }

        private static void AppendBuffIds(List<int> _target, List<int> _source)
        {
            if (_source == null)
            {
                return;
            }
            for (int i = 0; i < _source.Count; i++)
            {
                AppendBuffId(_target, _source[i]);
            }
        }

        private static void AppendBuffId(List<int> _target, int _buffId)
        {
            if (_buffId > 0 && !_target.Contains(_buffId))
            {
                _target.Add(_buffId);
            }
        }

        private void NotifyChanged(int _roleId, RoleChangeOp _op, string _field, object _oldValue, object _newValue)
        {
            Game.Event.SendMessage(RoleEvent.OnRoleChanged,
                new RoleChangedMsg(_roleId, _op, _field, _oldValue, _newValue));
        }

        #endregion

        #region 存档（文件 IO 由 RoleData / SystemDataBase 处理）

        /// <summary>
        /// 存档路径：Application.persistentDataPath/GameMain/Data/role.json
        /// 纯路径计算，不依赖运行时数据，编辑器里也能取。
        /// </summary>
        public static string GetSavePath()
        {
            return Application.persistentDataPath + "/GameMain/Data/" + RoleData.SaveFileName;
        }

        /// <summary>
        /// 保存全部角色到本地（JSON）。失败返回 false 并打日志。
        /// </summary>
        public bool Save()
        {
            return DB.Save();
        }

        /// <summary>
        /// 从本地读档。文件不存在返回 false（不算错误）。
        /// </summary>
        public bool Load(RoleAcquireReason _reason = RoleAcquireReason.Load)
        {
            RoleData kDB = DB;
            if (!kDB.Load())
            {
                return false;
            }

            Game.Event.SendMessage(RoleEvent.OnRolesChanged, new RolesChangedMsg(kDB.GetVersion(), kDB.GetRoleCount(), _reason));
            Game.Log.LogInfo("[角色] 读档完成：角色数 " + kDB.GetRoleCount());
            return true;
        }

        /// <summary>
        /// 删除本地存档
        /// </summary>
        public bool DeleteSave()
        {
            return DB.DeleteSave();
        }

        #endregion

        #region 调试

        /// <summary>
        /// 单角色的可读描述（含职称、装备、勋章、技能、最终属性）。
        /// _tables 为空时用运行时的配置表；编辑器里可以自己传一份表进来预览。
        /// </summary>
        public static string Describe(RoleRecord _role, cfg.Tables _tables = null)
        {
            if (_role == null)
            {
                return "(空角色)";
            }

            cfg.Tables kTables = _tables == null ? GetRuntimeTables() : _tables;
            RoleSnapshot kSnapshot = BuildSnapshot(_role, kTables);

            StringBuilder kBuilder = new StringBuilder();
            kBuilder.Append("[角色] ").Append(kSnapshot.GetDisplayName())
                .Append(" (id=").Append(_role.m_Id)
                .Append(", lv=").Append(_role.m_Level)
                .Append(", 职称=").Append(kSnapshot.m_TitleName)
                .Append(" ").Append(kSnapshot.m_TitleScore).Append("分)");

            if (kTables != null && kTables.role != null)
            {
                List<cfg.GameData_Role.RoleData> kTemplate = kTables.role.DataList;
                for (int i = 0; i < kTemplate.Count; i++)
                {
                    cfg.GameData_Role.RoleData kAttr = kTemplate[i];
                    if (kAttr == null)
                    {
                        continue;
                    }

                    int kBase = kSnapshot.GetBaseAttr(kAttr.ID);
                    int kFinal = kSnapshot.GetFinalAttr(kAttr.ID);
                    int kBonus = kFinal - kBase;
                    string kDisplay = kAttr.ID == RoleAttrId.Talent
                        ? GetTalentNameWith(kTables, kBase)
                        : kFinal.ToString();

                    kBuilder.Append("\n  ").Append(kAttr.Show ? "· " : "  (隐藏) ")
                        .Append(kAttr.Name).Append(" = ").Append(kDisplay);
                    if (kAttr.ID != RoleAttrId.Talent)
                    {
                        if (kBonus != 0)
                        {
                            kBuilder.Append(" (基础 " + kBase + " +" + kBonus + ")");
                        }
                        kBuilder.Append(" / 上限 ").Append(Mathf.RoundToInt(kAttr.MaxValue));
                    }
                }
            }
            else
            {
                kBuilder.Append(" 属性数=").Append(_role.m_Attributes.Count)
                    .Append(" 行为数=").Append(_role.m_Behaviors.Count);
            }

            kBuilder.Append("\n  天赋：").Append(kSnapshot.m_Talent == null ? "无" : kSnapshot.m_Talent.Name);
            kBuilder.Append("\n  主动技能：").Append(kSnapshot.m_ActiveSkill == null ? "无" : kSnapshot.m_ActiveSkill.Name);
            kBuilder.Append("\n  被动技能：").Append(kSnapshot.m_PassiveSkill == null ? "无" : kSnapshot.m_PassiveSkill.Name);

            kBuilder.Append("\n  装备(").Append(kSnapshot.m_Equips.Count).Append(")：");
            if (kSnapshot.m_Equips.Count == 0)
            {
                kBuilder.Append("无");
            }
            else
            {
                for (int i = 0; i < kSnapshot.m_Equips.Count; i++)
                {
                    if (i > 0)
                    {
                        kBuilder.Append("、");
                    }
                    kBuilder.Append(kSnapshot.m_Equips[i].Name);
                }
            }

            kBuilder.Append("\n  勋章(").Append(kSnapshot.m_Medals.Count).Append("/").Append(kSnapshot.m_MedalSlots).Append(")：");
            if (kSnapshot.m_Medals.Count == 0)
            {
                kBuilder.Append("无");
            }
            else
            {
                for (int i = 0; i < kSnapshot.m_Medals.Count; i++)
                {
                    if (i > 0)
                    {
                        kBuilder.Append("、");
                    }
                    kBuilder.Append(kSnapshot.m_Medals[i].Name);
                }
            }

            kBuilder.Append("\n  护甲槽 ").Append(kSnapshot.m_ArmorSlots)
                .Append("，防御 ").Append(kSnapshot.m_Defense)
                .Append("，总重量 ").Append(kSnapshot.m_TotalWeight);

            if (kSnapshot.m_BuffIds.Count > 0)
            {
                kBuilder.Append("\n  装备/勋章效果：");
                for (int i = 0; i < kSnapshot.m_BuffIds.Count; i++)
                {
                    if (i > 0)
                    {
                        kBuilder.Append("、");
                    }
                    int kBuffId = kSnapshot.m_BuffIds[i];
                    cfg.GameData_Skill.buffData kBuff = kTables == null || kTables.buff == null
                        ? null
                        : kTables.buff.GetOrDefault(kBuffId);
                    kBuilder.Append(kBuff == null ? kBuffId.ToString() : kBuff.Name);
                }
            }

            kBuilder.Append("\n  行为逻辑：");
            for (int i = 0; i < _role.m_Behaviors.Count; i++)
            {
                int kId = _role.m_Behaviors[i];
                cfg.GameData_Skill.behaviorLogicData kBehavior = kTables == null || kTables.behaviorLogic == null
                    ? null
                    : kTables.behaviorLogic.GetOrDefault(kId);
                string kName = kBehavior == null ? kId.ToString() : kBehavior.Desc;
                if (i > 0)
                {
                    kBuilder.Append("、");
                }
                kBuilder.Append(kId).Append(' ').Append(kName);
            }

            kBuilder.Append("\n  主手：").Append(kSnapshot.m_MainHand);

            kBuilder.Append("\n  持久效果：");
            if (_role.m_Effects.Count == 0)
            {
                kBuilder.Append("无");
            }
            else
            {
                for (int i = 0; i < _role.m_Effects.Count; i++)
                {
                    int kEffectId = _role.m_Effects[i].m_Id;
                    cfg.GameData_Skill.buffData kBuff = kTables == null || kTables.buff == null
                        ? null
                        : kTables.buff.GetOrDefault(kEffectId);
                    if (i > 0)
                    {
                        kBuilder.Append("、");
                    }
                    kBuilder.Append(kBuff == null ? ("未知效果(" + kEffectId + ")") : kBuff.Name)
                        .Append(" x").Append(_role.m_Effects[i].m_Stacks);
                }
            }

            kBuilder.Append("\n  异常适应性：");
            if (_role.m_Adaptations.Count == 0)
            {
                kBuilder.Append("无");
            }
            else
            {
                bool kFirst = true;
                Dictionary<int, int>.Enumerator kE = _role.m_Adaptations.GetEnumerator();
                while (kE.MoveNext())
                {
                    if (!kFirst)
                    {
                        kBuilder.Append("、");
                    }
                    kFirst = false;
                    kBuilder.Append("类型").Append(kE.Current.Key).Append(" Lv").Append(kE.Current.Value);
                }
            }

            return kBuilder.ToString();
        }

        /// <summary>
        /// 当前存档的可读快照
        /// </summary>
        public string Dump()
        {
            RoleData kDB = DB;
            StringBuilder kBuilder = new StringBuilder();
            kBuilder.Append("[角色库] version=").Append(kDB.GetVersion())
                .Append(" count=").Append(kDB.GetRoleCount())
                .Append(" main=").Append(kDB.GetMainRoleId());

            List<RoleRecord> kAll = kDB.GetAllRoles();
            for (int i = 0; i < kAll.Count; i++)
            {
                kBuilder.Append("\n  ").Append(kAll[i]);
            }
            return kBuilder.ToString();
        }

        private static string GetTalentNameWith(cfg.Tables _tables, int _talentId)
        {
            if (_talentId <= 0)
            {
                return "无天赋";
            }
            if (_tables == null || _tables.talent == null)
            {
                return _talentId.ToString();
            }
            cfg.GameData_Skill.TalentData kConfig = _tables.talent.GetOrDefault(_talentId);
            return kConfig == null ? ("未知天赋(" + _talentId + ")") : kConfig.Name;
        }

        #endregion

        /// <summary>
        /// 运行时配置表。游戏还没启动（编辑器里预览）时返回 null，避免误创建 DataManager。
        /// </summary>
        private static cfg.Tables GetRuntimeTables()
        {
            if (!GameConst.m_GameStart)
            {
                return null;
            }
            return Game.Data.GetData;
        }
    }
}
