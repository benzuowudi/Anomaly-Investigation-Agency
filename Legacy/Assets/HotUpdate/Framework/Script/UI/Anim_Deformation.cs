using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Framework.Script.UI
{
    /// <summary>
    /// componentËõ·Å
    /// </summary>
    public class Anim_Deformation : UIAnim
    {
        public Vector2 m_Start = Vector2.zero;
        public Vector2 m_End = Vector2.zero;

        public override void Init()
        {
            base.Init();
            transform.GetComponent<RectTransform>().sizeDelta = m_Start;
        }

        public override async UniTask Anim()
        {
            if (m_Delay > 0)
                await UniTask.Delay(m_Delay, cancellationToken: this.GetCancellationTokenOnDestroy());
            Game.Helper.Deformation(transform, m_End, m_Speed);
        }

        public override async UniTask Anim(int _delay)
        {
            if (_delay > 0)
                await UniTask.Delay(_delay, cancellationToken: this.GetCancellationTokenOnDestroy());
            Game.Helper.Deformation(transform, m_End, m_Speed);
        }
    }
}