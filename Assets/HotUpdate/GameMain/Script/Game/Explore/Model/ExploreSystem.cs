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
    /// - 节点移动与可达性刷新；
    /// - 进入战斗节点时驱动 BattleSystem，战斗结束后收回结果；
    /// - 把进度落盘（ExploreData），保证退出游戏还能接着走。
    ///
    /// 不做：节点事件的玩法细节（物资/调查/事件的结算留给后续）、UI 绘制。
    /// </summary>
    public class ExploreSystem : ModelBase
    {
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

        #endregion

        #region 调查流程

        /// <summary>
        /// 开始一次调查。已经有一局在进行时返回 false。
        /// seed = 0 时用时间戳，方便调试时换种子。
        /// </summary>
        public bool StartRun(int _zoneId = 0, int _seed = 0)
        {
            if (DB.HasRun())
            {
                Game.Log.LogWarning("[探索] 已经有进行中的调查，先撤离或完成");
                return false;
            }

            int kSeed = _seed == 0 ? System.Environment.TickCount : _seed;
            ExploreRun kRun = ExploreMapBuilder.Build(Game.Data.GetData, _zoneId, kSeed);
            DB.SetRun(kRun);
            DB.Save();

            Game.Event.SendMessage(ExploreEvent.OnRunStarted, new ExploreRunStartedMsg(kRun));
            Game.Event.SendMessage(ExploreEvent.OnRunChanged, new ExploreRunChangedMsg(kRun));
            Game.Log.LogInfo("[探索] 开始调查：" + kRun);
            return true;
        }

        /// <summary>
        /// 移动到相邻节点。只能走"相邻 + 可达（未锁定）"的节点。
        /// </summary>
        public bool MoveTo(int _nodeId)
        {
            ExploreRun kRun = DB.GetRun();
            if (kRun == null)
            {
                return false;
            }

            ExploreNode kCurrent = kRun.GetCurrentNode();
            ExploreNode kTarget = kRun.GetNode(_nodeId);
            if (kCurrent == null || kTarget == null)
            {
                Game.Log.LogWarning("[探索] 节点不存在：" + _nodeId);
                return false;
            }
            if (kTarget.m_Id == kCurrent.m_Id)
            {
                return false;
            }
            if (!kCurrent.IsLinkedTo(kTarget.m_Id))
            {
                Game.Log.LogWarning("[探索] 不相邻，走不过去：" + kCurrent.m_Id + " -> " + kTarget.m_Id);
                return false;
            }
            if (kTarget.m_State == ExploreNodeState.Locked)
            {
                Game.Log.LogWarning("[探索] 节点不可达：" + kTarget.m_Id);
                return false;
            }

            if (kCurrent.m_State == ExploreNodeState.Current)
            {
                kCurrent.m_State = ExploreNodeState.Explored;
            }

            kRun.m_CurrentNodeId = kTarget.m_Id;
            kRun.m_Steps++;
            kRun.RefreshAvailability();

            if (kTarget.m_Id == kRun.m_GoalNodeId)
            {
                kRun.m_GoalReached = true;
                Game.Log.LogInfo("[探索] 抵达目标节点：" + kTarget.m_Id);
            }

            DB.Save();
            Game.Event.SendMessage(ExploreEvent.OnNodeEntered, new ExploreNodeEnteredMsg(kTarget, kRun.m_Steps));
            Game.Event.SendMessage(ExploreEvent.OnRunChanged, new ExploreRunChangedMsg(kRun));

            HandleNode(kTarget);
            return true;
        }

        /// <summary>调试用：走到第一个可达节点</summary>
        public bool MoveToNextAvailable()
        {
            List<ExploreNode> kNodes = GetAvailableNodes();
            if (kNodes.Count == 0)
            {
                Game.Log.LogWarning("[探索] 没有可走的节点");
                return false;
            }
            return MoveTo(kNodes[0].m_Id);
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
            Game.Log.LogInfo("[探索] 调查结束：success=" + _success
                + " steps=" + kSteps + " goal=" + kGoalReached);
            return true;
        }

        #endregion

        #region 节点处理

        private void HandleNode(ExploreNode _node)
        {
            Game.Log.LogInfo("[探索] 进入节点 " + _node.m_Id + " 类型:" + (NodeType)_node.m_TypeId);

            if (_node.m_TypeId == (int)NodeType.battle)
            {
                StartBattleNode(_node);
            }
        }

        /// <summary>战斗节点：优先按遭遇开局，没有配置时退回调试编成</summary>
        private void StartBattleNode(ExploreNode _node)
        {
            BattleSystem kBattle = Game.Model.Get<BattleSystem>();
            if (kBattle == null)
            {
                return;
            }

            bool kStarted = kBattle.StartBattle(_node.m_EncounterId);
            if (!kStarted)
            {
                Game.Log.LogWarning("[探索] 遭遇 " + _node.m_EncounterId + " 无可用编成，改用调试编成");
                kStarted = kBattle.StartDebugBattle(4, 4);
            }

            if (kStarted)
            {
                Game.Log.LogInfo("[探索] 进入战斗节点：" + _node.m_Id + " 遭遇:" + _node.m_EncounterId);
            }
        }

        private void OnBattleEnded(object _msg)
        {
            BattleEndedMsg kMsg = _msg as BattleEndedMsg;
            ExploreRun kRun = DB.GetRun();
            if (kRun == null || kMsg == null || kMsg.m_Result == null)
            {
                return;
            }

            kRun.m_LastBattleOutcome = kMsg.m_Result.m_Outcome;
            DB.Save();
            Game.Log.LogInfo("[探索] 战斗结束：" + kMsg.m_Result.m_Outcome);

            // 战败（全灭）直接结束本次调查
            if (kRun.m_LastBattleOutcome == BattleOutcome.Lose)
            {
                Evacuate(false);
            }
        }

        #endregion
    }
}
