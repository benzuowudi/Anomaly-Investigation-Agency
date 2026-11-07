using Framework.Script.Base;
using UnityEngine;

namespace Framework.Script.Manager
{
    public class GameManager : Singleton<GameManager>
    {
        public Transform m_Dontdestroy { get; private set; }
        public Transform m_TLayer { get; private set; }
        public Transform m_MLayer { get; private set; }
        public Transform m_BLayer { get; private set; }
        public Transform m_GridUnits { get; private set; }
        public Transform m_RoleUnits { get; private set; }
        public Transform m_GameCameraTar { get; private set; }
        public Camera m_GameCamera { get; private set; }
        public Camera m_UICamera { get; private set; }
        public Transform m_Pool { get; private set; }

        public void Init(Game _init)
        {
            m_Dontdestroy = _init.transform;
            m_TLayer = _init.m_TLayer;
            m_MLayer = _init.m_MLayer;
            m_BLayer = _init.m_BLayer;
            m_GridUnits = _init.m_GridUnits;
            m_RoleUnits = _init.m_RoleUnits;
            m_GameCameraTar = _init.m_GameCameraTra;
            m_GameCamera = _init.m_GameCamera;
            m_UICamera = _init.m_UICamera;
            m_Pool= _init.m_Pool;
        }
    }
}