using FairyGUI;
using FairyGUI.Utils;

namespace GameMain.Script.UI
{
    public class CommandPlayerCom : GComponent
    {
        private GTextField txt_name, txt_uid, txt_lv;
        private HeadCom com_head;

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);
            txt_name = GetChild("txt_name").asTextField;
            txt_uid = GetChild("txt_uid").asTextField;
            txt_lv = GetChild("txt_lv").asTextField;
            com_head = GetChild("com_head") as HeadCom;
        }

        public void SetCom()
        {
            
        }
    }
}