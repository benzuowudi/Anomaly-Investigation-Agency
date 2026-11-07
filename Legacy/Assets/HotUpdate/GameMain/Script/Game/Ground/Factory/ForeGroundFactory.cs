using Framework.Factory.UI;
using Framework.Script.UI;
using GameMain.Script.UI;
using UnityEngine;

namespace GameMain.Factory.UI
{
    public class ForeGroundFactory : MonoBehaviour, IUIFactory
    {
        public void AddComponent()
        {
            gameObject.AddComponent<ForeGroundUI>();
        }
    }
}