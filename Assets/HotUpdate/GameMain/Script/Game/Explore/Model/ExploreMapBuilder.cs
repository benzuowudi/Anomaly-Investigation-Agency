using System.Collections.Generic;
using cfg.GameType;

namespace GameMain.Script.Explore
{
    /// <summary>
    /// 节点网生成器（纯函数，允许 static）：
    /// exploreZone（层数/密度/营地间隔/遭遇池）+ zoneNodeWeight（类型权重）+ 种子 → ExploreRun。
    ///
    /// 生成规则：
    /// - 第 0 层 1 个节点（出发阵地，类型固定露营地），最后一层 1 个节点（目标，类型固定战斗）；
    /// - 中间每层 nodesPerLayerMin~Max 个节点；
    /// - 相邻层之间连边：每个父节点连 1~2 个子节点，且保证每个子节点至少有一个父节点；
    /// - 每 campInterval 层保底一个露营地；
    /// - 配表缺失时全部走 ExploreConst 的默认值，保证没有数据也能跑通。
    /// </summary>
    public static class ExploreMapBuilder
    {
        public static ExploreRun Build(cfg.Tables _tables, int _zoneId, int _seed)
        {
            System.Random kRandom = new System.Random(_seed);

            cfg.GameData_Explore.ExploreZoneData kZone =
                _tables == null || _tables.exploreZone == null ? null : _tables.exploreZone.GetOrDefault(_zoneId);

            int kLayerCount = kZone != null && kZone.LayerCount > 0 ? kZone.LayerCount : ExploreConst.DefaultLayerCount;
            int kNodesMin = kZone != null && kZone.NodesPerLayerMin > 0 ? kZone.NodesPerLayerMin : ExploreConst.DefaultNodesMin;
            int kNodesMax = kZone != null && kZone.NodesPerLayerMax >= kNodesMin ? kZone.NodesPerLayerMax : ExploreConst.DefaultNodesMax;
            int kCampInterval = kZone != null && kZone.CampInterval > 0 ? kZone.CampInterval : ExploreConst.DefaultCampInterval;
            int kGoalEncounter = kZone == null ? 0 : kZone.GoalEncounterID;
            List<int> kEncounterPool = kZone == null ? null : kZone.EncounterPool;

            int[] kWeights = LoadWeights(_tables, _zoneId);

            ExploreRun kRun = new ExploreRun();
            kRun.m_ZoneId = _zoneId;
            kRun.m_Seed = _seed;
            kRun.m_LayerCount = kLayerCount;

            int kNextId = ExploreConst.NodeIdBase;
            List<List<ExploreNode>> kLayers = new List<List<ExploreNode>>();

            for (int layer = 0; layer < kLayerCount; layer++)
            {
                bool kIsStart = layer == 0;
                bool kIsGoal = layer == kLayerCount - 1;
                int kCount = kIsStart || kIsGoal ? 1 : kRandom.Next(kNodesMin, kNodesMax + 1);

                List<ExploreNode> kLayerNodes = new List<ExploreNode>();
                bool kCampPlaced = false;

                for (int i = 0; i < kCount; i++)
                {
                    int kType;
                    if (kIsStart)
                    {
                        kType = (int)NodeType.camp;
                    }
                    else if (kIsGoal)
                    {
                        kType = (int)NodeType.battle;
                    }
                    else
                    {
                        kType = PickType(kRandom, kWeights);
                    }

                    ExploreNode kNode = kRun.AddNode(kNextId++, layer, i, kType);
                    if (kType == (int)NodeType.camp)
                    {
                        kCampPlaced = true;
                    }
                    kLayerNodes.Add(kNode);
                }

                // 保底露营地：每 campInterval 层强制放一个（起点/终点层除外）
                if (!kIsStart && !kIsGoal && kCampInterval > 0 && layer % kCampInterval == 0 && !kCampPlaced)
                {
                    kLayerNodes[0].m_TypeId = (int)NodeType.camp;
                }

                // 战斗节点分配遭遇
                for (int i = 0; i < kLayerNodes.Count; i++)
                {
                    ExploreNode kNode = kLayerNodes[i];
                    if (kNode.m_TypeId == (int)NodeType.battle)
                    {
                        kNode.m_EncounterId = kIsGoal ? kGoalEncounter : PickEncounter(kRandom, kEncounterPool);
                    }
                }

                kLayers.Add(kLayerNodes);
            }

            // 连边
            for (int layer = 0; layer < kLayers.Count - 1; layer++)
            {
                ConnectLayers(kLayers[layer], kLayers[layer + 1], kRandom);
            }

            kRun.m_StartNodeId = kLayers[0][0].m_Id;
            kRun.m_GoalNodeId = kLayers[kLayers.Count - 1][0].m_Id;
            kRun.m_CurrentNodeId = kRun.m_StartNodeId;
            kRun.m_Steps = 0;
            kRun.m_GoalReached = false;
            kRun.RefreshAvailability();

            return kRun;
        }

        #region 内部

        /// <summary>按 GameType.NodeType 1~7 读取权重；没有配置时用默认值</summary>
        private static int[] LoadWeights(cfg.Tables _tables, int _zoneId)
        {
            int[] kWeights = new int[8];
            for (int i = 1; i <= 7; i++)
            {
                kWeights[i] = ExploreConst.DefaultWeights[i - 1];
            }

            if (_tables == null || _tables.zoneNodeWeight == null)
            {
                return kWeights;
            }

            bool kHasData = false;
            List<cfg.GameData_Explore.ZoneNodeWeightData> kRows = _tables.zoneNodeWeight.DataList;
            for (int i = 0; i < kRows.Count; i++)
            {
                cfg.GameData_Explore.ZoneNodeWeightData kRow = kRows[i];
                if (kRow == null || kRow.ZoneID != _zoneId)
                {
                    continue;
                }
                if (kRow.NodeTypeID < 1 || kRow.NodeTypeID > 7)
                {
                    continue;
                }
                kWeights[kRow.NodeTypeID] = kRow.Weight;
                kHasData = true;
            }

            if (!kHasData)
            {
                for (int i = 1; i <= 7; i++)
                {
                    kWeights[i] = ExploreConst.DefaultWeights[i - 1];
                }
            }
            return kWeights;
        }

        private static int PickType(System.Random _random, int[] _weights)
        {
            int kTotal = 0;
            for (int i = 1; i <= 7; i++)
            {
                if (_weights[i] > 0)
                {
                    kTotal += _weights[i];
                }
            }
            if (kTotal <= 0)
            {
                return (int)NodeType.battle;
            }

            int kRoll = _random.Next(0, kTotal);
            for (int i = 1; i <= 7; i++)
            {
                if (_weights[i] <= 0)
                {
                    continue;
                }
                kRoll -= _weights[i];
                if (kRoll < 0)
                {
                    return i;
                }
            }
            return (int)NodeType.battle;
        }

        private static int PickEncounter(System.Random _random, List<int> _pool)
        {
            if (_pool == null || _pool.Count == 0)
            {
                return 0;
            }
            return _pool[_random.Next(0, _pool.Count)];
        }

        /// <summary>相邻层连边：父节点各连 1~2 个，再补全没有父节点的子节点</summary>
        private static void ConnectLayers(List<ExploreNode> _parents, List<ExploreNode> _children, System.Random _random)
        {
            if (_parents.Count == 0 || _children.Count == 0)
            {
                return;
            }

            for (int i = 0; i < _parents.Count; i++)
            {
                ExploreNode kParent = _parents[i];
                int kLinkCount = _random.Next(1, 3);
                for (int c = 0; c < kLinkCount; c++)
                {
                    ExploreNode kChild = _children[_random.Next(0, _children.Count)];
                    kParent.Link(kChild);
                }
            }

            // 保证每个子节点至少有一个父节点
            for (int i = 0; i < _children.Count; i++)
            {
                ExploreNode kChild = _children[i];
                bool kHasParent = false;
                for (int p = 0; p < _parents.Count; p++)
                {
                    if (_parents[p].IsLinkedTo(kChild.m_Id))
                    {
                        kHasParent = true;
                        break;
                    }
                }
                if (!kHasParent)
                {
                    _parents[_random.Next(0, _parents.Count)].Link(kChild);
                }
            }
        }

        #endregion
    }
}
