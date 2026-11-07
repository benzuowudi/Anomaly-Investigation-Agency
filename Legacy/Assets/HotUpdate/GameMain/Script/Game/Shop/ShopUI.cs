using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Framework.Script.UI;
using UnityEngine.UI;

namespace GameMain.Script.UI
{
    public class ShopUI : UIWindow
    {
        //商品图片
        public Image img_show;
        //商品中文名称
        public Text txt_show_cn;
        //商品英文名称
        public Text txt_show_en;

        //商品价格
        public Text txt_price;
        //商品类型
        public Text txt_type;
        //商品说明（效果）
        public Text txt_des_1;
        //商品说明（叙事）
        public Text txt_des_2;
        //购买按钮
        public Button btn_buy;

        //商品类型（大类）
        public Dropdown dp_group_1;
        //商品类型（小类）
        public Dropdown dp_group_2;


        protected internal override void Init(object o)
        {
            base.Init(o);
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