using Framework.Script.Helper;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Framework.Script.UI
{
    /// <summary>
    /// componentÎ»ÒÆ
    /// </summary>
    public class Anim_Pan : UIAnim
    {
        public Vector3 m_Start = Vector3.zero;
        public Vector3 m_End = Vector3.zero;

        public override void Init()
        {
            base.Init();
            transform.SetLocalPositionAndRotation(m_Start, Quaternion.identity);
        }

        public override async UniTask Anim()
        {
            if (m_Delay > 0)
                await UniTask.Delay(m_Delay, cancellationToken: this.GetCancellationTokenOnDestroy());
            Game.Helper.Pan(transform, m_End, m_Speed);
        }

        public override async UniTask Anim(int _delay)
        {
            if (_delay > 0)
                await UniTask.Delay(_delay, cancellationToken: this.GetCancellationTokenOnDestroy());
            Game.Helper.Pan(transform, m_End, m_Speed);
        }
    }
}