using Framework.Script.UI;
using UnityEngine;
using UnityEngine.UI;

namespace GameMain.Script.UI
{
    public class RoleGrowUI : UIWindow
    {
        public ScrollRect list_RoleAtt;

        public Text txt_up_show;
        public ScrollRect list_up_RoleAtt;
        public Transform list_consume;
        public Button btn_LevelUp;
        protected internal override void Init(object o)
        {
            base.Init(o);
            list_RoleAtt = Game.UI.GetNode<ScrollRect>(this, "list_RoleAtt");

            txt_up_show = Game.UI.GetNode<Text>(this, "txt_up_show");
            list_up_RoleAtt = Game.UI.GetNode<ScrollRect>(this, "list_up_RoleAtt");
            list_consume = Game.UI.GetNode<Transform>(this, "list_consume");
            btn_LevelUp = Game.UI.GetNode<Button>(this, "btn_LevelUp");
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