using Framework.Script.Operate;
using GameMain.Script.Logic;
using UnityEngine;

namespace GameMain.Script.Operate
{
    public class BattleOperate : GameOperate
    {
        private BattleLogic m_Logic;
        protected internal override void OnEnable()
        {
            base.OnEnable();
            m_Logic = Game.Logic.GetLogic<BattleLogic>();
        }

        protected internal override void Update()
        {
            base.Update();
            if (Input.GetMouseButtonDown(0))
            {
                ClickObject();
            }
            ZoomCameraField();
        }

        protected internal override void OnDisable()
        {
            base.OnDisable();
        }

        private void ClickObject()
        {
            Ray ray = Game.Manager.m_GameCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))
            {
                Debug.Log(hit.transform.name);
                m_Logic.CheckClick(hit.transform);
            }
        }

        private void ZoomCameraField()
        {
            Game.Manager.m_GameCamera.fieldOfView -= (Input.GetAxis("Mouse ScrollWheel")) * 10;
        }
    }
}