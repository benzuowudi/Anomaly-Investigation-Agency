using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Framework.Script.UI
{
    public abstract class UIAnim : MonoBehaviour
    {
        public float m_Speed = 0.0f;
        public int m_Delay = 0;
        public bool m_IsStartShow = true;

        public virtual void Init() { }
        public virtual UniTask Anim() { return UniTask.CompletedTask; }
        public virtual UniTask Anim(int _delay) { return UniTask.CompletedTask; }
    }
}
