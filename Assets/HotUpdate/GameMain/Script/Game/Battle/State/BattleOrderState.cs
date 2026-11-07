using Framework.Script.State;
using GameMain.Script.Logic;
using System;

namespace GameMain.Script.State
{
    public class BattleOrderState : GameState
    {
        private Action<object> m_ActionButton;
        private BattleLogic m_Logic;
        protected internal override void Init(object o)
        {
            base.Init(o);
            //m_OrderRing = (BattleOrderRing)Game.UI.GetUI(UINameType.Battle_OrderRing);
            m_Logic = Game.Logic.GetLogic<BattleLogic>();
            Game.Event.Subscribe("OrderRing", OrderFinish);
        }

        protected internal override void Enter(object o)
        {
            base.Enter(o);
            int kIndex = (int)o;
            MoveCamera(kIndex);
        }

        protected internal override void Change()
        {
            base.Change();
        }

        protected internal override void Destroy()
        {
            base.Destroy();
            Game.Event.UnSubscribe("OrderRing", OrderFinish);
        }

        private void MoveCamera(int _index)
        {
            m_Logic.MoveCamera(_index);
        }

        private void OrderFinish(object _s)
        {
            Game.State.ChangeState<BattleActionState>("BattleState", _s);
        }

        private void StartActionState()
        {

        }
    }
}