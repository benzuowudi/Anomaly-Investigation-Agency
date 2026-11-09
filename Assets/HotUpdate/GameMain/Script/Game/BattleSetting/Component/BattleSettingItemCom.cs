using System.Collections.Generic;
using FairyGUI;
using FairyGUI.Utils;
using GameMain.Script.BattleSetting;

namespace GameMain.Script.UI
{
    /// <summary>
    /// 机械骡货仓（FairyGUI 包 BattleSetting / BattleSettingItemCom）。
    ///
    /// 结构：标题（中/英）+ list_item（3 个 CommonItemCom 格）。
    /// 数据来自 BattleSettingSnapshot.m_MuleItems；点击有物品的格子会把它清空。
    /// </summary>
    public class BattleSettingItemCom : GComponent
    {
        private GTextField txt_name_chinese, txt_name_english;
        private GList list_item;

        private IReadOnlyList<BattleSettingItemSlot> m_Slots;

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            txt_name_chinese = GetChild("txt_name_chinese").asTextField;
            txt_name_english = GetChild("txt_name_english").asTextField;
            list_item = GetChild("list_item").asList;

            txt_name_chinese.text = "机械骡";
            txt_name_english.text = "MECHANICAL MULE";

            list_item.itemRenderer = ItemRenderer;
            list_item.onClickItem.Add(OnClickItem);
        }

        public void SetCom(IReadOnlyList<BattleSettingItemSlot> _slots)
        {
            m_Slots = _slots;
            list_item.numItems = _slots == null ? 0 : _slots.Count;
        }

        private void ItemRenderer(int _index, GObject _obj)
        {
            if (_obj is not CommonItemCom kCom)
            {
                return;
            }

            BattleSettingItemSlot kSlot = GetSlot(_index);
            if (kSlot == null)
            {
                kCom.SetCom(CommonItemCom.StateEmpty);
                return;
            }

            kCom.SetCom(kSlot.IsEmpty ? CommonItemCom.StateEmpty : CommonItemCom.StateItem,
                CommonSkillIconCom.ResolveIconUrl(kSlot.GetIconUrl()), GetSlotTip(kSlot));
        }

        private void OnClickItem(EventContext _context)
        {
            int kIndex = list_item.GetChildIndex(_context.data as GObject);
            if (GetSlot(kIndex) == null || GetSlot(kIndex).IsEmpty)
            {
                return;
            }

            Game.Model.Get<BattleSettingSystem>().ClearItem(BattleSettingSupplySlot.Mule, kIndex);
        }

        private BattleSettingItemSlot GetSlot(int _index)
        {
            if (m_Slots == null || _index < 0 || _index >= m_Slots.Count)
            {
                return null;
            }

            return m_Slots[_index];
        }

        private static string GetSlotTip(BattleSettingItemSlot _slot)
        {
            if (_slot == null || _slot.IsEmpty)
            {
                return "空槽位";
            }

            string kName = _slot.m_Item == null ? ("物品 " + _slot.m_ItemId) : _slot.m_Item.Name;
            return kName + " ×" + _slot.m_Count + "（点击清空）";
        }
    }
}
