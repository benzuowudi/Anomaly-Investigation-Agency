using Framework.Script.Base;
using UnityEngine;

namespace Framework.Script.Manager
{
    public class LogManager : Singleton<LogManager>
    {
        public void LogInfo(object _msg, bool _show = true) { if (_show) Debug.Log(_msg.ToString()); }

        public void LogWarning(object _msg, bool _show = true) { if (_show) Debug.LogWarning(_msg.ToString()); }

        public void LogError(object _msg, bool _show = true) { if (_show) Debug.LogError(_msg.ToString()); }
    }
}