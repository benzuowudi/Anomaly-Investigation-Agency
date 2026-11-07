using System.Collections.Generic;
using GameMain.Script.Role;

namespace GameMain.Script.TalentAcquisition
{
    /// <summary>
    /// 招募结果。
    ///
    /// 人才吸纳：m_Candidates 是 3/30 名候选人，m_Required 是最终需要选择的 1/10 名；
    /// 定向猎头：m_IsDisplayOnly = true，m_Candidates 里是已经入职的结果角色，只展示。
    /// </summary>
    public class TalentAcquisitionResult
    {
        public bool m_IsTen;
        public bool m_IsDisplayOnly;
        public int m_Required;
        public List<RoleRecord> m_Candidates = new List<RoleRecord>();
    }
}
