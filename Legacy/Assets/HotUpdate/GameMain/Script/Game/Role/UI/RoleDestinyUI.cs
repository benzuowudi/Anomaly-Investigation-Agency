using Framework.Script.UI;
using UnityEngine.UI;

namespace GameMain.Script.UI
{
    public class RoleDestinyUI : UIWindow
    {
        private Image img_bg;
        private Image img_RingBg;
        private Text txt_title_cn;
        private Text txt_title_en;
        private Text txt_Name_cn;
        private Text txt_Name_en;
        private Text txt_level;
        private Text txt_star;
        private Button btn_node_1;
        private Button btn_node_2;
        private Button btn_node_3;
        private Button btn_node_4;
        private Button btn_node_5;
        private Image img_node_1;
        private Image img_node_2;
        private Image img_node_3;
        private Image img_node_4;
        private Image img_node_5;
        private Button btn_echo;
        protected internal override void Init(object o)
        {
            base.Init(o);
            img_bg = Game.UI.GetNode<Image>(this, "img_bg");
            img_RingBg = Game.UI.GetNode<Image>(this, "img_RingBg");
            txt_title_cn = Game.UI.GetNode<Text>(this, "txt_title_cn");
            txt_title_en = Game.UI.GetNode<Text>(this, "txt_title_en");
            txt_Name_cn = Game.UI.GetNode<Text>(this, "txt_Name_cn");
            txt_Name_en = Game.UI.GetNode<Text>(this, "txt_Name_en");
            txt_level = Game.UI.GetNode<Text>(this, "txt_level");
            txt_star = Game.UI.GetNode<Text>(this, "txt_star");
            btn_node_1 = Game.UI.GetNode<Button>(this, "btn_node_1");
            btn_node_2 = Game.UI.GetNode<Button>(this, "btn_node_2");
            btn_node_3 = Game.UI.GetNode<Button>(this, "btn_node_3");
            btn_node_4 = Game.UI.GetNode<Button>(this, "btn_node_4");
            btn_node_5 = Game.UI.GetNode<Button>(this, "btn_node_5");
            img_node_1 = Game.UI.GetNode<Image>(this, "img_node_1");
            img_node_2 = Game.UI.GetNode<Image>(this, "img_node_2");
            img_node_3 = Game.UI.GetNode<Image>(this, "img_node_3");
            img_node_4 = Game.UI.GetNode<Image>(this, "img_node_4");
            img_node_5 = Game.UI.GetNode<Image>(this, "img_node_5");
            btn_echo = Game.UI.GetNode<Button>(this, "btn_echo");
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