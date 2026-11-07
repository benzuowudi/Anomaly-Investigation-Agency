using FairyGUI;

namespace GameMain.Script.UI
{
    /// <summary>
    /// 猎头需求单条选择按钮（FairyGUI 包 TalentAcquisition / TASelectBtn）。
    /// 文本和选择状态都集中在按钮本身。
    /// </summary>
    public class TASelectBtn : GButton
    {
        /// <summary>该条件对应的猎头时间（秒）</summary>
        public int ConditionTime { get; private set; }

        public void SetCom(string _title, int _time, bool _selected)
        {
            ConditionTime = _time;
            title = _title;
            selected = _selected;
        }

        public bool IsSelected
        {
            get { return selected; }
        }
    }
}
