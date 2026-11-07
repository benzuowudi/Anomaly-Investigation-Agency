using UnityEngine;

namespace Framework.Script.Logic
{
    public abstract class LogicBase : MonoBehaviour
    {
        protected internal abstract void Init();
        protected internal abstract void Enter(object o);
        protected internal abstract void UpData(object o);
        protected internal abstract void Leave();
        protected internal abstract void Destroy();
    }
}