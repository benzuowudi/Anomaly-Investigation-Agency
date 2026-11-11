using System.Collections.Generic;
using FairyGUI;
using Framework.Script.UI;
using GameMain.Script.Battle;
using GameMain.Script.BattleSetting;
using GameMain.Script.Explore;
using GameMain.Script.Role;
using UnityEngine;

namespace GameMain.Script.UI
{
    /// <summary>
    /// 节点事件界面（FairyGUI 包 World / NodeSettingView，预制体 NodeSettingView）。
    ///
    /// 结构：
    ///   com_bg      通用背景框（标题 / 返回），Base 包 CommonBgCom
    ///   pro_node    节点推进度（NodePro，ProgressBar）
    ///   load_com    节点面板（GLoader）：
    ///                 战斗/强敌 → World/NodeBattleCom（布阵）
    ///                 物资搜索  → World/NodeInvestigationCom（调查点列表）
    ///
    /// 流程：
    ///   战斗：拖动卡片排站位 → 开始战斗 → BattleView → 结算/回地图；
    ///   物资搜索：选一个调查点 → 开始调查 → 标记节点已结算 → 回地图。
    /// </summary>
    public class NodeSettingView : UIWindow
    {
        private CommonBgCom com_bg;
        private GProgressBar pro_node;
        private GLoader load_com;
        private NodeBattleCom m_BattleCom;
        private NodeInvestigationCom m_InvestigationCom;

        /// <summary>当前 load_com 里的面板 URL，避免重复设置导致重建</summary>
        private string m_PanelUrl;

        /// <summary>ExploreBattleNodeMsg 或 ExploreNodeEventMsg</summary>
        private object m_Context;
        private NodeBattleInfo m_Info;

        private static ExploreSystem Explore
        {
            get { return Game.Model.Get<ExploreSystem>(); }
        }

        private static BattleSystem Battle
        {
            get { return Game.Model.Get<BattleSystem>(); }
        }

        protected internal override void Init(object o)
        {
            com_bg = ui.GetChild("com_bg") as CommonBgCom;
            pro_node = ui.GetChild("pro_node").asProgress;
            load_com = ui.GetChild("load_com").asLoader;

            base.Init(o);
        }

        protected internal override void Enter(object o)
        {
            base.Enter(o);

            m_Context = o;
            Refresh();
        }

        #region 刷新

        private void Refresh()
        {
            ExploreMapSnapshot kSnapshot = Explore.BuildMapSnapshot();
            if (kSnapshot == null)
            {
                Game.Log.LogWarning(ExploreSystem.c_LogTag + "打开节点界面时没有进行中的调查，返回指挥室");
                Game.UI.ShowWindow(UIDefine.CommandRoom);
                return;
            }

            int kNodeId = GetContextNodeId(kSnapshot.m_CurrentNodeId);
            ExploreNodeSnapshot kNode = kSnapshot.GetNode(kNodeId);
            if (kNode == null)
            {
                Game.Log.LogWarning(ExploreSystem.c_LogTag + "节点不存在：" + kNodeId);
                Game.UI.ShowWindow(UIDefine.Explore);
                return;
            }

            int kPercent = Mathf.RoundToInt(kNode.m_Progress * 100f);
            pro_node.max = 100;
            pro_node.value = kPercent;

            if (ExploreConst.IsBattleType(kNode.m_TypeId))
            {
                ShowBattlePanel(kNode, GetContextEncounterId(kNode), kPercent);
                return;
            }

            if (kNode.m_TypeId == (int)cfg.GameType.NodeType.supply)
            {
                ShowInvestigationPanel(kNode);
                return;
            }

            Game.Log.LogWarning(ExploreSystem.c_LogTag + "节点类型还没有面板：" + kNode.m_TypeId);
            Game.UI.ShowWindow(UIDefine.Explore);
        }

        private int GetContextNodeId(int _fallback)
        {
            ExploreBattleNodeMsg kBattle = m_Context as ExploreBattleNodeMsg;
            if (kBattle != null)
            {
                return kBattle.m_NodeId;
            }

            ExploreNodeEventMsg kEvent = m_Context as ExploreNodeEventMsg;
            if (kEvent != null)
            {
                return kEvent.m_NodeId;
            }

            return _fallback;
        }

        private int GetContextEncounterId(ExploreNodeSnapshot _node)
        {
            ExploreBattleNodeMsg kBattle = m_Context as ExploreBattleNodeMsg;
            if (kBattle != null && kBattle.m_EncounterId > 0)
            {
                return kBattle.m_EncounterId;
            }

            return _node == null ? 0 : _node.m_EncounterId;
        }

        /// <summary>切换 load_com 里的面板（同一个 URL 不重建）</summary>
        private void EnsurePanel(string _url)
        {
            if (m_PanelUrl == _url)
            {
                return;
            }

            m_PanelUrl = _url;
            load_com.url = _url;
        }

        private void ShowBattlePanel(ExploreNodeSnapshot _node, int _encounterId, int _percent)
        {
            EnsurePanel("ui://World/NodeBattleCom");

            m_InvestigationCom = null;
            m_BattleCom = load_com.component as NodeBattleCom;
            if (m_BattleCom == null)
            {
                Game.Log.LogError(ExploreSystem.c_LogTag +
                    "布阵面板创建失败：ui://World/NodeBattleCom（检查 ExploreViewData 的包注册）");
                return;
            }

            m_BattleCom.onBattle = OnClickBattle;
            m_BattleCom.onCardPositionChanged = OnPositionChanged;
            com_bg.Init("战斗节点", "BATTLE NODE", OnClickBack);

            m_Info = BuildInfo(_node, _encounterId, _percent);
            m_BattleCom.SetCom(m_Info);
        }

        private void ShowInvestigationPanel(ExploreNodeSnapshot _node)
        {
            EnsurePanel("ui://World/NodeInvestigationCom");

            m_BattleCom = null;
            m_InvestigationCom = load_com.component as NodeInvestigationCom;
            if (m_InvestigationCom == null)
            {
                Game.Log.LogError(ExploreSystem.c_LogTag +
                    "物资搜索面板创建失败：ui://World/NodeInvestigationCom（检查 ExploreViewData 的包注册）");
                return;
            }

            com_bg.Init("物资搜索", "SUPPLY SEARCH", OnClickBack);

            NodeInvestigationInfo kInfo = BuildInvestigationInfo(_node);
            m_InvestigationCom.SetCom(kInfo, OnClickInvestigate);
        }

        /// <summary>
        /// 物资搜索的调查点：目前没有专用配表，先按节点类型生成 3 个调查点；
        /// 后续接掉落表后把奖励 ID 填到 InvestigationSiteInfo.m_ItemIds。
        /// </summary>
        private NodeInvestigationInfo BuildInvestigationInfo(ExploreNodeSnapshot _node)
        {
            ExploreNodeTypeView kType = Explore.GetNodeTypeView(_node.m_TypeId);

            NodeInvestigationInfo kInfo = new NodeInvestigationInfo();
            kInfo.m_NodeId = _node.m_Id;
            kInfo.m_TypeId = _node.m_TypeId;
            kInfo.m_NodeName = kType.m_Name;
            kInfo.m_NodeDesc = kType.m_Desc;

            int kCount = 3;
            for (int i = 0; i < kCount; i++)
            {
                InvestigationSiteInfo kSite = new InvestigationSiteInfo();
                kSite.m_Index = i;
                kSite.m_Title = kType.m_Name + " " + (i + 1);
                kSite.m_Desc = string.IsNullOrEmpty(kType.m_Desc) ? "搜刮物资" : kType.m_Desc;
                kSite.m_SuccessRate = 75 - i * 10;
                kInfo.m_Sites.Add(kSite);
            }

            return kInfo;
        }

        /// <summary>组装布阵面板的展示数据（标题 / 战场 / 敌情 / 队员）</summary>
        private NodeBattleInfo BuildInfo(ExploreNodeSnapshot _node, int _encounterId, int _percent)
        {
            NodeBattleInfo kInfo = new NodeBattleInfo();
            kInfo.m_NodeId = _node == null ? 0 : _node.m_Id;
            kInfo.m_EncounterId = _encounterId;
            kInfo.m_ProgressPercent = _percent;

            cfg.Tables kTables = Game.Data.GetData;
            cfg.GameData_Battle.EncounterData kEncounter = null;
            if (kTables != null && kTables.encounter != null && _encounterId > 0)
            {
                kEncounter = kTables.encounter.GetOrDefault(_encounterId);
            }

            if (kEncounter != null)
            {
                kInfo.m_TitleCn = kEncounter.Name;
                kInfo.m_TitleEn = "ENCOUNTER " + kEncounter.ID.ToString("D3");

                cfg.GameData_Battle.BattlefieldData kField = null;
                if (kTables != null && kTables.battlefield != null)
                {
                    kField = kTables.battlefield.GetOrDefault(kEncounter.BattlefieldID);
                }

                if (kField != null)
                {
                    kInfo.m_InfoNameCn = kField.Name;
                    kInfo.m_Desc = "战场全长 " + kField.Length.ToString("F0") + " 米 · 天气 "
                        + kEncounter.Weather + " · 胜利条件 " + kEncounter.WinCondition;
                }
                else
                {
                    kInfo.m_InfoNameCn = "未配置战场";
                    kInfo.m_Desc = "遭遇已配置，但 battlefield 表里找不到 ID " + kEncounter.BattlefieldID;
                }
            }
            else
            {
                kInfo.m_TitleCn = _node == null ? "战斗" : _node.m_TypeName;
                kInfo.m_TitleEn = "BATTLE NODE";
                kInfo.m_InfoNameCn = "调试战场";
                kInfo.m_Desc = "未配置遭遇战（encounter 表为空），开战后按队伍人数配平调试编成。";
            }

            FillRoles(kInfo);
            return kInfo;
        }

        /// <summary>
        /// 队员：优先用战前整备的队伍（上限 = 战前整备的槽位数，不能写死 4，
        /// 否则编队里第 5 个人会被静默丢掉），队伍为空时取前几名角色；同时带上各自站位。
        /// </summary>
        private static void FillRoles(NodeBattleInfo _info)
        {
            BattleSettingSystem kSetting = Game.Model.Get<BattleSettingSystem>();
            RoleSystem kRole = Game.Model.Get<RoleSystem>();

            int kMaxRoles = BattleSettingDefine.c_MaxTeamCount;
            IReadOnlyList<int> kTeam = kSetting.GetTeamRoleIds();
            HashSet<int> kTeamSet = new HashSet<int>(kTeam);
            for (int i = 0; i < kTeam.Count && _info.m_Roles.Count < kMaxRoles; i++)
            {
                RoleSnapshot kSnapshot = kRole.GetRoleData(kTeam[i]);
                if (kSnapshot != null && kSnapshot.m_Role != null)
                {
                    _info.m_Roles.Add(kSnapshot);
                }
            }

            if (_info.m_Roles.Count == 0)
            {
                List<RoleRecord> kAll = kRole.GetAllRoles();
                for (int i = 0; i < kAll.Count && _info.m_Roles.Count < kMaxRoles; i++)
                {
                    RoleSnapshot kSnapshot = kRole.GetRoleData(kAll[i].m_Id);
                    if (kSnapshot != null)
                    {
                        _info.m_Roles.Add(kSnapshot);
                    }
                }
            }

            // 站位：队伍里的角色用战前整备存的 0~1；临时补进列表的角色没有站位，按序号均分
            int kCount = _info.m_Roles.Count;
            for (int i = 0; i < kCount; i++)
            {
                int kRoleId = _info.m_Roles[i].m_Role.m_Id;
                float kPosition = kTeamSet.Contains(kRoleId)
                    ? kSetting.GetPosition(kRoleId)
                    : (i + 0.5f) / kCount;
                _info.m_Positions.Add(kPosition);
            }

            // 历史存档里可能出现多人同一站位（早期界面写进过 1），进布阵时按顺序均分，避免卡片叠在一起
            NormalizePositions(_info, kSetting, kTeamSet);
        }

        private static void NormalizePositions(NodeBattleInfo _info, BattleSettingSystem _setting, HashSet<int> _team)
        {
            int kCount = _info.m_Positions.Count;
            if (kCount < 2)
            {
                return;
            }

            bool kOverlap = false;
            for (int i = 0; i < kCount && !kOverlap; i++)
            {
                for (int j = i + 1; j < kCount; j++)
                {
                    if (Mathf.Abs(_info.m_Positions[i] - _info.m_Positions[j]) < 0.05f)
                    {
                        kOverlap = true;
                        break;
                    }
                }
            }

            if (!kOverlap)
            {
                return;
            }

            for (int i = 0; i < kCount; i++)
            {
                float kPosition = (i + 0.5f) / kCount;
                _info.m_Positions[i] = kPosition;

                int kRoleId = _info.m_Roles[i].m_Role.m_Id;
                if (_team.Contains(kRoleId))
                {
                    _setting.SetPosition(kRoleId, kPosition);
                }
            }
        }

        #endregion

        #region 输入

        private void OnClickBattle()
        {
            if (m_Info == null)
            {
                return;
            }

            List<int> kRoleIds = new List<int>();
            for (int i = 0; i < m_Info.m_Roles.Count; i++)
            {
                if (m_Info.m_Roles[i].m_Role != null)
                {
                    kRoleIds.Add(m_Info.m_Roles[i].m_Role.m_Id);
                }
            }

            bool kStarted = false;
            if (m_Info.m_EncounterId > 0)
            {
                kStarted = Battle.StartBattle(m_Info.m_EncounterId, kRoleIds);
            }

            if (!kStarted)
            {
                // 调试编成按队伍人数配平：1v1 / … / 5v5，避免 1 个人打 4 个直接全灭
                int kAllyCount = kRoleIds.Count > 0 ? kRoleIds.Count : BattleSettingDefine.c_MaxTeamCount;
                int kEnemyCount = Mathf.Clamp(kAllyCount, 1, BattleSettingDefine.c_MaxTeamCount);
                kStarted = Battle.StartDebugBattle(kAllyCount, kEnemyCount, kRoleIds);
                if (kStarted)
                {
                    Game.Log.LogWarning(ExploreSystem.c_LogTag + "遭遇战无法开始，改用调试编成（"
                        + kAllyCount + " v " + kEnemyCount + "）");
                }
            }

            if (!kStarted)
            {
                Game.Log.LogError(ExploreSystem.c_LogTag + "开战失败：遭遇 " + m_Info.m_EncounterId);
                return;
            }

            // 布阵界面拖出来的站位应用到刚生成的己方单位
            Game.Model.Get<BattleSettingSystem>().ApplyPositionsToBattle();

            // 战斗画面交给 Battle 包的 BattleView 外壳
            Game.UI.ShowWindow(UIDefine.Battle);
        }

        private void OnClickBack()
        {
            Game.UI.ShowWindow(UIDefine.Explore);
        }

        /// <summary>物资搜索：选了一个调查点 → 结算节点事件 → 回地图（奖励/成功率结算后续接）</summary>
        private void OnClickInvestigate(InvestigationSiteInfo _site)
        {
            if (_site != null)
            {
                Game.Log.LogInfo(ExploreSystem.c_LogTag + "开始调查：" + _site.m_Title
                    + " 成功率 " + _site.m_SuccessRate + "%");
            }

            Explore.ResolveCurrentNodeEvent();
            Game.UI.ShowWindow(UIDefine.Explore);
        }

        private void OnPositionChanged(int _roleId, float _position)
        {
            Game.Model.Get<BattleSettingSystem>().SetPosition(_roleId, _position);
        }

        #endregion
    }
}
