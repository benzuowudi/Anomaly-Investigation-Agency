using System.Collections.Generic;

namespace GameMain.Script.Explore
{
    /// <summary>
    /// 探索地图上的一个节点（运行时值对象）。
    ///
    /// 节点是每局程序生成的：表里配的是"生成规则 + 类型权重"，不是固定的节点列表。
    /// m_X / m_Y 是节点**中心点**在地图画布里的坐标（画布尺寸见 ExploreConst.MapWidth/MapHeight，
    /// 与 FairyGUI 的 World/NodeView 一一对应），随存档持久化，界面只做"坐标 → 摆放"。
    /// </summary>
    public class ExploreNode
    {
        /// <summary>节点ID（一次探索内唯一）</summary>
        public int m_Id;
        /// <summary>所在层（0 = 出发阵地）</summary>
        public int m_Layer;
        /// <summary>层内序号（0 开始）</summary>
        public int m_IndexInLayer;
        /// <summary>节点类型（对应 cfg.GameType.NodeType：1 战斗 … 7 露营地）</summary>
        public int m_TypeId;
        /// <summary>战斗节点的遭遇ID（0 = 无配置，走调试编成）</summary>
        public int m_EncounterId;
        /// <summary>节点状态</summary>
        public ExploreNodeState m_State = ExploreNodeState.Locked;
        /// <summary>相邻节点ID</summary>
        public readonly List<int> m_Links = new List<int>();

        /// <summary>中心点 X（地图画布坐标）</summary>
        public float m_X;
        /// <summary>中心点 Y（地图画布坐标）</summary>
        public float m_Y;
        /// <summary>进入次数（0 = 从未触发过事件）</summary>
        public int m_VisitCount;
        /// <summary>节点事件是否已结算（战斗节点打完置 true，避免回退/重进重复触发）</summary>
        public bool m_Resolved;

        public ExploreNode()
        {
        }

        public ExploreNode(int _id, int _layer, int _indexInLayer, int _typeId)
        {
            m_Id = _id;
            m_Layer = _layer;
            m_IndexInLayer = _indexInLayer;
            m_TypeId = _typeId;
        }

        public bool IsLinkedTo(int _nodeId)
        {
            return m_Links.Contains(_nodeId);
        }

        public void Link(ExploreNode _other)
        {
            if (_other == null || _other.m_Id == m_Id)
            {
                return;
            }
            if (!m_Links.Contains(_other.m_Id))
            {
                m_Links.Add(_other.m_Id);
            }
            if (!_other.m_Links.Contains(m_Id))
            {
                _other.m_Links.Add(m_Id);
            }
        }

        public override string ToString()
        {
            return "{ node:" + m_Id + " L" + m_Layer + "#" + m_IndexInLayer
                + " type:" + m_TypeId + " state:" + m_State
                + " pos:(" + m_X.ToString("F0") + "," + m_Y.ToString("F0") + ")"
                + " visit:" + m_VisitCount
                + " links:[" + string.Join(",", m_Links) + "] }";
        }
    }
}
