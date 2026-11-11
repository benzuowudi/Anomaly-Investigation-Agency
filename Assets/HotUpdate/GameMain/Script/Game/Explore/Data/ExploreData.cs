using System.Collections.Generic;
using Framework.Script.Data;
using GameMain.Script.Battle;
using SimpleJSON;

namespace GameMain.Script.Explore
{
    /// <summary>
    /// 探索系统的 Data（SystemDataBase）：持久化"进行中的一次调查"。
    ///
    /// 存档内容 = ExploreRun（节点网 + 坐标 + 位置 + 进度）。战斗过程不在这里，
    /// 现场只保留最近一场战斗的结果枚举。
    ///
    /// v2 起节点坐标随存档保存；读到 v1 旧档（没有坐标）时用 seed 重建一次布局，
    /// 类型/连边/进度一律保留。
    /// </summary>
    public class ExploreData : SystemDataBase
    {
        public const string SaveFileName = "explore.json";

        private const int c_Version = 2;

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
            kRoot.Add("version", c_Version);
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
            kRun.Add("depth", m_Run.m_Depth);
            kRun.Add("mapWidth", m_Run.m_MapWidth);
            kRun.Add("mapHeight", m_Run.m_MapHeight);
            kRun.Add("startNodeId", m_Run.m_StartNodeId);
            kRun.Add("goalNodeId", m_Run.m_GoalNodeId);
            kRun.Add("currentNodeId", m_Run.m_CurrentNodeId);
            kRun.Add("steps", m_Run.m_Steps);
            kRun.Add("goalReached", m_Run.m_GoalReached);
            kRun.Add("lastBattleOutcome", (int)m_Run.m_LastBattleOutcome);

            JSONArray kVisited = new JSONArray();
            for (int i = 0; i < m_Run.m_VisitedOrder.Count; i++)
            {
                kVisited.Add(m_Run.m_VisitedOrder[i]);
            }
            kRun.Add("visitedOrder", kVisited);

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
                kNodeJson.Add("x", kNode.m_X);
                kNodeJson.Add("y", kNode.m_Y);
                kNodeJson.Add("visit", kNode.m_VisitCount);
                kNodeJson.Add("resolved", kNode.m_Resolved);

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

            int kVersion = kRoot["version"] == null ? 1 : kRoot["version"].AsInt;
            m_Version = kVersion;
            m_CompletedRuns = kRoot["completedRuns"] == null ? 0 : kRoot["completedRuns"].AsInt;

            JSONNode kRunNode = kRoot["run"];
            if (kRunNode == null || !kRunNode.IsObject)
            {
                m_Run = null;
                return true;
            }

            ExploreRun kRun = new ExploreRun();
            kRun.m_ZoneId = GetInt(kRunNode, "zoneId");
            kRun.m_Seed = GetInt(kRunNode, "seed");
            kRun.m_LayerCount = GetInt(kRunNode, "layerCount");
            kRun.m_Depth = GetInt(kRunNode, "depth");
            kRun.m_MapWidth = GetFloat(kRunNode, "mapWidth", ExploreConst.MapWidth);
            kRun.m_MapHeight = GetFloat(kRunNode, "mapHeight", ExploreConst.MapHeight);
            kRun.m_StartNodeId = GetInt(kRunNode, "startNodeId");
            kRun.m_GoalNodeId = GetInt(kRunNode, "goalNodeId");
            kRun.m_CurrentNodeId = GetInt(kRunNode, "currentNodeId");
            kRun.m_Steps = GetInt(kRunNode, "steps");
            kRun.m_GoalReached = kRunNode["goalReached"] != null && kRunNode["goalReached"].AsBool;
            kRun.m_LastBattleOutcome = (BattleOutcome)GetInt(kRunNode, "lastBattleOutcome");

            JSONNode kVisited = kRunNode["visitedOrder"];
            if (kVisited != null && kVisited.IsArray)
            {
                for (int i = 0; i < kVisited.Count; i++)
                {
                    int kId = kVisited[i].AsInt;
                    if (kId > 0)
                    {
                        kRun.m_VisitedOrder.Add(kId);
                    }
                }
            }

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
                        GetInt(kN, "id"),
                        GetInt(kN, "layer"),
                        GetInt(kN, "index"),
                        GetInt(kN, "type"));
                    kNode.m_EncounterId = GetInt(kN, "encounter");
                    kNode.m_State = (ExploreNodeState)GetInt(kN, "state");
                    kNode.m_X = GetFloat(kN, "x", 0f);
                    kNode.m_Y = GetFloat(kN, "y", 0f);
                    kNode.m_VisitCount = GetInt(kN, "visit");
                    kNode.m_Resolved = kN["resolved"] != null && kN["resolved"].AsBool;

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

            if (kRun.m_Nodes.Count == 0)
            {
                m_Run = null;
                return true;
            }

            // v1 旧档没有坐标：用 seed 重建一次，随后由 Model 存档
            if (kVersion < c_Version || !HasLayout(kRun))
            {
                ExploreMapLayout.Rebuild(kRun);
            }

            if (kRun.m_VisitedOrder.Count == 0)
            {
                kRun.m_VisitedOrder.Add(kRun.m_CurrentNodeId);
            }

            m_Run = kRun;
            return true;
        }

        /// <summary>所有节点都有合法坐标（x/y 都为 0 视为没生成过）</summary>
        private static bool HasLayout(ExploreRun _run)
        {
            foreach (KeyValuePair<int, ExploreNode> kPair in _run.m_Nodes)
            {
                ExploreNode kNode = kPair.Value;
                if (kNode.m_X <= 0f || kNode.m_Y <= 0f)
                {
                    return false;
                }
            }
            return true;
        }

        private static int GetInt(JSONNode _node, string _key)
        {
            return _node[_key] == null ? 0 : _node[_key].AsInt;
        }

        private static float GetFloat(JSONNode _node, string _key, float _default)
        {
            return _node[_key] == null ? _default : _node[_key].AsFloat;
        }

        #endregion
    }
}
