using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Framework.Script.UI;
using UnityEngine.SceneManagement;
using Framework.Script.State;
using GameMain.Script.State;
using Framework.Script.Manager;
using cfg.GameType;

namespace GameMain.Script.UI
{
    public class FightUI : UIWindow
    {
        public GameObject m_Fight;
        public GameObject m_Activty;

        public Button btn_fight;
        public Button btn_activity;

        public Button btn_character;
        public Button btn_roletrial;
        public Button btn_main;
        public Button btn_pvp;
        public Button btn_custom;
        public Button btn_stronghold;
        public Button btn_boss;
        protected internal override void Init(object o)
        {
            base.Init(o);

            Game.Res.Load("mapstatus", "ui");

            btn_fight.onClick.AddListener(onClickFightBtn);
            btn_activity.onClick.AddListener(onClickActivityBtn);
            btn_character.onClick.AddListener(onClickCharacterBtn);
            btn_roletrial.onClick.AddListener(onClickRoletrialBtn);
            btn_main.onClick.AddListener(onClickMainBtn);
            btn_pvp.onClick.AddListener(onClickPvpBtn);
            btn_custom.onClick.AddListener(onClickCustomBtn);
            btn_stronghold.onClick.AddListener(onClickStronghold);
            btn_boss.onClick.AddListener(onClickBossBtn);
        }

        protected internal override void Enter(object o)
        {
            base.Enter(o);
        }

        protected internal override void Leave()
        {
            base.Leave();
        }

        protected internal override void Destroy()
        {
            base.Destroy();
        }

        public void onClickFightBtn()
        {

        }

        public void onClickActivityBtn()
        {

        }

        public void onClickCharacterBtn()
        {

        }

        public void onClickRoletrialBtn()
        {

        }

        public void onClickMainBtn()
        {
            //Game.UI.ShowSystemUI(UINameType.MainStory, null);
            //Game.Scene.ChangeScene(SceneType.Game, GameAction);
            //Game.UI.Show(UINameType.FightProcess);
        }

        private void GameAction()
        {
            //Game.UI.Hide(UINameType.Fight);
            //Game.State.StartState<BattleStartState>("BattleState");
        }

        public void onClickPvpBtn()
        {

        }

        public void onClickCustomBtn()
        {

        }

        public void onClickStronghold()
        {

        }

        public void onClickBossBtn()
        {

        }
    }
}