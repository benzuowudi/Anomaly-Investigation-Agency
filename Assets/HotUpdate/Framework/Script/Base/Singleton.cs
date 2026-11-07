using UnityEngine;

namespace Framework.Script.Base
{
    /// <summary>
    /// 懒加载单例基类。
    ///
    /// 用法：
    ///   public class XxxManager : Singleton&lt;XxxManager&gt;
    ///   {
    ///       protected override void OnInit() { ... }   // 首次访问时执行一次
    ///   }
    ///
    /// 约定：
    /// - 管理器内部不再持有自己的静态实例字段，实例统一由基类保存；
    /// - 第一次访问 Instance 时创建对象并执行 OnInit，之后返回同一个实例；
    /// - 需要在游戏启动时就绪的单例，在 Game.StartGame 中按依赖顺序访问一次即可完成"预热"，
    ///   其余单例在第一次被访问时自动创建。
    /// </summary>
    public abstract class Singleton<T> where T : Singleton<T>, new()
    {
        private static T s_Instance;

        /// <summary>
        /// 全局唯一实例，首次访问时懒加载
        /// </summary>
        public static T Instance
        {
            get
            {
                if (s_Instance == null)
                {
                    T kInstance = new T();
                    // 先赋值再初始化：防止 OnInit 内部再次访问 Instance 导致递归创建
                    s_Instance = kInstance;
                    try
                    {
                        kInstance.OnInit();
                    }
                    catch (System.Exception _e)
                    {
                        s_Instance = null;
                        Debug.LogError("[Singleton] " + typeof(T).Name + " 初始化失败：" + _e);
                        throw;
                    }
                }
                return s_Instance;
            }
        }

        /// <summary>
        /// 实例是否已经创建（不影响懒加载，仅用于调试与测试）
        /// </summary>
        public static bool IsCreated
        {
            get { return s_Instance != null; }
        }

        protected Singleton()
        {
        }

        /// <summary>
        /// 首次创建实例时执行一次
        /// </summary>
        protected virtual void OnInit()
        {
        }

        /// <summary>
        /// 清空当前实例，仅用于测试或需要重新加载的场景，正常游戏流程不要调用
        /// </summary>
        public static void ResetInstance()
        {
            s_Instance = null;
        }
    }
}
