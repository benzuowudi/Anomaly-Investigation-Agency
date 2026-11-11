using System.Collections.Generic;
using GameMain.Script.Battle;

namespace GameMain.Script.Explore
{
    /// <summary>
    /// 一次调查（run）的运行时状态：节点网 + 当前位置 + 进度。
    ///
    /// 只描述"这一局"，不含任何 UI；UI 读它来画地图。
    /// 节点坐标（ExploreNode.m_X/m_Y）是权威数据的一部分：同 seed 可复现，
    /// 但读档时以存档里的坐标为准（布局参数改了也不挪动已有进度）。
    /// </summary>
    public class ExploreRun
    {
        /// <summary>区域ID（exploreZone.ID）</summary>
        public int m_ZoneId;
        /// <summary>随机种子（同种子可复现同一张网）</summary>
        public int m_Seed;
        /// <summary>层数</summary>
        public int m_LayerCount;
        /// <summary>深入次数（0 = 首次进入）</summary>
        public int m_Depth;
        /// <summary>起点节点ID</summary>
        public int m_StartNodeId;
        /// <summary>终点节点ID（目标节点）</summary>
        public int m_GoalNodeId;
        /// <summary>当前节点ID</summary>
        public int m_CurrentNodeId;
        /// <summary>已移动步数</summary>
        public int m_Steps;
        /// <summary>目标节点是否已完成</summary>
        public bool m_GoalReached;
        /// <summary>最近一场战斗的结果</summary>
        public BattleOutcome m_LastBattleOutcome = BattleOutcome.None;
        /// <summary>地图画布宽（虚拟像素）</summary>
        public float m_MapWidth = ExploreConst.MapWidth;
        /// <summary>地图画布高（虚拟像素）</summary>
        public float m_MapHeight = ExploreConst.MapHeight;
        /// <summary>行走路径（按进入顺序，画轨迹 / 调试用）</summary>
        public readonly List<int> m_VisitedOrder = new List<int>();
        /// <summary>节点表：id → 节点</summary>
        public readonly Dictionary<int, ExploreNode> m_Nodes = new Dictionary<int, ExploreNode>();

        public ExploreNode AddNode(int _id, int _layer, int _indexInLayer, int _typeId)
        {
            ExploreNode kNode = new ExploreNode(_id, _layer, _indexInLayer, _typeId);
            m_Nodes[_id] = kNode;
            return kNode;
        }

        public ExploreNode GetNode(int _nodeId)
        {
            ExploreNode kNode;
            return m_Nodes.TryGetValue(_nodeId, out kNode) ? kNode : null;
        }

        public ExploreNode GetCurrentNode()
        {
            return GetNode(m_CurrentNodeId);
        }

        public List<ExploreNode> GetNodesInLayer(int _layer)
        {
            List<ExploreNode> kResult = new List<ExploreNode>();
            foreach (KeyValuePair<int, ExploreNode> kPair in m_Nodes)
            {
                if (kPair.Value.m_Layer == _layer)
                {
                    kResult.Add(kPair.Value);
                }
            }
            kResult.Sort((a, b) => a.m_IndexInLayer.CompareTo(b.m_IndexInLayer));
            return kResult;
        }

        /// <summary>重算可达状态：已探索保持 Explored，其它节点按相邻关系置 Current / Available / Locked</summary>
        public void RefreshAvailability()
        {
            ExploreNode kCurrent = GetCurrentNode();
            if (kCurrent == null)
            {
                return;
            }

            foreach (KeyValuePair<int, ExploreNode> kPair in m_Nodes)
            {
                ExploreNode kNode = kPair.Value;
                if (kNode.m_State != ExploreNodeState.Explored)
                {
                    kNode.m_State = ExploreNodeState.Locked;
                }
            }

            kCurrent.m_State = ExploreNodeState.Current;

            for (int i = 0; i < kCurrent.m_Links.Count; i++)
            {
                ExploreNode kLink = GetNode(kCurrent.m_Links[i]);
                if (kLink != null && kLink.m_State != ExploreNodeState.Explored)
                {
                    kLink.m_State = ExploreNodeState.Available;
                }
            }
        }

        public override string ToString()
        {
            return "{ run zone:" + m_ZoneId + " seed:" + m_Seed
                + " nodes:" + m_Nodes.Count + " layers:" + m_LayerCount
                + " depth:" + m_Depth
                + " current:" + m_CurrentNodeId + " steps:" + m_Steps
                + " goal:" + (m_GoalReached ? "done" : "todo") + " }";
        }
    }
}
