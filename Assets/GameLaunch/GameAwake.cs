using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace GameAwake.Script.GameLaunch
{
    public class GameAwake : MonoBehaviour
    {
        public Assembly hotUpdateAss;
        private void Start()
        {
            // Editor环境下，HotUpdate.dll.bytes已经被自动加载，不需要加载，重复加载反而会出问题。
#if !UNITY_EDITOR
            hotUpdateAss = Assembly.Load(File.ReadAllBytes($"{Application.streamingAssetsPath}/HotUpdate.dll.bytes"));
#else
            // Editor下无需加载，直接查找获得HotUpdate程序集
            hotUpdateAss = AppDomain.CurrentDomain.GetAssemblies().First(a => a.GetName().Name == "HotUpdate");
#endif
            try
            {
                Type type = hotUpdateAss.GetType("GameLaunch", true, false);
                type.GetMethod("Run")?.Invoke(null, null);
            }
            catch (Exception ex) { Debug.LogError(ex); }

            DontDestroyOnLoad(gameObject);
        }
    }
}