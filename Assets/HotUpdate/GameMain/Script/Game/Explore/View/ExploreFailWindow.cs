using FairyGUI;
using Framework.Script.UI;
using GameMain.Script.Explore;

namespace GameMain.Script.UI
{
    /// <summary>
    /// 探索结算 · 失败弹窗（FairyGUI 包 World / ExploreFailWindow，预制体 ExploreFailWindow）。
    ///
    /// UIPopup：带背景模糊、点空白/右上关闭按钮可关闭；关闭后回指挥室。
    /// 结构：
    ///   com_popup   Base/PopupWindow（UIPopup 自动绑定 btn_close）
    ///   txt_title   "调查失败"
    ///   txt_tip     失败原因
    ///   btn_back    返回指挥室
    /// </summary>
    public class ExploreFailWindow : UIPopup
    {
        private GTextField txt_title;
        private GTextField txt_tip;
        private GButton btn_back;

        /// <summary>正在关窗回指挥室，避免 Leave 里再次触发</summary>
        private bool m_Returning;

        protected internal override void Init(object o)
        {
            txt_title = ui.GetChild("txt_title").asTextField;
            txt_tip = ui.GetChild("txt_tip").asTextField;
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

            ExploreRunEndedMsg kMsg = o as ExploreRunEndedMsg;

            txt_title.text = "调查失败";

            // 全灭 → 全员失踪；主动提前撤离但没到目标 → 未达成目标
            bool kWiped = kMsg != null && !kMsg.m_Success;
            txt_tip.text = kWiped
                ? "失败原因【全员失踪】"
                : "失败原因【未达成目标】";
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
