using FairyGUI;
using Framework.Script.UI;
using GameMain.Script.Role;
using GameMain.Script.Warehouse;

namespace GameMain.Script.UI
{
    /// <summary>
    /// 指挥室主界面（FairyGUI 包 CommandRoom / CommandBaseView）。
    ///
    /// 结构：
    ///   load_role      左侧角色立绘
    ///   com_player     左下玩家信息
    ///   com_role       角色卡（角色数量/职称/宿舍），点击进入角色界面
    ///   com_TA         人才招募
    ///   com_inventory  仓库
    ///   com_armory     军械库
    ///   com_action     开始行动
    ///   com_currency   右上货币
    ///   btn_task / btn_achievement / btn_mail / btn_player 顶部功能按钮
    ///
    /// 目前只有角色、仓库有权威数据，其余卡片保留 FGUI 里的展示值；
    /// 对应系统接入后只需要补各自组件的 SetCom。
    /// </summary>
    public class CommandBaseView : UIWindow
    {
        private GLoader load_role;
        private GButton btn_task, btn_achievement, btn_mail, btn_player;

        private CommandPlayerCom com_player;
        private CommandRoleCom com_role;
        private CommandTACom com_TA;
        private CommandInventoryCom com_inventory;
        private CommandArmoryCom com_armory;
        private CommandActionCom com_action;
        private CommonCurrencyCom com_currency;

        private bool m_Subscribed;

        protected internal override void Init(object o)
        {
            load_role = ui.GetChild("load_role").asLoader;
            btn_task = ui.GetChild("btn_task").asButton;
            btn_achievement = ui.GetChild("btn_achievement").asButton;
            btn_mail = ui.GetChild("btn_mail").asButton;
            btn_player = ui.GetChild("btn_player").asButton;

            com_player = ui.GetChild("com_player") as CommandPlayerCom;
            com_role = ui.GetChild("com_role") as CommandRoleCom;
            com_TA = ui.GetChild("com_TA") as CommandTACom;
            com_inventory = ui.GetChild("com_inventory") as CommandInventoryCom;
            com_armory = ui.GetChild("com_armory") as CommandArmoryCom;
            com_action = ui.GetChild("com_action") as CommandActionCom;
            com_currency = ui.GetChild("com_currency") as CommonCurrencyCom;

            // 节点绑定完成后再触发 AddEvent
            base.Init(o);
        }

        protected internal override void AddEvent()
        {
            btn_task.onClick.Add(OnClickNotOpen);
            btn_achievement.onClick.Add(OnClickNotOpen);
            btn_mail.onClick.Add(OnClickNotOpen);
            btn_player.onClick.Add(OnClickNotOpen);

            com_player.onClick.Add(OnClickNotOpen);
            com_TA.onClick.Add(OnClickTA);
            com_inventory.onClick.Add(OnClickNotOpen);
            com_armory.onClick.Add(OnClickNotOpen);
            com_action.onClick.Add(OnClickNotOpen);
            com_currency.onClick.Add(OnClickNotOpen);
        }

        protected internal override void RemoveEvent()
        {
            btn_task.onClick.Clear();
            btn_achievement.onClick.Clear();
            btn_mail.onClick.Clear();
            btn_player.onClick.Clear();

            com_player.onClick.Clear();
            com_TA.onClick.Clear();
            com_inventory.onClick.Clear();
            com_armory.onClick.Clear();
            com_action.onClick.Clear();
            com_currency.onClick.Clear();
        }

        protected internal override void Enter(object o)
        {
            base.Enter(o);
            Subscribe();
            Refresh();
        }

        protected internal override void Leave()
        {
            UnSubscribe();
            base.Leave();
        }

        protected internal override void Destroy()
        {
            UnSubscribe();
            base.Destroy();
        }

        private void Refresh()
        {
            com_role.SetCom(RoleSystem.GetRoleCount(), CommandRoleCom.RoleCapacity, false);

            com_inventory.SetCom(WarehouseSystem.GetUsedSlots(), WarehouseSystem.GetMaxSlots());

            com_player.SetCom();
            com_TA.SetCom();
            com_armory.SetCom();
            com_action.SetCom();
        }

        private void Subscribe()
        {
            if (m_Subscribed)
            {
                return;
            }

            Game.Event.Subscribe(RoleEventDefine.OnRoleChanged, OnRoleChanged);
            Game.Event.Subscribe(RoleEventDefine.OnRolesChanged, OnRolesChanged);
            Game.Event.Subscribe(RoleEventDefine.OnMainRoleChanged, OnMainRoleChanged);
            Game.Event.Subscribe(WarehouseEventDefine.OnWarehouseChanged, OnWarehouseChanged);
            m_Subscribed = true;
        }

        private void UnSubscribe()
        {
            if (!m_Subscribed)
            {
                return;
            }

            Game.Event.UnSubscribe(RoleEventDefine.OnRoleChanged, OnRoleChanged);
            Game.Event.UnSubscribe(RoleEventDefine.OnRolesChanged, OnRolesChanged);
            Game.Event.UnSubscribe(RoleEventDefine.OnMainRoleChanged, OnMainRoleChanged);
            Game.Event.UnSubscribe(WarehouseEventDefine.OnWarehouseChanged, OnWarehouseChanged);
            m_Subscribed = false;
        }

        private void OnRoleChanged(object _o)
        {
            Refresh();
        }

        private void OnRolesChanged(object _o)
        {
            Refresh();
        }

        private void OnMainRoleChanged(object _o)
        {
            Refresh();
        }

        private void OnWarehouseChanged(object _o)
        {
            Refresh();
        }

        private void OnClickTA()
        {
            Game.UI.ShowWindow(UIDefine.TalentAcquisition);
        }

        private void OnClickNotOpen(EventContext _context)
        {
            GObject kSender = (GObject)_context.sender;
            Game.Log.LogInfo("[指挥室] " + kSender.name + " 功能尚未接入");
        }
    }
}
