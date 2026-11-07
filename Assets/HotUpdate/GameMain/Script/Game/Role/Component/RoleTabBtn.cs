using FairyGUI;
using FairyGUI.Utils;

namespace GameMain.Script.UI
{
    /// <summary>
    /// 角色界面的页签按钮（FairyGUI 包 Role / RoleTabBtn，Radio 按钮）。
    /// 选中态由 GList 的 selectionMode 统一管理，这里只负责中英文标题。
    /// </summary>
    public class RoleTabBtn : GButton
    {
        private Controller state;
        private GTextField txt_titleC, txt_titleE;

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);
            state = GetController("state");
            txt_titleC = GetChild("txt_title_1").asTextField;
            txt_titleE = GetChild("txt_title_2").asTextField;
        }

        public void SetTab(string _titleCn, string _titleEn, bool _selected)
        {
            txt_titleC.text = _titleCn;
            txt_titleE.text = _titleEn;
            selected = _selected;
        }
    }
}