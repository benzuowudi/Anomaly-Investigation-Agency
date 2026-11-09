#if UNITY_EDITOR
using cfg.GameType;
using Framework.Script.Constant;
using UnityEditor;
using UnityEngine;

namespace GameMain.Script.Battle
{
    /// <summary>
    /// 战斗系统的编辑器调试入口（只在编辑器里编译，打包时整段剔除）。
    ///
    /// "跑一场"要求先运行游戏（模型由 ModelManager 注册之后才存在）；
    /// 会走调试编成，不依赖 encounter / enemy 表的数据，用来验证单位生成与后续内核。
    /// </summary>
    public static class BattleDebug
    {
        private static BattleSystem Battle
        {
            get { return Game.Model.Get<BattleSystem>(); }
        }

        [MenuItem("Framework/战斗/跑一场 4v4（Play 模式）", false, 200)]
        public static void StartDebugBattle()
        {
            if (!GameConst.m_GameStart)
            {
                Debug.LogError("[战斗] 请先运行游戏再点这个菜单（GameConst.m_GameStart = false）");
                return;
            }

            BattleSystem kBattle = Battle;
            if (kBattle == null)
            {
                return;
            }

            kBattle.StartDebugBattle(4, 4);
        }

        [MenuItem("Framework/战斗/打印当前单位", false, 201)]
        public static void DumpUnits()
        {
            if (!GameConst.m_GameStart)
            {
                Debug.LogError("[战斗] 请先运行游戏再点这个菜单（GameConst.m_GameStart = false）");
                return;
            }

            BattleSystem kBattle = Battle;
            if (kBattle == null)
            {
                return;
            }

            Debug.Log("[战斗] 阶段：" + kBattle.GetPhase() + " 单位数：" + kBattle.GetUnits().Count);
            kBattle.DumpUnits();
        }

        [MenuItem("Framework/战斗/推进 10 秒", false, 203)]
        public static void TickTenSeconds()
        {
            if (!GameConst.m_GameStart)
            {
                Debug.LogError("[战斗] 请先运行游戏再点这个菜单（GameConst.m_GameStart = false）");
                return;
            }

            BattleSystem kBattle = Battle;
            if (kBattle == null)
            {
                return;
            }

            for (int i = 0; i < 10; i++)
            {
                kBattle.Tick(1f);
            }

            Debug.Log("[战斗] 推进 10 秒后 elapsed=" + kBattle.GetElapsed().ToString("F1")
                + " 阶段=" + kBattle.GetPhase());
            kBattle.DumpUnits();
        }

        [MenuItem("Framework/战斗/推进 30 秒", false, 204)]
        public static void TickThirtySeconds()
        {
            if (!GameConst.m_GameStart)
            {
                Debug.LogError("[战斗] 请先运行游戏再点这个菜单（GameConst.m_GameStart = false）");
                return;
            }

            BattleSystem kBattle = Battle;
            if (kBattle == null)
            {
                return;
            }

            for (int i = 0; i < 30; i++)
            {
                kBattle.Tick(1f);
            }

            Debug.Log("[战斗] 推进 30 秒后 elapsed=" + kBattle.GetElapsed().ToString("F1")
                + " 阶段=" + kBattle.GetPhase());
            kBattle.DumpUnits();
        }

        [MenuItem("Framework/战斗/跑到结束（最多 600 秒）", false, 205)]
        public static void RunToEnd()
        {
            if (!GameConst.m_GameStart)
            {
                Debug.LogError("[战斗] 请先运行游戏再点这个菜单（GameConst.m_GameStart = false）");
                return;
            }

            BattleSystem kBattle = Battle;
            if (kBattle == null)
            {
                return;
            }

            if (!kBattle.IsFighting())
            {
                kBattle.StartDebugBattle(4, 4);
            }

            int kSecond = 0;
            while (!kBattle.IsFinished() && kSecond < 600)
            {
                kBattle.Tick(1f);
                kSecond++;
            }

            Debug.Log("[战斗] 跑到结束：推进 " + kSecond + " 秒，阶段=" + kBattle.GetPhase());
            DumpResult();
        }

        [MenuItem("Framework/战斗/打印战斗结果", false, 206)]
        public static void DumpResult()
        {
            if (!GameConst.m_GameStart)
            {
                Debug.LogError("[战斗] 请先运行游戏再点这个菜单（GameConst.m_GameStart = false）");
                return;
            }

            BattleSystem kBattle = Battle;
            if (kBattle == null)
            {
                return;
            }

            BattleResult kResult = kBattle.GetLastResult();
            if (kResult == null)
            {
                Debug.Log("[战斗] 还没有战斗结果");
                return;
            }

            Debug.Log("[战斗] 结果：" + kResult);
            for (int i = 0; i < kResult.m_Units.Count; i++)
            {
                Debug.Log("[战斗]   单位 " + kResult.m_Units[i]);
            }
        }

        [MenuItem("Framework/战斗/指令-集火首个敌人", false, 210)]
        public static void CommandFocus()
        {
            BattleSystem kBattle = Battle;
            if (!CanCommand(kBattle))
            {
                return;
            }

            BattleUnit kEnemy = FindFirstEnemy(kBattle);
            if (kEnemy == null)
            {
                Debug.LogWarning("[战斗] 没有可集火的敌人");
                return;
            }

            BattleCommandResult kResult = kBattle.IssueCommand(BattleCommandId.Focus, kEnemy.m_UnitId);
            Debug.Log("[战斗] 集火 " + kEnemy.m_UnitId + " 结果：" + kResult);
        }

        [MenuItem("Framework/战斗/指令-全体撤离", false, 211)]
        public static void CommandRetreat()
        {
            BattleSystem kBattle = Battle;
            if (!CanCommand(kBattle))
            {
                return;
            }

            BattleCommandResult kResult = kBattle.IssueCommand(BattleCommandId.Retreat);
            Debug.Log("[战斗] 全体撤离结果：" + kResult);
        }

        [MenuItem("Framework/战斗/指令-自由行动", false, 212)]
        public static void CommandFree()
        {
            BattleSystem kBattle = Battle;
            if (!CanCommand(kBattle))
            {
                return;
            }

            BattleCommandResult kResult = kBattle.IssueCommand(BattleCommandId.Free);
            Debug.Log("[战斗] 自由行动结果：" + kResult);
        }

        private static bool CanCommand(BattleSystem _battle)
        {
            if (!GameConst.m_GameStart)
            {
                Debug.LogError("[战斗] 请先运行游戏再点这个菜单（GameConst.m_GameStart = false）");
                return false;
            }
            if (_battle == null || !_battle.IsFighting())
            {
                Debug.LogWarning("[战斗] 当前没有进行中的战斗");
                return false;
            }
            return true;
        }

        private static BattleUnit FindFirstEnemy(BattleSystem _battle)
        {
            System.Collections.Generic.IReadOnlyList<BattleUnit> kUnits = _battle.GetUnits();
            for (int i = 0; i < kUnits.Count; i++)
            {
                BattleUnit kUnit = kUnits[i];
                if (kUnit.m_Faction == Faction.enemy && BattleField.IsActive(kUnit))
                {
                    return kUnit;
                }
            }
            return null;
        }

        [MenuItem("Framework/战斗/打印配置常量", false, 202)]
        public static void DumpConfig()
        {
            if (!GameConst.m_GameStart)
            {
                Debug.LogError("[战斗] 请先运行游戏再点这个菜单（GameConst.m_GameStart = false）");
                return;
            }

            BattleSystem kBattle = Battle;
            if (kBattle == null)
            {
                return;
            }

            BattleConfig kConfig = kBattle.GetConfig();
            Debug.Log("[战斗] 常量条数：" + kConfig.GetCount()
                + " dodgePerAgi:" + kConfig.DodgePerAgi
                + " dodgeCap:" + kConfig.DodgeCap
                + " downedCountdown:" + kConfig.DownedCountdown
                + " moveSpeedBase:" + kConfig.MoveSpeedBase
                + " atbFillBase:" + kConfig.AtbFillBase);
        }
    }
}
#endif
