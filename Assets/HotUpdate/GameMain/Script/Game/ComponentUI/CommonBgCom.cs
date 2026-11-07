using System;
using FairyGUI;
using FairyGUI.Utils;

namespace GameMain.Script.UI
{
    /// <summary>
    /// 通用背景框（FairyGUI 包 Base / CommonBgCom）。
    /// 各界面把它铺在背景之上，替换掉每张界面自己的内容边框、返回按钮和中英文标题，界面里只留业务内容。
    /// 品牌名、日期、版本号在常驻的 BackGroundVIew 上，这里不处理。
    ///
    /// 结构：
    ///   n1                 内容边框 ui_bg_box（九宫格，跟随父级拉伸）
    ///   btn_back           返回按钮（CommonBackBtn），点击事件由 Init 传入，界面自己不持有
    ///   txt_title_chinese  中文标题
    ///   txt_title_english  英文标题
    /// </summary>
    public class CommonBgCom : GComponent
    {
        private GButton btn_back;
        private GTextField txt_title_chinese;
        private GTextField txt_title_english;

        /// <summary>界面传进来的返回事件，为空表示这个界面不需要返回按钮</summary>
        private Action m_OnBack;

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);
            btn_back = GetChild("btn_back").asButton;
            txt_title_chinese = GetChild("txt_title_chinese").asTextField;
            txt_title_english = GetChild("txt_title_english").asTextField;

            // 只注册一次，具体返回目标由 Init 传进来
            btn_back.onClick.Add(OnClickBack);
        }

        /// <summary>
        /// 初始化背景框：填中英文标题，并把返回事件挂到自己的 btn_back 上。
        /// 界面在 Init 里取到 com_bg 之后调用一次即可：
        /// com_bg.Init("人才招募", "TALENT ACQUISITION", OnClickBack);
        /// 传 null 表示这个界面没有返回，按钮会自动隐藏。
        /// </summary>
        public void Init(string _titleChinese, string _titleEnglish, Action _onBack)
        {
            txt_title_chinese.text = _titleChinese;
            txt_title_english.text = _titleEnglish;

            m_OnBack = _onBack;
            btn_back.visible = _onBack != null;
        }

        private void OnClickBack()
        {
            m_OnBack?.Invoke();
        }
    }
}
