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
    ///   com_bg     通用背景框（Base/CommonBgCom），标题和返回按钮都在里面，返回事件由本界面传进去
    ///   list_tab   左侧页签（RoleTabBtn），按“详情/培养/战技/武器/勋章/装备/时装/道具”初始化
    ///   load_com   内容区 GLoader，按页签加载对应组件（详情 = Role/RoleInfoCom）
    ///
    /// 页签切换不重建界面，只把 load_com.url 换成对应组件 URL；
    /// 页签组件还没做的，c_PageComUrls 留空，显示占位文字。
    /// 新增页签组件时只要补 c_PageComUrls 里的 URL、再在 RefreshPageCom 里补一次数据填充。
    /// </summary>
    public class RoleBaseView : UIWindow
    {
        /// <summary>页签索引，必须与 c_TabTitleCn / c_PageComUrls / c_PageStateNames 的顺序一致</summary>
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

        /// <summary>页签对应的内容组件 URL，还没做的页签留空（显示占位）</summary>
        private static readonly string[] c_PageComUrls =
        {
            "ui://Role/RoleInfoCom",    // 详情
            string.Empty,               // 培养
            string.Empty,               // 战技
            string.Empty,               // 武器
            string.Empty,               // 勋章
            string.Empty,               // 装备
            string.Empty,               // 时装
            string.Empty,               // 道具
        };

        /// <summary>页签对应的 RoleViewData 界面状态名（沿用原 Role 模块的命名）</summary>
        private static readonly string[] c_PageStateNames =
        {
            "RoleInfoView", "RoleGrowView", "RoleSkillView", "RoleWeaponView",
            "RoleMedalView", "RoleEquipView", "RoleFashionView", "RoleItemView",
        };

        /// <summary>角色模型（由 ModelManager 注册，界面只通过它读写角色数据）</summary>
        private static RoleSystem Role
        {
            get { return Game.Model.Get<RoleSystem>(); }
        }

        private CommonBgCom com_bg;
        private GList list_tab;
        private GLoader load_com;
        private GTextField txt_placeholder;

        private RolePage m_CurrentPage = RolePage.Detail;
        private int m_RoleId;
        private bool m_HasRole;
        private bool m_Subscribed;

        protected internal override void Init(object o)
        {
            com_bg = ui.GetChild("com_bg") as CommonBgCom;
            list_tab = ui.GetChild("list_tab").asList;
            load_com = ui.GetChild("load_com").asLoader;

            CreatePlaceholder();

            // 节点绑定完成后再触发 AddEvent
            base.Init(o);
            InitTabs();
            com_bg.Init("角色详情", "ROLE INFO", OnClickBack);
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

        /// <summary>切页签：把 load_com 换成对应组件，再按需填数据或显示占位</summary>
        private void ShowPage(RolePage _page)
        {
            m_CurrentPage = _page;

            if (!m_HasRole)
            {
                HidePageCom();
                ShowPlaceholder("暂无角色");
            }
            else if (string.IsNullOrEmpty(c_PageComUrls[(int)_page]))
            {
                HidePageCom();
                ShowPlaceholder("「" + c_TabTitleCn[(int)_page] + "」界面制作中");
            }
            else
            {
                ShowPageCom(c_PageComUrls[(int)_page]);
                HidePlaceholder();
                RefreshPageCom();
            }

            RoleViewData kData = Game.Data.GetGameData<RoleViewData>();
            kData.SetCurrentType(c_PageStateNames[(int)_page]);
        }

        private void ShowPageCom(string _url)
        {
            // url 没变时 GLoader 内部会直接返回，不会重建组件
            load_com.url = _url;
            load_com.visible = true;
        }

        private void HidePageCom()
        {
            load_com.url = null;
            load_com.visible = false;
        }

        /// <summary>把当前角色数据塞进 load_com 里正在显示的组件；新增页签在这里补分支</summary>
        private void RefreshPageCom()
        {
            switch (m_CurrentPage)
            {
                case RolePage.Detail:
                    RoleInfoCom kInfo = load_com.component as RoleInfoCom;
                    if (kInfo != null)
                    {
                        kInfo.SetCom(Role.GetRoleData(m_RoleId));
                    }

                    break;
            }
        }

        #endregion

        #region 数据

        private void Refresh()
        {
            RoleRecord kRole = m_RoleId > 0 ? Role.GetRole(m_RoleId) : null;
            if (kRole == null)
            {
                kRole = Role.GetMainRole();
            }

            if (kRole == null)
            {
                List<RoleRecord> kAll = Role.GetAllRoles();
                if (kAll.Count > 0)
                {
                    kRole = kAll[0];
                }
            }

            if (kRole == null)
            {
                m_RoleId = 0;
                m_HasRole = false;
                ShowPage(m_CurrentPage);
                return;
            }

            m_HasRole = true;
            m_RoleId = kRole.m_Id;
            ShowPage(m_CurrentPage);
        }

        private static int ResolveRoleId(object _o)
        {
            if (_o is int)
            {
                int kId = (int)_o;
                if (Role.HasRole(kId))
                {
                    return kId;
                }
            }

            RoleRecord kRecord = _o as RoleRecord;
            if (kRecord != null && Role.HasRole(kRecord.m_Id))
            {
                return kRecord.m_Id;
            }

            return Role.GetMainRoleId();
        }

        private void Subscribe()
        {
            if (m_Subscribed)
            {
                return;
            }

            Game.Event.Subscribe(RoleEvent.OnRoleChanged, OnRoleChanged);
            Game.Event.Subscribe(RoleEvent.OnRolesChanged, OnRolesChanged);
            Game.Event.Subscribe(RoleEvent.OnMainRoleChanged, OnMainRoleChanged);
            m_Subscribed = true;
        }

        private void UnSubscribe()
        {
            if (!m_Subscribed)
            {
                return;
            }

            Game.Event.UnSubscribe(RoleEvent.OnRoleChanged, OnRoleChanged);
            Game.Event.UnSubscribe(RoleEvent.OnRolesChanged, OnRolesChanged);
            Game.Event.UnSubscribe(RoleEvent.OnMainRoleChanged, OnMainRoleChanged);
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
            const float c_Width = 1000f;
            const float c_Height = 200f;

            txt_placeholder = new GTextField();
            txt_placeholder.name = "txt_role_placeholder";
            txt_placeholder.touchable = false;
            txt_placeholder.SetSize(c_Width, c_Height);
            txt_placeholder.SetXY(load_com.x + (load_com.width - c_Width) * 0.5f,
                load_com.y + (load_com.height - c_Height) * 0.5f);

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
            list_tab.onClickItem.Add(OnClickTab);
        }

        protected internal override void RemoveEvent()
        {
            list_tab.onClickItem.Remove(OnClickTab);
        }

        #endregion
    }
}
