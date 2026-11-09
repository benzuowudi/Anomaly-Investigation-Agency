using FairyGUI;
using FairyGUI.Utils;

public class CommonAttrCom : GComponent
{
    private GLoader load_icon;
    private GTextField txt_value;

    public override void ConstructFromXML(XML xml)
    {
        base.ConstructFromXML(xml);
        load_icon = GetChild("load_icon").asLoader;
        txt_value = GetChild("txt_value").asTextField;
    }

    public void SetCom(string _icon, string _value)
    {
        load_icon.url = _icon;
        txt_value.text = _value;
    }
}