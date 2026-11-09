#if UNITY_EDITOR
using System.Collections.Generic;
using Framework.Script.Constant;
using UnityEditor;
using UnityEngine;

namespace GameMain.Script.Explore
{
    /// <summary>
    /// 探索系统的编辑器调试入口（只在编辑器里编译）。
    ///
    /// 用法：先运行游戏 → 开始一次调查 → 反复"走到下一个节点"，遇到战斗节点会自动开打，
    /// 战斗结束（或玩家在战斗中下达撤离）后回到节点网继续走。
    /// </summary>
    public static class ExploreDebug
    {
        private static ExploreSystem Explore
        {
            get { return Game.Model.Get<ExploreSystem>(); }
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
                        + "/" + kNode.m_State + "]";
                }
                Debug.Log(kLine);
            }
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
