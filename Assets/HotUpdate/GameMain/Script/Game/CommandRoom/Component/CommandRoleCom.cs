using FairyGUI;
using FairyGUI.Utils;
using Framework.Script.UI;

namespace GameMain.Script.UI
{
    /// <summary>
    /// 指挥室里的角色卡（FairyGUI 包 CommandRoom / CommandRoleCom）。
    /// 左侧显示角色数量/当前职称，右侧显示宿舍入口；点击角色卡进入角色界面。
    /// </summary>
    public class CommandRoleCom : GComponent
    {
        /// <summary>角色容量（设计稿 23/24；角色容量配置接入前先用常量）</summary>
        public const int RoleCapacity = 24;

        private GButton btn_role, btn_room;
        private GTextField txt_roleTitleE, txt_roleTitleC, txt_roleNum;
        private GTextField txt_roomNum, txt_roomTitleE, txt_roolTitleC;
        private GGroup part_red;

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            btn_role = GetChild("btn_role").asButton;
            btn_room = GetChild("btn_room").asButton;

            txt_roleTitleE = GetChild("txt_roleTitleE").asTextField;
            txt_roleTitleC = GetChild("txt_roleTitleC").asTextField;
            txt_roleNum = GetChild("txt_roleNum").asTextField;
            txt_roomNum = GetChild("txt_roomNum").asTextField;
            txt_roomTitleE = GetChild("txt_roomTitleE").asTextField;
            txt_roolTitleC = GetChild("txt_roolTitleC").asTextField;
            part_red = GetChild("part_red").asGroup;

            btn_role.onClick.Add(OnClickRole);
            btn_room.onClick.Add(OnClickRoom);
        }

        /// <summary>
        /// 角色卡数据。标题保留 FGUI 里的“专员 / SPECIALIST”分类文案。
        /// </summary>
        public void SetCom(int _roleCount, int _roleMax, bool _showNew)
        {
            txt_roleNum.text = _roleCount + "/" + _roleMax;
            part_red.visible = _showNew;
        }

        /// <summary>
        /// 需要动态改标题时使用（例如后续按职称分类）。
        /// </summary>
        public void SetTitle(string _titleCn, string _titleEn)
        {
            txt_roleTitleC.text = _titleCn;
            txt_roleTitleE.text = _titleEn;
        }

        /// <summary>
        /// 宿舍卡数据（宿舍系统接入前可先保留 FGUI 里的设计值）。
        /// </summary>
        public void SetRoom(int _roomCount, int _roomMax, string _titleCn, string _titleEn)
        {
            txt_roomNum.text = _roomCount + "/" + _roomMax;
            txt_roolTitleC.text = _titleCn;
            txt_roomTitleE.text = _titleEn;
        }

        private void OnClickRole()
        {
            // 角色列表与指挥室是平级界面：显示列表时，指挥室回收到对象池
            Game.UI.ShowWindow(UIDefine.RoleList);
        }

        private void OnClickRoom()
        {
            Game.Log.LogInfo("[指挥室] 宿舍界面尚未接入");
        }
    }
}
