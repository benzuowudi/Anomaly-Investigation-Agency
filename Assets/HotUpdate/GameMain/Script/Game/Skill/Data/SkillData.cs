using Framework.Script.Data;
using SimpleJSON;

namespace GameMain.Script.Skill
{
    /// <summary>
    /// 技能系统的 Data（本期为空壳，见技能文档 §9.6）。
    ///
    /// 现在只存版本号，作用有三个：
    ///   1. 占住 skill.json，将来加字段不改注册、不改路径；
    ///   2. 留出读档版本号，后续字段变更时能按版本迁移；
    ///   3. 守住规范"一个系统一个 Data"的边界。
    ///
    /// 注意：技能/天赋的 **ID** 存在 RoleData 的 RoleRecord 上（m_ActiveSkillId / m_TalentId），
    /// 这里不重复存；将来技能等级、装配、解锁才写进这个 Data。
    /// </summary>
    public class SkillData : SystemDataBase
    {
        public const string SaveFileName = "skill.json";

        private const int c_Version = 1;

        private int m_Version = c_Version;

        protected override string GetSaveFileName()
        {
            return SaveFileName;
        }

        protected override string GetLogTag()
        {
            return "技能";
        }

        public override void Clear()
        {
            m_Version = c_Version;
        }

        public int GetVersion()
        {
            return m_Version;
        }

        protected override string ToJson()
        {
            JSONObject kRoot = new JSONObject();
            kRoot.Add("version", m_Version);
            return kRoot.ToString();
        }

        protected override bool FromJson(string _json)
        {
            JSONNode kRoot = JSON.Parse(_json);
            if (kRoot == null || !kRoot.IsObject)
            {
                return false;   // 坏档：保留现有数据（规范 §5.4）
            }

            m_Version = kRoot["version"] == null ? c_Version : kRoot["version"].AsInt;
            return true;        // 缺字段不算坏档
        }
    }
}
