using FairyGUI;
using FairyGUI.Utils;

namespace GameMain.Script.UI
{
    /// <summary>
    /// 仓库界面的页签按钮（FairyGUI 包 Inventory / InventoryTabBtn，Radio 按钮）。
    /// 结构：图形 n5（选中态底框）+ 底图 n3 + 标题 title；选中态颜色由 gearColor 控制。
    /// </summary>
    public class InventoryTabBtn : GButton
    {
        private GTextField txt_title;

        public override void ConstructFromXML(XML _xml)
        {
            base.ConstructFromXML(_xml);
            txt_title = GetChild("title").asTextField;
        }

        /// <summary>设置页签标题与选中态（选中态由界面统一维护，别依赖 Radio 的自动互斥）</summary>
        public void SetTab(string _title, bool _selected)
        {
            txt_title.text = _title;
            selected = _selected;
        }
    }
}
