namespace GameMain.Script.Explore
{
    /// <summary>调查开始的消息体</summary>
    public class ExploreRunStartedMsg
    {
        public ExploreRun m_Run;

        public ExploreRunStartedMsg(ExploreRun _run)
        {
            m_Run = _run;
        }

        public override string ToString()
        {
            return "[探索开始] " + m_Run;
        }
    }

    /// <summary>进入节点的消息体</summary>
    public class ExploreNodeEnteredMsg
    {
        public ExploreNode m_Node;
        public int m_Steps;

        public ExploreNodeEnteredMsg(ExploreNode _node, int _steps)
        {
            m_Node = _node;
            m_Steps = _steps;
        }

        public override string ToString()
        {
            return "[进入节点] " + m_Node + " 步数:" + m_Steps;
        }
    }

    /// <summary>进入战斗节点的消息体：由界面打开战前整备（NodeSettingView）</summary>
    public class ExploreBattleNodeMsg
    {
        public int m_NodeId;
        public int m_EncounterId;

        public ExploreBattleNodeMsg(int _nodeId, int _encounterId)
        {
            m_NodeId = _nodeId;
            m_EncounterId = _encounterId;
        }

        public override string ToString()
        {
            return "[战斗节点] node:" + m_NodeId + " encounter:" + m_EncounterId;
        }
    }

    /// <summary>进入非战斗节点的消息体（事件内容后续接）</summary>
    public class ExploreNodeEventMsg
    {
        public int m_NodeId;
        public int m_TypeId;

        public ExploreNodeEventMsg(int _nodeId, int _typeId)
        {
            m_NodeId = _nodeId;
            m_TypeId = _typeId;
        }

        public override string ToString()
        {
            return "[节点事件] node:" + m_NodeId + " type:" + m_TypeId;
        }
    }

    /// <summary>调查结束的消息体</summary>
    public class ExploreRunEndedMsg
    {
        public bool m_Success;
        public int m_Steps;
        public bool m_GoalReached;

        public ExploreRunEndedMsg(bool _success, int _steps, bool _goalReached)
        {
            m_Success = _success;
            m_Steps = _steps;
            m_GoalReached = _goalReached;
        }

        public override string ToString()
        {
            return "[探索结束] success:" + m_Success + " steps:" + m_Steps
                + " goal:" + m_GoalReached;
        }
    }

    /// <summary>探索进度变化的消息体</summary>
    public class ExploreRunChangedMsg
    {
        public ExploreRun m_Run;

        public ExploreRunChangedMsg(ExploreRun _run)
        {
            m_Run = _run;
        }

        public override string ToString()
        {
            return "[探索变化] " + m_Run;
        }
    }
}
