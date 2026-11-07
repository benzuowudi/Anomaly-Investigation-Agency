using cfg.GameType;
using Framework.Script.State;
using GameMain.Script.Data;
using GameMain.Script.Logic;

namespace GameMain.Script.State
{
    public class BattleFinishState : GameState
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
            SaveBattleSate();
        }

        protected internal override void Change()
        {
            base.Change();
        }

        protected internal override void Destroy()
        {
            base.Destroy();
        }

        private bool CheckFinish()
        {
            bool kIsFinish = false;
            return kIsFinish;
        }

        private void SaveBattleSate()
        {
            if (!CheckFinish())
            {
                StartOrderState();
            }
            else
            {
                Game.Scene.ChangeScene(SceneType.Menu, FinishBattle);
            }
        }

        private void StartOrderState()
        {
            Game.State.ChangeState<BattleOrderState>("BattleState", m_Logic.m_TempRoleIndex);
        }

        private void FinishBattle()
        {
            //Game.UI.Hide(UINameType.Battle);

            //Game.UI.ShowSystemUI(UINameType.BackGround, null);
            //Game.UI.ShowSystemUI(UINameType.Fight, null);

            Game.Operate.DestroyOperate();
            Game.Data.DestroyData<BattleData>();
            Game.Logic.DestroyLogic<BattleLogic>();
            Game.State.DestroyState("BattleState");
        }
    }
}