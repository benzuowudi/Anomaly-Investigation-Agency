using System.Collections.Generic;
using GameMain.Script.Data;
using GameMain.Script.Role;

namespace GameMain.Script.TalentAcquisition
{
    /// <summary>
    /// 人才招募系统逻辑。
    ///
    /// 规则来自《3异常调查事务所-角色招募系统》：
    ///   · 人才吸纳：200 欧元/张合同，1 张合同展示 3 名候选人，最多同时用 10 张（30 名候选人）；
    ///   · 本实现没有候选人选择界面，按属性总分自动从候选人中录取 N 名（N = 合同数）；
    ///   · 定向猎头：10 黄金/张委托，开始后按配置时间倒计时，时间到后带回 1 名角色。
    /// </summary>
    public static class TalentAcquisitionSystem
    {
        /// <summary>每张人才吸纳合同消耗欧元</summary>
        public const int EuroPerContract = 200;
        /// <summary>每张定向猎头委托消耗黄金</summary>
        public const int GoldPerHeadhunter = 10;
        /// <summary>每张合同展示的候选人数</summary>
        public const int CandidatesPerContract = 3;

        private static TalentAcquisitionData DB
        {
            get { return Game.Data.GetGameData<TalentAcquisitionData>(); }
        }

        /// <summary>
        /// 当前欧元
        /// </summary>
        public static int GetEuro()
        {
            return DB.GetEuro();
        }

        /// <summary>
        /// 当前黄金
        /// </summary>
        public static int GetGold()
        {
            return DB.GetGold();
        }

        /// <summary>
        /// 使用 _contracts 张合同进行人才吸纳，返回最终入职的角色。
        /// </summary>
        public static TalentAcquisitionResult Recruit(int _contracts)
        {
            if (_contracts <= 0)
            {
                return null;
            }

            cfg.Tables kTables = Game.Data.GetData;
            if (kTables == null || kTables.role == null)
            {
                Game.Log.LogError("[人才招募] 招募失败：role 表未加载");
                return null;
            }

            TalentAcquisitionData kDB = DB;
            int kCost = EuroPerContract * _contracts;
            if (!kDB.SpendEuro(kCost))
            {
                Game.Log.LogWarning("[人才招募] 欧元不足，需要 " + kCost + "，当前 " + kDB.GetEuro());
                return null;
            }

            List<RoleRecord> kCandidates = new List<RoleRecord>();
            int kStartId = RoleSystem.GetNextRoleId();
            int kCandidateCount = _contracts * CandidatesPerContract;
            for (int i = 0; i < kCandidateCount; i++)
            {
                RoleRecord kCandidate = RoleFactory.Roll(kTables, kStartId + i);
                if (kCandidate != null)
                {
                    kCandidates.Add(kCandidate);
                }
            }

            if (kCandidates.Count == 0)
            {
                // 生成失败时退回本次消耗
                kDB.AddEuro(kCost);
                Game.Log.LogWarning("[人才招募] 没有生成任何角色，已退回欧元");
                return null;
            }

            TalentAcquisitionResult kResult = new TalentAcquisitionResult();
            kResult.m_IsTen = _contracts > 1;
            kResult.m_Required = _contracts;
            kResult.m_Candidates = kCandidates;

            Game.Log.LogInfo("[人才招募] 合同 " + _contracts + " 张，候选 " + kCandidates.Count
                + " 名，等待选择 " + kResult.m_Required + " 名");
            return kResult;
        }

        /// <summary>
        /// 玩家在结果界面选满候选人后，把选中的角色写入角色库。
        /// </summary>
        public static int CommitSelection(TalentAcquisitionResult _result, List<RoleRecord> _selected)
        {
            if (_result == null || _selected == null || _selected.Count == 0)
            {
                return 0;
            }

            int kCount = 0;
            for (int i = 0; i < _selected.Count; i++)
            {
                if (RoleSystem.AddGeneratedRole(_selected[i], RoleAcquireReason.Lottery))
                {
                    kCount++;
                }
            }

            Game.Log.LogInfo("[人才招募] 确认入职 " + kCount + " 名角色");
            return kCount;
        }

        /// <summary>
        /// 开始定向猎头：扣 10 黄金并记录结束时间。
        /// </summary>
        public static bool StartHeadhunter(int _seconds, IList<string> _conditions)
        {
            if (_seconds <= 0)
            {
                return false;
            }

            TalentAcquisitionData kDB = DB;
            if (kDB.IsHeadhunterActive())
            {
                Game.Log.LogWarning("[人才招募] 已有猎头正在进行中");
                return false;
            }
            if (!kDB.SpendGold(GoldPerHeadhunter))
            {
                Game.Log.LogWarning("[人才招募] 黄金不足，需要 " + GoldPerHeadhunter + "，当前 " + kDB.GetGold());
                return false;
            }

            kDB.StartHeadhunter(_seconds, _conditions);
            return true;
        }

        /// <summary>
        /// 猎头到期结算：带回 1 名角色。
        /// </summary>
        public static RoleRecord CompleteHeadhunter()
        {
            TalentAcquisitionData kDB = DB;
            if (kDB.GetHeadhunterEndTime() <= 0)
            {
                return null;
            }

            kDB.ClearHeadhunter();
            RoleRecord kRole = RoleSystem.AcquireRole(RoleAcquireReason.Lottery);
            if (kRole != null)
            {
                Game.Log.LogInfo("[人才招募] 猎头带回角色：" + kRole.GetDisplayName());
            }
            return kRole;
        }

    }
}
