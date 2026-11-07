using FairyGUI;
using FairyGUI.Utils;

namespace GameMain.Script.UI
{
    /// <summary>
    /// 人才招募条目按钮（FairyGUI 包 TalentAcquisition / TABtn_1）。
    /// 标题 + 数量 + 合同图标，对应 TACom 里的“发送合同 1 / 发送合同 10”。
    /// </summary>
    public class TABtnCom : GButton
    {
        private GTextField txt_num;

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);
            txt_num = GetChild("num").asTextField;
        }

        public void SetCom(string _title, int _num)
        {
            title = _title;
            txt_num.text = _num.ToString();
        }
    }
}
