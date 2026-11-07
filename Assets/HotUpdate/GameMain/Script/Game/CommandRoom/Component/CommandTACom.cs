using FairyGUI;
using FairyGUI.Utils;

namespace GameMain.Script.UI
{
    public class CommandTACom : GComponent
    {
        private GTextField txt_titleC, txt_titleE, txt_back, txt_backTime;
        private GGroup part_back;

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);
            txt_titleC = GetChild("txt_titleC").asTextField;
            txt_titleE = GetChild("txt_titleE").asTextField;
            txt_back = GetChild("txt_back").asTextField;
            txt_backTime = GetChild("txt_backTime").asTextField;
            part_back = GetChild("part_back").asGroup;
        }

        public void SetCom()
        {
        }
    }
}
