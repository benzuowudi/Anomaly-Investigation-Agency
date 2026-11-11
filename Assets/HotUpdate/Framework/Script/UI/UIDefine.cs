namespace Framework.Script.UI
{
    /// <summary>
    /// 界面名常量。界面名同时就是界面预制体的 Addressables 地址（默认同名）。
    /// 界面本体是 UGUI 预制体（挂在 DontDestroy 下 Canvas 的层节点上），
    /// 预制体里的 FairyGUI UIPanel 负责渲染 GComponent 内容。
    /// 新增界面：预制体放进 Assets/Res/Prefab 并注册进 Addressables，然后在这里加一个常量。
    /// </summary>
    public static class UIDefine
    {
        /// <summary>背景界面（预制体 BackGroundView，内容包 Base/BackGroundVIew）</summary>
        public const string BackGround = "BackGroundView";

        /// <summary>前景界面（预制体 ForeGroundView，内容包 Base/ForeGroundView）</summary>
        public const string ForeGround = "ForeGroundView";

        /// <summary>指挥室界面（预制体 CommandBaseView，内容包 CommandRoom/CommandBaseView）</summary>
        public const string CommandRoom = "CommandBaseView";

        /// <summary>人才招募（预制体 TalentAcquisition，内容包 TalentAcquisition/TalentAcquisition）</summary>
        public const string TalentAcquisition = "TalentAcquisition";

        /// <summary>人才招募结果（预制体 TalentAcquisitionResult，内容包 TalentAcquisition/TalentAcquisitionResultView）</summary>
        public const string TalentAcquisitionResult = "TalentAcquisitionResult";

        /// <summary>角色列表（预制体 RoleListView，内容包 Role/RoleListView）</summary>
        public const string RoleList = "RoleListView";

        /// <summary>角色选择弹窗（预制体 RoleListWindow，内容包 Role/RoleListWindow）</summary>
        public const string RoleListWindow = "RoleListWindow";

        /// <summary>角色界面（预制体 RoleBaseView，内容包 Role/RoleBaseView）</summary>
        public const string RoleBase = "RoleBaseView";

        /// <summary>战前整备（预制体 BattleSettingView，内容包 BattleSetting/BattleSettingView）</summary>
        public const string BattleSetting = "BattleSettingView";

        /// <summary>探索场景（预制体 ExploreView，内容包 World/ExploreView）</summary>
        public const string Explore = "ExploreView";

        /// <summary>战斗节点布阵（预制体 NodeSettingView，内容包 World/NodeSettingView）</summary>
        public const string NodeSetting = "NodeSettingView";

        /// <summary>战斗画面（预制体 BattleView，内容包 Battle/BattleView）</summary>
        public const string Battle = "BattleView";

        /// <summary>探索结算 · 成功（预制体 ExploreSuccessWindow，内容包 World/ExploreSuccessWindow）</summary>
        public const string ExploreSuccess = "ExploreSuccessWindow";

        /// <summary>探索结算 · 失败（预制体 ExploreFailWindow，内容包 World/ExploreFailWindow）</summary>
        public const string ExploreFail = "ExploreFailWindow";

        /// <summary>军械库（预制体 ArmoryView，内容包 Armory/ArmoryView）</summary>
        public const string Armory = "ArmoryView";

        /// <summary>仓库（预制体 InventoryView，内容包 Inventory/InventoryView）</summary>
        public const string Inventory = "InventoryView";

        public const string GameStart = "GameStartView";
    }
}
