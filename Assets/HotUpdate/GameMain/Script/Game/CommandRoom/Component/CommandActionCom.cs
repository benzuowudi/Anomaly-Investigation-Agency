using System;
using FairyGUI;
using FairyGUI.Utils;

namespace GameMain.Script.UI
{
    /// <summary>
    /// 指挥室「开始行动」区块（FairyGUI 包 CommandRoom / CommandActionCom）。
    ///
    /// 结构：章节信息（txt_bookNum / txt_bookTitle / txt_bookTime / txt_chapter / txt_titleC / txt_titleE）
    ///       btn_bookAdd 章节选择、btn_action 开始行动（CommonBtn）。
    ///
    /// 组件只负责把按钮点击转给 View（Init 传入回调），不自己打开界面，
    /// 保持「组件不碰业务跳转」的约定。
    /// </summary>
    public class CommandActionCom : GComponent
    {
        private GTextField txt_bookNum, txt_bookTitle, txt_bookTime, txt_chapter, txt_titleE, txt_titleC, txt_red;
        private GButton btn_bookAdd;
        private GButton btn_action;
        private GGroup part_red;

        /// <summary>「开始行动」回调，由 CommandBaseView 传入</summary>
        private Action m_OnAction;

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);
            txt_bookNum = GetChild("txt_bookNum").asTextField;
            txt_bookTitle = GetChild("txt_bookTitle").asTextField;
            txt_bookTime = GetChild("txt_bookTime").asTextField;
            txt_chapter = GetChild("txt_chapter").asTextField;
            txt_titleE = GetChild("txt_titleE").asTextField;
            txt_titleC = GetChild("txt_titleC").asTextField;
            txt_red = GetChild("txt_red").asTextField;
            btn_bookAdd = GetChild("btn_bookAdd").asButton;
            btn_action = GetChild("btn_action").asButton;
            part_red = GetChild("part_red").asGroup;

            btn_action.onClick.Add(OnClickAction);
        }

        /// <summary>
        /// 挂「开始行动」的点击回调（CommandBaseView.Init 里调用一次）。
        /// </summary>
        public void Init(Action _onAction)
        {
            m_OnAction = _onAction;
        }

        public void SetCom()
        {
        }

        private void OnClickAction()
        {
            if (m_OnAction != null)
            {
                m_OnAction();
            }
        }
    }
}
