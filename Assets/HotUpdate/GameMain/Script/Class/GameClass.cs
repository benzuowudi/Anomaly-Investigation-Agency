using cfg.GameData_Item;
using cfg.GameData_Skill;
using cfg.GameType;
using Nino.Serialization;
using System.Collections.Generic;
using UnityEngine;

//需要持久化储存的数据需要生成nino脚本
//需要生成nino脚本的类需要添加[NinoSerialize]特性对类进行序列化
//需要生成nino脚本的类需要继承GameClassBase
//需要生成nino脚本的类中不要添加带有形参的构造函数
//需要生成nino脚本的类需要DataManager类中的LoadGameData中添加自定义数据类型方法
//WrapperManifest.AddWrapper(typeof(T : GameClassBase), new T : GameClassBase.SerializationHelper());

//不需要生成nino脚本的类需要添加//[CodeGenIgnore]特性

namespace GameMain.Script.Class
{
    /// <summary>
    /// 临时数据——用于记录生成在场景中的地图瓦片的信息
    /// </summary>
    [CodeGenIgnore]
    public partial class MapTileClass
    {
        public int m_ID;
        public TileType m_Type;
        public Transform m_Transform;
    }

    /// <summary>
    /// 持久化数据——用于储存玩家已获得的同伴数量
    /// </summary>
    [CodeGenIgnore]
    public partial class LotteryData
    {
        public int m_RoleNum = 0;
        public Dictionary<int, RoleClass> m_RoleDic = new Dictionary<int, RoleClass>();
    }

    [CodeGenIgnore]
    public partial class BattleClass
    {
        public int m_BattleID = 0;
        //卷
        public int m_Group = 0;
        //章节
        public int m_Chapter = 0;
        //关
        public int m_Level = 0;
        //天气信息
        public WeatherType m_WeatherType = WeatherType.sunny;
        //地图属性信息
        //TODO
        //地图信息
        public int m_Map = 0;
        //角色信息
        public Dictionary<int, BattleRoleClass> m_BattleRoleClassDic = new Dictionary<int, BattleRoleClass>();
    }

    /// <summary>
    /// 临时数据——用于保存战斗中的单个角色数据
    /// </summary>
    [CodeGenIgnore]
    public partial class BattleRoleClass
    {
        public int m_BattleRoleID = 0;
        public int m_Camp = 0;
        public RoleClass m_RoleData;
    }

    /// <summary>
    /// 持久化数据——用于记录玩家已获得的同伴的培养数据
    /// </summary>
    [CodeGenIgnore]
    public partial class RoleClass
    {
        #region  动态数据-基础数据
        //从本地配置表读取的配置
        //角色ID
        public int m_ID = 0;
        //角色中文名
        public string m_Name_cn = "";
        //角色英文名
        public string m_Name_en = "";
        //角色头像
        public string m_Headicon = "";
        //角色全身像
        public string m_Image_full = "";
        //角色半身像
        public string m_Image_half = "";
        //角色卡片图像
        public string m_Image_card = "";
        //角色模型
        public string m_Modle = "";
        //角色碎片ID
        public int m_ChipID = 0;
        //角色职业
        public int m_Reofession = 0;
        #endregion
        #region  动态数据-基础数据
        //动态数据
        //基础数据
        //角色等级
        public int m_LV = 0;
        //角色进阶等级
        public int m_Advanced = 0;
        //角色可学习的技能
        public List<skill> m_AllSkillList = new List<skill>();
        //角色已学习的技能
        public List<SkillDataClass> m_GetSkillList = new List<SkillDataClass>();
        //角色感悟等级
        public int m_Perception = 0;
        //角色可获得天赋
        public List<talent> m_AllTalentList = new List<talent>();
        //角色已解锁天赋
        public List<TalentDataClass> m_GetTalentList = new List<TalentDataClass>();
        //角色当前定位
        public PositionType m_Position = PositionType.balanced;
        //角色定位精通
        public List<int> m_PostionMater = new List<int>();
        //角色武器精通
        public List<int> m_EquipMater = new List<int>();
        //角色道具精通
        public List<int> m_ItemMaster = new List<int>();
        //装备数据
        //角色装备——武器
        public int m_Equip_Weapon = 0;
        //角色装备——武器下挂
        public int m_Equip_weapon_hanging = 0;
        //角色装备——道具列表
        public List<item> m_ItemList = new List<item>();
        //角色装备——防具列表
        public List<equip> m_Equip = new List<equip>();
        //角色装备——勋章列表
        public List<medal> m_Medal = new List<medal>();
        #endregion
        #region  动态数据-角色属性
        //基础属性
        //角色力量值
        public int STR = 0;
        //角色耐力值
        public int END = 0;
        //角色感知值
        public int REP = 0;
        //角色识破值
        public int FORE = 0;
        //角色敏捷
        public int AGI = 0;
        //角色智力
        public int INTE = 0;
        //战斗属性
        //角色血量
        public int m_Hp = 0;
        //角色魔力
        public int m_Mp = 0;
        //角色近战物理攻击力
        public int m_ATK_cp = 0;
        //角色远程物理攻击力
        public int m_ATK_lp = 0;
        //角色法术攻击力
        public int m_ATK_m = 0;
        //角色物理防御力
        public int m_DEF_p = 0;
        //角色法术防御力
        public int m_DEF_m = 0;
        //角色暴击率
        public int CTR = 0;
        //角色暴击伤害
        public int CTD = 0;
        //角色闪避
        public int DOD = 0;
        //角色移动范围
        public int ROM = 0;
        //角色攻击速度
        public int ATKSPD = 0;
        //角色命中
        public int HITR = 0;
        //角色索敌
        public int TDE = 0;
        //角色装备重量上限
        public int EWL = 0;
        #endregion
    }

    /// <summary>
    /// 技能数据类
    /// </summary>
    [CodeGenIgnore]
    public partial class SkillDataClass
    {
        public int m_SkillLV = 0;
        public int m_GetTime = 0;
        public skill m_Skill;
    }

    /// <summary>
    /// 天赋数据类
    /// </summary>
    [CodeGenIgnore]
    public partial class TalentDataClass
    {
        public int m_TalentLV = 0;
        public int m_GetTime = 0;
        public talent m_Talent;
    }

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