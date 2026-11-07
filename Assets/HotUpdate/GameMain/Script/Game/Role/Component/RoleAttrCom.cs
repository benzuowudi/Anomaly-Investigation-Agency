using FairyGUI;
using FairyGUI.Utils;

namespace GameMain.Script.UI
{
    /// <summary>
    /// 角色属性行（FairyGUI 包 Role / RoleAttrCom）：
    /// 图标 + 属性名 + 数值，由 RoleInfoCom 的属性列表复用。
    /// </summary>
    public class RoleAttrCom : GComponent
    {
        private GLoader load_icon;
        private GTextField txt_name, txt_value;

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);
            load_icon = GetChild("load_icon").asLoader;
            txt_name = GetChild("txt_name").asTextField;
            txt_value = GetChild("txt_value").asTextField;
        }

        public void SetCom(string _icon, string _name, string _value)
        {
            load_icon.url = _icon;
            txt_name.text = _name;
            txt_value.text = _value;
        }
    }
}
