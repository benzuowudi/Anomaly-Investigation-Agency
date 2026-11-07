using FairyGUI;
using FairyGUI.Utils;
using UnityEngine;

namespace GameMain.Script.UI
{
    /// <summary>
    /// 职称/职位徽章（FairyGUI 包 Base / CommonPosCom）。
    /// 根据职级切换 ui_bg_pos1/2/3 底图。
    /// </summary>
    public class CommonPosCom : GComponent
    {
        private GLoader load_bg;
        private GTextField txt_title;

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);
            load_bg = GetChild("load_bg").asLoader;
            txt_title = GetChild("txt_title").asTextField;
        }

        public void SetCom(string _title, int _level)
        {
            txt_title.text = _title;
            load_bg.url = "ui://Base/ui_bg_pos" + (Mathf.Clamp(_level, 0, 2) + 1);
        }
    }
}
