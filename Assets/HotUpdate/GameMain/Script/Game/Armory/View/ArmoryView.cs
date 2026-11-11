using System.Collections.Generic;
using FairyGUI;
using Framework.Script.UI;
using GameMain.Script.Armory;
using GameMain.Script.Inventory;

namespace GameMain.Script.UI
{
    /// <summary>
    /// 军械库 · 装备合成（FairyGUI 包 Armory / ArmoryView，预制体 ArmoryView）。
    ///
    /// 结构：
    ///   com_bg            BackGroundVIew/CommonBgCom（标题 / 返回）
    ///   txt_title         "材料"
    ///   list_composite    材料/蓝图网格（Base/CommonItemCom）
    ///   com_item          产物预览（CommonItemCom）
    ///   list_item         4 个合成材料槽（CommonItemCom）
    ///   btn_confirm       "开始合成"
    ///
    /// 操作：点材料加入 4 个槽（再点一次移除），槽里最多 1 张蓝图；
    /// 有蓝图时 com_item 预览对应装备，点开始合成扣材料并写入 ArmoryData。
    ///
    /// 配方暂时用 ArmoryRecipeRule（蓝图名字去掉"蓝图" = equip.name），
    /// 以后接配方表只改规则类。
    /// </summary>
    public class ArmoryView : UIWindow
    {
        private CommonBgCom com_bg;
        private GTextField txt_title;
        private GList list_composite;
        private GList list_item;
        private CommonItemCom com_item;
        private GButton btn_confirm;

        private readonly List<cfg.GameData_Item.ItemData> m_Materials = new List<cfg.GameData_Item.ItemData>();
        private readonly List<int> m_Selected = new List<int>();

        private static ArmorySystem Armory
        {
            get { return Game.Model.Get<ArmorySystem>(); }
        }

        /// <summary>仓库模型（材料数量查询 / 消耗都走它）</summary>
        private static InventorySystem Inventory
        {
            get { return Game.Model.Get<InventorySystem>(); }
        }

        protected internal override void Init(object o)
        {
            com_bg = ui.GetChild("com_bg") as CommonBgCom;
            txt_title = ui.GetChild("txt_title").asTextField;
            list_composite = ui.GetChild("list_composite").asList;
            list_item = ui.GetChild("list_item").asList;
            com_item = ui.GetChild("com_item") as CommonItemCom;
            btn_confirm = ui.GetChild("btn_confirm").asButton;

            com_bg.Init("军械库", "ARMORY", OnClickBack);
            txt_title.text = "材料";
            btn_confirm.title = "开始合成";

            list_composite.itemRenderer = RenderMaterial;
            list_item.itemRenderer = RenderSlot;
            list_composite.onClickItem.Add(OnClickMaterial);
            list_item.onClickItem.Add(OnClickSlot);
            btn_confirm.onClick.Add(OnConfirm);

            base.Init(o);
        }

        protected internal override void Enter(object o)
        {
            base.Enter(o);

            m_Selected.Clear();
            RefreshMaterials();
            RefreshSlots();
            RefreshPreview();
        }

        #region 刷新

        private void RefreshMaterials()
        {
            m_Materials.Clear();

            cfg.Tables kTables = Game.Data.GetData;
            if (kTables != null && kTables.item != null)
            {
                List<cfg.GameData_Item.ItemData> kAll = kTables.item.DataList;
                for (int i = 0; i < kAll.Count; i++)
                {
                    cfg.GameData_Item.ItemData kItem = kAll[i];
                    // 3 = 材料（含蓝图）；1 普通道具 / 2 货币 / 4 消耗品 暂不显示
                    if (kItem != null && kItem.TypeShow == 3)
                    {
                        m_Materials.Add(kItem);
                    }
                }
            }

            m_Materials.Sort(delegate (cfg.GameData_Item.ItemData _a, cfg.GameData_Item.ItemData _b)
            {
                return _a.ID.CompareTo(_b.ID);
            });

            list_composite.numItems = m_Materials.Count;
        }

        private void RenderMaterial(int _index, GObject _obj)
        {
            CommonItemCom kCom = _obj as CommonItemCom;
            if (kCom == null || _index < 0 || _index >= m_Materials.Count)
            {
                return;
            }

            cfg.GameData_Item.ItemData kItem = m_Materials[_index];
            int kCount = Inventory.GetItemCount(kItem.ID);
            bool kSelected = m_Selected.Contains(kItem.ID);

            kCom.SetCom(CommonItemCom.StateItem, kItem.Icon, kItem.Quality,
                kItem.Name + (kCount > 0 ? " x" + kCount : "（未持有）"));
            kCom.grayed = kCount <= 0;
            kCom.alpha = kCount <= 0 ? 0.35f : (kSelected ? 0.6f : 1f);
        }

        private void RefreshSlots()
        {
            // GList 的 numItems 没变时不会重新走 itemRenderer，这里强制刷一遍
            list_item.numItems = 0;
            list_item.numItems = 4;
        }

        private void RenderSlot(int _index, GObject _obj)
        {
            CommonItemCom kCom = _obj as CommonItemCom;
            if (kCom == null)
            {
                return;
            }

            if (_index >= 0 && _index < m_Selected.Count)
            {
                kCom.SetItem(m_Selected[_index]);
            }
            else
            {
                kCom.SetCom(CommonItemCom.StateEmpty);
            }
        }

        private void RefreshPreview()
        {
            int kEquipId = 0;
            cfg.Tables kTables = Game.Data.GetData;

            for (int i = 0; i < m_Selected.Count; i++)
            {
                int kId = ArmoryRecipeRule.ResolveEquipId(kTables, m_Selected[i]);
                if (kId > 0)
                {
                    kEquipId = kId;
                    break;
                }
            }

            if (kEquipId <= 0)
            {
                com_item.SetCom(CommonItemCom.StateEmpty);
                btn_confirm.grayed = true;
                return;
            }

            com_item.SetEquip(kEquipId);
            btn_confirm.grayed = false;
        }

        #endregion

        #region 输入

        private void OnClickMaterial(EventContext _context)
        {
            GObject kObj = _context.data as GObject;
            if (kObj == null)
            {
                return;
            }

            int kIndex = list_composite.GetChildIndex(kObj);
            if (kIndex < 0 || kIndex >= m_Materials.Count)
            {
                return;
            }

            cfg.GameData_Item.ItemData kItem = m_Materials[kIndex];
            if (Inventory.GetItemCount(kItem.ID) <= 0)
            {
                return;
            }

            if (m_Selected.Contains(kItem.ID))
            {
                m_Selected.Remove(kItem.ID);
            }
            else
            {
                if (m_Selected.Count >= 4)
                {
                    return;
                }

                if (ArmoryRecipeRule.IsBlueprint(kItem))
                {
                    for (int i = 0; i < m_Selected.Count; i++)
                    {
                        cfg.GameData_Item.ItemData kOld = kTablesItem(m_Selected[i]);
                        if (ArmoryRecipeRule.IsBlueprint(kOld))
                        {
                            return;
                        }
                    }
                }

                m_Selected.Add(kItem.ID);
            }

            RefreshMaterialCells();
            RefreshSlots();
            RefreshPreview();
        }

        private void OnClickSlot(EventContext _context)
        {
            GObject kObj = _context.data as GObject;
            if (kObj == null)
            {
                return;
            }

            int kIndex = list_item.GetChildIndex(kObj);
            if (kIndex < 0 || kIndex >= m_Selected.Count)
            {
                return;
            }

            m_Selected.RemoveAt(kIndex);
            RefreshMaterialCells();
            RefreshSlots();
            RefreshPreview();
        }

        /// <summary>重画已创建的格子（选中态改 alpha），不重建列表</summary>
        private void RefreshMaterialCells()
        {
            for (int i = 0; i < list_composite.numChildren; i++)
            {
                RenderMaterial(i, list_composite.GetChildAt(i));
            }
        }

        private void OnConfirm()
        {
            int kEquipId;
            string kError;
            if (!Armory.Craft(m_Selected, out kEquipId, out kError))
            {
                Game.Log.LogWarning(ArmorySystem.c_LogTag + "合成失败：" + kError);
                return;
            }

            m_Selected.Clear();
            RefreshMaterials();
            RefreshSlots();

            // 合成结果留在产物格里，下一次选材料时再被预览覆盖
            com_item.SetEquip(kEquipId);
            btn_confirm.grayed = true;

            Game.Log.LogInfo(ArmorySystem.c_LogTag + "合成完成，装备 ID：" + kEquipId);
        }

        private void OnClickBack()
        {
            Game.UI.ShowWindow(UIDefine.CommandRoom);
        }

        private static cfg.GameData_Item.ItemData kTablesItem(int _itemId)
        {
            cfg.Tables kTables = Game.Data.GetData;
            return kTables == null || kTables.item == null ? null : kTables.item.GetOrDefault(_itemId);
        }

        #endregion
    }
}
