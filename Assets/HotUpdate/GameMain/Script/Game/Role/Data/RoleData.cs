using FairyGUI;
using Framework.Script.Data;
using GameMain.Script.UI;

namespace GameMain.Script.Data
{
    /// <summary>
    /// Role 包的界面类注册。
    ///
    /// 与 CommandRoomData / BaseData 一样，包内组件必须在这里用
    /// UIObjectFactory.SetPackageItemExtension 绑定 C# 类，UIPanel 创建内容时
    /// 才会实例化对应的 GComponent 子类；否则只是普通 GComponent，取不到 SetCom。
    /// </summary>
    public class RoleData : GameData
    {
        protected internal override void Init()
        {
            base.Init();

            UIObjectFactory.SetPackageItemExtension("ui://Role/RoleInfoCom", typeof(RoleInfoCom));
            UIObjectFactory.SetPackageItemExtension("ui://Role/RoleSkillCom", typeof(RoleSkillCom));
            UIObjectFactory.SetPackageItemExtension("ui://Role/RoleAttrCom", typeof(RoleAttrCom));
            UIObjectFactory.SetPackageItemExtension("ui://Role/RoleTitleCom", typeof(RoleTitleCom));
            UIObjectFactory.SetPackageItemExtension("ui://Role/RoleTabBtn", typeof(RoleTabBtn));
        }
    }
}
