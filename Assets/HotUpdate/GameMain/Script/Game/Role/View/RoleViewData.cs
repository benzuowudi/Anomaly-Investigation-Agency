using FairyGUI;
using Framework.Script.Data;
using Framework.Script.UI;

namespace GameMain.Script.UI
{
    /// <summary>
    /// 角色系统的 ViewData：FairyGUI 包注册 + 界面状态（当前页签）。
    ///
    /// 职责边界：
    /// - 包内组件必须在这里用 UIObjectFactory.SetPackageItemExtension 绑定 C# 类，
    ///   UIPanel 创建内容时才会实例化对应的 GComponent 子类；否则只是普通 GComponent，取不到 SetCom；
    /// - 界面状态只在本系统界面之间共享，不落盘，也不属于 RoleData 的业务数据；
    /// - 由 DataManager.LoadGameData() 注册，必须早于任何角色界面创建。
    /// </summary>
    public class RoleViewData : GameData
    {
        /// <summary>界面状态（角色界面内当前页签）</summary>
        private string m_UIState = "RoleInfoView";

        protected internal override void Init()
        {
            base.Init();
            m_UIState = "RoleInfoView";

            UIObjectFactory.SetPackageItemExtension("ui://Role/RoleInfoCom", typeof(RoleInfoCom));
            UIObjectFactory.SetPackageItemExtension("ui://Role/RoleSkillCom", typeof(RoleSkillCom));
            UIObjectFactory.SetPackageItemExtension("ui://Role/RoleAttrCom", typeof(RoleAttrCom));
            UIObjectFactory.SetPackageItemExtension("ui://Role/RoleTitleCom", typeof(RoleTitleCom));
            UIObjectFactory.SetPackageItemExtension("ui://Role/RoleTabBtn", typeof(RoleTabBtn));
        }

        protected internal override void Destroy()
        {
            m_UIState = "RoleInfoView";
        }

        /// <summary>
        /// 切换角色界面内的子页签，并把对应界面显示出来
        /// </summary>
        public void ChangeUIState(string _type, object _obj = null)
        {
            if (_type != m_UIState)
            {
                m_UIState = _type;
                Game.UI.ShowWindow(_type, UIDefine.RoleBase, _obj, false);
            }
        }

        public string GetCurrentType()
        {
            return m_UIState;
        }

        public void SetCurrentType(string _type)
        {
            m_UIState = _type;
        }
    }
}
