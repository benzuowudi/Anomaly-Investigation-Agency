using System.Collections.Generic;
using FairyGUI;
using Framework.Script.UI;
using GameMain.Script.Role;
using UnityEngine;

namespace GameMain.Script.UI
{
    /// <summary>
    /// 角色主界面（FairyGUI 包 Role / RoleBaseView）。
    ///
    /// 结构：
    ///   btn_back   返回指挥室
    ///   list_tab   左侧页签（RoleTabBtn），按“详情/培养/战技/武器/勋章/装备/时装/道具”初始化
    ///   com_info   详情页（RoleInfoCom），数据来自 RoleSystem.GetRoleData(主角色)
    ///
    /// 页签内容区约定：详情页固定是 com_info，其它页签的组件命名为
    /// com_grow / com_skill / com_weapon / com_medal / com_equip / com_fashion / com_item。
    /// 组件还没做时显示占位提示，后续在 FGUI 里补上同名组件即可自动接管，不需要改这里。
    /// </summary>
    public class RoleBaseView : UIWindow
    {
        /// <summary>页签索引，必须与 c_TabTitleCn / c_PageComNames 的顺序一致</summary>
        private enum RolePage
        {
            Detail = 0,
            Nourish = 1,
            Skill = 2,
            Weapon = 3,
            Medal = 4,
            Equip = 5,
            Fashion = 6,
            Item = 7,
        }

        private static readonly string[] c_TabTitleCn =
        {
            "详情", "培养", "战技", "武器", "勋章", "装备", "时装", "道具",
        };

        private static readonly string[] c_TabTitleEn =
        {
            "DETAIL", "NOURISH", "SKILL", "WEAPON", "MEDAL", "EQUIP", "FASHION", "ITEM",
        };

        /// <summary>页签对应的内容组件名（详情页就是 com_info）</summary>
        private static readonly string[] c_PageComNames =
        {
            "com_info", "com_grow", "com_skill", "com_weapon", "com_medal", "com_equip", "com_fashion", "com_item",
        };

        /// <summary>页签对应的 RoleSystemData 界面状态名（沿用原 Role 模块的命名）</summary>
        private static readonly string[] c_PageStateNames =
        {
            "RoleInfoView", "RoleGrowView", "RoleSkillView", "RoleWeaponView",
            "RoleMedalView", "RoleEquipView", "RoleFashionView", "RoleItemView",
        };

        private GButton btn_back;
        private GList list_tab;
        private RoleInfoCom com_info;
        private GTextField txt_placeholder;

        private RolePage m_CurrentPage = RolePage.Detail;
        private int m_RoleId;
        private bool m_HasRole;
        private bool m_Subscribed;

        protected internal override void Init(object o)
        {
            btn_back = ui.GetChild("btn_back").asButton;
            list_tab = ui.GetChild("list_tab").asList;
            com_info = ui.GetChild("com_info") as RoleInfoCom;

            CreatePlaceholder();

            // 节点绑定完成后再触发 AddEvent
            base.Init(o);
            InitTabs();
        }

        protected internal override void Enter(object o)
        {
            base.Enter(o);
            m_RoleId = ResolveRoleId(o);
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

        #region 页签

        private void InitTabs()
        {
            list_tab.RemoveChildrenToPool();
            for (int i = 0; i < c_TabTitleCn.Length; i++)
            {
                RoleTabBtn kBtn = list_tab.AddItemFromPool() as RoleTabBtn;
                kBtn.SetTab(c_TabTitleCn[i], c_TabTitleEn[i], false);
            }

            if (list_tab.selectionMode == ListSelectionMode.None)
            {
                list_tab.GetChildAt(0).asButton.selected = true;
            }
            else
            {
                list_tab.selectedIndex = (int)RolePage.Detail;
            }

            ShowPage(RolePage.Detail);
        }

        private void OnClickTab(EventContext _context)
        {
            GObject kItem = _context.data as GObject;
            int kIndex = list_tab.GetChildIndex(kItem);
            if (kIndex < 0 || kIndex >= c_TabTitleCn.Length)
            {
                return;
            }

            if (list_tab.selectionMode == ListSelectionMode.None)
            {
                for (int i = 0; i < list_tab.numItems; i++)
                {
                    list_tab.GetChildAt(i).asButton.selected = i == kIndex;
                }
            }
            else
            {
                list_tab.selectedIndex = kIndex;
            }

            ShowPage((RolePage)kIndex);
        }

        private void ShowPage(RolePage _page)
        {
            m_CurrentPage = _page;

            for (int i = 0; i < c_PageComNames.Length; i++)
            {
                GComponent kCom = GetPageCom(i);
                if (kCom != null)
                {
                    kCom.visible = i == (int)_page;
                }
            }

            GComponent kCurrent = GetPageCom((int)_page);
            if (_page == RolePage.Detail && !m_HasRole)
            {
                ShowPlaceholder("暂无角色");
            }
            else if (kCurrent != null)
            {
                HidePlaceholder();
            }
            else if (_page == RolePage.Detail)
            {
                ShowPlaceholder("详情界面缺失");
            }
            else
            {
                ShowPlaceholder("「" + c_TabTitleCn[(int)_page] + "」界面制作中");
            }

            RoleSystemData kData = Game.Data.GetGameData<RoleSystemData>();
            kData.SetCurrentType(c_PageStateNames[(int)_page]);
        }

        private GComponent GetPageCom(int _index)
        {
            if (_index == (int)RolePage.Detail)
            {
                return com_info;
            }

            return ui.GetChild(c_PageComNames[_index]) as GComponent;
        }

        #endregion

        #region 数据

        private void Refresh()
        {
            RoleRecord kRole = m_RoleId > 0 ? RoleSystem.GetRole(m_RoleId) : null;
            if (kRole == null)
            {
                kRole = RoleSystem.GetMainRole();
            }
            if (kRole == null)
            {
                List<RoleRecord> kAll = RoleSystem.GetAllRoles();
                if (kAll.Count > 0)
                {
                    kRole = kAll[0];
                }
            }

            if (kRole == null)
            {
                m_RoleId = 0;
                m_HasRole = false;
                com_info.Clear();
                if (m_CurrentPage == RolePage.Detail)
                {
                    ShowPlaceholder("暂无角色");
                }
                return;
            }

            m_HasRole = true;
            m_RoleId = kRole.m_Id;
            RoleSnapshot kSnapshot = RoleSystem.GetRoleData(m_RoleId);
            com_info.SetCom(kSnapshot);

            if (m_CurrentPage == RolePage.Detail)
            {
                ShowPage(RolePage.Detail);
            }
        }

        private static int ResolveRoleId(object _o)
        {
            if (_o is int)
            {
                int kId = (int)_o;
                if (RoleSystem.HasRole(kId))
                {
                    return kId;
                }
            }

            RoleRecord kRecord = _o as RoleRecord;
            if (kRecord != null && RoleSystem.HasRole(kRecord.m_Id))
            {
                return kRecord.m_Id;
            }

            return RoleSystem.GetMainRoleId();
        }

        private void Subscribe()
        {
            if (m_Subscribed)
            {
                return;
            }

            Game.Event.Subscribe(RoleEventDefine.OnRoleChanged, OnRoleChanged);
            Game.Event.Subscribe(RoleEventDefine.OnRolesChanged, OnRolesChanged);
            Game.Event.Subscribe(RoleEventDefine.OnMainRoleChanged, OnMainRoleChanged);
            m_Subscribed = true;
        }

        private void UnSubscribe()
        {
            if (!m_Subscribed)
            {
                return;
            }

            Game.Event.UnSubscribe(RoleEventDefine.OnRoleChanged, OnRoleChanged);
            Game.Event.UnSubscribe(RoleEventDefine.OnRolesChanged, OnRolesChanged);
            Game.Event.UnSubscribe(RoleEventDefine.OnMainRoleChanged, OnMainRoleChanged);
            m_Subscribed = false;
        }

        private void OnRoleChanged(object _o)
        {
            RoleChangedMsg kMsg = _o as RoleChangedMsg;
            if (m_RoleId <= 0 || kMsg.m_RoleId == m_RoleId)
            {
                Refresh();
            }
        }

        private void OnRolesChanged(object _o)
        {
            Refresh();
        }

        private void OnMainRoleChanged(object _o)
        {
            if (m_RoleId <= 0)
            {
                Refresh();
            }
        }

        #endregion

        #region 占位 / 返回

        private void CreatePlaceholder()
        {
            txt_placeholder = new GTextField();
            txt_placeholder.name = "txt_role_placeholder";
            txt_placeholder.touchable = false;
            txt_placeholder.SetSize(1000f, 200f);
            txt_placeholder.SetXY(420f, 400f);

            TextFormat kFormat = new TextFormat();
            kFormat.size = 48;
            kFormat.color = new Color(0.45f, 0.44f, 0.47f, 1f);
            kFormat.align = AlignType.Center;
            txt_placeholder.textFormat = kFormat;
            txt_placeholder.verticalAlign = VertAlignType.Middle;

            ui.AddChild(txt_placeholder);
            txt_placeholder.visible = false;
        }

        private void ShowPlaceholder(string _text)
        {
            txt_placeholder.text = _text;
            txt_placeholder.visible = true;
        }

        private void HidePlaceholder()
        {
            txt_placeholder.visible = false;
        }

        private void OnClickBack()
        {
            // 返回角色列表；列表再返回指挥室
            Game.UI.ShowWindow(UIDefine.RoleList);
        }

        #endregion

        #region 事件绑定

        protected internal override void AddEvent()
        {
            btn_back.onClick.Add(OnClickBack);
            list_tab.onClickItem.Add(OnClickTab);
        }

        protected internal override void RemoveEvent()
        {
            btn_back.onClick.Remove(OnClickBack);
            list_tab.onClickItem.Remove(OnClickTab);
        }

        #endregion
    }
}
