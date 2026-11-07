using FairyGUI;
using Framework.Script.Data;
using GameMain.Script.UI;

namespace GameMain.Script.Data
{
    public class CommandRoomData : GameData
    {
        protected internal override void Init()
        {
            base.Init();
            UIObjectFactory.SetPackageItemExtension("ui://CommandRoom/CommandPlayerCom", typeof(CommandPlayerCom));
            UIObjectFactory.SetPackageItemExtension("ui://CommandRoom/CommandRoleCom", typeof(CommandRoleCom));
            UIObjectFactory.SetPackageItemExtension("ui://CommandRoom/CommandCurrencyCOm", typeof(CommonCurrencyCom));
            UIObjectFactory.SetPackageItemExtension("ui://CommandRoom/CommandTACom", typeof(CommandTACom));
            UIObjectFactory.SetPackageItemExtension("ui://CommandRoom/CommandInventoryCom", typeof(CommandInventoryCom));
            UIObjectFactory.SetPackageItemExtension("ui://CommandRoom/CommandArmoryCom", typeof(CommandArmoryCom));
            UIObjectFactory.SetPackageItemExtension("ui://CommandRoom/CommandActionCom", typeof(CommandActionCom));
        }
    }
}