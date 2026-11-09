using System.Collections.Generic;
using Framework.Script.Data;
using GameMain.Script.Battle;
using SimpleJSON;

namespace GameMain.Script.Explore
{
    /// <summary>
    /// 探索系统的 Data（SystemDataBase）：持久化"进行中的一次调查"。
    ///
    /// 存档内容 = ExploreRun（节点网 + 位置 + 进度）。战斗过程不在这里，
    /// 现场只保留最近一场战斗的结果枚举。
    /// </summary>
    public class ExploreData : SystemDataBase
    {
        public const string SaveFileName = "explore.json";

        private const int c_Version = 1;

        private int m_Version = c_Version;
        private ExploreRun m_Run;
        private int m_CompletedRuns;

        protected override string GetSaveFileName()
        {
            return SaveFileName;
        }

        protected override string GetLogTag()
        {
            return "探索";
        }

        public override void Clear()
        {
            m_Version = c_Version;
            m_Run = null;
            m_CompletedRuns = 0;
        }

        #region 查询 / 写入

        public ExploreRun GetRun()
        {
            return m_Run;
        }

        public bool HasRun()
        {
            return m_Run != null;
        }

        public int GetCompletedRuns()
        {
            return m_CompletedRuns;
        }

        internal void SetRun(ExploreRun _run)
        {
            m_Run = _run;
            m_Version++;
        }

        internal void ClearRun(bool _completed)
        {
            m_Run = null;
            if (_completed)
            {
                m_CompletedRuns++;
            }
            m_Version++;
        }

        #endregion

        #region 序列化

        protected override string ToJson()
        {
            JSONObject kRoot = new JSONObject();
            kRoot.Add("version", m_Version);
            kRoot.Add("completedRuns", m_CompletedRuns);

            if (m_Run == null)
            {
                kRoot.Add("run", null);
                return kRoot.ToString();
            }

            JSONObject kRun = new JSONObject();
            kRun.Add("zoneId", m_Run.m_ZoneId);
            kRun.Add("seed", m_Run.m_Seed);
            kRun.Add("layerCount", m_Run.m_LayerCount);
            kRun.Add("startNodeId", m_Run.m_StartNodeId);
            kRun.Add("goalNodeId", m_Run.m_GoalNodeId);
            kRun.Add("currentNodeId", m_Run.m_CurrentNodeId);
            kRun.Add("steps", m_Run.m_Steps);
            kRun.Add("goalReached", m_Run.m_GoalReached);
            kRun.Add("lastBattleOutcome", (int)m_Run.m_LastBattleOutcome);

            JSONArray kNodes = new JSONArray();
            foreach (KeyValuePair<int, ExploreNode> kPair in m_Run.m_Nodes)
            {
                ExploreNode kNode = kPair.Value;
                JSONObject kNodeJson = new JSONObject();
                kNodeJson.Add("id", kNode.m_Id);
                kNodeJson.Add("layer", kNode.m_Layer);
                kNodeJson.Add("index", kNode.m_IndexInLayer);
                kNodeJson.Add("type", kNode.m_TypeId);
                kNodeJson.Add("encounter", kNode.m_EncounterId);
                kNodeJson.Add("state", (int)kNode.m_State);

                JSONArray kLinks = new JSONArray();
                for (int i = 0; i < kNode.m_Links.Count; i++)
                {
                    kLinks.Add(kNode.m_Links[i]);
                }
                kNodeJson.Add("links", kLinks);
                kNodes.Add(kNodeJson);
            }
            kRun.Add("nodes", kNodes);
            kRoot.Add("run", kRun);
            return kRoot.ToString();
        }

        protected override bool FromJson(string _json)
        {
            JSONNode kRoot = JSON.Parse(_json);
            if (kRoot == null || !kRoot.IsObject)
            {
                return false;
            }

            m_Version = kRoot["version"] == null ? c_Version : kRoot["version"].AsInt;
            m_CompletedRuns = kRoot["completedRuns"] == null ? 0 : kRoot["completedRuns"].AsInt;

            JSONNode kRunNode = kRoot["run"];
            if (kRunNode == null || !kRunNode.IsObject)
            {
                m_Run = null;
                return true;
            }

            ExploreRun kRun = new ExploreRun();
            kRun.m_ZoneId = kRunNode["zoneId"] == null ? 0 : kRunNode["zoneId"].AsInt;
            kRun.m_Seed = kRunNode["seed"] == null ? 0 : kRunNode["seed"].AsInt;
            kRun.m_LayerCount = kRunNode["layerCount"] == null ? 0 : kRunNode["layerCount"].AsInt;
            kRun.m_StartNodeId = kRunNode["startNodeId"] == null ? 0 : kRunNode["startNodeId"].AsInt;
            kRun.m_GoalNodeId = kRunNode["goalNodeId"] == null ? 0 : kRunNode["goalNodeId"].AsInt;
            kRun.m_CurrentNodeId = kRunNode["currentNodeId"] == null ? 0 : kRunNode["currentNodeId"].AsInt;
            kRun.m_Steps = kRunNode["steps"] == null ? 0 : kRunNode["steps"].AsInt;
            kRun.m_GoalReached = kRunNode["goalReached"] != null && kRunNode["goalReached"].AsBool;
            kRun.m_LastBattleOutcome = (BattleOutcome)(kRunNode["lastBattleOutcome"] == null
                ? 0
                : kRunNode["lastBattleOutcome"].AsInt);

            JSONNode kNodes = kRunNode["nodes"];
            if (kNodes != null && kNodes.IsArray)
            {
                for (int i = 0; i < kNodes.Count; i++)
                {
                    JSONNode kN = kNodes[i];
                    if (kN == null)
                    {
                        continue;
                    }

                    ExploreNode kNode = kRun.AddNode(
                        kN["id"] == null ? 0 : kN["id"].AsInt,
                        kN["layer"] == null ? 0 : kN["layer"].AsInt,
                        kN["index"] == null ? 0 : kN["index"].AsInt,
                        kN["type"] == null ? 0 : kN["type"].AsInt);
                    kNode.m_EncounterId = kN["encounter"] == null ? 0 : kN["encounter"].AsInt;
                    kNode.m_State = (ExploreNodeState)(kN["state"] == null
                        ? 0
                        : kN["state"].AsInt);

                    JSONNode kLinks = kN["links"];
                    if (kLinks != null && kLinks.IsArray)
                    {
                        for (int j = 0; j < kLinks.Count; j++)
                        {
                            int kLinkId = kLinks[j].AsInt;
                            if (kLinkId > 0 && !kNode.m_Links.Contains(kLinkId))
                            {
                                kNode.m_Links.Add(kLinkId);
                            }
                        }
                    }
                }
            }

            m_Run = kRun.m_Nodes.Count > 0 ? kRun : null;
            return true;
        }

        #endregion
    }
}
