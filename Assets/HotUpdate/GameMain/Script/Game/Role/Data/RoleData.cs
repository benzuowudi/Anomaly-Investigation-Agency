using System.Collections.Generic;
using Framework.Script.Data;
using SimpleJSON;

namespace GameMain.Script.Role
{
    /// <summary>
    /// 角色系统的 Data 辅助模块：本存档已获得的全部角色（权威数据库）+ 持久化。
    ///
    /// 职责边界：
    /// - 只维护内存中的权威角色库，唯一写入口是 RoleSystem（业务侧只调模型，不直接改这里）；
    /// - ToJson / FromJson 只做数据转换，文件 IO、路径、日志由 SystemDataBase 统一处理；
    /// - 版本号 m_Version 每次变化 +1，供 UI 判断是否需要重绘。
    ///
    /// 由 DataManager.LoadGameData() 注册，用 Game.Data.GetGameData&lt;RoleData&gt;() 取回；
    /// 读档由 RoleSystem.OnStart() 调用 Load() 触发。
    /// </summary>
    public class RoleData : SystemDataBase
    {
        /// <summary>存档文件名（位于 Application.persistentDataPath/GameMain/Data 下）</summary>
        public const string SaveFileName = "role.json";

        /// <summary>角色唯一ID → 角色记录</summary>
        private readonly Dictionary<int, RoleRecord> m_Roles = new Dictionary<int, RoleRecord>();
        /// <summary>下一个可用的角色唯一ID（自增，不复用已删除的ID）</summary>
        private int m_NextRoleId = 1;
        /// <summary>当前出战（主）角色ID，0 表示未指定</summary>
        private int m_MainRoleId = 0;
        /// <summary>数据版本号，每次变化 +1，供 UI 判断是否需要重绘</summary>
        private long m_Version = 0;

        #region SystemDataBase

        protected override string GetSaveFileName()
        {
            return SaveFileName;
        }

        protected override string GetLogTag()
        {
            return "角色";
        }

        public override void Clear()
        {
            m_Roles.Clear();
            m_NextRoleId = 1;
            m_MainRoleId = 0;
            m_Version = 0;
        }

        #endregion

        #region 角色库（查询）

        /// <summary>
        /// 下一个可用的角色唯一ID
        /// </summary>
        public int GetNextRoleId()
        {
            return m_NextRoleId;
        }

        public int GetRoleCount()
        {
            return m_Roles.Count;
        }

        public long GetVersion()
        {
            return m_Version;
        }

        public bool HasRole(int _roleId)
        {
            return m_Roles.ContainsKey(_roleId);
        }

        /// <summary>
        /// 取角色记录，不存在返回 null
        /// </summary>
        public RoleRecord GetRole(int _roleId)
        {
            RoleRecord kRole;
            return m_Roles.TryGetValue(_roleId, out kRole) ? kRole : null;
        }

        /// <summary>
        /// 全部角色（按角色ID升序，保证 UI 顺序稳定）
        /// </summary>
        public List<RoleRecord> GetAllRoles()
        {
            List<RoleRecord> kList = new List<RoleRecord>(m_Roles.Values);
            kList.Sort(delegate (RoleRecord _a, RoleRecord _b)
            {
                return _a.m_Id.CompareTo(_b.m_Id);
            });
            return kList;
        }

        /// <summary>
        /// 当前出战（主）角色，未指定或已被删除时返回 null
        /// </summary>
        public RoleRecord GetMainRole()
        {
            return GetRole(m_MainRoleId);
        }

        public int GetMainRoleId()
        {
            return m_MainRoleId;
        }

        /// <summary>
        /// 角色在 role.xlsx 里配置的属性条数（模板行数）
        /// </summary>
        public int GetTemplateCount()
        {
            cfg.Tables kTables = GetTables();
            return kTables == null || kTables.role == null ? 0 : kTables.role.DataList.Count;
        }

        #endregion

        #region 角色库（写入，仅供 RoleSystem 调用）

        /// <summary>
        /// 放入一条角色记录。ID 非法或已存在时返回 false。
        /// </summary>
        internal bool AddRole(RoleRecord _role)
        {
            if (_role == null || _role.m_Id <= 0)
            {
                Game.Log.LogWarning("[角色] AddRole 失败：角色记录或ID非法");
                return false;
            }
            if (m_Roles.ContainsKey(_role.m_Id))
            {
                Game.Log.LogWarning("[角色] AddRole 失败：角色ID已存在 " + _role.m_Id);
                return false;
            }

            m_Roles.Add(_role.m_Id, _role);
            if (_role.m_Id >= m_NextRoleId)
            {
                m_NextRoleId = _role.m_Id + 1;
            }
            if (m_MainRoleId <= 0)
            {
                m_MainRoleId = _role.m_Id;
            }
            m_Version++;
            return true;
        }

        /// <summary>
        /// 移除一条角色记录，主角色被移除时自动顺延到剩下的第一个
        /// </summary>
        internal bool RemoveRole(int _roleId)
        {
            if (!m_Roles.Remove(_roleId))
            {
                return false;
            }

            m_Version++;
            if (m_MainRoleId == _roleId)
            {
                m_MainRoleId = 0;
                List<RoleRecord> kRest = GetAllRoles();
                if (kRest.Count > 0)
                {
                    m_MainRoleId = kRest[0].m_Id;
                }
            }
            return true;
        }

        /// <summary>
        /// 设置出战（主）角色，角色不存在时返回 false
        /// </summary>
        internal bool SetMainRole(int _roleId)
        {
            if (!m_Roles.ContainsKey(_roleId))
            {
                return false;
            }
            if (m_MainRoleId == _roleId)
            {
                return true;
            }
            m_MainRoleId = _roleId;
            m_Version++;
            return true;
        }

        /// <summary>
        /// 清空全部角色（调试/新档用）
        /// </summary>
        internal void ClearAll()
        {
            m_Roles.Clear();
            m_NextRoleId = 1;
            m_MainRoleId = 0;
            m_Version++;
        }

        /// <summary>
        /// 数据变化后由 RoleSystem 调用，保证版本号只在一个地方递增
        /// </summary>
        internal void MarkDirty()
        {
            m_Version++;
        }

        #endregion

        #region 序列化（文件 IO 在 SystemDataBase）

        /// <summary>
        /// 序列化为 JSON 字符串
        /// </summary>
        protected override string ToJson()
        {
            JSONObject kRoot = new JSONObject();
            kRoot.Add("version", m_Version);
            kRoot.Add("nextRoleId", m_NextRoleId);
            kRoot.Add("mainRoleId", m_MainRoleId);

            JSONArray kRoles = new JSONArray();
            List<RoleRecord> kList = GetAllRoles();
            for (int i = 0; i < kList.Count; i++)
            {
                kRoles.Add(kList[i].ToJson());
            }
            kRoot.Add("roles", kRoles);
            return kRoot.ToString(2);
        }

        /// <summary>
        /// 从 JSON 字符串还原。解析失败时返回 false 且不改动现有数据。
        /// </summary>
        protected override bool FromJson(string _json)
        {
            if (string.IsNullOrEmpty(_json))
            {
                return false;
            }

            JSONNode kRoot;
            try
            {
                kRoot = JSON.Parse(_json);
            }
            catch (System.Exception _e)
            {
                Game.Log.LogError("[角色] 存档解析失败：" + _e.Message);
                return false;
            }

            if (kRoot == null)
            {
                return false;
            }

            Dictionary<int, RoleRecord> kNewRoles = new Dictionary<int, RoleRecord>();
            int kMaxId = 0;
            if (kRoot["roles"] != null && kRoot["roles"].IsArray)
            {
                JSONArray kRoles = kRoot["roles"].AsArray;
                for (int i = 0; i < kRoles.Count; i++)
                {
                    RoleRecord kRole = RoleRecord.FromJson(kRoles[i]);
                    if (kRole == null || kRole.m_Id <= 0)
                    {
                        Game.Log.LogWarning("[角色] 读档跳过一条非法角色数据，index=" + i);
                        continue;
                    }
                    if (kNewRoles.ContainsKey(kRole.m_Id))
                    {
                        Game.Log.LogWarning("[角色] 读档跳过重复角色ID：" + kRole.m_Id);
                        continue;
                    }
                    kNewRoles.Add(kRole.m_Id, kRole);
                    if (kRole.m_Id > kMaxId)
                    {
                        kMaxId = kRole.m_Id;
                    }
                }
            }

            m_Roles.Clear();
            Dictionary<int, RoleRecord>.Enumerator kE = kNewRoles.GetEnumerator();
            while (kE.MoveNext())
            {
                m_Roles.Add(kE.Current.Key, kE.Current.Value);
            }

            m_NextRoleId = kRoot["nextRoleId"] == null ? 0 : kRoot["nextRoleId"].AsInt;
            if (m_NextRoleId <= kMaxId)
            {
                m_NextRoleId = kMaxId + 1;
            }

            m_MainRoleId = kRoot["mainRoleId"] == null ? 0 : kRoot["mainRoleId"].AsInt;
            if (m_MainRoleId > 0 && !m_Roles.ContainsKey(m_MainRoleId))
            {
                Game.Log.LogWarning("[角色] 读档发现主角色不存在，已重置：" + m_MainRoleId);
                m_MainRoleId = 0;
            }
            if (m_MainRoleId <= 0 && m_Roles.Count > 0)
            {
                List<RoleRecord> kAll = GetAllRoles();
                m_MainRoleId = kAll[0].m_Id;
            }

            m_Version = kRoot["version"] == null ? 0 : kRoot["version"].AsLong;
            m_Version++;
            return true;
        }

        #endregion

        private static cfg.Tables GetTables()
        {
            if (!Framework.Script.Constant.GameConst.m_GameStart)
            {
                return null;
            }
            return Game.Data.GetData;
        }
    }
}
