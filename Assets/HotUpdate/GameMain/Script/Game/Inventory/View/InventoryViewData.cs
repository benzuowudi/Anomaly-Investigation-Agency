using FairyGUI;
using Framework.Script.Data;
using GameMain.Script.Inventory;

namespace GameMain.Script.UI
{
    /// <summary>
    /// 仓库（FairyGUI 包 Inventory）的包注册 + 界面状态。
    ///
    /// 只放不落盘的东西：
    /// - 包内页签按钮的 C# 类绑定；
    /// - 当前页签（道具 / 装备）。
    ///
    /// 道具的权威数据在 InventoryData，不要把这些状态混进系统 Data（规范 §5.5）。
    /// </summary>
    public class InventoryViewData : GameData
    {
        /// <summary>当前页签（重新打开界面时恢复）</summary>
        public InventoryTab currentTab = InventoryTab.Item;

        protected internal override void Init()
        {
            base.Init();
            currentTab = InventoryTab.Item;

            UIObjectFactory.SetPackageItemExtension("ui://Inventory/InventoryTabBtn", typeof(InventoryTabBtn));
        }

        protected internal override void Destroy()
        {
            currentTab = InventoryTab.Item;
        }
    }
}
