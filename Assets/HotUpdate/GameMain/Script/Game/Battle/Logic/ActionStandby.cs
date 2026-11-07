using System;

namespace GameMain.Script.Logic
{
    public class ActionStandby : IOrderAction
    {
        public void OrderIssue()
        {

        }

        public void OrderRun(object o, Action _callback)
        {

        }

        public bool OrderCheck()
        {
            return false;
        }
        public void OrderFinish(bool _IsSuccess)
        {

        }
    }
}