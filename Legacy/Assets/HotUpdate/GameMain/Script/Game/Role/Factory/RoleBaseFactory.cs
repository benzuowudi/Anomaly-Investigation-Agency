using Framework.Factory.UI;
using Framework.Script.UI;
using GameMain.Script.UI;
using UnityEngine;

namespace GameMain.Factory.UI
{
    public class RoleBaseFactory : MonoBehaviour, IUIFactory
    {
        public void AddComponent()
        {
            gameObject.AddComponent<RoleBaseUI>();
        }
    }
}