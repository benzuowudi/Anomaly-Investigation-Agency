using FairyGUI;
using Framework.Script.UI;

namespace GameMain.Script.UI
{
    public class GameStartView : UIWindow
    {
        private GTextField txt_title;
        private GButton btn_continue, btn_newGame, btn_option, btn_exit;

        protected internal override void Init(object o)
        {
            txt_title = ui.GetChild("txt_title").asTextField;
            btn_continue = ui.GetChild("btn_continue").asButton;
            btn_newGame = ui.GetChild("btn_newGame").asButton;
            btn_option = ui.GetChild("btn_option").asButton;
            btn_exit = ui.GetChild("btn_exit").asButton;

            // 节点绑定完成后再触发 AddEvent
            base.Init(o);
            ApplyButtonFont();
        }

        protected internal override void AddEvent()
        {
            btn_newGame.onClick.Add(OnClickNewGame);
        }

        protected internal override void RemoveEvent()
        {
            btn_newGame.onClick.Remove(OnClickNewGame);
        }

        protected internal override void Enter(object o)
        {
            base.Enter(o);
            btn_continue.visible = false;
            btn_newGame.title = "进入游戏";
            btn_option.title = "设置选项";
            btn_exit.title = "退出游戏";
        }

        /// <summary>
        /// CommonBtn2 的 title 在 FGUI 里用的是 AGENCYR（纯英文字体），
        /// 运行时代码写入中文后会静默回退到系统字体，这里统一换成中英混排字体（见 UIFont）。
        /// </summary>
        private void ApplyButtonFont()
        {
            UIFont.Apply(btn_continue);
            UIFont.Apply(btn_newGame);
            UIFont.Apply(btn_option);
            UIFont.Apply(btn_exit);
        }

        private void OnClickNewGame()
        {
            Game.UI.ShowWindow(UIDefine.CommandRoom, _isHideCurrentUI: true);
        }
    }
}
