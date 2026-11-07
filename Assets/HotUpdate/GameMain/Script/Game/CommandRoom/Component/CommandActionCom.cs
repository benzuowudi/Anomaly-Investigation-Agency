using FairyGUI;
using FairyGUI.Utils;

namespace GameMain.Script.UI
{
    public class CommandActionCom : GComponent
    {
        private GTextField txt_bookNum, txt_bookTitle, txt_bookTime, txt_chapter, txt_titleE, txt_titleC, txt_red;
        private GButton btn_bookAdd;
        private GGroup part_red;

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);
            txt_bookNum = GetChild("txt_bookNum").asTextField;
            txt_bookTitle = GetChild("txt_bookTitle").asTextField;
            txt_bookTime = GetChild("txt_bookTime").asTextField;
            txt_chapter = GetChild("txt_chapter").asTextField;
            txt_titleE = GetChild("txt_titleE").asTextField;
            txt_titleC = GetChild("txt_titleC").asTextField;
            txt_red = GetChild("txt_red").asTextField;
            btn_bookAdd = GetChild("btn_bookAdd").asButton;
            part_red = GetChild("part_red").asGroup;
        }

        public void SetCom()
        {
        }
    }
}
