using System.Collections.Generic;

namespace GameMain.Script.Explore
{
    /// <summary>
    /// 节点坐标生成器（纯函数，允许 static）：
    /// 分层 DAG → 每个节点在 2000×2000 的地图画布上取一个中心点。
    ///
    /// 规则：
    /// - 出发阵地在最下、目标节点在最上（纵向推进），层间等距；
    /// - 层内节点沿横向均分槽位，再叠加一个有上限的随机抖动（保证同层最小间距）；
    /// - 坐标由 seed 决定但走**独立随机流**（seed*31+7919），调节点权重不会改变布局；
    /// - 已存在的节点永远不重排：深入新层时只给新层算坐标。
    /// </summary>
    public static class ExploreMapLayout
    {
        /// <summary>一次生成：给 run 里所有节点算坐标（新局）</summary>
        public static void Build(ExploreRun _run)
        {
            if (_run == null || _run.m_LayerCount <= 0)
            {
                return;
            }

            _run.m_MapWidth = ExploreConst.MapWidth;
            _run.m_MapHeight = ExploreConst.MapHeight;

            System.Random kRandom = new System.Random(_run.m_Seed * 31 + 7919);
            float kBottom = ExploreConst.MapHeight - ExploreConst.MapPadY;
            float kTop = ExploreConst.MapPadY;
            float kGap = _run.m_LayerCount > 1 ? (kBottom - kTop) / (_run.m_LayerCount - 1) : 0f;

            for (int layer = 0; layer < _run.m_LayerCount; layer++)
            {
                List<ExploreNode> kNodes = _run.GetNodesInLayer(layer);
                PlaceLayer(kNodes, kBottom - kGap * layer, kRandom);
            }
        }

        /// <summary>
        /// 旧存档补坐标：节点网/连边都在，只是没有 x/y。
        /// 同 seed + 同层结构 → 结果与当初生成时一致（与节点类型无关）。
        /// </summary>
        public static void Rebuild(ExploreRun _run)
        {
            Build(_run);
        }

        /// <summary>
        /// 继续深入：给新追加的层补坐标。已有节点一个都不动（y 只往上长）。
        /// </summary>
        public static void Extend(ExploreRun _run, int _oldLayerCount)
        {
            if (_run == null || _run.m_LayerCount <= _oldLayerCount)
            {
                return;
            }

            float kGap = _run.m_LayerCount > 1
                ? (ExploreConst.MapHeight - ExploreConst.MapPadY * 2) / (_run.m_LayerCount - 1)
                : 0f;

            System.Random kRandom = new System.Random(_run.m_Seed * 31 + 7919 + _oldLayerCount * 131);
            for (int layer = _oldLayerCount; layer < _run.m_LayerCount; layer++)
            {
                List<ExploreNode> kNodes = _run.GetNodesInLayer(layer);
                PlaceLayer(kNodes, ExploreConst.MapHeight - ExploreConst.MapPadY - kGap * layer, kRandom);
            }
        }

        /// <summary>单层布局：_y 是节点中心线的 Y</summary>
        private static void PlaceLayer(List<ExploreNode> _nodes, float _y, System.Random _random)
        {
            int kCount = _nodes.Count;
            if (kCount == 0)
            {
                return;
            }

            float kLeft = ExploreConst.MapPadX + ExploreConst.NodeHalfWidth;
            float kRight = ExploreConst.MapWidth - ExploreConst.MapPadX - ExploreConst.NodeHalfWidth;

            if (kCount == 1)
            {
                _nodes[0].m_X = (kLeft + kRight) * 0.5f;
                _nodes[0].m_Y = _y;
                return;
            }

            float kSlot = (kRight - kLeft) / (kCount - 1);
            float kJitter = ExploreConst.MapJitterX;

            // 抖动上限：保证相邻节点中心距 >= MapMinGap
            float kMaxJitter = System.Math.Max(0f, (kSlot - ExploreConst.MapMinGap) * 0.5f);
            if (kJitter > kMaxJitter)
            {
                kJitter = kMaxJitter;
            }

            for (int i = 0; i < kCount; i++)
            {
                float kX = kLeft + kSlot * i;
                if (kJitter > 0.01f)
                {
                    kX += (float)(_random.NextDouble() * 2.0 - 1.0) * kJitter;
                }

                kX = Clamp(kX, kLeft, kRight);
                _nodes[i].m_X = kX;
                _nodes[i].m_Y = _y;
            }
        }

        private static float Clamp(float _value, float _min, float _max)
        {
            if (_value < _min)
            {
                return _min;
            }
            return _value > _max ? _max : _value;
        }
    }
}
