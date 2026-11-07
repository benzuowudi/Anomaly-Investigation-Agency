using FairyGUI;
using FairyGUI.Utils;

namespace GameMain.Script.UI
{
    /// <summary>
    /// 角色技能 / 天赋条目（FairyGUI 包 Role / RoleSkillCom）：
    /// 标题（RoleTitleCom）+ 图标 + 名称 + 说明，由 RoleInfoCom 的技能列表复用。
    /// </summary>
    public class RoleSkillCom : GComponent
    {
        private RoleTitleCom com_title;
        private GLoader load_icon;
        private GTextField txt_name, txt_desc;

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);
            com_title = GetChild("n16") as RoleTitleCom;
            load_icon = GetChild("load_icon").asLoader;
            txt_name = GetChild("load_name").asTextField;
            txt_desc = GetChild("load_desc").asTextField;
        }

        public void SetCom(string _titleCn, string _titleEn, string _icon, string _name, string _desc)
        {
            com_title.SetCom(_titleCn, _titleEn);
            load_icon.url = _icon;
            txt_name.text = _name;
            txt_desc.text = _desc;

            // 组件在 FairyGUI 里已经用「底边延伸」关联跟随描述文本的高度。
            // 这里再兜底一次：保证运行时条目高度不小于内容高度（关联失效、包没重发时也能兜住），
            // 否则描述文本会溢出到下一个条目上，被误认为列表内容重叠。
            float kContentBottom = txt_desc.y + txt_desc.height;
            if (height < kContentBottom)
            {
                height = kContentBottom;
            }
        }
    }
}
