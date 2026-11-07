using System.Collections.Generic;
using FairyGUI;
using Framework.Script.UI;
using GameMain.Script.Role;

namespace GameMain.Script.UI
{
    /// <summary>
    /// 角色列表界面（FairyGUI 包 Role / RoleListView）。
    ///
    /// 结构：
    ///   btn_back    返回按钮（CommonBackBtn）
    ///   n1          背景
    ///   list_role   角色卡列表（RoleCardCom，Base 包）
    ///
    /// 列表数据来自 RoleSystem.GetAllRoles()；点击卡片打开 RoleBaseView 并传入角色 ID。
    /// </summary>
    public class RoleListView : UIWindow
    {
        private GButton btn_back;
        private GList list_role;

        private bool m_Subscribed;

        protected internal override void Init(object o)
        {
            btn_back = ui.GetChild("btn_back").asButton;
            list_role = ui.GetChild("list_role").asList;

            base.Init(o);
        }

        protected internal override void AddEvent()
        {
            btn_back.onClick.Add(OnClickBack);
            list_role.onClickItem.Add(OnClickItem);
        }

        protected internal override void RemoveEvent()
        {
            btn_back.onClick.Remove(OnClickBack);
            list_role.onClickItem.Remove(OnClickItem);
        }

        protected internal override void Enter(object o)
        {
            base.Enter(o);
            Subscribe();
            Refresh();
        }

        protected internal override void Leave()
        {
            UnSubscribe();
            base.Leave();
        }

        protected internal override void Destroy()
        {
            UnSubscribe();
            base.Destroy();
        }

        private void Refresh()
        {
            List<RoleRecord> kRoles = RoleSystem.GetAllRoles();

            list_role.RemoveChildrenToPool();
            for (int i = 0; i < kRoles.Count; i++)
            {
                RoleCardCom kCard = list_role.AddItemFromPool() as RoleCardCom;
                kCard.SetCom(kRoles[i]);
                kCard.data = kRoles[i].m_Id;
            }
        }

        private void OnClickItem(EventContext _context)
        {
            GObject kItem = _context.data as GObject;
            int kRoleId = (int)kItem.data;

            Game.UI.ShowWindow(UIDefine.RoleBase, null, kRoleId, true);
        }

        private void OnClickBack()
        {
            Game.UI.ShowWindow(UIDefine.CommandRoom);
        }

        private void Subscribe()
        {
            if (m_Subscribed)
            {
                return;
            }

            Game.Event.Subscribe(RoleEventDefine.OnRolesChanged, OnRolesChanged);
            Game.Event.Subscribe(RoleEventDefine.OnRoleChanged, OnRoleChanged);
            m_Subscribed = true;
        }

        private void UnSubscribe()
        {
            if (!m_Subscribed)
            {
                return;
            }

            Game.Event.UnSubscribe(RoleEventDefine.OnRolesChanged, OnRolesChanged);
            Game.Event.UnSubscribe(RoleEventDefine.OnRoleChanged, OnRoleChanged);
            m_Subscribed = false;
        }

        private void OnRolesChanged(object _o)
        {
            Refresh();
        }

        private void OnRoleChanged(object _o)
        {
            Refresh();
        }
    }
}
