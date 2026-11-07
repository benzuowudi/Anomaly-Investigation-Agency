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
        /// 运行时代码写入中文后会静默回退到系统字体，这里统一改成中文字体 FZYTK。
        /// </summary>
        private void ApplyButtonFont()
        {
            ApplyFont(btn_continue);
            ApplyFont(btn_newGame);
            ApplyFont(btn_option);
            ApplyFont(btn_exit);
        }

        private static void ApplyFont(GButton _button)
        {
            GTextField kText = _button.GetTextField();
            TextFormat kFormat = kText.textFormat;
            kFormat.font = "FZYTK";
            kText.textFormat = kFormat;
        }

        private void OnClickNewGame()
        {
            Game.UI.ShowWindow(UIDefine.CommandRoom, _isHideCurrentUI: true);
        }
    }
}
