using System.Collections.Generic;
using cfg.GameType;
using Framework.Script.Model;
using GameMain.Script.Battle;

namespace GameMain.Script.Explore
{
    /// <summary>
    /// 探索系统的主模型（M 层，一次调查的唯一对外入口）。
    ///
    /// 职责：
    /// - 开始/结束一次调查（ExploreRun 生命周期）；
    /// - 节点移动、可达性刷新、撤离地点判定；
    /// - 给界面提供只读快照（ExploreMapSnapshot）；
    /// - 进入战斗节点时广播事件，由界面打开战前整备；
    /// - 把进度落盘（ExploreData），保证退出游戏还能接着走。
    ///
    /// 不做：节点事件的玩法细节（物资/调查/事件的结算留给后续）、UI 绘制、直接开战。
    /// </summary>
    public class ExploreSystem : ModelBase
    {
        public const string c_LogTag = "[探索]";

        private ExploreData DB
        {
            get { return Game.Data.GetGameData<ExploreData>(); }
        }

        #region 生命周期

        protected internal override void OnInit()
        {
            Game.Event.Subscribe(BattleEvent.OnBattleEnded, OnBattleEnded);
        }

        protected internal override void OnStart()
        {
            DB.Load();
        }

        protected internal override void OnDestroy()
        {
            Game.Event.UnSubscribe(BattleEvent.OnBattleEnded, OnBattleEnded);
        }

        #endregion

        #region 查询

        public ExploreRun GetRun()
        {
            return DB.GetRun();
        }

        public bool HasRun()
        {
            return DB.HasRun();
        }

        public int GetZoneId()
        {
            ExploreRun kRun = DB.GetRun();
            return kRun == null ? ExploreConst.DefaultZoneId : kRun.m_ZoneId;
        }

        public List<ExploreNode> GetAvailableNodes()
        {
            List<ExploreNode> kResult = new List<ExploreNode>();
            ExploreRun kRun = DB.GetRun();
            if (kRun == null)
            {
                return kResult;
            }

            foreach (KeyValuePair<int, ExploreNode> kPair in kRun.m_Nodes)
            {
                if (kPair.Value.m_State == ExploreNodeState.Available)
                {
                    kResult.Add(kPair.Value);
                }
            }
            kResult.Sort((a, b) => a.m_Layer.CompareTo(b.m_Layer));
            return kResult;
        }

        /// <summary>节点类型的展示视图（表为空时走兜底）</summary>
        public ExploreNodeTypeView GetNodeTypeView(int _typeId)
        {
            return ExploreNodeTypeRule.Get(Game.Data.GetData, _typeId);
        }

        /// <summary>
        /// 组装给界面的只读快照。没有进行中的调查时返回 null。
        /// </summary>
        public ExploreMapSnapshot BuildMapSnapshot()
        {
            ExploreRun kRun = DB.GetRun();
            if (kRun == null)
            {
                return null;
            }

            cfg.Tables kTables = Game.Data.GetData;
            cfg.GameData_Explore.ExploreZoneData kZone = null;
            if (kTables != null && kTables.exploreZone != null)
            {
                kZone = kTables.exploreZone.GetOrDefault(kRun.m_ZoneId);
            }

            ExploreMapSnapshot kSnapshot = new ExploreMapSnapshot();
            kSnapshot.m_ZoneId = kRun.m_ZoneId;
            kSnapshot.m_ZoneName = kZone != null && !string.IsNullOrEmpty(kZone.Name)
                ? kZone.Name
                : "异常区域 " + kRun.m_ZoneId;
            kSnapshot.m_ZoneDesc = kZone == null ? string.Empty : kZone.Desc;
            kSnapshot.m_LayerCount = kRun.m_LayerCount;
            kSnapshot.m_Depth = kRun.m_Depth;
            kSnapshot.m_Steps = kRun.m_Steps;
            kSnapshot.m_GoalReached = kRun.m_GoalReached;
            kSnapshot.m_StartNodeId = kRun.m_StartNodeId;
            kSnapshot.m_GoalNodeId = kRun.m_GoalNodeId;
            kSnapshot.m_CurrentNodeId = kRun.m_CurrentNodeId;
            kSnapshot.m_MapWidth = kRun.m_MapWidth > 0f ? kRun.m_MapWidth : ExploreConst.MapWidth;
            kSnapshot.m_MapHeight = kRun.m_MapHeight > 0f ? kRun.m_MapHeight : ExploreConst.MapHeight;

            string kEvacuateReason;
            kSnapshot.m_CanEvacuate = CanEvacuate(out kEvacuateReason);
            kSnapshot.m_EvacuateReason = kEvacuateReason;

            foreach (KeyValuePair<int, ExploreNode> kPair in kRun.m_Nodes)
            {
                ExploreNode kNode = kPair.Value;
                ExploreNodeTypeView kTypeView = ExploreNodeTypeRule.Get(kTables, kNode.m_TypeId);

                ExploreNodeSnapshot kNodeSnapshot = new ExploreNodeSnapshot();
                kNodeSnapshot.m_Id = kNode.m_Id;
                kNodeSnapshot.m_Layer = kNode.m_Layer;
                kNodeSnapshot.m_IndexInLayer = kNode.m_IndexInLayer;
                kNodeSnapshot.m_TypeId = kNode.m_TypeId;
                kNodeSnapshot.m_EncounterId = kNode.m_EncounterId;
                kNodeSnapshot.m_X = kNode.m_X;
                kNodeSnapshot.m_Y = kNode.m_Y;
                kNodeSnapshot.m_State = kNode.m_State;
                kNodeSnapshot.m_IsCurrent = kNode.m_Id == kRun.m_CurrentNodeId;
                kNodeSnapshot.m_IsGoal = kNode.m_Id == kRun.m_GoalNodeId;
                kNodeSnapshot.m_IsStart = kNode.m_Id == kRun.m_StartNodeId;
                kNodeSnapshot.m_Progress = kRun.m_LayerCount > 1
                    ? (kNode.m_Layer + 1) / (float)kRun.m_LayerCount
                    : 1f;

                string kReason;
                kNodeSnapshot.m_CanEnter = CanMoveTo(kNode.m_Id, out kReason);

                // 地图上直接标出"当前节点开着哪一场遭遇"，目标节点统一用 BOSS 图标
                if (kNodeSnapshot.m_IsGoal && ExploreConst.IsBattleType(kNode.m_TypeId))
                {
                    kNodeSnapshot.m_IconKey = ExploreConst.GoalNodeIcon;
                }
                else
                {
                    kNodeSnapshot.m_IconKey = kTypeView.m_Icon;
                }

                // 节点下方的文字框只有 56px 宽，名字必须短：出发 / 目标 / 两字短名
                string kShortName = kTypeView.m_Name;
                if (kNode.m_TypeId > 0
                    && kNode.m_TypeId < ExploreConst.NodeTypeShortNames.Length
                    && !string.IsNullOrEmpty(ExploreConst.NodeTypeShortNames[kNode.m_TypeId]))
                {
                    kShortName = ExploreConst.NodeTypeShortNames[kNode.m_TypeId];
                }

                kNodeSnapshot.m_TypeName = kNodeSnapshot.m_IsGoal
                    ? "目标"
                    : (kNodeSnapshot.m_IsStart ? "出发" : kShortName);
                kNodeSnapshot.m_Desc = kTypeView.m_Desc;
                kNodeSnapshot.m_Links = new List<int>(kNode.m_Links);

                kSnapshot.m_Nodes.Add(kNodeSnapshot);
            }

            kSnapshot.SortNodes();
            return kSnapshot;
        }

        #endregion

        #region 调查流程

        /// <summary>
        /// 开始一次调查。已经有一局在进行时返回 false。
        /// seed = 0 时用时间戳，方便调试时换种子。
        /// </summary>
        public bool StartRun(int _zoneId = ExploreConst.DefaultZoneId, int _seed = 0)
        {
            if (DB.HasRun())
            {
                Game.Log.LogWarning(c_LogTag + "已经有进行中的调查，先撤离或完成");
                return false;
            }

            int kSeed = _seed == 0 ? System.Environment.TickCount : _seed;
            ExploreRun kRun = ExploreMapBuilder.Build(Game.Data.GetData, _zoneId, kSeed);
            DB.SetRun(kRun);
            DB.Save();

            Game.Event.SendMessage(ExploreEvent.OnRunStarted, new ExploreRunStartedMsg(kRun));
            Game.Event.SendMessage(ExploreEvent.OnRunChanged, new ExploreRunChangedMsg(kRun));
            Game.Log.LogInfo(c_LogTag + "开始调查：" + kRun);
            return true;
        }

        /// <summary>
        /// 是否能走到某个节点（相邻 + 未锁定 + 不是当前节点）。
        /// </summary>
        public bool CanMoveTo(int _nodeId, out string _reason)
        {
            ExploreRun kRun = DB.GetRun();
            if (kRun == null)
            {
                _reason = "当前没有进行中的调查";
                return false;
            }

            ExploreNode kCurrent = kRun.GetCurrentNode();
            ExploreNode kTarget = kRun.GetNode(_nodeId);
            if (kCurrent == null || kTarget == null)
            {
                _reason = "节点不存在：" + _nodeId;
                return false;
            }
            if (kTarget.m_Id == kCurrent.m_Id)
            {
                _reason = "已经在这个节点上";
                return false;
            }
            if (!kCurrent.IsLinkedTo(kTarget.m_Id))
            {
                _reason = "只能走到相邻节点";
                return false;
            }
            if (kTarget.m_State == ExploreNodeState.Locked)
            {
                _reason = "节点不可达";
                return false;
            }

            _reason = string.Empty;
            return true;
        }

        /// <summary>
        /// 移动到相邻节点。只有首次进入才会触发节点事件（已探索节点可以回退，但不再触发）。
        /// </summary>
        public bool MoveTo(int _nodeId)
        {
            string kReason;
            if (!CanMoveTo(_nodeId, out kReason))
            {
                Game.Log.LogWarning(c_LogTag + "不能移动：" + kReason);
                return false;
            }

            ExploreRun kRun = DB.GetRun();
            ExploreNode kCurrent = kRun.GetCurrentNode();
            ExploreNode kTarget = kRun.GetNode(_nodeId);

            bool kFirstVisit = kTarget.m_VisitCount <= 0;

            if (kCurrent.m_State == ExploreNodeState.Current)
            {
                kCurrent.m_State = ExploreNodeState.Explored;
            }

            kRun.m_CurrentNodeId = kTarget.m_Id;
            kRun.m_Steps++;
            kTarget.m_VisitCount++;
            if (kRun.m_VisitedOrder.Count == 0 || kRun.m_VisitedOrder[kRun.m_VisitedOrder.Count - 1] != kTarget.m_Id)
            {
                kRun.m_VisitedOrder.Add(kTarget.m_Id);
            }

            kRun.RefreshAvailability();

            if (kTarget.m_Id == kRun.m_GoalNodeId)
            {
                kRun.m_GoalReached = true;
                Game.Log.LogInfo(c_LogTag + "抵达目标节点：" + kTarget.m_Id);
            }

            DB.Save();
            Game.Event.SendMessage(ExploreEvent.OnNodeEntered, new ExploreNodeEnteredMsg(kTarget, kRun.m_Steps));
            Game.Event.SendMessage(ExploreEvent.OnRunChanged, new ExploreRunChangedMsg(kRun));

            if (kFirstVisit)
            {
                HandleNode(kTarget);
            }
            else
            {
                Game.Log.LogInfo(c_LogTag + "回退到已探索节点 " + kTarget.m_Id + "，不触发事件");
            }

            return true;
        }

        /// <summary>
        /// 重新进入当前节点的事件（取消面板后，再点一次当前节点可以重新打开）。
        /// </summary>
        public bool EnterCurrentNode()
        {
            ExploreRun kRun = DB.GetRun();
            if (kRun == null)
            {
                return false;
            }

            ExploreNode kCurrent = kRun.GetCurrentNode();
            if (kCurrent == null)
            {
                return false;
            }

            if (kCurrent.m_Resolved)
            {
                Game.Log.LogInfo(c_LogTag + "当前节点已结算，不再重复触发");
                return false;
            }

            if (ExploreConst.IsBattleType(kCurrent.m_TypeId))
            {
                StartBattleNode(kCurrent);
            }
            else
            {
                Game.Event.SendMessage(ExploreEvent.OnNodeEventTriggered,
                    new ExploreNodeEventMsg(kCurrent.m_Id, kCurrent.m_TypeId));
            }

            return true;
        }

        /// <summary>
        /// 结算当前非战斗节点的事件（物资搜索等）：标记节点已结算并存档。
        /// 后续掉落/奖励/成功率结算在这里接。
        /// </summary>
        public bool ResolveCurrentNodeEvent()
        {
            ExploreRun kRun = DB.GetRun();
            if (kRun == null)
            {
                return false;
            }

            ExploreNode kCurrent = kRun.GetCurrentNode();
            if (kCurrent == null || kCurrent.m_Resolved)
            {
                return false;
            }

            kCurrent.m_Resolved = true;
            DB.Save();
            Game.Event.SendMessage(ExploreEvent.OnRunChanged, new ExploreRunChangedMsg(kRun));
            Game.Log.LogInfo(c_LogTag + "节点事件结算：" + kCurrent.m_Id + " 类型:" + kCurrent.m_TypeId);
            return true;
        }

        /// <summary>调试用：走到第一个可达节点</summary>
        public bool MoveToNextAvailable()
        {
            List<ExploreNode> kNodes = GetAvailableNodes();
            if (kNodes.Count == 0)
            {
                Game.Log.LogWarning(c_LogTag + "没有可走的节点");
                return false;
            }
            return MoveTo(kNodes[0].m_Id);
        }

        /// <summary>
        /// 当前位置能不能撤离：露营地（canEvacuate），或者目标节点（乘直升机，且目标战斗已结算）。
        /// </summary>
        public bool CanEvacuate(out string _reason)
        {
            ExploreRun kRun = DB.GetRun();
            if (kRun == null)
            {
                _reason = "当前没有进行中的调查";
                return false;
            }

            ExploreNode kCurrent = kRun.GetCurrentNode();
            if (kCurrent == null)
            {
                _reason = "当前节点不存在";
                return false;
            }

            ExploreNodeTypeView kTypeView = GetNodeTypeView(kCurrent.m_TypeId);
            if (kTypeView.m_CanEvacuate)
            {
                _reason = string.Empty;
                return true;
            }

            if (kCurrent.m_Id == kRun.m_GoalNodeId
                && (kCurrent.m_Resolved || !ExploreConst.IsBattleType(kCurrent.m_TypeId)))
            {
                _reason = string.Empty;
                return true;
            }

            _reason = "只能在中途的露营地，或目标节点战斗结束后撤离";
            return false;
        }

        /// <summary>
        /// 结束调查（撤离/完成/失败）。success 表示这次调查是否算成功撤离。
        /// </summary>
        public bool Evacuate(bool _success = true)
        {
            ExploreRun kRun = DB.GetRun();
            if (kRun == null)
            {
                return false;
            }

            bool kGoalReached = kRun.m_GoalReached;
            int kSteps = kRun.m_Steps;

            DB.ClearRun(_success && kGoalReached);
            DB.Save();

            Game.Event.SendMessage(ExploreEvent.OnRunEnded,
                new ExploreRunEndedMsg(_success, kSteps, kGoalReached));
            Game.Log.LogInfo(c_LogTag + "调查结束：success=" + _success
                + " steps=" + kSteps + " goal=" + kGoalReached);
            return true;
        }

        #endregion

        #region 节点处理

        private void HandleNode(ExploreNode _node)
        {
            Game.Log.LogInfo(c_LogTag + "进入节点 " + _node.m_Id + " 类型:" + (NodeType)_node.m_TypeId);

            if (ExploreConst.IsBattleType(_node.m_TypeId))
            {
                StartBattleNode(_node);
                return;
            }

            // 非战斗节点：事件内容后续接，先把"进入了什么节点"广播出去
            Game.Event.SendMessage(ExploreEvent.OnNodeEventTriggered,
                new ExploreNodeEventMsg(_node.m_Id, _node.m_TypeId));
        }

        /// <summary>战斗节点：广播给界面，由界面打开战前整备（不再自动开战）</summary>
        private void StartBattleNode(ExploreNode _node)
        {
            Game.Event.SendMessage(ExploreEvent.OnBattleNodeEntered,
                new ExploreBattleNodeMsg(_node.m_Id, _node.m_EncounterId));
            Game.Log.LogInfo(c_LogTag + "打开战斗节点整备：" + _node.m_Id + " 遭遇:" + _node.m_EncounterId);
        }

        private void OnBattleEnded(object _msg)
        {
            BattleEndedMsg kMsg = _msg as BattleEndedMsg;
            ExploreRun kRun = DB.GetRun();
            if (kRun == null || kMsg == null || kMsg.m_Result == null)
            {
                return;
            }

            // 只有"站在战斗节点上"的这场战斗才写回探索状态；
            // 调试台 / 战前整备直接开的战斗不应该改探索进度
            ExploreNode kCurrent = kRun.GetCurrentNode();
            if (kCurrent == null || !ExploreConst.IsBattleType(kCurrent.m_TypeId))
            {
                return;
            }

            kRun.m_LastBattleOutcome = kMsg.m_Result.m_Outcome;
            kCurrent.m_Resolved = true;

            DB.Save();
            Game.Log.LogInfo(c_LogTag + "战斗结束：" + kMsg.m_Result.m_Outcome
                + " 节点:" + kCurrent.m_Id);

            // 目标节点战斗胜利：暂时自动撤离（正式版由"继续深入 / 撤离"选择界面接管）
            if (kCurrent.m_Id == kRun.m_GoalNodeId
                && kMsg.m_Result.m_Outcome == BattleOutcome.Win)
            {
                Game.Log.LogInfo(c_LogTag + "目标节点战斗胜利，完成调查");
                Evacuate(true);
                return;
            }

            // 战败（全灭）：正式规则是任务结束；测试期保留进度，方便连续测战斗
            if (kRun.m_LastBattleOutcome == BattleOutcome.Lose)
            {
                if (ExploreConst.TestKeepRunOnDefeat)
                {
                    Game.Log.LogWarning(c_LogTag + "战斗失败（全灭），测试期保留调查进度，节点 " + kCurrent.m_Id);
                }
                else
                {
                    Evacuate(false);
                }
            }
        }

        #endregion
    }
}
