using Framework.Script.UI;
using UnityEngine;

namespace Framework.Factory.UI
{
    public interface IUIFactory
    {
        /// <summary>
        /// Add Components to GameObject and return a Component array
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public void AddComponent();
    }
}