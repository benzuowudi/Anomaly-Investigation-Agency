using Framework.Factory.UI;
using UnityEngine;

namespace GameMain.Script.UI
{
    public class RoleDestinyFactory : MonoBehaviour, IUIFactory
    {
        public void AddComponent()
        {
            gameObject.AddComponent<RoleDestinyUI>();
        }
    }
}