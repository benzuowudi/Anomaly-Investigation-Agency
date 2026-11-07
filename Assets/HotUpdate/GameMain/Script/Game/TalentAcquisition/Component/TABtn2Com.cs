using FairyGUI;
using FairyGUI.Utils;

namespace GameMain.Script.UI
{
    /// <summary>
    /// 定向猎头的“猎头委托”按钮（FairyGUI 包 TalentAcquisition / TABtn_2）。
    /// 左侧主标题 + 右侧状态/说明。
    /// </summary>
    public class TABtn2Com : GButton
    {
        private GTextField txt_desc;

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);
            txt_desc = GetChild("desc").asTextField;
        }

        public void SetCom(string _title, string _desc)
        {
            title = _title;
            txt_desc.text = _desc;
            txt_desc.visible = true;
        }

        /// <summary>猎头进行中：标题变成“猎头中…”，隐藏右侧说明。</summary>
        public void SetRunning(string _title)
        {
            title = _title;
            txt_desc.visible = false;
        }
    }
}
