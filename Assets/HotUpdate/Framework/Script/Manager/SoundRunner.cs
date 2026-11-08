using System;
using UnityEngine;

namespace Framework.Script.Manager
{
    /// <summary>
    /// 声音驱动组件：SoundManager 是普通 C# 单例，没有逐帧回调，
    /// 由这个组件挂在 [SoundRoot] 上，把 Update 转给管理器推进背景音乐的淡变。
    /// 只由 SoundManager 创建，不对外使用。
    /// </summary>
    public class SoundRunner : MonoBehaviour
    {
        /// <summary>
        /// 每帧回调，参数为不受 timeScale 影响的时间增量（暂停游戏时淡变也能走完）
        /// </summary>
        public Action<float> OnUpdate { get; set; }

        private void Update()
        {
            if (OnUpdate != null)
            {
                OnUpdate(Time.unscaledDeltaTime);
            }
        }
    }
}
