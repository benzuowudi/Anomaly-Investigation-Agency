using System.Collections.Generic;
using FairyGUI;
using Framework.Script.Manager;
using Framework.Script.UI;
using GameMain.Script.Role;
using GameMain.Script.TalentAcquisition;

namespace GameMain.Script.UI
{
    /// <summary>
    /// 招募结果界面（FairyGUI 包 TalentAcquisition / TalentAcquisitionResultView）。
    ///
    /// state 控制器：
    ///   once  单张合同：3 名候选人，选 1 名
    ///   ten   十张合同：30 名候选人，选 10 名
    ///
    /// 选满后立即把选中的角色写入角色库并关闭结果界面；
    /// 定向猎头结果是已入职角色，只展示，不参与选择。
    /// </summary>
    public class TalentAcquisitionResultView : UIWindow
    {
        private GButton btn_back;
        private Controller controller_state;

        /// <summary>FGUI 发布时重名节点可能带 _1/_2/_3 后缀，全部收进来统一隐藏</summary>
        private readonly RoleCardCom[] m_OnceCards = new RoleCardCom[4];
        private readonly RoleCardCom[] m_Cards = new RoleCardCom[30];

        private readonly List<RoleCardCom> m_ActiveCards = new List<RoleCardCom>();
        private readonly List<RoleRecord> m_ActiveRoles = new List<RoleRecord>();
        private readonly List<RoleRecord> m_SelectedRoles = new List<RoleRecord>();

        private TalentAcquisitionResult m_Result;

        protected internal override void Init(object o)
        {
            // RoleCardCom 在 Base 包里，但立绘仍引用 Role 包的图片，结果界面需要确保 Role 已加载
            FairyGUIManager.Instance.LoadPackage("Role");

            btn_back = ui.GetChild("btn_back").asButton;
            controller_state = ui.GetController("state");

            m_OnceCards[0] = ui.GetChild("com_card_once") as RoleCardCom;
            m_OnceCards[1] = ui.GetChild("com_card_once_1") as RoleCardCom;
            m_OnceCards[2] = ui.GetChild("com_card_once_2") as RoleCardCom;
            m_OnceCards[3] = ui.GetChild("com_card_once_3") as RoleCardCom;

            for (int i = 0; i < m_OnceCards.Length; i++)
            {
                AddCardClick(m_OnceCards[i]);
            }

            for (int i = 0; i < m_Cards.Length; i++)
            {
                m_Cards[i] = ui.GetChild("com_card_" + (i + 1)) as RoleCardCom;
                AddCardClick(m_Cards[i]);
            }

            base.Init(o);
        }

        protected internal override void AddEvent()
        {
            btn_back.onClick.Add(OnClickBack);
        }

        protected internal override void RemoveEvent()
        {
            btn_back.onClick.Remove(OnClickBack);
        }

        protected internal override void Enter(object o)
        {
            base.Enter(o);
            m_Result = o as TalentAcquisitionResult;
            m_SelectedRoles.Clear();
            ShowResult();
        }

        private void AddCardClick(RoleCardCom _card)
        {
            if (_card == null)
            {
                return;
            }

            _card.onClick.Add(OnCardClick);
        }

        private void OnCardClick(EventContext _context)
        {
            OnClickCard((RoleCardCom)_context.sender);
        }

        private void ShowResult()
        {
            HideAllCards();
            m_ActiveCards.Clear();
            m_ActiveRoles.Clear();

            if (m_Result == null || m_Result.m_Candidates.Count == 0)
            {
                return;
            }

            if (m_Result.m_IsTen)
            {
                controller_state.selectedIndex = 1;
                ShowActive(m_Cards, m_Result.m_Candidates, m_Result.m_IsDisplayOnly);
            }
            else
            {
                controller_state.selectedIndex = 0;
                ShowActive(m_OnceCards, m_Result.m_Candidates, m_Result.m_IsDisplayOnly);
            }
        }

        private void ShowActive(RoleCardCom[] _slots, List<RoleRecord> _roles, bool _displayOnly)
        {
            int kCount = 0;
            for (int i = 0; i < _slots.Length && kCount < _roles.Count; i++)
            {
                RoleCardCom kCard = _slots[i];
                if (kCard == null)
                {
                    continue;
                }

                kCard.visible = true;
                kCard.SetCom(_roles[kCount]);
                kCard.SetSelected(_displayOnly);

                m_ActiveCards.Add(kCard);
                m_ActiveRoles.Add(_roles[kCount]);
                kCount++;
            }
        }

        private void OnClickCard(RoleCardCom _card)
        {
            if (m_Result == null || m_Result.m_IsDisplayOnly)
            {
                return;
            }

            int kIndex = m_ActiveCards.IndexOf(_card);
            if (kIndex < 0)
            {
                return;
            }

            RoleRecord kRole = m_ActiveRoles[kIndex];
            if (_card.IsSelected)
            {
                _card.SetSelected(false);
                m_SelectedRoles.Remove(kRole);
                return;
            }

            if (m_SelectedRoles.Count >= m_Result.m_Required)
            {
                return;
            }

            _card.SetSelected(true);
            m_SelectedRoles.Add(kRole);

            if (m_SelectedRoles.Count >= m_Result.m_Required)
            {
                CommitAndClose();
            }
        }

        private void CommitAndClose()
        {
            TalentAcquisitionSystem.CommitSelection(m_Result, m_SelectedRoles);
            m_Result = null;
            Game.UI.ShowWindow(UIDefine.TalentAcquisition);
        }

        private void HideAllCards()
        {
            for (int i = 0; i < m_OnceCards.Length; i++)
            {
                if (m_OnceCards[i] != null)
                {
                    m_OnceCards[i].visible = false;
                    m_OnceCards[i].SetSelected(false);
                }
            }

            for (int i = 0; i < m_Cards.Length; i++)
            {
                m_Cards[i].visible = false;
                m_Cards[i].SetSelected(false);
            }
        }

        private void OnClickBack()
        {
            m_Result = null;
            Game.UI.ShowWindow(UIDefine.TalentAcquisition);
        }
    }
}
