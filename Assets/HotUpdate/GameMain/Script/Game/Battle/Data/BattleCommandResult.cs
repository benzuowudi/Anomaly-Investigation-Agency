using System.Collections.Generic;

namespace GameMain.Script.Battle
{
    /// <summary>单个接收者的响应结果</summary>
    public struct CommandReceiverResult
    {
        public int m_UnitId;
        public CommandResponse m_Response;

        public CommandReceiverResult(int _unitId, CommandResponse _response)
        {
            m_UnitId = _unitId;
            m_Response = _response;
        }

        public override string ToString()
        {
            return "unit:" + m_UnitId + "=" + m_Response;
        }
    }

    /// <summary>
    /// 一次指令下达的结果：谁响应了、谁没响应、为什么。
    /// 只做数据汇总，UI/日志直接读它。
    /// </summary>
    public class BattleCommandResult
    {
        public int m_CommandId;
        public int m_ParamUnitId;
        public readonly List<CommandReceiverResult> m_Receivers = new List<CommandReceiverResult>();

        public BattleCommandResult(int _commandId, int _paramUnitId)
        {
            m_CommandId = _commandId;
            m_ParamUnitId = _paramUnitId;
        }

        public void Add(int _unitId, CommandResponse _response)
        {
            m_Receivers.Add(new CommandReceiverResult(_unitId, _response));
        }

        public int GetAcceptedCount()
        {
            int kCount = 0;
            for (int i = 0; i < m_Receivers.Count; i++)
            {
                if (m_Receivers[i].m_Response == CommandResponse.Accepted)
                {
                    kCount++;
                }
            }
            return kCount;
        }

        public override string ToString()
        {
            return "{ cmd:" + m_CommandId + " param:" + m_ParamUnitId
                + " accepted:" + GetAcceptedCount() + "/" + m_Receivers.Count + " }";
        }
    }
}
