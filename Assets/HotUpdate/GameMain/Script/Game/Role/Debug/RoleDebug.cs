#if UNITY_EDITOR
using System.IO;
using Framework.Script.Constant;
using Framework.Script.UI;
using SimpleJSON;
using UnityEditor;
using UnityEngine;

namespace GameMain.Script.Role
{
    /// <summary>
    /// 角色系统的编辑器调试入口（只在编辑器里编译，打包时整段剔除）。
    ///
    /// "预览"系列不依赖游戏运行，直接从 StreamingAssets/Data 读一份配置表掷点，
    /// 用来快速验证 role.xlsx 填得对不对；"获得角色"系列要求先运行游戏。
    /// </summary>
    public static class RoleDebug
    {
        /// <summary>
        /// 当前运行中的角色模型；必须由 ModelManager 注册后才能取到。
        /// </summary>
        private static RoleSystem Role
        {
            get { return Game.Model.Get<RoleSystem>(); }
        }

        [MenuItem("Framework/角色系统/预览 5 个随机角色（不存档）", false, 100)]
        public static void PreviewRoles()
        {
            cfg.Tables kTables = LoadTables();
            if (kTables == null)
            {
                return;
            }

            for (int i = 1; i <= 5; i++)
            {
                // 固定种子，方便对比两次生成结果是否稳定
                RoleRecord kRole = RoleFactory.Roll(kTables, i, new System.Random(1000 + i));
                Debug.Log(RoleSystem.Describe(kRole, kTables));
            }
        }

        [MenuItem("Framework/角色系统/获得一个随机角色（写存档）", false, 101)]
        public static void AcquireRole()
        {
            if (!GameConst.m_GameStart)
            {
                Debug.LogError("[角色] 请先运行游戏再点这个菜单（GameConst.m_GameStart = false）");
                return;
            }

            RoleRecord kRole = Role.AcquireRole(RoleAcquireReason.Debug);
            if (kRole != null)
            {
                Debug.Log(RoleSystem.Describe(kRole));
                Debug.Log(Role.Dump());
            }
        }

        [MenuItem("Framework/角色系统/职称 +5（非任务途径）", false, 104)]
        public static void AddTitleScoreDaily()
        {
            AddTitleScoreToMain(5, false);
        }

        [MenuItem("Framework/角色系统/职称 +10（任务途径）", false, 105)]
        public static void AddTitleScoreTask()
        {
            AddTitleScoreToMain(10, true);
        }

        [MenuItem("Framework/角色系统/打开角色界面（运行中）", false, 107)]
        public static void OpenRoleView()
        {
            if (!GameConst.m_GameStart)
            {
                Debug.LogError("[角色] 请先运行游戏再点这个菜单（GameConst.m_GameStart = false）");
                return;
            }

            // 和指挥室平级：显示角色界面时，指挥室自动回收到对象池
            Game.UI.ShowWindow(UIDefine.RoleBase);
        }

        [MenuItem("Framework/角色系统/打印主角色整体数据", false, 106)]
        public static void DumpMainRoleData()
        {
            if (!GameConst.m_GameStart)
            {
                Debug.LogError("[角色] 请先运行游戏再点这个菜单（GameConst.m_GameStart = false）");
                return;
            }

            RoleSnapshot kSnapshot = Role.GetMainRoleData();
            if (kSnapshot == null)
            {
                Debug.LogWarning("[角色] 当前没有角色，先点“获得一个随机角色（写存档）”");
                return;
            }
            Debug.Log(RoleSystem.Describe(kSnapshot.m_Role));
        }

        private static void AddTitleScoreToMain(int _score, bool _isTask)
        {
            if (!GameConst.m_GameStart)
            {
                Debug.LogError("[角色] 请先运行游戏再点这个菜单（GameConst.m_GameStart = false）");
                return;
            }

            int kRoleId = Role.GetMainRoleId();
            if (kRoleId <= 0)
            {
                Debug.LogWarning("[角色] 当前没有主角色，先点“获得一个随机角色（写存档）”");
                return;
            }

            bool kOk = Role.AddTitleScore(kRoleId, _score, _isTask);
            Debug.Log("[角色] 加职称评分 " + _score + (kOk ? " 成功" : " 失败")
                + "，当前职称 " + Role.GetTitleName(kRoleId)
                + "（" + Role.GetTitleScore(kRoleId) + " 分）");
        }

        [MenuItem("Framework/角色系统/打印当前角色库", false, 102)]
        public static void DumpRoles()
        {
            if (!GameConst.m_GameStart)
            {
                Debug.LogError("[角色] 请先运行游戏再点这个菜单（GameConst.m_GameStart = false）");
                return;
            }
            Debug.Log(Role.Dump());
        }

        [MenuItem("Framework/角色系统/存档路径", false, 103)]
        public static void LogSavePath()
        {
            Debug.Log("[角色] 存档路径：" + RoleSystem.GetSavePath());
        }

        [MenuItem("Framework/角色系统/删除角色存档", false, 120)]
        public static void DeleteSave()
        {
            if (!GameConst.m_GameStart)
            {
                Debug.LogError("[角色] 请先运行游戏再点这个菜单（GameConst.m_GameStart = false）");
                return;
            }

            bool kOk = Role.DeleteSave();
            Debug.Log(kOk ? "[角色] 存档已删除" : "[角色] 没有可删除的存档");
        }

        /// <summary>
        /// 按 DataManager 的方式从 StreamingAssets/Data 读表，不需要游戏运行
        /// </summary>
        private static cfg.Tables LoadTables()
        {
            try
            {
                return new cfg.Tables(delegate (string _name)
                {
                    string kPath = string.Format(GameConst.m_DataJsonPath, _name);
                    return JSON.Parse(File.ReadAllText(kPath));
                });
            }
            catch (System.Exception _e)
            {
                Debug.LogError("[角色] 读取配置表失败：" + _e.Message);
                return null;
            }
        }
    }
}
#endif
