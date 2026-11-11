using FairyGUI;
using Framework.Script.Data;

namespace GameMain.Script.UI
{
    /// <summary>
    /// 探索界面（FairyGUI 包 World）的包注册 + 界面状态。
    ///
    /// 只放不落盘的东西：包内组件绑定、当前选中的节点。
    /// 权威数据在 ExploreData / ExploreSystem，界面状态不要混进去。
    /// </summary>
    public class ExploreViewData : GameData
    {
        /// <summary>当前选中的节点 ID（0 = 没选）</summary>
        public int selectedNodeId;

        protected internal override void Init()
        {
            base.Init();

            selectedNodeId = 0;

            UIObjectFactory.SetPackageItemExtension("ui://World/ExploreNodeCom", typeof(ExploreNodeCom));
            UIObjectFactory.SetPackageItemExtension("ui://World/NodeBattleCom", typeof(NodeBattleCom));
            UIObjectFactory.SetPackageItemExtension("ui://World/NodeInvestigationCom", typeof(NodeInvestigationCom));
            UIObjectFactory.SetPackageItemExtension("ui://World/InvestigationCom", typeof(InvestigationCom));
        }
    }
}
