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
