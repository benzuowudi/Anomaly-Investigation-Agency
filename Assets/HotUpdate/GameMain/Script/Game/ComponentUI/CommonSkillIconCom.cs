using FairyGUI;
using FairyGUI.Utils;

namespace GameMain.Script.UI
{
    /// <summary>
    /// 通用技能/装备图标（FairyGUI 包 Base / CommonSkillIconCom）。
    /// 只负责显示图标和控制显隐，具体图标由调用方传入。
    /// </summary>
    public class CommonSkillIconCom : GComponent
    {
        private GLoader load_icon;

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);
            load_icon = GetChild("load_icon").asLoader;
        }

        public void SetCom(string _iconUrl, bool _visible)
        {
            visible = _visible;
            if (!string.IsNullOrEmpty(_iconUrl))
            {
                load_icon.url = _iconUrl;
            }
        }
    }
}
