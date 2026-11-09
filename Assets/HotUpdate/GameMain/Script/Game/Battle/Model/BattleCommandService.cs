using System;
using System.Collections.Generic;
using cfg.GameType;

namespace GameMain.Script.Battle
{
    /// <summary>
    /// 指令系统：把玩家的指令变成"每个单位是否响应、响应后做什么"。
    ///
    /// 响应判定四段式（文档 1：指令是模糊的，角色可能不响应）：
    ///   ① 硬性不可响应：死亡 / 重伤 / 被控制 / 已离场
    ///   ② 自身状态优先：正在撤离的单位忽略其它指令
    ///   ③ 天赋拒绝：由外部注入的拒绝率（天赋系统接入后挂上；未接入时恒为 0）
    ///   ④ 基础响应率：command.xlsx 的 baseSuccess（万分比）
    ///
    /// 指令只对"我方"生效（玩家指挥自己的队伍），敌方不用指令系统。
    /// </summary>
    public class BattleCommandService
    {
        private const float c_TenThousand = 10000f;

        private readonly BattleField m_Field;
        private readonly BattleConfig m_Config;
        private readonly System.Random m_Random;
        private readonly Dictionary<int, float> m_Cooldown = new Dictionary<int, float>();

        /// <summary>天赋拒绝率注入点：(单位, 指令ID) → 0~1；返回 0 表示不拒绝</summary>
        private Func<BattleUnit, int, float> m_TalentRefuseProvider;

        public BattleCommandService(BattleField _field, BattleConfig _config, System.Random _random)
        {
            m_Field = _field;
            m_Config = _config;
            m_Random = _random;
        }

        public void SetTalentRefuseProvider(Func<BattleUnit, int, float> _provider)
        {
            m_TalentRefuseProvider = _provider;
        }

        /// <summary>指令冷却推进（暂停时调用方传 0）</summary>
        public void Tick(float _dt)
        {
            if (m_Cooldown.Count == 0 || _dt <= 0f)
            {
                return;
            }

            List<int> kKeys = new List<int>(m_Cooldown.Keys);
            for (int i = 0; i < kKeys.Count; i++)
            {
                float kRemain = m_Cooldown[kKeys[i]] - _dt;
                if (kRemain <= 0f)
                {
                    m_Cooldown.Remove(kKeys[i]);
                }
                else
                {
                    m_Cooldown[kKeys[i]] = kRemain;
                }
            }
        }

        /// <summary>指令剩余冷却（秒）</summary>
        public float GetCooldown(int _commandId)
        {
            float kRemain;
            return m_Cooldown.TryGetValue(_commandId, out kRemain) ? kRemain : 0f;
        }

        /// <summary>
        /// 下达指令。paramUnitId 对集火是目标敌人ID，其它指令可传 0。
        /// </summary>
        public BattleCommandResult Issue(int _commandId, int _paramUnitId)
        {
            BattleCommandResult kResult = new BattleCommandResult(_commandId, _paramUnitId);

            cfg.GameData_Battle.CommandData kCommand = GetCommand(_commandId);
            if (kCommand == null)
            {
                Game.Log.LogWarning("[战斗] 指令不存在：" + _commandId);
                return kResult;
            }

            float kCd = GetCooldown(_commandId);
            if (kCd > 0f)
            {
                Game.Log.LogWarning("[战斗] 指令冷却中：" + kCommand.Name + " 还剩 " + kCd.ToString("F1") + " 秒");
                return null;
            }

            List<BattleUnit> kReceivers = m_Field.GetAllies(Faction.ally);
            for (int i = 0; i < kReceivers.Count; i++)
            {
                BattleUnit kUnit = kReceivers[i];
                CommandResponse kResponse = Judge(kUnit, kCommand, _commandId);
                if (kResponse == CommandResponse.Accepted)
                {
                    Apply(kUnit, _commandId, _paramUnitId);
                }
                kResult.Add(kUnit.m_UnitId, kResponse);
            }

            Game.Log.LogInfo("[战斗] 指令 " + kCommand.Name + "：" + kResult);

            if (kCommand.Cd > 0f)
            {
                m_Cooldown[_commandId] = kCommand.Cd;
            }
            return kResult;
        }

        #region 判定与执行

        private CommandResponse Judge(BattleUnit _unit, cfg.GameData_Battle.CommandData _command, int _commandId)
        {
            // ① 硬性不可响应
            if (_unit == null || !_unit.IsAlive() || _unit.m_LeftField)
            {
                return CommandResponse.RejectedDisabled;
            }
            if (_unit.m_State == UnitBrainState.Downed || _unit.m_State == UnitBrainState.Controlled)
            {
                return CommandResponse.RejectedDisabled;
            }

            // ② 自身状态优先：撤离中的单位不再理会别的指令
            if (_unit.m_Retreating && _commandId != BattleCommandId.Retreat)
            {
                return CommandResponse.RejectedSelfPriority;
            }

            // ③ 天赋拒绝（未接入时为 0）
            if (m_TalentRefuseProvider != null)
            {
                float kRefuse = m_TalentRefuseProvider(_unit, _commandId);
                if (kRefuse > 0f && (float)m_Random.NextDouble() < kRefuse)
                {
                    return CommandResponse.RejectedTalent;
                }
            }

            // ④ 基础响应率
            float kBaseSuccess = _command.BaseSuccess <= 0 ? 1f : _command.BaseSuccess / c_TenThousand;
            kBaseSuccess = kBaseSuccess > 1f ? 1f : kBaseSuccess;
            if ((float)m_Random.NextDouble() > kBaseSuccess)
            {
                return CommandResponse.RejectedRoll;
            }

            return CommandResponse.Accepted;
        }

        private void Apply(BattleUnit _unit, int _commandId, int _paramUnitId)
        {
            switch (_commandId)
            {
                case BattleCommandId.Focus:
                    _unit.m_FocusTargetUnitId = _paramUnitId;
                    _unit.m_Retreating = false;
                    break;
                case BattleCommandId.Retreat:
                    _unit.m_Retreating = true;
                    _unit.m_FocusTargetUnitId = 0;
                    break;
                case BattleCommandId.Free:
                    _unit.m_Retreating = false;
                    _unit.m_FocusTargetUnitId = 0;
                    break;
                default:
                    break;
            }
        }

        private cfg.GameData_Battle.CommandData GetCommand(int _commandId)
        {
            cfg.Tables kTables = Game.Data.GetData;
            if (kTables == null || kTables.command == null)
            {
                return null;
            }
            return kTables.command.GetOrDefault(_commandId);
        }

        #endregion
    }
}
