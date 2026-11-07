using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Framework.Script.UI
{
    /// <summary>
    /// ÇÐ»»componentÏÔÒþ×´Ì¬
    /// </summary>
    public class Anim_Change : UIAnim
    {
        public float m_State_1_alpha = 0.0f;
        public bool m_State_1_touch = false;
        public float m_State_2_alpha = 0.0f;
        public bool m_State_2_touch = false;

        private int m_StateTag = 1;

        public override void Init()
        {
            base.Init();
            if (transform.GetComponent<CanvasGroup>() == null)
            {
                transform.gameObject.AddComponent<CanvasGroup>();
            }
            m_StateTag = 1;
            transform.GetComponent<CanvasGroup>().alpha = m_State_1_alpha;
            transform.GetComponent<CanvasGroup>().interactable = m_State_1_touch;
        }

        public override async UniTask Anim()
        {
            transform.GetComponent<CanvasGroup>().alpha = m_StateTag == 1 ? m_State_1_alpha : m_State_2_alpha;
            if (m_Delay > 0)
                await UniTask.Delay(m_Delay, cancellationToken: this.GetCancellationTokenOnDestroy());
            Game.Helper.Reveal(transform, m_StateTag == 1 ? m_State_2_alpha : m_State_1_alpha, m_Speed);
            transform.GetComponent<CanvasGroup>().interactable = m_StateTag == 1 ? m_State_2_touch : m_State_1_touch;
            m_StateTag *= -1;
        }

        public override async UniTask Anim(int _delay)
        {
            transform.GetComponent<CanvasGroup>().alpha = m_StateTag == 1 ? m_State_1_alpha : m_State_2_alpha;
            if (_delay > 0)
                await UniTask.Delay(_delay, cancellationToken: this.GetCancellationTokenOnDestroy());
            Game.Helper.Reveal(transform, m_StateTag == 1 ? m_State_2_alpha : m_State_1_alpha, m_Speed);
            transform.GetComponent<CanvasGroup>().interactable = m_StateTag == 1 ? m_State_2_touch : m_State_1_touch;
            m_StateTag *= -1;
        }
    }
}