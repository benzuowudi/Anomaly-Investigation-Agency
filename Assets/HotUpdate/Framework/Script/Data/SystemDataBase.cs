using System.IO;
using System.Text;
using UnityEngine;

namespace Framework.Script.Data
{
    /// <summary>
    /// 业务系统数据基类（一个系统的 Data 辅助模块）。
    ///
    /// 分工：
    /// - 子类只维护内存中的权威数据，以及 ToJson / FromJson 两个纯数据转换；
    /// - 存档路径、文件读写、日志、异常处理统一由基类完成，子类不要再出现 File.*；
    /// - 存档位置固定为 Application.persistentDataPath/GameMain/Data/&lt;文件名&gt;，UTF-8 JSON。
    ///
    /// 使用约定：
    /// - 由 DataManager.LoadGameData() 注册；
    /// - 读档由本系统 Model 的 OnStart 调用 Load() 触发；
    /// - 写库只能由本系统 Model 调用（Data 层不直接接收 View 的调用）。
    /// </summary>
    public abstract class SystemDataBase : GameData
    {
        /// <summary>
        /// 注册时清空一次内存数据，保证每次启动都是干净状态（读档由 Model 的 OnStart 负责）
        /// </summary>
        protected internal override void Init()
        {
            Clear();
            Game.Log.LogInfo("[" + GetLogTag() + "] 数据初始化完成，存档文件：" + GetSaveFileName());
        }

        protected internal override void Destroy()
        {
            Clear();
        }

        #region 子类必须实现

        /// <summary>
        /// 存档文件名（不含路径），例如 "role.json"
        /// </summary>
        protected abstract string GetSaveFileName();

        /// <summary>
        /// 日志前缀，默认用类名；建议返回系统中名，例如 "角色"
        /// </summary>
        protected virtual string GetLogTag()
        {
            return GetType().Name;
        }

        /// <summary>
        /// 内存数据 → JSON 字符串
        /// </summary>
        protected abstract string ToJson();

        /// <summary>
        /// JSON 字符串 → 内存数据。解析失败返回 false，且不得破坏现有数据。
        /// </summary>
        protected abstract bool FromJson(string _json);

        /// <summary>
        /// 清空内存数据（新档 / Destroy 时调用）
        /// </summary>
        public virtual void Clear()
        {
        }

        #endregion

        #region 持久化（统一入口）

        /// <summary>
        /// 存档路径：Application.persistentDataPath/GameMain/Data/&lt;文件名&gt;
        /// </summary>
        public string GetSavePath()
        {
            return Application.persistentDataPath + "/GameMain/Data/" + GetSaveFileName();
        }

        /// <summary>
        /// 保存到本地。失败返回 false 并打日志，不抛异常。
        /// </summary>
        public virtual bool Save()
        {
            try
            {
                Game.Data.CreatPersistencePath();
                File.WriteAllText(GetSavePath(), ToJson(), Encoding.UTF8);
                Game.Log.LogInfo("[" + GetLogTag() + "] 存档完成：" + GetSavePath());
                return true;
            }
            catch (System.Exception _e)
            {
                Game.Log.LogError("[" + GetLogTag() + "] 存档失败：" + _e.Message);
                return false;
            }
        }

        /// <summary>
        /// 从本地读档。文件不存在返回 false（新档，不算错误）。
        /// </summary>
        public virtual bool Load()
        {
            string kPath = GetSavePath();
            if (!File.Exists(kPath))
            {
                Game.Log.LogInfo("[" + GetLogTag() + "] 没有找到存档（新档）：" + kPath);
                return false;
            }

            try
            {
                string kJson = File.ReadAllText(kPath, Encoding.UTF8);
                bool kOk = FromJson(kJson);
                if (kOk)
                {
                    Game.Log.LogInfo("[" + GetLogTag() + "] 读档完成：" + kPath);
                }
                return kOk;
            }
            catch (System.Exception _e)
            {
                Game.Log.LogError("[" + GetLogTag() + "] 读档失败：" + _e.Message);
                return false;
            }
        }

        /// <summary>
        /// 删除本地存档。文件不存在返回 false。
        /// </summary>
        public virtual bool DeleteSave()
        {
            string kPath = GetSavePath();
            if (!File.Exists(kPath))
            {
                return false;
            }

            try
            {
                File.Delete(kPath);
                Game.Log.LogInfo("[" + GetLogTag() + "] 已删除存档：" + kPath);
                return true;
            }
            catch (System.Exception _e)
            {
                Game.Log.LogError("[" + GetLogTag() + "] 删除存档失败：" + _e.Message);
                return false;
            }
        }

        #endregion
    }
}
