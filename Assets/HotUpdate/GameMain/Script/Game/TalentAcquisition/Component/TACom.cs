using System.Collections.Generic;
using FairyGUI;
using FairyGUI.Utils;
using Framework.Script.UI;

namespace GameMain.Script.UI
{
    /// <summary>
    /// 人才招募/定向猎头主卡片（FairyGUI 包 TalentAcquisition / TACom）。
    ///
    /// list_com 按模式填内容：
    ///   SetDesc()            -> TADescCom（合同说明）
    ///   SetDescAndConditions -> TADescCom + TAConditionCom（猎头说明 + 需求）
    /// </summary>
    public class TACom : GComponent
    {
        public enum TAState
        {
            Recruit = 0,
            Headhunter = 1,
        }

        private Controller controller_state;
        private GTextField txt_title;
        private GTextField txt_headhunterTime;
        private GRichTextField txt_consume_1, txt_consume_10;
        private TABtnCom btn_once, btn_ten;
        private TABtn2Com btn_headhunter;
        private GList list_com;
        private TAConditionCom m_ConditionCom;

        private int m_SelectedHeadhunterTime;
        private bool m_HeadhunterRunning;

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            controller_state = GetController("state");
            txt_title = GetChild("txt_title").asTextField;
            txt_headhunterTime = GetChild("txt_headhunterTime").asTextField;
            txt_consume_1 = GetChild("txt_consume_1").asRichTextField;
            txt_consume_10 = GetChild("txt_consume_10").asRichTextField;
            btn_once = GetChild("btn_once") as TABtnCom;
            btn_ten = GetChild("btn_ten") as TABtnCom;
            btn_headhunter = GetChild("btn_headhunter ") as TABtn2Com;
            list_com = GetChild("list_com").asList;

            // 消耗文本会写 "200/1.8万" 这种格式化数字，FGUI 里是 AGENCYR（纯英文字体），
            // 不换字体的话"万"会静默回退到系统字体，和数字变成两种字体混排。
            UIFont.Apply(txt_consume_1);
            UIFont.Apply(txt_consume_10);
        }

        public void SetState(TAState _state)
        {
            controller_state.selectedIndex = (int)_state;
        }

        public void SetTitle(string _title)
        {
            txt_title.text = _title;
        }

        /// <summary>
        /// 设置消耗显示，_icon 传 CommonUI/Base 的图标地址。
        /// </summary>
        public void SetConsume(string _icon, string _once, string _ten)
        {
            txt_consume_1.text = "<img src='" + _icon + "'/>  " + _once;
            txt_consume_10.text = "<img src='" + _icon + "'/>  " + _ten;
        }

        public void SetButtons(string _titleOnce, int _numOnce, string _titleTen, int _numTen)
        {
            btn_once.SetCom(_titleOnce, _numOnce);
            btn_ten.SetCom(_titleTen, _numTen);
        }

        public void SetHeadhunterButton(string _title, string _desc)
        {
            btn_headhunter.enabled = true;
            btn_headhunter.SetCom(_title, _desc);
        }

        /// <summary>
        /// list_com 填 TADescCom（合同说明）。
        /// </summary>
        public void SetDesc(string _title)
        {
            list_com.RemoveChildrenToPool();
            list_com.defaultItem = "ui://TalentAcquisition/TADescCom";

            TADescCom kDesc = list_com.AddItemFromPool() as TADescCom;
            kDesc.SetCom(_title);
        }

        /// <summary>
        /// 定向猎头卡：上面是 TADescCom（猎头说明），下面是 TAConditionCom（猎头需求）。
        /// </summary>
        public void SetDescAndConditions(string _descTitle, string _desc, string _conditionTitle,
            IList<string> _conditions, IList<int> _times, IList<string> _selectedConditions = null)
        {
            list_com.RemoveChildrenToPool();

            list_com.defaultItem = "ui://TalentAcquisition/TADescCom";
            TADescCom kDesc = list_com.AddItemFromPool() as TADescCom;
            kDesc.SetCom(_descTitle, _desc);

            list_com.defaultItem = "ui://TalentAcquisition/TAConditionCom";
            m_ConditionCom = list_com.AddItemFromPool() as TAConditionCom;
            m_ConditionCom.OnSelectionChanged -= OnHeadhunterConditionChanged;
            m_ConditionCom.OnSelectionChanged += OnHeadhunterConditionChanged;
            m_ConditionCom.SetCom(_conditionTitle, _conditions, _times, _selectedConditions);

            m_SelectedHeadhunterTime = m_ConditionCom.GetSelectedTime();
            txt_headhunterTime.text = "--:--:--";
        }

        /// <summary>当前选中条件对应的猎头时间（秒）</summary>
        public int GetSelectedHeadhunterTime()
        {
            return m_SelectedHeadhunterTime;
        }

        /// <summary>当次猎头选中的需求文本</summary>
        public List<string> GetSelectedConditions()
        {
            return m_ConditionCom.GetSelectedConditions();
        }

        /// <summary>猎头进行中：按钮变“猎头中…”，隐藏说明并禁止点击。</summary>
        public void SetHeadhunterRunning(bool _running)
        {
            m_HeadhunterRunning = _running;
            m_ConditionCom.SetSelectable(!_running);

            if (_running)
            {
                btn_headhunter.enabled = false;
                btn_headhunter.SetRunning("猎头中…");
            }
            else
            {
                btn_headhunter.enabled = true;
                btn_headhunter.SetCom("猎头委托", "准备就绪");
            }
        }

        /// <summary>
        /// 外部可以直接设置猎头时间（倒计时/任务剩余时间）。
        /// </summary>
        public void SetHeadhunterTime(string _time)
        {
            txt_headhunterTime.text = _time;
        }

        private void OnHeadhunterConditionChanged(int _totalSeconds)
        {
            m_SelectedHeadhunterTime = _totalSeconds;
            if (!m_HeadhunterRunning)
            {
                txt_headhunterTime.text = _totalSeconds > 0 ? FormatTime(_totalSeconds) : "--:--:--";
            }
        }

        public static string FormatTime(int _totalSeconds)
        {
            int kHours = _totalSeconds / 3600;
            int kMinutes = (_totalSeconds % 3600) / 60;
            int kSeconds = _totalSeconds % 60;
            return string.Format("{0:D2}:{1:D2}:{2:D2}", kHours, kMinutes, kSeconds);
        }

        public EventListener onClickOnce
        {
            get { return btn_once.onClick; }
        }

        public EventListener onClickTen
        {
            get { return btn_ten.onClick; }
        }

        public EventListener onClickHeadhunter
        {
            get { return btn_headhunter.onClick; }
        }
    }
}
