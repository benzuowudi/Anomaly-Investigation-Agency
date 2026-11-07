using Framework.Factory.UI;
using Framework.Script.UI;
using GameMain.Script.UI;
using UnityEngine;

namespace GameMain.Factory.UI
{
    public class RoleInfoFactory : MonoBehaviour, IUIFactory
    {
        public void AddComponent()
        {
            gameObject.AddComponent<RoleInfoUI>();
        }
    }
}