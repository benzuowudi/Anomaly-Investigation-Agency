using System;
using System.Collections.Generic;
using FairyGUI;
using Framework.Script.UI;
using GameMain.Script.Role;

namespace GameMain.Script.UI
{
    /// <summary>
    /// 选择角色的弹窗上下文（UIManager.ShowPopup 的 o 参数）。
    /// </summary>
    public class RoleListSelectContext
    {
        /// <summary>选中后的回调（参数：角色 ID）</summary>
        public Action<int> m_OnSelected;

        /// <summary>不出现在列表里的角色（已经在队伍里的人）</summary>
        public List<int> m_Exclude;
    }

    /// <summary>
    /// 角色选择弹窗（FairyGUI 包 Role / RoleListWindow，按 UIPopup 规范）。
    ///
    /// 用途：战前整备里点空队员格，弹出它挑一名角色。
    /// 行为：点卡片 → 先关闭自己 → 回调把角色加进队伍（BattleSetting 不改这里的数据）。
    ///
    /// 打开方式：
    ///   RoleListWindow.Open(_roleId => ..., 已选角色列表);
    /// </summary>
    public class RoleListWindow : UIPopup
    {
        private GList list_role;

        private RoleListSelectContext m_Context;

        /// <summary>打开选择弹窗</summary>
        /// <param name="_onSelected">选中回调</param>
        /// <param name="_exclude">不显示的角色（已在队伍里）</param>
        public static RoleListWindow Open(Action<int> _onSelected, IList<int> _exclude = null)
        {
            RoleListSelectContext kContext = new RoleListSelectContext();
            kContext.m_OnSelected = _onSelected;
            if (_exclude != null)
            {
                kContext.m_Exclude = new List<int>(_exclude);
            }

            return Game.UI.ShowPopup(UIDefine.RoleListWindow, kContext) as RoleListWindow;
        }

        protected internal override void Init(object o)
        {
            list_role = ui.GetChild("list_role").asList;
            list_role.onClickItem.Add(OnClickItem);

            base.Init(o);
        }

        protected internal override void Enter(object o)
        {
            base.Enter(o);
            m_Context = o as RoleListSelectContext;
            Refresh();
        }

        protected internal override void Leave()
        {
            m_Context = null;
            base.Leave();
        }

        private void Refresh()
        {
            List<RoleRecord> kRoles = Game.Model.Get<RoleSystem>().GetAllRoles();

            list_role.RemoveChildrenToPool();
            for (int i = 0; i < kRoles.Count; i++)
            {
                RoleRecord kRole = kRoles[i];
                if (IsExcluded(kRole.m_Id))
                {
                    continue;
                }

                RoleCardCom kCard = list_role.AddItemFromPool() as RoleCardCom;
                kCard.SetCom(kRole);
                kCard.data = kRole.m_Id;
            }
        }

        private bool IsExcluded(int _roleId)
        {
            if (m_Context == null || m_Context.m_Exclude == null)
            {
                return false;
            }

            return m_Context.m_Exclude.Contains(_roleId);
        }

        private void OnClickItem(EventContext _context)
        {
            GObject kItem = _context.data as GObject;
            if (kItem == null || kItem.data == null)
            {
                return;
            }

            int kRoleId = (int)kItem.data;
            Action<int> kOnSelected = m_Context == null ? null : m_Context.m_OnSelected;

            // 先关自己，再回调：回调里通常要刷新下面的界面，避免弹窗盖在上面
            Close();

            if (kOnSelected != null)
            {
                kOnSelected(kRoleId);
            }
        }
    }
}
