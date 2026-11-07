using FairyGUI;
using FairyGUI.Utils;

namespace GameMain.Script.UI
{
    public class HeadCom : GComponent
    {
        private GLoader load_head;
        private GImage img_point;

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);
            load_head = GetChild("load_head").asLoader;
            img_point = GetChild("img_point").asImage;
        }

        public void SetCom()
        {
            
        }
    }
}