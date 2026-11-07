using FairyGUI;

namespace Framework.Script.UI
{
    /// <summary>
    /// FairyGUI 字体小工具（中英混排字体）。
    ///
    /// 字体约定（详见 UIManager.OnInit 的注释）：
    ///   Assets/Resources/Fonts/FZYTK.TTF 是用 fontTools 合并过的字体——
    ///   ASCII 字母/数字/半角标点取自 Agency FB，汉字和中文标点取自方正姚体，
    ///   所以一个 GTextField 里就能自动中英混排。
    ///   AGENCYR / AGENCYB 是纯英文字体，往里写中文不会报错，而是静默回退到系统字体，
    ///   于是同一段文字里出现两种字体（例如 NumberFormat 出来的 "1.8万"）。
    ///
    /// 结论：运行时代码会写中文或格式化数字的文本，在绑定节点后调用一次 Apply 换成 FZYTK。
    /// </summary>
    public static class UIFont
    {
        /// <summary>中英混排字体名（Assets/Resources/Fonts/FZYTK.TTF）</summary>
        public const string c_Mixed = "FZYTK";

        /// <summary>
        /// 文本字段换成中英混排字体：数字/英文字形不变，汉字不再回退系统字体。
        /// </summary>
        public static void Apply(GTextField _text)
        {
            TextFormat kFormat = _text.textFormat;
            kFormat.font = c_Mixed;
            _text.textFormat = kFormat;
        }

        /// <summary>
        /// 按钮标题换成中英混排字体（FGUI 里按钮标题默认是 AGENCYR 之类的纯英文字体）。
        /// </summary>
        public static void Apply(GButton _button)
        {
            Apply(_button.GetTextField());
        }
    }
}
