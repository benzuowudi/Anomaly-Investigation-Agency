using System.Collections.Generic;
using FairyGUI;
using Framework.Script.UI;
using GameMain.Script.BattleSetting;

namespace GameMain.Script.UI
{
    /// <summary>
    /// 战前整备界面（FairyGUI 包 BattleSetting / BattleSettingView，预制体 BattleSettingView）。
    ///
    /// 结构：
    ///   com_bg              通用背景框（标题 / 返回）
    ///   txt_name_chinese    关卡名（来自 encounter 表）
    ///   txt_name_english    关卡编号
    ///   com_team            行动队员（最多 5 人，BattleSettingACCom）
    ///   com_support         队伍支援栏（5 格，BattleSettingSupportCom）
    ///   com_item            机械骡货仓（3 格，BattleSettingItemCom）
    ///   btn_battle          开始战斗
    ///
    /// 数据全部来自 BattleSettingSystem.BuildSnapshot()，点击只调 Model；
    /// 事件刷新：BattleSettingEvent.OnBattleSettingChanged。
    /// </summary>
    public class BattleSettingView : UIWindow
    {
        private CommonBgCom com_bg;
        private GTextField txt_name_chinese, txt_name_english;
        private BattleSettingACCom com_team;
        private BattleSettingSupportCom com_support;
        private BattleSettingItemCom com_item;
        private GButton btn_battle;

        private bool m_Subscribed;

        /// <summary>战前整备模型（由 ModelManager 注册）</summary>
        private static BattleSettingSystem Setting
        {
            get { return Game.Model.Get<BattleSettingSystem>(); }
        }

        protected internal override void Init(object o)
        {
            com_bg = ui.GetChild("com_bg") as CommonBgCom;
            txt_name_chinese = ui.GetChild("txt_name_chinese").asTextField;
            txt_name_english = ui.GetChild("txt_name_english").asTextField;
            com_team = ui.GetChild("com_team") as BattleSettingACCom;
            com_support = ui.GetChild("com_support") as BattleSettingSupportCom;
            com_item = ui.GetChild("com_item") as BattleSettingItemCom;
            btn_battle = ui.GetChild("btn_battle").asButton;
            // 战前整备在探索流程里是"出发"：确认后进入异常区域，而不是直接开战
            btn_battle.title = "出　发";

            // 空队员格 → 角色选择弹窗；已填格子的 btn_cut → 移出队伍
            com_team.onAdd = OnClickAddSlot;
            com_team.onCut = OnClickCutSlot;

            com_bg.Init("战前整备", "BATTLE SETTING", OnClickBack);

            base.Init(o);
        }

        protected internal override void AddEvent()
        {
            base.AddEvent();
            btn_battle.onClick.Add(OnClickBattle);
        }

        protected internal override void RemoveEvent()
        {
            base.RemoveEvent();
            btn_battle.onClick.Remove(OnClickBattle);
        }

        protected internal override void Enter(object o)
        {
            base.Enter(o);
            Subscribe();
            Refresh();
        }

        protected internal override void Leave()
        {
            UnSubscribe();
            base.Leave();
        }

        protected internal override void Destroy()
        {
            UnSubscribe();
            base.Destroy();
        }

        #region 刷新

        private void Refresh()
        {
            BattleSettingSnapshot kSnapshot = Setting.BuildSnapshot();

            txt_name_chinese.text = string.IsNullOrEmpty(kSnapshot.m_EncounterName)
                ? "未选择关卡"
                : kSnapshot.m_EncounterName;
            txt_name_english.text = kSnapshot.m_EncounterId > 0
                ? "ENCOUNTER " + kSnapshot.m_EncounterId.ToString("D3")
                : "NO ENCOUNTER";

            com_team.SetCom(kSnapshot);
            com_support.SetCom(kSnapshot.m_SupportItems);
            com_item.SetCom(kSnapshot.m_MuleItems);

            // 这个界面在探索流程里是"出发"：队伍为空也能出发（战斗节点退回调试编成），
            // 只有正在战斗时才置灰
            string kDepartReason;
            bool kCanDepart = Setting.CanDepart(out kDepartReason);
            btn_battle.grayed = !kCanDepart;
            // 同上：没有配置 tooltip 组件时不挂 tooltips，避免 hover 刷警告
            btn_battle.tooltips = string.IsNullOrEmpty(UIConfig.tooltipsWin) || kCanDepart
                ? null
                : kDepartReason;
        }

        #endregion

        #region 输入

        private void OnClickBattle()
        {
            if (!Setting.ConfirmAndDepart())
            {
                Refresh();
                return;
            }

            Game.UI.ShowWindow(UIDefine.Explore);
        }

        private void OnClickBack()
        {
            // 由指挥室「开始行动」进入，返回时回到指挥室（与角色列表的返回方式一致）
            Game.UI.ShowWindow(UIDefine.CommandRoom);
        }

        /// <summary>点空队员格：弹出角色选择弹窗，已在队伍里的角色不再列出</summary>
        private void OnClickAddSlot(int _slot)
        {
            List<int> kExclude = new List<int>(Setting.GetTeamRoleIds());
            RoleListWindow.Open(OnRoleSelected, kExclude);
        }

        /// <summary>角色选择弹窗选中后的回调</summary>
        private void OnRoleSelected(int _roleId)
        {
            // 写数据交给模型，界面刷新由 OnBattleSettingChanged 事件驱动
            Setting.AddToTeam(_roleId);
        }

        /// <summary>点已填格子的 btn_cut：把该队员移出队伍</summary>
        private void OnClickCutSlot(int _slot)
        {
            IReadOnlyList<int> kTeam = Setting.GetTeamRoleIds();
            if (_slot < 0 || _slot >= kTeam.Count)
            {
                return;
            }

            Setting.RemoveFromTeam(kTeam[_slot]);
        }

        #endregion

        #region 事件

        private void Subscribe()
        {
            if (m_Subscribed)
            {
                return;
            }

            Game.Event.Subscribe(BattleSettingEvent.OnBattleSettingChanged, OnBattleSettingChanged);
            m_Subscribed = true;
        }

        private void UnSubscribe()
        {
            if (!m_Subscribed)
            {
                return;
            }

            Game.Event.UnSubscribe(BattleSettingEvent.OnBattleSettingChanged, OnBattleSettingChanged);
            m_Subscribed = false;
        }

        private void OnBattleSettingChanged(object _o)
        {
            Refresh();
        }

        #endregion
    }
}
