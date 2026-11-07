using cfg.GameType;
using Framework.Script.State;
using GameMain.Script.Logic;

namespace GameMain.Script.State
{
    public class BattleActionState : GameState
    {
        private BattleLogic m_Logic;
        protected internal override void Init(object o)
        {
            base.Init(o);
            m_Logic = Game.Logic.GetLogic<BattleLogic>();
        }

        protected internal override void Enter(object o)
        {
            base.Enter(o);
            BattleOrderType ks = (BattleOrderType)o;
            RoleBeginAction(ks);
        }

        protected internal override void Change()
        {
            base.Change();
        }

        protected internal override void Destroy()
        {
            base.Destroy();
        }

        /// <summary>
        /// 角色开始执行指令
        /// </summary>
        /// <param name="_s">执行的指令的类别</param>
        private void RoleBeginAction(BattleOrderType _s)
        {
            //角色开始执行指令
            m_Logic.ChackAction(_s, ActionCallBack);
        }

        /// <summary>
        /// 指令结束回调
        /// </summary>
        private void ActionCallBack()
        {
            //指令结束回调
            UnityEngine.Debug.Log("指令执行结束");
            m_Logic.OrderFinish();
            StartFinishState();
        }

        /// <summary>
        /// 跳过指令执行动画
        /// </summary>
        private void OverAction()
        {
            //跳过指令执行动画
        }

        /// <summary>
        /// 跳转到行动结束状态
        /// </summary>
        private void StartFinishState()
        {
            //跳转到行动结束状态
            Game.State.ChangeState<BattleFinishState>("BattleState");
        }
    }
}