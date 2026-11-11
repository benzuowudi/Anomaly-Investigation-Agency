using cfg.GameType;

namespace GameMain.Script.Explore
{
    /// <summary>节点状态</summary>
    public enum ExploreNodeState
    {
        /// <summary>不可达（隔层的节点）</summary>
        Locked = 0,
        /// <summary>可从当前节点走到的相邻节点</summary>
        Available = 1,
        /// <summary>当前所在节点</summary>
        Current = 2,
        /// <summary>已探索（可以走回去，但不再触发事件）</summary>
        Explored = 3,
    }

    /// <summary>
    /// 探索系统常量（配表缺失时的兜底默认值；有 exploreZone / zoneNodeWeight 时以表为准）。
    /// </summary>
    public static class ExploreConst
    {
        #region 测试开关（打探索流程时用；正式版本要关掉）

        /// <summary>
        /// 全战斗测试：除出发阵地外，所有节点都生成成战斗节点，
        /// 同时跳过"每 N 层保底露营地"，并把层数压到 <see cref="TestLayerCount"/>。
        /// 现在要按 nodeType / zoneNodeWeight 生成，默认关闭。
        /// </summary>
        public static readonly bool TestAllBattle = false;

        /// <summary>测试用的层数（TestAllBattle = true 时生效）</summary>
        public static readonly int TestLayerCount = 4;

        /// <summary>
        /// 测试期战败是否保留调查进度。
        /// 现在有 ExploreFailWindow 结算界面，默认关掉：全灭 → 结算失败 → 回指挥室。
        /// 需要连续测多场战斗时再临时设为 true。
        /// </summary>
        public static readonly bool TestKeepRunOnDefeat = false;

        #endregion

        /// <summary>默认层数</summary>
        public const int DefaultLayerCount = 7;
        /// <summary>每层最少节点</summary>
        public const int DefaultNodesMin = 2;
        /// <summary>每层最多节点</summary>
        public const int DefaultNodesMax = 3;
        /// <summary>每 N 层保底一个露营地</summary>
        public const int DefaultCampInterval = 3;
        /// <summary>每次移动扣除的 SAN</summary>
        public const int DefaultSanCostPerMove = 5;
        /// <summary>节点ID起始值</summary>
        public const int NodeIdBase = 1;
        /// <summary>默认区域ID（表里没有数据时也按这个区域开局）</summary>
        public const int DefaultZoneId = 1;

        /// <summary>
        /// 强敌节点类型 ID：nodeType 表新增的第 8 类，GameType.NodeType 枚举里还没有，
        /// 生成 / 战斗判定时单独认它。
        /// </summary>
        public const int BossNodeType = 8;

        /// <summary>默认节点权重（下标 1~8：战斗/救援/物资/考察/回收/场景/露营地/强敌）</summary>
        public static readonly int[] DefaultWeights = { 40, 10, 15, 10, 5, 10, 10, 5 };

        /// <summary>这个节点类型是不是要进战斗（普通战斗 + 强敌）</summary>
        public static bool IsBattleType(int _typeId)
        {
            return _typeId == (int)NodeType.battle || _typeId == BossNodeType;
        }

        #region 地图布局（与 FairyGUI 包 World / NodeView 的 2000×2000 画布对齐）

        /// <summary>地图画布宽（= NodeView 宽）</summary>
        public const float MapWidth = 2000f;
        /// <summary>地图画布高（= NodeView 高）</summary>
        public const float MapHeight = 2000f;
        /// <summary>左右留白</summary>
        public const float MapPadX = 140f;
        /// <summary>上下留白</summary>
        public const float MapPadY = 160f;
        /// <summary>节点半宽（ExploreNodeCom 宽 120）</summary>
        public const float NodeHalfWidth = 60f;
        /// <summary>节点半高（ExploreNodeCom 高 181）</summary>
        public const float NodeHalfHeight = 90f;
        /// <summary>层内横向随机抖动幅度</summary>
        public const float MapJitterX = 60f;
        /// <summary>同层节点最小中心距</summary>
        public const float MapMinGap = 200f;

        #endregion

        #region 节点类型兜底（nodeType 表为空时用）

        /// <summary>1~8 的中文名（下标 0 占位）</summary>
        public static readonly string[] NodeTypeNames =
        {
            string.Empty, "战斗", "救援", "物资搜索", "异常考察", "异常回收", "场景事件", "露营地", "强敌",
        };

        /// <summary>
        /// 地图节点上显示的短名（节点文字框只有 56px，最多两个汉字）。
        /// 下标与节点类型 ID 一致；完整名字/说明仍从 nodeType 表读。
        /// </summary>
        public static readonly string[] NodeTypeShortNames =
        {
            string.Empty, "战斗", "救援", "物资", "考察", "回收", "事件", "营地", "强敌",
        };

        /// <summary>
        /// 1~8 的图标名（World 包内图片资源名，View 拼成 ui://World/&lt;name&gt;）。
        /// 现有美术：fight 战斗、boss 强敌/目标、evacuate 撤离/救援、ruins 遗迹/回收、
        /// abnormal 异常现象、camp 露营地、pro/proBg 进度条。
        /// </summary>
        public static readonly string[] NodeTypeIcons =
        {
            string.Empty, "ui_node_fight", "ui_node_evacuate", "ui_node_ruins",
            "ui_node_abnormal", "ui_node_boss", "ui_node_ruins", "ui_node_camp", "ui_node_boss",
        };

        /// <summary>目标节点的图标（战斗节点 + 终点时覆盖成 boss）</summary>
        public const string GoalNodeIcon = "ui_node_boss";

        #endregion
    }
}
