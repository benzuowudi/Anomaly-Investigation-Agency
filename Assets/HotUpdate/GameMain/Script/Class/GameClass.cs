using System.Collections.Generic;
using Nino.Serialization;

// 说明：本文件只保留还没迁移到新 MVC 结构的遗留数据类。
// 旧的 MapTileClass / BattleClass / BattleRoleClass / RoleClass / SkillDataClass / TalentDataClass
// 已在 2026-11-09 的战斗重构中删除（瓦片地图 + 手动指令体系作废，角色数据统一走 RoleRecord）。

namespace GameMain.Script.Class
{
    [CodeGenIgnore]
    public partial class MainStoryData
    {
        public int m_CurrentGroupID = 0;
        public int m_CurrenChapterID = 0;
        public int m_CurrenLevelID = 0;
    }

    [CodeGenIgnore]
    public partial class MainStoryData_Group
    {
        public int _id = 0;
        public bool m_IsFinish = false;
        public Dictionary<int, bool> m_ChapterFinishList = new Dictionary<int, bool>();
    }

    [CodeGenIgnore]
    public partial class MainStoryData_Chapter
    {
        public int _id = 0;
        public bool m_IsFinish = false;
        public Dictionary<int, bool> m_LevelFinishList = new Dictionary<int, bool>();
    }

    [CodeGenIgnore]
    public partial class MainStoryData_Level
    {
        public int _id = 0;
        public bool m_IsFinish = false;
        public int m_StarNum = 0;
    }
}
