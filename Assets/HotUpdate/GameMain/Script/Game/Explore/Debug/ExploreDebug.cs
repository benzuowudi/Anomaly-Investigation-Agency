#if UNITY_EDITOR
using System.Collections.Generic;
using Framework.Script.Constant;
using Framework.Script.UI;
using UnityEditor;
using UnityEngine;

namespace GameMain.Script.Explore
{
    /// <summary>
    /// 探索系统的编辑器调试入口（只在编辑器里编译）。
    ///
    /// 用法：先运行游戏 → "打开探索界面" 开一局/接着上一局 →
    /// 在地图上点相邻节点推进；战斗节点会打开布阵界面，开战后等战斗结束回地图。
    /// </summary>
    public static class ExploreDebug
    {
        private static ExploreSystem Explore
        {
            get { return Game.Model.Get<ExploreSystem>(); }
        }

        [MenuItem("Framework/探索/打开探索界面（开始或继续）", false, 299)]
        public static void OpenExploreUI()
        {
            if (!GameConst.m_GameStart)
            {
                Debug.LogError("[探索] 请先运行游戏再点这个菜单（GameConst.m_GameStart = false）");
                return;
            }

            ExploreSystem kExplore = Explore;
            if (kExplore == null)
            {
                return;
            }

            if (!kExplore.HasRun())
            {
                kExplore.StartRun(ExploreConst.DefaultZoneId, 0);
            }

            Game.UI.ShowWindow(UIDefine.Explore);
        }

        [MenuItem("Framework/探索/开始一次调查（默认区域）", false, 300)]
        public static void StartRun()
        {
            if (!GameConst.m_GameStart)
            {
                Debug.LogError("[探索] 请先运行游戏再点这个菜单（GameConst.m_GameStart = false）");
                return;
            }

            ExploreSystem kExplore = Explore;
            if (kExplore == null)
            {
                return;
            }

            kExplore.StartRun(0, 0);
        }

        [MenuItem("Framework/探索/走到下一个节点", false, 301)]
        public static void MoveNext()
        {
            if (!GameConst.m_GameStart)
            {
                Debug.LogError("[探索] 请先运行游戏再点这个菜单（GameConst.m_GameStart = false）");
                return;
            }

            ExploreSystem kExplore = Explore;
            if (kExplore == null || !kExplore.HasRun())
            {
                Debug.LogWarning("[探索] 当前没有进行中的调查");
                return;
            }

            kExplore.MoveToNextAvailable();
        }

        [MenuItem("Framework/探索/打印节点网", false, 302)]
        public static void DumpMap()
        {
            if (!GameConst.m_GameStart)
            {
                Debug.LogError("[探索] 请先运行游戏再点这个菜单（GameConst.m_GameStart = false）");
                return;
            }

            ExploreSystem kExplore = Explore;
            if (kExplore == null || !kExplore.HasRun())
            {
                Debug.LogWarning("[探索] 当前没有进行中的调查");
                return;
            }

            ExploreRun kRun = kExplore.GetRun();
            Debug.Log("[探索] 当前：" + kRun);

            for (int layer = 0; layer < kRun.m_LayerCount; layer++)
            {
                List<ExploreNode> kNodes = kRun.GetNodesInLayer(layer);
                string kLine = "[探索] L" + layer + "：";
                for (int i = 0; i < kNodes.Count; i++)
                {
                    ExploreNode kNode = kNodes[i];
                    kLine += " [" + kNode.m_Id + ":" + (cfg.GameType.NodeType)kNode.m_TypeId
                        + "/" + kNode.m_State
                        + " (" + kNode.m_X.ToString("F0") + "," + kNode.m_Y.ToString("F0") + ")"
                        + " visit:" + kNode.m_VisitCount
                        + (kNode.m_Resolved ? " done" : string.Empty) + "]";
                }
                Debug.Log(kLine);
            }
        }

        [MenuItem("Framework/探索/校验配置", false, 304)]
        public static void ValidateConfig()
        {
            if (!GameConst.m_GameStart)
            {
                Debug.LogError("[探索] 请先运行游戏再点这个菜单（GameConst.m_GameStart = false）");
                return;
            }

            cfg.Tables kTables = Game.Data.GetData;
            if (kTables == null)
            {
                Debug.LogError("[探索] 配置表没有加载");
                return;
            }

            int kErrors = 0;
            if (kTables.exploreZone == null || kTables.exploreZone.DataList.Count == 0)
            {
                Debug.LogWarning("[探索] exploreZone 表为空，将使用 ExploreConst 默认值（"
                    + ExploreConst.DefaultLayerCount + " 层 / "
                    + ExploreConst.DefaultNodesMin + "~" + ExploreConst.DefaultNodesMax + " 节点）");
            }

            if (kTables.nodeType == null || kTables.nodeType.DataList.Count == 0)
            {
                Debug.LogWarning("[探索] nodeType 表为空，节点名/图标将使用 ExploreConst 兜底");
            }
            else
            {
                for (int typeId = 1; typeId <= 7; typeId++)
                {
                    if (kTables.nodeType.GetOrDefault(typeId) == null)
                    {
                        Debug.LogError("[探索] nodeType 缺少 ID " + typeId + "（对应 GameType.NodeType）");
                        kErrors++;
                    }
                }
            }

            if (kTables.zoneNodeWeight == null || kTables.zoneNodeWeight.DataList.Count == 0)
            {
                Debug.LogWarning("[探索] zoneNodeWeight 表为空" +
                    (ExploreConst.TestAllBattle ? "（当前是全战斗测试模式，不影响）" : "，节点类型将全部走默认权重"));
            }

            Debug.Log("[探索] 配置校验完成，errors = " + kErrors);
        }

        [MenuItem("Framework/探索/撤离（结束调查）", false, 303)]
        public static void Evacuate()
        {
            if (!GameConst.m_GameStart)
            {
                Debug.LogError("[探索] 请先运行游戏再点这个菜单（GameConst.m_GameStart = false）");
                return;
            }

            ExploreSystem kExplore = Explore;
            if (kExplore == null || !kExplore.HasRun())
            {
                Debug.LogWarning("[探索] 当前没有进行中的调查");
                return;
            }

            kExplore.Evacuate(true);
        }
    }
}
#endif
