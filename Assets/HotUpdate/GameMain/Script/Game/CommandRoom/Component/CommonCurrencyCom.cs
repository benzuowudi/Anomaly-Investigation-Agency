using FairyGUI;
using FairyGUI.Utils;

namespace GameMain.Script.UI
{
    /// <summary>
    /// 右上角货币显示（FairyGUI 包 CommandRoom / CommonCurrencyCOm）。
    /// 目前只有展示接口，等玩家钱包系统接入后由 CommandBaseView 传入数值。
    /// </summary>
    public class CommonCurrencyCom : GComponent
    {
        private GLoader load_icon;
        private GTextField txt_num;

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);
            load_icon = GetChild("n56").asLoader;
            txt_num = GetChild("n57").asTextField;
        }

        public void SetCom(long _amount)
        {
            txt_num.text = _amount.ToString("N0");
        }

        public void SetIcon(string _url)
        {
            load_icon.url = _url;
        }
    }
}
