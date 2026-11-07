using UnityEngine;
using Framework.Script.UI;

namespace GameMain.Script.UI
{
    public class RoleInfoUI : UIWindow
    {
        public Transform part_roleshow;
        public Transform part_roleskill;
        public Transform part_roleinfo;


        protected internal override void Init(object o)
        {
            base.Init(o);
            part_roleshow = Game.UI.GetNode<Transform>(this, "part_roleshow");
            part_roleskill = Game.UI.GetNode<Transform>(this, "part_roleskill");
            part_roleinfo = Game.UI.GetNode<Transform>(this, "part_roleinfo");
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
        }
    }
}