using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Framework.Script.UI
{
    /// <summary>
    /// componentœ‘“˛
    /// </summary>
    public class Anim_Reveal : UIAnim
    {
        public float m_Start = 0.0f;
        public float m_End = 0.0f;

        public override void Init()
        {
            base.Init();
            if (transform.GetComponent<CanvasGroup>() == null)
            {
                transform.gameObject.AddComponent<CanvasGroup>();
            }
            transform.GetComponent<CanvasGroup>().alpha = m_Start;
        }

        public override async UniTask Anim()
        {
            if (m_Delay > 0)
                await UniTask.Delay(m_Delay, cancellationToken: this.GetCancellationTokenOnDestroy());
            Game.Helper.Reveal(transform, m_End, m_Speed);
        }

        public override async UniTask Anim(int _delay)
        {
            if (_delay > 0)
                await UniTask.Delay(_delay, cancellationToken: this.GetCancellationTokenOnDestroy());
            Game.Helper.Reveal(transform, m_End, m_Speed);
        }
    }
}