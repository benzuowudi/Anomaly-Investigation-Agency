using System;
using System.Collections.Generic;
using FairyGUI;
using FairyGUI.Utils;
using GameMain.Script.BattleSetting;

namespace GameMain.Script.UI
{
    /// <summary>
    /// 「行动队员」区块（FairyGUI 包 BattleSetting / BattleSettingACCom）。
    ///
    /// 结构：标题（中/英）+ list_role（固定 5 格，每格是 BattleSettingRoleCom）。
    /// 空槽位显示「+」，点击由 View 决定打开角色选择弹窗；已填格子的 btn_cut 由 View 处理移除。
    /// </summary>
    public class BattleSettingACCom : GComponent
    {
        private GTextField txt_name_chinese, txt_name_english;
        private GList list_role;

        /// <summary>快照里的队伍（可能不足 5 人，后面补空槽）</summary>
        private List<BattleSettingTeamEntry> m_Team;

        /// <summary>点了空槽位（参数：槽位下标）</summary>
        public Action<int> onAdd;

        /// <summary>点了已填槽位的 btn_cut（参数：槽位下标）</summary>
        public Action<int> onCut;

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            txt_name_chinese = GetChild("txt_name_chinese").asTextField;
            txt_name_english = GetChild("txt_name_english").asTextField;
            list_role = GetChild("list_role").asList;

            txt_name_chinese.text = "行动队员";
            txt_name_english.text = "ACTIVITY TEAM";

            list_role.itemRenderer = ItemRenderer;
        }

        public void SetCom(BattleSettingSnapshot _snapshot)
        {
            m_Team = _snapshot == null ? null : _snapshot.m_Team;
            list_role.numItems = BattleSettingDefine.c_MaxTeamCount;
        }

        private void ItemRenderer(int _index, GObject _obj)
        {
            if (_obj is not BattleSettingRoleCom kCom)
            {
                return;
            }

            BattleSettingTeamEntry kEntry = null;
            if (m_Team != null && _index >= 0 && _index < m_Team.Count)
            {
                kEntry = m_Team[_index];
            }

            kCom.SetCom(kEntry, _index, onAdd, onCut);
        }
    }
}
