using Framework.Factory.UI;
using UnityEngine;

namespace GameMain.Script.UI
{
    public class RoleGrowFactory : MonoBehaviour, IUIFactory
    {
        public void AddComponent()
        {
            gameObject.AddComponent<RoleGrowUI>();
        }
    }
}