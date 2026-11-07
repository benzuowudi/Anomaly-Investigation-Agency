using FairyGUI;
using FairyGUI.Utils;

namespace GameMain.Script.UI
{
    public class CommandArmoryCom : GComponent
    {
        private GTextField txt_titleE, txt_titleC, txt_title_num;

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);
            txt_titleE = GetChild("txt_titleE").asTextField;
            txt_titleC = GetChild("txt_titleC").asTextField;
            txt_title_num = GetChild("txt_title_num").asTextField;
        }

        public void SetCom()
        {
        }
    }
}
