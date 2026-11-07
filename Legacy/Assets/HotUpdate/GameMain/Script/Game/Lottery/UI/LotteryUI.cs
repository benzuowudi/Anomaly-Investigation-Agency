using Framework.Script.UI;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GameMain.Script.UI
{
    public class LotteryUI : UIWindow
    {
        //购买界面
        public GameObject obj_buyUI;
        //奖励界面
        public GameObject obj_rewardUI;

        //看板角色
        public Image img_kanban;

        //常驻卡池-按钮
        public Button btn_normal;
        //常驻卡池-中文名称
        public Text txt_normal_cn;
        //常驻卡池-英文名称
        public Text txt_normal_en;

        //皮肤卡池-按钮
        public Button btn_skin;
        //皮肤卡池-中文名称
        public Text txt_skin_cn;
        //皮肤卡池-英文名称
        public Text txt_skin_en;

        //UP卡池-按钮
        public Button btn_special;
        //UP卡池-中文名称
        public Text txt_special_cn;
        //UP卡池-英文名称
        public Text txt_special_en;

        //说明按钮
        public Button btn_des;
        //卡池展示图片
        public Image img_caipiao;

        //号码1~7
        public Text txt_zhongjiang_01;
        public Text txt_zhongjiang_02;
        public Text txt_zhongjiang_03;
        public Text txt_zhongjiang_04;
        public Text txt_zhongjiang_05;
        public Text txt_zhongjiang_06;
        public Text txt_zhongjiang_07;

        //记录查看按钮
        public Button btn_jilu_des;
        //首页展示的七个抽奖记录
        public List<LotteryJiLuItem> m_LotteryJiLuItemList = new List<LotteryJiLuItem>();

        //购买一次-按钮
        public Button btn_buyonce;
        //购买一次-代币
        public Image img_doller_once;
        //购买一次-消耗
        public Text txt_once_pay;

        //购买十次-按钮
        public Button btn_buyten;
        //购买十次-代币
        public Image img_doller_ten;
        //购买十次-消耗
        public Text txt_ten_pay;

        //当期中奖号码
        public Text txt_reward_num;

        //中奖物品
        public List<LotteryItem> m_LotteryItemList = new List<LotteryItem>();

        //关闭弹窗按钮
        public Button btn_reward_close;

        //抽奖号码
        public List<Text> m_RewardNumList = new List<Text>();

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

        private void ShowRewardWindow()
        {

        }

        private void SaveReward()
        {
            //BattleRole kReward = new BattleRole();
            //GameInit.Data.SaveData(kReward);
        }
    }
}