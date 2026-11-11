using System.Collections.Generic;
using FairyGUI;
using Framework.Script.UI;
using GameMain.Script.Explore;
using UnityEngine;

namespace GameMain.Script.UI
{
    /// <summary>
    /// 探索场景主界面（FairyGUI 包 World / ExploreView，预制体 ExploreView）。
    ///
    /// 结构（由美术在 FGUI 里摆好）：
    ///   com_nodexView  地图画布（NodeView，2000×2000），节点在运行时按坐标塞进去
    ///   btn_back       返回（Base 包 CommonBackBtn）
    ///
    /// 职责：
    /// - 把 ExploreSystem.BuildMapSnapshot() 画成节点 + 连线；
    /// - 拖动地图、自动定位到当前节点；
    /// - 点节点 → ExploreSystem.CanMoveTo / MoveTo；点当前节点 → 重开战斗整备；
    /// - 收到 OnBattleNodeEntered → 打开 NodeSettingView。
    ///
    /// 不读写 ExploreData，也不拼配置表：数据全部来自快照。
    /// </summary>
    public class ExploreView : UIWindow
    {
        /// <summary>ExploreView 外壳的设计分辨率</summary>
        private const float c_ViewWidth = 1920f;
        private const float c_ViewHeight = 1080f;

        /// <summary>连线粗细</summary>
        private const float c_LinkWidth = 3f;

        private GComponent m_Map;
        private GButton btn_back;

        /// <summary>连线层（节点层在它上面）</summary>
        private GComponent m_LinkLayer;

        private readonly Dictionary<int, ExploreNodeCom> m_NodeViews = new Dictionary<int, ExploreNodeCom>();
        private readonly List<int> m_StaleIds = new List<int>();

        private ExploreMapSnapshot m_Snapshot;
        private bool m_Subscribed;

        // 事件回调里不能直接 ShowWindow：UIManager.Hide 会触发 Leave → UnSubscribe，
        // 而 EventManager 正在按索引遍历订阅列表，边遍历边删会漏掉后面的订阅者。
        // 所以这里只记"待执行动作"，下一帧 Update 里再切界面。
        private ExploreBattleNodeMsg m_PendingBattleNode;
        /// <summary>非战斗节点的事件消息（目前只有物资搜索会打开 NodeSettingView）</summary>
        private ExploreNodeEventMsg m_PendingNodeEvent;
        private bool m_PendingReturn;
        /// <summary>调查结束消息（下一帧弹成功/失败结算）</summary>
        private ExploreRunEndedMsg m_PendingResult;

        private static ExploreSystem Explore
        {
            get { return Game.Model.Get<ExploreSystem>(); }
        }

        protected internal override void Init(object o)
        {
            m_Map = ui.GetChild("com_nodexView").asCom;
            btn_back = ui.GetChild("btn_back").asButton;

            SetupMapDrag();
            CreateLinkLayer();

            base.Init(o);
        }

        protected internal override void AddEvent()
        {
            base.AddEvent();
            btn_back.onClick.Add(OnClickBack);
        }

        protected internal override void RemoveEvent()
        {
            base.RemoveEvent();
            btn_back.onClick.Remove(OnClickBack);
        }

        protected internal override void Enter(object o)
        {
            base.Enter(o);

            // 上一次隐藏前没执行完的切界面动作作废，重新按当前进度刷新
            m_PendingBattleNode = null;
            m_PendingNodeEvent = null;
            m_PendingReturn = false;
            m_PendingResult = null;

            Subscribe();
            Refresh();
        }

        protected internal override void Leave()
        {
            UnSubscribe();
            base.Leave();
        }

        protected internal override void Destroy()
        {
            UnSubscribe();
            ClearNodes();
            base.Destroy();
        }

        private void Update()
        {
            // 调查结束：先弹结算（成功/失败）
            if (m_PendingResult != null)
            {
                ExploreRunEndedMsg kResult = m_PendingResult;
                m_PendingResult = null;
                ExploreSettlement.Show(kResult);
                return;
            }

            if (m_PendingReturn)
            {
                m_PendingReturn = false;
                Game.UI.ShowWindow(UIDefine.CommandRoom);
                return;
            }

            if (m_PendingBattleNode != null)
            {
                ExploreBattleNodeMsg kMsg = m_PendingBattleNode;
                m_PendingBattleNode = null;
                Game.UI.ShowWindow(UIDefine.NodeSetting, null, kMsg);
                return;
            }

            if (m_PendingNodeEvent != null)
            {
                ExploreNodeEventMsg kMsg = m_PendingNodeEvent;
                m_PendingNodeEvent = null;

                // 目前只有物资搜索接了面板；其它类型等玩法补齐
                if (kMsg.m_TypeId == (int)cfg.GameType.NodeType.supply)
                {
                    Game.UI.ShowWindow(UIDefine.NodeSetting, null, kMsg);
                }
            }
        }

        #region 地图初始化

        /// <summary>地图可拖动：2000×2000 的画布放在 1920×1080 的窗口里，允许上下左右看</summary>
        private void SetupMapDrag()
        {
            m_Map.opaque = true;
            m_Map.draggable = true;

            float kMinX = Mathf.Min(0f, c_ViewWidth - ExploreConst.MapWidth);
            float kMinY = Mathf.Min(0f, c_ViewHeight - ExploreConst.MapHeight);
            m_Map.dragBounds = new Rect(
                kMinX,
                kMinY,
                ExploreConst.MapWidth - kMinX,
                ExploreConst.MapHeight - kMinY);
        }

        private void CreateLinkLayer()
        {
            m_LinkLayer = new GComponent();
            m_LinkLayer.SetSize(ExploreConst.MapWidth, ExploreConst.MapHeight);
            m_LinkLayer.touchable = false;
            m_Map.AddChild(m_LinkLayer);
        }

        #endregion

        #region 刷新

        private void Refresh()
        {
            m_Snapshot = Explore.BuildMapSnapshot();
            if (m_Snapshot == null)
            {
                // 没有进行中的调查（比如战斗失败被撤离）：下一帧回指挥室
                Game.Log.LogWarning(ExploreSystem.c_LogTag + "没有进行中的调查，返回指挥室");
                m_PendingReturn = true;
                return;
            }

            RebuildNodes();
            DrawLinks();
            CenterOnCurrent();
        }

        /// <summary>按快照增删节点（数量不变时只刷新状态，不重建）</summary>
        private void RebuildNodes()
        {
            m_StaleIds.Clear();
            m_StaleIds.AddRange(m_NodeViews.Keys);

            for (int i = 0; i < m_Snapshot.m_Nodes.Count; i++)
            {
                ExploreNodeSnapshot kNode = m_Snapshot.m_Nodes[i];
                m_StaleIds.Remove(kNode.m_Id);

                ExploreNodeCom kCom;
                if (!m_NodeViews.TryGetValue(kNode.m_Id, out kCom))
                {
                    kCom = UIPackage.CreateObject("World", "ExploreNodeCom") as ExploreNodeCom;
                    if (kCom == null)
                    {
                        Game.Log.LogError(ExploreSystem.c_LogTag +
                            "节点组件创建失败：ui://World/ExploreNodeCom（检查 ExploreViewData 的包注册与 FGUI 导出设置）");
                        continue;
                    }

                    m_Map.AddChild(kCom);
                    m_NodeViews.Add(kNode.m_Id, kCom);
                }

                // m_X / m_Y 是圆形底图的中心；NodeCom 的圆在组件内 y=58
                kCom.SetXY(
                    kNode.m_X - ExploreNodeCom.c_Width * 0.5f,
                    kNode.m_Y - ExploreNodeCom.c_CircleCenterY);
                kCom.SetCom(kNode, OnClickNode);
            }

            for (int i = 0; i < m_StaleIds.Count; i++)
            {
                ExploreNodeCom kOld;
                if (m_NodeViews.TryGetValue(m_StaleIds[i], out kOld))
                {
                    m_NodeViews.Remove(m_StaleIds[i]);
                    kOld.Dispose();
                }
            }
        }

        private void ClearNodes()
        {
            foreach (KeyValuePair<int, ExploreNodeCom> kPair in m_NodeViews)
            {
                kPair.Value.Dispose();
            }
            m_NodeViews.Clear();
        }

        /// <summary>画连线：只画 id 小的那一端，避免同一条边画两次</summary>
        private void DrawLinks()
        {
            // 每次重绘都重建连线：RemoveChildren() 默认不销毁，必须显式 dispose，否则 GGraph 会越积越多
            m_LinkLayer.RemoveChildren(0, -1, true);

            for (int i = 0; i < m_Snapshot.m_Nodes.Count; i++)
            {
                ExploreNodeSnapshot kNode = m_Snapshot.m_Nodes[i];
                for (int j = 0; j < kNode.m_Links.Count; j++)
                {
                    int kLinkId = kNode.m_Links[j];
                    if (kLinkId <= kNode.m_Id)
                    {
                        continue;
                    }

                    ExploreNodeSnapshot kTarget = m_Snapshot.GetNode(kLinkId);
                    if (kTarget == null)
                    {
                        continue;
                    }

                    DrawLink(kNode, kTarget);
                }
            }
        }

        private void DrawLink(ExploreNodeSnapshot _from, ExploreNodeSnapshot _to)
        {
            float kDx = _to.m_X - _from.m_X;
            float kDy = _to.m_Y - _from.m_Y;
            float kLength = Mathf.Sqrt(kDx * kDx + kDy * kDy);
            if (kLength < 1f)
            {
                return;
            }

            // 当前节点连出去的边亮一点，其它压暗，一眼能看出"下一步能去哪"
            bool kActive = _from.m_IsCurrent || _to.m_IsCurrent;
            Color kColor = kActive
                ? new Color(0.93f, 0.78f, 0.35f, 0.75f)
                : new Color(1f, 1f, 1f, 0.16f);

            GGraph kLine = new GGraph();

            // 两端各让出一个半径：线从圆边到圆边，不会从节点名下面穿过去
            float kDrawLength = kLength - ExploreNodeCom.c_Radius * 2f;
            if (kDrawLength < 1f)
            {
                return;
            }

            float kUnitX = kDx / kLength;
            float kUnitY = kDy / kLength;

            kLine.DrawRect(kDrawLength, c_LinkWidth, 0, Color.clear, kColor);
            kLine.SetXY(
                _from.m_X + kUnitX * ExploreNodeCom.c_Radius,
                _from.m_Y + kUnitY * ExploreNodeCom.c_Radius);
            // FairyGUI 的 rotation 以"屏幕顺时针"为正（内部对 Z 取了负），
            // 这里直接用 atan2，不能再取负，否则线会朝反方向长出去。
            kLine.rotation = Mathf.Atan2(kDy, kDx) * Mathf.Rad2Deg;
            kLine.touchable = false;
            m_LinkLayer.AddChild(kLine);
        }

        /// <summary>把当前节点挪到屏幕中央（带边界钳制）</summary>
        private void CenterOnCurrent()
        {
            ExploreNodeSnapshot kCurrent = m_Snapshot.GetCurrentNode();
            if (kCurrent == null)
            {
                return;
            }

            float kMinX = Mathf.Min(0f, c_ViewWidth - m_Snapshot.m_MapWidth);
            float kMinY = Mathf.Min(0f, c_ViewHeight - m_Snapshot.m_MapHeight);

            float kX = Mathf.Clamp(c_ViewWidth * 0.5f - kCurrent.m_X, kMinX, 0f);
            float kY = Mathf.Clamp(c_ViewHeight * 0.5f - kCurrent.m_Y, kMinY, 0f);
            m_Map.SetXY(kX, kY);
        }

        #endregion

        #region 输入

        private void OnClickNode(int _nodeId)
        {
            if (m_Snapshot == null)
            {
                return;
            }

            ExploreNodeSnapshot kNode = m_Snapshot.GetNode(_nodeId);
            if (kNode == null)
            {
                return;
            }

            // 点当前节点：如果它是还没结算的战斗节点，重新打开战前整备
            if (kNode.m_IsCurrent)
            {
                Explore.EnterCurrentNode();
                return;
            }

            string kReason;
            if (!Explore.CanMoveTo(_nodeId, out kReason))
            {
                Game.Log.LogInfo(ExploreSystem.c_LogTag + "无法前往节点 " + _nodeId + "：" + kReason);
                return;
            }

            Explore.MoveTo(_nodeId);
        }

        private void OnClickBack()
        {
            Game.UI.ShowWindow(UIDefine.CommandRoom);
        }

        #endregion

        #region 事件

        private void Subscribe()
        {
            if (m_Subscribed)
            {
                return;
            }

            Game.Event.Subscribe(ExploreEvent.OnRunChanged, OnRunChanged);
            Game.Event.Subscribe(ExploreEvent.OnBattleNodeEntered, OnBattleNodeEntered);
            Game.Event.Subscribe(ExploreEvent.OnNodeEventTriggered, OnNodeEventTriggered);
            Game.Event.Subscribe(ExploreEvent.OnRunEnded, OnRunEnded);
            m_Subscribed = true;
        }

        private void UnSubscribe()
        {
            if (!m_Subscribed)
            {
                return;
            }

            Game.Event.UnSubscribe(ExploreEvent.OnRunChanged, OnRunChanged);
            Game.Event.UnSubscribe(ExploreEvent.OnBattleNodeEntered, OnBattleNodeEntered);
            Game.Event.UnSubscribe(ExploreEvent.OnNodeEventTriggered, OnNodeEventTriggered);
            Game.Event.UnSubscribe(ExploreEvent.OnRunEnded, OnRunEnded);
            m_Subscribed = false;
        }

        private void OnRunChanged(object _msg)
        {
            Refresh();
        }

        private void OnBattleNodeEntered(object _msg)
        {
            ExploreBattleNodeMsg kMsg = _msg as ExploreBattleNodeMsg;
            if (kMsg == null)
            {
                return;
            }

            m_PendingBattleNode = kMsg;
        }

        private void OnNodeEventTriggered(object _msg)
        {
            m_PendingNodeEvent = _msg as ExploreNodeEventMsg;
        }

        private void OnRunEnded(object _msg)
        {
            m_PendingResult = _msg as ExploreRunEndedMsg;
            if (m_PendingResult == null)
            {
                m_PendingReturn = true;
            }
        }

        #endregion
    }
}
