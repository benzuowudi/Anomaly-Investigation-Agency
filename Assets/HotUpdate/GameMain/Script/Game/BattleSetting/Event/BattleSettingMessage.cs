namespace GameMain.Script.BattleSetting
{
    /// <summary>
    /// 战前整备发生变化的消息（只读快照，发送后不再修改）。
    /// </summary>
    public class BattleSettingChangedMsg
    {
        /// <summary>变化来源</summary>
        public BattleSettingChangeReason m_Reason;

        /// <summary>相关角色 ID（0 表示与具体角色无关）</summary>
        public int m_RoleId;

        /// <summary>相关物品 ID（0 表示与物品无关）</summary>
        public int m_ItemId;

        public BattleSettingChangedMsg(BattleSettingChangeReason _reason, int _roleId = 0, int _itemId = 0)
        {
            m_Reason = _reason;
            m_RoleId = _roleId;
            m_ItemId = _itemId;
        }

        public override string ToString()
        {
            return "{ reason:" + m_Reason + ", roleId:" + m_RoleId + ", itemId:" + m_ItemId + " }";
        }
    }

    /// <summary>
    /// 确认开战的消息（只读快照）。
    /// </summary>
    public class BattleSettingCommittedMsg
    {
        /// <summary>遭遇战 ID（0 表示调试战斗）</summary>
        public int m_EncounterId;

        /// <summary>出战角色 ID（按队伍顺序）</summary>
        public int[] m_RoleIds;

        public BattleSettingCommittedMsg(int _encounterId, int[] _roleIds)
        {
            m_EncounterId = _encounterId;
            m_RoleIds = _roleIds;
        }

        public override string ToString()
        {
            int kCount = m_RoleIds == null ? 0 : m_RoleIds.Length;
            return "{ encounter:" + m_EncounterId + ", team:" + kCount + " }";
        }
    }
}
