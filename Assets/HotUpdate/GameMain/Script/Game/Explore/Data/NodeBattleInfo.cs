using System.Collections.Generic;
using GameMain.Script.Role;

namespace GameMain.Script.Explore
{
    /// <summary>
    /// 战斗节点布阵界面（NodeSettingView / NodeBattleCom）的一次性展示数据。
    ///
    /// 由界面从 ExploreMapSnapshot + 配置表 + 队伍快照组装，属于"只读展示数据"，
    /// 不落盘、不进 ExploreData。
    /// </summary>
    public class NodeBattleInfo
    {
        public int m_NodeId;
        public int m_EncounterId;

        /// <summary>节点推进度（0~100，显示在顶部进度条）</summary>
        public int m_ProgressPercent;

        /// <summary>大标题：战斗 / 遭遇名</summary>
        public string m_TitleCn = "战斗";
        public string m_TitleEn = "BATTLE NODE";

        /// <summary>战场名</summary>
        public string m_InfoNameCn = "战场";
        public string m_InfoNameEn = "BATTLEFIELD";

        /// <summary>战场说明 / 敌情</summary>
        public string m_Desc = string.Empty;

        /// <summary>参战角色（按队伍顺序，最多 4 人）</summary>
        public readonly List<RoleSnapshot> m_Roles = new List<RoleSnapshot>();

        /// <summary>角色归一化站位（0 = 战场最左，1 = 最右），与 m_Roles 一一对应</summary>
        public readonly List<float> m_Positions = new List<float>();
    }
}
