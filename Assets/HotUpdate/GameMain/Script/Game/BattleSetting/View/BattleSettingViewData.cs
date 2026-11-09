using FairyGUI;
using Framework.Script.Data;

namespace GameMain.Script.UI
{
    /// <summary>
    /// 战前整备（FairyGUI 包 BattleSetting）的包注册 + 界面状态。
    ///
    /// 只放不落盘的东西：
    /// - FairyGUI 包内组件的 C# 类绑定；
    /// - 界面交互状态（当前选中的队员 / 物资格）。
    ///
    /// 权威数据在 BattleSettingData（<see cref="GameMain.Script.BattleSetting.BattleSettingData"/>），
    /// 不要把这些状态混进系统 Data（规范 §5.5）。
    /// </summary>
    public class BattleSettingViewData : GameData
    {
        /// <summary>当前选中的队员 ID（0 = 没选）</summary>
        public int selectedRoleId;

        /// <summary>当前点中的物资格下标（-1 = 没选）</summary>
        public int selectedItemSlot = -1;

        protected internal override void Init()
        {
            base.Init();

            UIObjectFactory.SetPackageItemExtension("ui://BattleSetting/BattleSettingRoleCom", typeof(BattleSettingRoleCom));
            UIObjectFactory.SetPackageItemExtension("ui://BattleSetting/BattleSettingACCom", typeof(BattleSettingACCom));
            UIObjectFactory.SetPackageItemExtension("ui://BattleSetting/BattleSettingSupportCom", typeof(BattleSettingSupportCom));
            UIObjectFactory.SetPackageItemExtension("ui://BattleSetting/BattleSettingItemCom", typeof(BattleSettingItemCom));
        }
    }
}
