using FairyGUI;
using FairyGUI.Utils;

namespace GameMain.Script.UI
{
    /// <summary>
    /// 合同说明文本（FairyGUI 包 TalentAcquisition / TADescCom）。
    /// 文本 autoSize=height，设置说明后按文本高度调整背景和组件高度。
    /// </summary>
    public class TADescCom : GComponent
    {
        private GTextField txt_title, txt_desc;
        private GImage img_bg;

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);
            txt_title = GetChild("txt_title").asTextField;
            txt_desc = GetChild("txt_desc").asTextField;
            img_bg = GetChild("n10").asImage;
        }

        public void SetCom(string _title)
        {
            txt_title.text = _title;
            ResizeToFit();
        }

        public void SetCom(string _title, string _desc)
        {
            txt_title.text = _title;
            txt_desc.text = _desc;
            ResizeToFit();
        }

        public void ResizeToFit()
        {
            float kHeight = txt_desc.y + txt_desc.height + 16f;
            SetSize(width, kHeight);
            img_bg.height = kHeight - img_bg.y;
        }
    }
}
