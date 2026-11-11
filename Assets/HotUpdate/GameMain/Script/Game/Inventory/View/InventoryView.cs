using System.Collections.Generic;
using FairyGUI;
using Framework.Script.UI;
using GameMain.Script.Inventory;
using UnityEngine;

namespace GameMain.Script.UI
{
    /// <summary>
    /// 仓库界面（FairyGUI 包 Inventory / InventoryView，预制体 InventoryView）。
    ///
    /// 结构：
    ///   com_bg      BackGroundVIew/CommonBgCom（标题 / 返回）
    ///   txt_title   当前页签标题（道具 / 装备）
    ///   list_item   物品网格（Base/CommonItemCom，flow_hz）
    ///   list_tab    底部页签（InventoryTabBtn，Radio）
    ///
    /// 数据全部来自 InventorySystem.BuildSnapshot()：
    /// - "道具"页签 = item 表中已持有的道具；
    /// - "装备"页签 = 军械库已合成的装备（ArmoryData）。
    /// 界面只调模型、订阅 InventoryEvent.OnInventoryChanged 刷新，不直接读写 InventoryData。
    /// </summary>
    public class InventoryView : UIWindow
    {
        private CommonBgCom com_bg;
        private GTextField txt_title;
        private GList list_item;
        private GList list_tab;
        private GTextField txt_empty;

        private bool m_Subscribed;
        private InventoryTab m_Tab = InventoryTab.Item;
        private readonly List<InventoryEntry> m_Entries = new List<InventoryEntry>();

        /// <summary>仓库模型（由 ModelManager 注册）</summary>
        private static InventorySystem Inventory
        {
            get { return Game.Model.Get<InventorySystem>(); }
        }

        /// <summary>界面状态（当前页签）</summary>
        private static InventoryViewData ViewData
        {
            get { return Game.Data.GetGameData<InventoryViewData>(); }
        }

        protected internal override void Init(object o)
        {
            com_bg = ui.GetChild("com_bg") as CommonBgCom;
            txt_title = ui.GetChild("txt_title").asTextField;
            list_item = ui.GetChild("list_item").asList;
            list_tab = ui.GetChild("list_tab").asList;

            com_bg.Init(InventoryDefine.c_TitleChinese, InventoryDefine.c_TitleEnglish, OnClickBack);
            list_item.itemRenderer = RenderItem;

            CreateEmptyText();
            base.Init(o);
        }

        protected internal override void AddEvent()
        {
            base.AddEvent();
            list_tab.onClickItem.Add(OnClickTab);
        }

        protected internal override void RemoveEvent()
        {
            base.RemoveEvent();
            list_tab.onClickItem.Remove(OnClickTab);
        }

        protected internal override void Enter(object o)
        {
            base.Enter(o);
            Subscribe();

            m_Tab = ViewData.currentTab;
            BuildTabs();
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

        /// <summary>按 Define 里的页签顺序补按钮（数量与 list_tab 的设计期按钮无关）</summary>
        private void BuildTabs()
        {
            list_tab.RemoveChildrenToPool();

            int kCount = InventoryDefine.GetTabCount();
            for (int i = 0; i < kCount; i++)
            {
                InventoryTabBtn kBtn = list_tab.AddItemFromPool() as InventoryTabBtn;
                InventoryTab kTab = InventoryDefine.ToTab(i);
                kBtn.SetTab(InventoryDefine.GetTabTitle(kTab), kTab == m_Tab);
            }

            if (list_tab.selectionMode != ListSelectionMode.None)
            {
                list_tab.selectedIndex = (int)m_Tab;
            }
        }

        private void OnClickTab(EventContext _context)
        {
            GObject kItem = _context.data as GObject;
            int kIndex = list_tab.GetChildIndex(kItem);
            if (kIndex < 0 || kIndex >= InventoryDefine.GetTabCount())
            {
                return;
            }

            SetTab(InventoryDefine.ToTab(kIndex));
        }

        private void SetTab(InventoryTab _tab)
        {
            m_Tab = _tab;
            ViewData.currentTab = _tab;

            for (int i = 0; i < list_tab.numItems; i++)
            {
                GObject kObj = list_tab.GetChildAt(i);
                if (kObj is InventoryTabBtn)
                {
                    ((InventoryTabBtn)kObj).selected = i == (int)_tab;
                }
            }

            if (list_tab.selectionMode != ListSelectionMode.None)
            {
                list_tab.selectedIndex = (int)_tab;
            }

            Refresh();
        }

        #endregion

        #region 刷新

        private void Refresh()
        {
            InventorySnapshot kSnapshot = Inventory.BuildSnapshot(m_Tab);

            m_Entries.Clear();
            m_Entries.AddRange(kSnapshot.m_Items);

            txt_title.text = InventoryDefine.GetTabTitle(m_Tab);
            RefreshGrid();

            txt_empty.text = InventoryDefine.GetEmptyText(m_Tab);
            txt_empty.visible = m_Entries.Count == 0;
        }

        /// <summary>
        /// 重画网格。GList 的 numItems 不变时不会重新走 itemRenderer，
        /// 而数量变化（拾取/消耗）必须重画，所以数量没变时手动重画已有格子，
        /// 数量变了才重建列表（顺便保留滚动位置）。
        /// </summary>
        private void RefreshGrid()
        {
            if (list_item.numItems == m_Entries.Count)
            {
                for (int i = 0; i < list_item.numChildren; i++)
                {
                    RenderItem(i, list_item.GetChildAt(i));
                }

                return;
            }

            float kScrollPos = GetScrollPos();

            list_item.numItems = 0;
            list_item.numItems = m_Entries.Count;

            RestoreScrollPos(kScrollPos);
        }

        private void RenderItem(int _index, GObject _obj)
        {
            CommonItemCom kCom = _obj as CommonItemCom;
            if (kCom == null || _index < 0 || _index >= m_Entries.Count)
            {
                return;
            }

            InventoryEntry kEntry = m_Entries[_index];
            int kState = kEntry.m_IsEquip ? CommonItemCom.StateEquip : CommonItemCom.StateItem;
            kCom.SetCom(kState, kEntry.m_Icon, kEntry.m_Quality, kEntry.m_Name);
            kCom.SetCount(kEntry.m_Count);
        }

        private float GetScrollPos()
        {
            return list_item.scrollPane == null ? 0f : list_item.scrollPane.posY;
        }

        private void RestoreScrollPos(float _pos)
        {
            if (list_item.scrollPane == null || _pos <= 0f)
            {
                return;
            }

            // posY 的 setter 会自己夹到 [0, maxPosY]，列表变短时不会越界
            list_item.scrollPane.posY = _pos;
        }

        /// <summary>空态占位文案（列表为空时显示）</summary>
        private void CreateEmptyText()
        {
            const float c_Height = 120f;

            txt_empty = new GTextField();
            txt_empty.name = "txt_empty";
            txt_empty.touchable = false;
            txt_empty.SetSize(list_item.width, c_Height);
            txt_empty.SetXY(list_item.x, list_item.y + (list_item.height - c_Height) * 0.5f);

            TextFormat kFormat = new TextFormat();
            kFormat.font = UIConfig.defaultFont;
            kFormat.size = 36;
            kFormat.color = new Color(0.45f, 0.44f, 0.47f, 1f);
            kFormat.align = AlignType.Center;
            txt_empty.textFormat = kFormat;

            // 自动尺寸放在 textFormat 之后设置：套用格式时可能重算尺寸
            txt_empty.autoSize = AutoSizeType.None;
            txt_empty.verticalAlign = VertAlignType.Middle;
            txt_empty.visible = false;

            ui.AddChild(txt_empty);
        }

        #endregion

        #region 事件

        private void Subscribe()
        {
            if (m_Subscribed)
            {
                return;
            }

            Game.Event.Subscribe(InventoryEvent.OnInventoryChanged, OnInventoryChanged);
            m_Subscribed = true;
        }

        private void UnSubscribe()
        {
            if (!m_Subscribed)
            {
                return;
            }

            Game.Event.UnSubscribe(InventoryEvent.OnInventoryChanged, OnInventoryChanged);
            m_Subscribed = false;
        }

        private void OnInventoryChanged(object _o)
        {
            Refresh();
        }

        #endregion

        #region 输入

        private void OnClickBack()
        {
            // 由指挥室「仓库」进入，返回时回到指挥室（与军械库一致）
            Game.UI.ShowWindow(UIDefine.CommandRoom);
        }

        #endregion
    }
}
