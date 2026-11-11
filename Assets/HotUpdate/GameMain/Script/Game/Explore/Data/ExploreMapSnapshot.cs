using System.Collections.Generic;

namespace GameMain.Script.Explore
{
    /// <summary>
    /// 探索地图上单个节点的只读投影（给 View 用）。
    ///
    /// 坐标是节点**中心点**；图标已经是 World 包内的资源名（不含 ui:// 前缀），
    /// 类型名/说明已经从 nodeType 表解析好，View 不需要再碰 cfg.Tables。
    /// </summary>
    public class ExploreNodeSnapshot
    {
        public int m_Id;
        public int m_Layer;
        public int m_IndexInLayer;
        public int m_TypeId;
        public int m_EncounterId;

        /// <summary>中心点 X（地图画布坐标）</summary>
        public float m_X;
        /// <summary>中心点 Y（地图画布坐标）</summary>
        public float m_Y;

        public ExploreNodeState m_State;
        public bool m_IsCurrent;
        public bool m_IsGoal;
        public bool m_IsStart;
        /// <summary>是否能从当前节点走过去（View 置灰/点击判定用）</summary>
        public bool m_CanEnter;

        /// <summary>节点类型名（nodeType.name，缺表时用 ExploreConst 兜底）</summary>
        public string m_TypeName = string.Empty;
        /// <summary>节点图标资源名（World 包内）</summary>
        public string m_IconKey = string.Empty;
        /// <summary>节点说明</summary>
        public string m_Desc = string.Empty;

        /// <summary>层进度 0~1（(layer+1)/layerCount，节点上的百分比文本用）</summary>
        public float m_Progress;

        /// <summary>相邻节点ID</summary>
        public List<int> m_Links = new List<int>();

        public override string ToString()
        {
            return "{ node:" + m_Id + " L" + m_Layer + " type:" + m_TypeId
                + " state:" + m_State + " pos:(" + m_X.ToString("F0") + "," + m_Y.ToString("F0") + ")"
                + " enter:" + m_CanEnter + " }";
        }
    }

    /// <summary>
    /// 一次调查的只读投影：地图窗口一次取全，之后只做重绘。
    /// 由 <see cref="ExploreSystem.BuildMapSnapshot"/> 组装，View 不允许直接摸 ExploreRun。
    /// </summary>
    public class ExploreMapSnapshot
    {
        public int m_ZoneId;
        public string m_ZoneName = string.Empty;
        public string m_ZoneDesc = string.Empty;
        public int m_LayerCount;
        public int m_Depth;
        public int m_Steps;
        public bool m_GoalReached;
        /// <summary>当前节点是否可撤离（露营地 / 目标节点）</summary>
        public bool m_CanEvacuate;
        /// <summary>撤离不可用时的原因</summary>
        public string m_EvacuateReason = string.Empty;

        public int m_StartNodeId;
        public int m_GoalNodeId;
        public int m_CurrentNodeId;

        public float m_MapWidth = ExploreConst.MapWidth;
        public float m_MapHeight = ExploreConst.MapHeight;

        public readonly List<ExploreNodeSnapshot> m_Nodes = new List<ExploreNodeSnapshot>();

        public ExploreNodeSnapshot GetNode(int _nodeId)
        {
            for (int i = 0; i < m_Nodes.Count; i++)
            {
                if (m_Nodes[i].m_Id == _nodeId)
                {
                    return m_Nodes[i];
                }
            }
            return null;
        }

        public ExploreNodeSnapshot GetCurrentNode()
        {
            return GetNode(m_CurrentNodeId);
        }

        /// <summary>按层再按层内序号排好序（UI 遍历顺序稳定）</summary>
        public void SortNodes()
        {
            m_Nodes.Sort((a, b) =>
            {
                if (a.m_Layer != b.m_Layer)
                {
                    return a.m_Layer.CompareTo(b.m_Layer);
                }
                return a.m_IndexInLayer.CompareTo(b.m_IndexInLayer);
            });
        }
    }
}
