using cfg.GameType;
using Framework.Script.State;
using GameMain.Script.Logic;
using UnityEngine;

namespace GameMain.Script.State
{
    public class BattleStartState : GameState
    {
        private BattleLogic m_Logic;

        protected internal override void Init(object o)
        {
            base.Init(o);
            //Game.UI.ShowSystemUI(UINameType.Battle, null);
            //Game.UI.ShowSystemUI(UINameType.Battle_OrderRing, null);
            m_Logic = Game.Logic.GetLogic<BattleLogic>();
            SetBattleOperate();
            SetMap();
            SetRolePos();
        }

        protected internal override void Enter(object o)
        {
            base.Enter(o);
        }

        protected internal override void Change()
        {
            base.Change();
        }

        protected internal override void Destroy()
        {
            base.Destroy();
        }

        private void SetBattleOperate()
        {
            Game.Operate.LoadOperate("BattleOperate");
        }

        /// <summary>
        /// 设置场景
        /// </summary>
        private void SetMap()
        {
            //加载场景
            Debug.Log("开始加载场景");
            var kMap = Game.Data.GetData.map.DataList[0].Tiles;
            for (int i = 0; i < kMap.Count; i++)
            {
                int temp = i;
                Vector3 pos = new Vector3(kMap[temp].TilePos[0], kMap[temp].TilePos[1]);
                TileType tileType = kMap[temp].TileType;
                m_Logic.SetTileOnInit(kMap[temp].TileID, tileType, pos, kMap[temp].TileMaterial);
            }
        }

        private void SetRolePos()
        {
            Debug.Log("开始将角色放置在场景中");
            //将角色放置在场景中
            m_Logic.SetRoleOnInit(1);
            StartOrderState(1);
        }

        private void StartOrderState(int _index)
        {
            //切换到指令状态
            Game.State.ChangeState<BattleOrderState>("BattleState", _index);
        }
    }
}