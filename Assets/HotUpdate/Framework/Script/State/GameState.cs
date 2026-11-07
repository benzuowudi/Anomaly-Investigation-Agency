using UnityEngine;

namespace Framework.Script.State
{
    public class GameState
    {
        /// <summary>
        /// 进入初次进入状态执行一次
        /// </summary>
        /// <param name="o"></param>
        protected internal virtual void Init(object o)
        {
            Debug.Log("有限状态机切换，进入状态     " + this.ToString());
        }

        /// <summary>
        /// 每一次进入状态都会执行一次
        /// </summary>
        /// <param name="o"></param>
        protected internal virtual void Enter(object o)
        {

        }

        /// <summary>
        /// 切换状态时执行一次
        /// </summary>
        protected internal virtual void Change()
        {

        }

        /// <summary>
        /// 销毁状态时执行一次
        /// </summary>
        protected internal virtual void Destroy()
        {

        }
    }
}