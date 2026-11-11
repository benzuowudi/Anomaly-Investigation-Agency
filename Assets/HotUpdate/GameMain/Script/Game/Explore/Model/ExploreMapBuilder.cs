using System.Collections.Generic;
using cfg.GameType;

namespace GameMain.Script.Explore
{
    /// <summary>
    /// 节点网生成器（纯函数，允许 static）：
    /// exploreZone（层数/密度/营地间隔/遭遇池）+ nodeType（可选类型）+ zoneNodeWeight（类型权重）+ 种子 → ExploreRun。
    ///
    /// 生成规则：
    /// - 第 0 层 1 个节点（出发阵地，类型固定露营地），最后一层 1 个节点（目标：有"强敌"配置时用强敌，否则战斗）；
    /// - 中间每层 nodesPerLayerMin~Max 个节点；
    /// - 相邻层之间连边：每个父节点连 1~2 个子节点，且保证每个子节点至少有一个父节点；
    /// - 每 campInterval 层保底一个露营地；
    /// - 配表缺失时全部走 ExploreConst 的默认值，保证没有数据也能跑通；
    /// - 节点类型集合以 nodeType 表为准（表里有几类就能出几类，含 8 强敌）；
    ///   权重优先用 zoneNodeWeight，没有配置时用 ExploreConst.DefaultWeights；
    /// - 生成完拓扑后调 ExploreMapLayout 给每个节点算地图坐标（2000×2000 画布）。
    ///
    /// 测试开关 ExploreConst.TestAllBattle = true 时：除出发阵地外全部生成战斗节点、
    /// 跳过露营地保底、层数压到 ExploreConst.TestLayerCount（调试用，默认关闭）。
    /// </summary>
    public static class ExploreMapBuilder
    {
        public static ExploreRun Build(cfg.Tables _tables, int _zoneId, int _seed)
        {
            System.Random kRandom = new System.Random(_seed);

            cfg.GameData_Explore.ExploreZoneData kZone =
                _tables == null || _tables.exploreZone == null ? null : _tables.exploreZone.GetOrDefault(_zoneId);

            int kLayerCount = kZone != null && kZone.LayerCount > 0
                ? kZone.LayerCount
                : (ExploreConst.TestAllBattle ? ExploreConst.TestLayerCount : ExploreConst.DefaultLayerCount);
            int kNodesMin = kZone != null && kZone.NodesPerLayerMin > 0 ? kZone.NodesPerLayerMin : ExploreConst.DefaultNodesMin;
            int kNodesMax = kZone != null && kZone.NodesPerLayerMax >= kNodesMin ? kZone.NodesPerLayerMax : ExploreConst.DefaultNodesMax;
            int kCampInterval = kZone != null && kZone.CampInterval > 0 ? kZone.CampInterval : ExploreConst.DefaultCampInterval;
            int kGoalEncounter = kZone == null ? 0 : kZone.GoalEncounterID;
            List<int> kEncounterPool = kZone == null ? null : kZone.EncounterPool;

            // 可选类型 + 权重：类型集合来自 nodeType 表，权重来自 zoneNodeWeight / 默认值
            List<int> kTypeIds = new List<int>();
            Dictionary<int, int> kWeights = new Dictionary<int, int>();
            LoadTypeWeights(_tables, _zoneId, kTypeIds, kWeights);

            int kBattleType = ResolveBattleType(kTypeIds);
            int kCampType = (int)NodeType.camp;
            int kGoalType = kTypeIds.Contains(ExploreConst.BossNodeType)
                ? ExploreConst.BossNodeType
                : kBattleType;

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
                        kType = kCampType;
                    }
                    else if (kIsGoal)
                    {
                        kType = kGoalType;
                    }
                    else if (ExploreConst.TestAllBattle)
                    {
                        kType = kBattleType;
                    }
                    else
                    {
                        kType = PickType(kRandom, kTypeIds, kWeights, kBattleType);
                    }

                    ExploreNode kNode = kRun.AddNode(kNextId++, layer, i, kType);
                    if (kType == kCampType)
                    {
                        kCampPlaced = true;
                    }
                    kLayerNodes.Add(kNode);
                }

                // 保底露营地：每 campInterval 层强制放一个（起点/终点层除外；全战斗测试时不要）
                if (!ExploreConst.TestAllBattle
                    && !kIsStart && !kIsGoal && kCampInterval > 0
                    && layer % kCampInterval == 0 && !kCampPlaced
                    && kTypeIds.Contains(kCampType))
                {
                    kLayerNodes[0].m_TypeId = kCampType;
                }

                // 战斗/强敌节点分配遭遇
                for (int i = 0; i < kLayerNodes.Count; i++)
                {
                    ExploreNode kNode = kLayerNodes[i];
                    if (ExploreConst.IsBattleType(kNode.m_TypeId))
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

            // 坐标：拓扑定完再算，布局用的是独立随机流
            ExploreMapLayout.Build(kRun);

            kRun.RefreshAvailability();
            kRun.m_VisitedOrder.Add(kRun.m_StartNodeId);

            return kRun;
        }

        #region 类型与权重

        /// <summary>
        /// 类型集合以 nodeType 表为准；表为空时退回 1~7。
        /// 权重：zoneNodeWeight 有该区域的行就覆盖，否则用 ExploreConst.DefaultWeights。
        /// </summary>
        private static void LoadTypeWeights(cfg.Tables _tables, int _zoneId,
            List<int> _types, Dictionary<int, int> _weights)
        {
            if (_tables != null && _tables.nodeType != null)
            {
                List<cfg.GameData_Explore.NodeTypeData> kRows = _tables.nodeType.DataList;
                for (int i = 0; i < kRows.Count; i++)
                {
                    cfg.GameData_Explore.NodeTypeData kRow = kRows[i];
                    if (kRow != null && kRow.ID > 0 && !_types.Contains(kRow.ID))
                    {
                        _types.Add(kRow.ID);
                    }
                }
            }

            if (_types.Count == 0)
            {
                for (int i = 1; i <= 7; i++)
                {
                    _types.Add(i);
                }
            }

            _types.Sort();

            for (int i = 0; i < _types.Count; i++)
            {
                _weights[_types[i]] = GetDefaultWeight(_types[i]);
            }

            if (_tables == null || _tables.zoneNodeWeight == null)
            {
                return;
            }

            List<cfg.GameData_Explore.ZoneNodeWeightData> kWeightRows = _tables.zoneNodeWeight.DataList;
            for (int i = 0; i < kWeightRows.Count; i++)
            {
                cfg.GameData_Explore.ZoneNodeWeightData kRow = kWeightRows[i];
                if (kRow == null || kRow.ZoneID != _zoneId)
                {
                    continue;
                }
                if (_weights.ContainsKey(kRow.NodeTypeID))
                {
                    _weights[kRow.NodeTypeID] = kRow.Weight > 0 ? kRow.Weight : 0;
                }
            }

            // 所有类型权重都被配成 0 时，至少让战斗类型能出
            int kTotal = 0;
            foreach (KeyValuePair<int, int> kPair in _weights)
            {
                if (kPair.Value > 0)
                {
                    kTotal += kPair.Value;
                }
            }

            if (kTotal <= 0)
            {
                int kBattleType = ResolveBattleType(_types);
                if (_weights.ContainsKey(kBattleType))
                {
                    _weights[kBattleType] = 100;
                }
            }
        }

        private static int GetDefaultWeight(int _typeId)
        {
            int kIndex = _typeId - 1;
            if (kIndex >= 0 && kIndex < ExploreConst.DefaultWeights.Length)
            {
                return ExploreConst.DefaultWeights[kIndex];
            }
            return 1;
        }

        /// <summary>优先用 1 战斗；表里没有就找第一个战斗类（含强敌），再退回 1</summary>
        private static int ResolveBattleType(List<int> _types)
        {
            if (_types.Contains((int)NodeType.battle))
            {
                return (int)NodeType.battle;
            }

            for (int i = 0; i < _types.Count; i++)
            {
                if (ExploreConst.IsBattleType(_types[i]))
                {
                    return _types[i];
                }
            }

            return (int)NodeType.battle;
        }

        private static int PickType(System.Random _random, List<int> _types,
            Dictionary<int, int> _weights, int _fallback)
        {
            int kTotal = 0;
            for (int i = 0; i < _types.Count; i++)
            {
                int kWeight;
                if (_weights.TryGetValue(_types[i], out kWeight) && kWeight > 0)
                {
                    kTotal += kWeight;
                }
            }

            if (kTotal <= 0)
            {
                return _fallback;
            }

            int kRoll = _random.Next(0, kTotal);
            for (int i = 0; i < _types.Count; i++)
            {
                int kWeight;
                if (!_weights.TryGetValue(_types[i], out kWeight) || kWeight <= 0)
                {
                    continue;
                }

                kRoll -= kWeight;
                if (kRoll < 0)
                {
                    return _types[i];
                }
            }

            return _fallback;
        }

        private static int PickEncounter(System.Random _random, List<int> _pool)
        {
            if (_pool == null || _pool.Count == 0)
            {
                return 0;
            }
            return _pool[_random.Next(0, _pool.Count)];
        }

        #endregion

        #region 连边

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
