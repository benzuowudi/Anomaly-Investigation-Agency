namespace GameMain.Script.Role
{
    /// <summary>
    /// 获得新角色的事件体（OnRoleAcquired）。
    /// </summary>
    public class RoleAcquiredMsg
    {
        /// <summary>新角色（已经是权威库里的一条记录）</summary>
        public RoleRecord m_Role;
        /// <summary>获得来源</summary>
        public RoleAcquireReason m_Reason;

        public RoleAcquiredMsg(RoleRecord _role, RoleAcquireReason _reason)
        {
            m_Role = _role;
            m_Reason = _reason;
        }

        public override string ToString()
        {
            return "[获得角色] id=" + (m_Role == null ? 0 : m_Role.m_Id)
                + " name=" + (m_Role == null ? string.Empty : m_Role.m_Name)
                + " reason=" + m_Reason;
        }
    }

    /// <summary>
    /// 单个角色变化的事件体（OnRoleChanged）。
    /// </summary>
    public class RoleChangedMsg
    {
        public int m_RoleId;
        public RoleChangeOp m_Op;
        /// <summary>发生变化的字段名（属性变化时为 "attr:属性ID"）</summary>
        public string m_Field;
        public object m_OldValue;
        public object m_NewValue;
        public RoleAcquireReason m_Reason;

        public RoleChangedMsg(int _roleId, RoleChangeOp _op, RoleAcquireReason _reason = RoleAcquireReason.None)
            : this(_roleId, _op, string.Empty, null, null, _reason)
        {
        }

        public RoleChangedMsg(int _roleId, RoleChangeOp _op, string _field, object _oldValue, object _newValue, RoleAcquireReason _reason = RoleAcquireReason.None)
        {
            m_RoleId = _roleId;
            m_Op = _op;
            m_Field = _field;
            m_OldValue = _oldValue;
            m_NewValue = _newValue;
            m_Reason = _reason;
        }

        public override string ToString()
        {
            return "[角色变化] id=" + m_RoleId + " op=" + m_Op + " field=" + m_Field
                + " " + m_OldValue + " -> " + m_NewValue;
        }
    }

    /// <summary>
    /// 角色列表整体变化的事件体（OnRolesChanged）。
    /// </summary>
    public class RolesChangedMsg
    {
        /// <summary>数据版本号，每次变化 +1，UI 可据此判断是否需要重绘</summary>
        public long m_Version;
        /// <summary>当前持有的角色数量</summary>
        public int m_Count;
        public RoleAcquireReason m_Reason;

        public RolesChangedMsg(long _version, int _count, RoleAcquireReason _reason)
        {
            m_Version = _version;
            m_Count = _count;
            m_Reason = _reason;
        }

        public override string ToString()
        {
            return "[角色列表] version=" + m_Version + " count=" + m_Count + " reason=" + m_Reason;
        }
    }

    /// <summary>
    /// 出战（主）角色变化的事件体（OnMainRoleChanged）。
    /// </summary>
    public class MainRoleChangedMsg
    {
        public int m_OldRoleId;
        public int m_NewRoleId;

        public MainRoleChangedMsg(int _oldRoleId, int _newRoleId)
        {
            m_OldRoleId = _oldRoleId;
            m_NewRoleId = _newRoleId;
        }

        public override string ToString()
        {
            return "[主角色] " + m_OldRoleId + " -> " + m_NewRoleId;
        }
    }
}
