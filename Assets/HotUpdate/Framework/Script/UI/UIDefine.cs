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

        /// <summary>角色界面（预制体 RoleBaseView，内容包 Role/RoleBaseView）</summary>
        public const string RoleBase = "RoleBaseView";
        public const string GameStart = "GameStartView";
    }
}
