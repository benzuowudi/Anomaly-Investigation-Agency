using Framework.Script.UI;
using GameMain.Script.Explore;

namespace GameMain.Script.UI
{
    /// <summary>
    /// 探索结算入口：按 RunEnded 消息决定打开成功还是失败界面。
    ///
    /// 判定：
    ///   成功 = 成功撤离 且 目标节点已完成；
    ///   失败 = 全灭 / 未达成目标就撤离。
    ///
    /// ExploreView（主动撤离）和 BattleView（战斗结束触发撤离/全灭）都走这里，
    /// 保证只有一份结算判定。
    /// </summary>
    public static class ExploreSettlement
    {
        public static void Show(ExploreRunEndedMsg _msg)
        {
            if (_msg == null)
            {
                return;
            }

            bool kSuccess = _msg.m_Success && _msg.m_GoalReached;
            string kView = kSuccess ? UIDefine.ExploreSuccess : UIDefine.ExploreFail;
            Game.UI.ShowPopup(kView, _msg);
        }
    }
}
