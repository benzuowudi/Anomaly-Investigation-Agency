using System;
using System.Collections.Generic;
using FairyGUI;
using FairyGUI.Utils;

namespace GameMain.Script.UI
{
    /// <summary>
    /// 猎头需求列表（FairyGUI 包 TalentAcquisition / TAConditionCom）。
    /// list_select 按内容填 TASelectBtn，填完后列表和自身高度都要 ResizeToFit。
    /// </summary>
    public class TAConditionCom : GComponent
    {
        private GTextField txt_title;
        private GImage img_bg;
        private GList list_select;

        /// <summary>勾选/取消勾选条件时触发，参数是当前选中条件的总时间（秒）。</summary>
        public event Action<int> OnSelectionChanged;

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);
            txt_title = GetChild("txt_title").asTextField;
            img_bg = GetChild("n10").asImage;
            list_select = GetChild("list_select").asList;
            list_select.onClickItem.Add(OnClickItem);
        }

        public void SetCom(string _title, IList<string> _conditions, IList<int> _times,
            IList<string> _selected = null)
        {
            txt_title.text = _title;

            list_select.RemoveChildrenToPool();
            for (int i = 0; i < _conditions.Count; i++)
            {
                TASelectBtn kItem = list_select.AddItemFromPool() as TASelectBtn;
                kItem.SetCom(_conditions[i], _times[i], IsSelected(_selected, _conditions[i]));
            }

            ResizeToFit();
        }

        /// <summary>猎头进行中禁止改选，保持当次选择的猎头需求。</summary>
        public void SetSelectable(bool _selectable)
        {
            list_select.touchable = _selectable;
        }

        private static bool IsSelected(IList<string> _selected, string _condition)
        {
            if (_selected == null)
            {
                return false;
            }

            for (int i = 0; i < _selected.Count; i++)
            {
                if (_selected[i] == _condition)
                {
                    return true;
                }
            }

            return false;
        }

        public List<string> GetSelectedConditions()
        {
            List<string> kResult = new List<string>();
            List<int> kIndices = list_select.GetSelection();
            for (int i = 0; i < kIndices.Count; i++)
            {
                TASelectBtn kBtn = list_select.GetChildAt(kIndices[i]) as TASelectBtn;
                kResult.Add(kBtn.title);
            }

            return kResult;
        }

        public int GetSelectedTime()
        {
            int kTotal = 0;
            List<int> kIndices = list_select.GetSelection();
            for (int i = 0; i < kIndices.Count; i++)
            {
                TASelectBtn kBtn = list_select.GetChildAt(kIndices[i]) as TASelectBtn;
                kTotal += kBtn.ConditionTime;
            }

            return kTotal;
        }

        private void OnClickItem(EventContext _context)
        {
            if (OnSelectionChanged != null)
            {
                OnSelectionChanged(GetSelectedTime());
            }
        }

        /// <summary>
        /// 列表内容变化后按实际内容重算高度，背景和组件本体一起跟着变。
        /// </summary>
        public void ResizeToFit()
        {
            list_select.ResizeToFit();

            float kHeight = 61f + list_select.height + 16f;
            SetSize(width, kHeight);
            img_bg.height = kHeight - img_bg.y;
        }
    }
}
