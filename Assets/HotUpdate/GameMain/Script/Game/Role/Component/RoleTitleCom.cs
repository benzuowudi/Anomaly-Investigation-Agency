using FairyGUI;
using FairyGUI.Utils;

namespace GameMain.Script.UI
{
    /// <summary>
    /// 中英文小标题（FairyGUI 包 Role / RoleTitleCom）：
    /// 中文标题 + 英文副标题 + 下划线，属性 / 装备 / 勋章 / 技能条目共用。
    /// </summary>
    public class RoleTitleCom : GComponent
    {
        private GTextField txt_titleC, txt_titleE;

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);
            txt_titleC = GetChild("txt_titleC").asTextField;
            txt_titleE = GetChild("txt_titleE").asTextField;
        }

        public void SetCom(string _titleCn, string _titleEn)
        {
            txt_titleC.text = _titleCn;
            txt_titleE.text = _titleEn;
        }
    }
}
