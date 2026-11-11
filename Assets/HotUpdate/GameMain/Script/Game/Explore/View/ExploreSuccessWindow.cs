using FairyGUI;
using Framework.Script.UI;

namespace GameMain.Script.UI
{
    /// <summary>
    /// 探索结算 · 成功弹窗（FairyGUI 包 World / ExploreSuccessWindow，预制体 ExploreSuccessWindow）。
    ///
    /// UIPopup：带背景模糊、点空白/右上关闭按钮可关闭；关闭后回指挥室。
    /// 结构：
    ///   com_popup   Base/PopupWindow（UIPopup 自动绑定 btn_close）
    ///   txt_title   "调查成功"
    ///   txt_tip     "获得奖励"
    ///   list_item   奖励列表（Base/CommonItemCom，奖励系统接好后填）
    ///   btn_back    返回指挥室
    /// </summary>
    public class ExploreSuccessWindow : UIPopup
    {
        private GTextField txt_title;
        private GTextField txt_tip;
        private GList list_item;
        private GButton btn_back;

        /// <summary>正在关窗回指挥室，避免 Leave 里再次触发</summary>
        private bool m_Returning;

        protected internal override void Init(object o)
        {
            txt_title = ui.GetChild("txt_title").asTextField;
            txt_tip = ui.GetChild("txt_tip").asTextField;
            list_item = ui.GetChild("list_item").asList;
            btn_back = ui.GetChild("btn_back").asButton;
            btn_back.title = "返回";

            base.Init(o);
        }

        protected internal override void AddEvent()
        {
            base.AddEvent();
            btn_back.onClick.Add(OnClickBack);
        }

        protected internal override void RemoveEvent()
        {
            base.RemoveEvent();
            btn_back.onClick.Remove(OnClickBack);
        }

        protected internal override void Enter(object o)
        {
            m_Returning = false;
            base.Enter(o);

            txt_title.text = "调查成功";
            txt_tip.text = "获得奖励";

            // 奖励系统还没接：先清空列表；后续由 ExploreResult / 掉落表填 numItems
            list_item.numItems = 0;
        }

        protected internal override void Leave()
        {
            base.Leave();

            if (m_Returning)
            {
                return;
            }

            m_Returning = true;
            Game.UI.ShowWindow(UIDefine.CommandRoom);
            m_Returning = false;
        }

        private void OnClickBack()
        {
            // 关掉弹窗后由 Leave 统一回指挥室
            Close();
        }
    }
}
