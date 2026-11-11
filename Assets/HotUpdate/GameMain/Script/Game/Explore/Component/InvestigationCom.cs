using System;
using FairyGUI;
using FairyGUI.Utils;
using GameMain.Script.Explore;

namespace GameMain.Script.UI
{
    /// <summary>
    /// 物资搜索节点里的单个调查点（FairyGUI 包 World / InvestigationCom，381×684）。
    ///
    /// 结构：
    ///   txt_title    调查点名称
    ///   txt_desc     调查点说明
    ///   txt_title1   "可能获得的物品"
    ///   list_item    可能获得的物品（Base/CommonItemCom，掉落表接好后填）
    ///   btn_confirm  "开始调查 成功率 xx%"
    ///
    /// 由 NodeInvestigationCom 的 GList 创建，点击确认后回调给 NodeSettingView。
    /// </summary>
    public class InvestigationCom : GComponent
    {
        private GTextField txt_title;
        private GTextField txt_desc;
        private GTextField txt_title1;
        private GList list_item;
        private GButton btn_confirm;

        private InvestigationSiteInfo m_Info;
        private Action<InvestigationSiteInfo> m_OnConfirm;

        public override void ConstructFromXML(XML _xml)
        {
            base.ConstructFromXML(_xml);

            txt_title = GetChild("txt_title").asTextField;
            txt_desc = GetChild("txt_desc").asTextField;
            txt_title1 = GetChild("txt_title1").asTextField;
            list_item = GetChild("list_item").asList;
            btn_confirm = GetChild("btn_confirm").asButton;

            list_item.itemRenderer = RenderItem;
            btn_confirm.onClick.Add(OnClickConfirm);
        }

        public void SetCom(InvestigationSiteInfo _info, Action<InvestigationSiteInfo> _onConfirm)
        {
            m_Info = _info;
            m_OnConfirm = _onConfirm;

            if (_info == null)
            {
                txt_title.text = string.Empty;
                txt_desc.text = string.Empty;
                list_item.numItems = 0;
                return;
            }

            txt_title.text = _info.m_Title;
            txt_desc.text = _info.m_Desc;
            txt_title1.text = "可能获得的物品";
            btn_confirm.title = "开始调查 成功率 " + _info.m_SuccessRate + "%";

            // 掉落/奖励表还没接：m_ItemIds 为空时列表自然为空；
            // 接好后这里会按物品 ID 把图片加载到 CommonItemCom.load_icon。
            list_item.numItems = _info.m_ItemIds.Count;
        }

        private void RenderItem(int _index, GObject _obj)
        {
            CommonItemCom kCom = _obj as CommonItemCom;
            if (kCom == null || m_Info == null || _index < 0 || _index >= m_Info.m_ItemIds.Count)
            {
                return;
            }

            kCom.SetItem(m_Info.m_ItemIds[_index]);
        }

        private void OnClickConfirm()
        {
            if (m_OnConfirm != null)
            {
                m_OnConfirm(m_Info);
            }
        }
    }
}
