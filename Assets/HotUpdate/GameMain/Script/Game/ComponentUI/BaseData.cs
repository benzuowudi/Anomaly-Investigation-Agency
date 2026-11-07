using FairyGUI;
using Framework.Script.Data;
using GameMain.Script.UI;

namespace GameMain.Script.Data
{
    public class BaseData : GameData
    {
        protected internal override void Init()
        {
            base.Init();
            UIObjectFactory.SetPackageItemExtension("ui://Base/HeadCom", typeof(HeadCom));
            UIObjectFactory.SetPackageItemExtension("ui://Base/CommonPosCom", typeof(CommonPosCom));
            UIObjectFactory.SetPackageItemExtension("ui://Base/CommonSkillIconCom", typeof(CommonSkillIconCom));
            UIObjectFactory.SetPackageItemExtension("ui://Base/RoleCardCom", typeof(RoleCardCom));
        }
    }
}
