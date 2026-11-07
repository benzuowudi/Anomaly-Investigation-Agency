using FairyGUI;
using FairyGUI.Utils;
using Framework.Script.Expand;
using Framework.Script.UI;

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

            // 数字会被 NumberFormat 缩写成 "1.8万"，而 FGUI 里这个文本是 AGENCYR（纯英文字体），
            // 不换字体的话"万"会静默回退到系统字体，和数字变成两种字体混排。
            UIFont.Apply(txt_num);
        }

        public void SetCom(long _amount)
        {
            txt_num.text = NumberFormat.Format(_amount);
        }

        public void SetIcon(string _url)
        {
            load_icon.url = _url;
        }
    }
}
