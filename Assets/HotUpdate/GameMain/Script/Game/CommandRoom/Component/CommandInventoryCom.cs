using FairyGUI;
using FairyGUI.Utils;

namespace GameMain.Script.UI
{
    public class CommandInventoryCom : GComponent
    {
        private GTextField txt_num, txt_titleC, txt_titleE;

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);
            txt_num = GetChild("txt_num").asTextField;
            txt_titleC = GetChild("txt_titleC").asTextField;
            txt_titleE = GetChild("txt_titleE").asTextField;
        }

        public void SetCom(int _usedSlots, int _maxSlots)
        {
            // 0 表示不限制
            txt_num.text = _usedSlots + "/" + (_maxSlots > 0 ? _maxSlots.ToString() : "∞");
        }
    }
}
