using Framework.Script.UI;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using GameMain.Script.Logic;
using cfg.GameType;

namespace GameMain.Script.UI
{
    public class BattleOrderRing : UIWindow
    {
        public Image img_OrderRing;
        public Button btn_OrderButton_Retreat;
        public Button btn_OrderButton_Attack;
        public Button btn_OrderButton_Move;
        public Button btn_OrderButton_Standby;
        public Button btn_OrderButton_Equip;
        public Button btn_OrderButton_Item;

        private List<Transform> m_AlphaList = new List<Transform>();

        private BattleLogic m_Logic;

        private float m_ShowSpeed = 2f;
        protected internal override void Init(object o)
        {
            base.Init(o);
            btn_OrderButton_Retreat.onClick.AddListener(OnClickRetreatBtn);
            btn_OrderButton_Attack.onClick.AddListener(OnClickAttackBtn);
            btn_OrderButton_Move.onClick.AddListener(OnClickMoveBtn);
            btn_OrderButton_Standby.onClick.AddListener(OnClickStandbyBtn);
            btn_OrderButton_Equip.onClick.AddListener(OnClickEquipBtn);
            btn_OrderButton_Item.onClick.AddListener(OnClickItemBtn);

            m_AlphaList.Add(btn_OrderButton_Retreat.transform);
            m_AlphaList.Add(btn_OrderButton_Attack.transform);
            m_AlphaList.Add(btn_OrderButton_Move.transform);
            m_AlphaList.Add(btn_OrderButton_Standby.transform);
            m_AlphaList.Add(btn_OrderButton_Item.transform);
            m_AlphaList.Add(btn_OrderButton_Equip.transform);

            m_Logic = Game.Logic.GetLogic<BattleLogic>();
        }

        protected internal override void Enter(object o)
        {
            base.Enter(o);
        }

        protected internal override void Leave()
        {
            base.Leave();
        }

        protected internal override void Destroy()
        {
            base.Destroy();
            btn_OrderButton_Retreat.onClick.RemoveListener(OnClickRetreatBtn);
            btn_OrderButton_Attack.onClick.RemoveListener(OnClickAttackBtn);
            btn_OrderButton_Move.onClick.RemoveListener(OnClickMoveBtn);
            btn_OrderButton_Standby.onClick.RemoveListener(OnClickStandbyBtn);
            btn_OrderButton_Equip.onClick.RemoveListener(OnClickEquipBtn);
            btn_OrderButton_Item.onClick.RemoveListener(OnClickItemBtn);
        }

        protected void OnClickRetreatBtn()
        {
            HideRingUI();
            Game.Event.SendMessage("OrderRing", BattleOrderType.Retreat);
        }

        private void OnClickAttackBtn()
        {
            HideRingUI();
            Game.Event.SendMessage("OrderRing", BattleOrderType.Attack);
        }

        private void OnClickMoveBtn()
        {
            HideRingUI();
            Game .Event.SendMessage("OrderRing", BattleOrderType.Move);
        }

        private void OnClickStandbyBtn()
        {
            HideRingUI();
            Game.Event.SendMessage("OrderRing", BattleOrderType.Standby);
        }

        private void OnClickSkillBtn()
        {
            HideRingUI();
            Game.Event.SendMessage("OrderRing", BattleOrderType.Skill);
        }

        private void OnClickEquipBtn()
        {
            HideRingUI();
            Game.Event.SendMessage("OrderRing", BattleOrderType.Equip);

        }

        private void OnClickItemBtn()
        {
            HideRingUI();
            Game.Event.SendMessage("OrderRing", BattleOrderType.Item);
        }

        public void ShowRingUI()
        {
            btn_OrderButton_Retreat.interactable = true;
            btn_OrderButton_Attack.interactable = true;
            btn_OrderButton_Move.interactable = true;
            btn_OrderButton_Standby.interactable = true;
            btn_OrderButton_Equip.interactable = true;
            btn_OrderButton_Item.interactable = true;
            ShowRingAnim();

            ShowButtonsAsync().Forget();
        }

        private async UniTaskVoid ShowButtonsAsync()
        {
            var cancellationToken = this.GetCancellationTokenOnDestroy();
            for (int i = 0; i < m_AlphaList.Count; i++)
            {
                ShowButtonAnim(m_AlphaList[i]);
                await UniTask.Delay(200, cancellationToken: cancellationToken);
            }
        }

        private void HideRingUI()
        {
            btn_OrderButton_Retreat.interactable = false;
            btn_OrderButton_Attack.interactable = false;
            btn_OrderButton_Move.interactable = false;
            btn_OrderButton_Standby.interactable = false;
            btn_OrderButton_Equip.interactable = false;
            btn_OrderButton_Item.interactable = false;
            HideRingAnim();

            HideButtonsAsync().Forget();
        }

        private async UniTaskVoid HideButtonsAsync()
        {
            var cancellationToken = this.GetCancellationTokenOnDestroy();
            for (int i = m_AlphaList.Count - 1; i >= 0; i--)
            {
                HideButtonAnim(m_AlphaList[i]);
                await UniTask.Delay(200, cancellationToken: cancellationToken);
            }
        }

        private void ShowRingAnim()
        {
            DOTween.To(() => img_OrderRing.fillAmount, x => img_OrderRing.fillAmount = x, 1, m_ShowSpeed);
        }

        private void ShowButtonAnim(Transform _alpha)
        {
            DOTween.To(() => _alpha.GetComponent<CanvasGroup>().alpha, x => _alpha.GetComponent<CanvasGroup>().alpha = x, 1, m_ShowSpeed);
        }

        private void HideRingAnim()
        {
            DOTween.To(() => img_OrderRing.fillAmount, x => img_OrderRing.fillAmount = x, 0, m_ShowSpeed);
        }

        private void HideButtonAnim(Transform _alpha)
        {
            DOTween.To(() => _alpha.GetComponent<CanvasGroup>().alpha, x => _alpha.GetComponent<CanvasGroup>().alpha = x, 0, m_ShowSpeed);
        }
    }
}