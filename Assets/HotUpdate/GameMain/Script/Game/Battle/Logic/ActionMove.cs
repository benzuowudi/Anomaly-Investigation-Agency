using System;

namespace GameMain.Script.Logic
{
    public class ActionMove : IOrderAction
    {
        private BattleLogic m_Logic;
        public void OrderIssue()
        {
            m_Logic = Game.Logic.GetLogic<BattleLogic>();
        }

        public void OrderRun(object o, Action _callback)
        {
            m_Logic.RunAction();
            m_Logic.MoveRole(m_Logic.GetCurrenTile().m_ID, _callback);
            OrderCheck();
        }

        public bool OrderCheck()
        {
            OrderFinish(true);
            return true;
        }
        public void OrderFinish(bool _IsSuccess)
        {
            if (_IsSuccess)
            {
            }
        }
    }
}