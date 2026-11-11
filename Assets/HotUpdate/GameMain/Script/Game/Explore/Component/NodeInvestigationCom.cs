using System;
using FairyGUI;
using FairyGUI.Utils;
using GameMain.Script.Explore;

namespace GameMain.Script.UI
{
    /// <summary>
    /// 物资搜索面板（FairyGUI 包 World / NodeInvestigationCom，1761×723）。
    ///
    /// 结构：
    ///   list_investigation  调查点列表（World/InvestigationCom 卡片）
    ///
    /// 由 NodeSettingView 的 load_com 加载（和 NodeBattleCom 一样占满 1761×723），
    /// 点某张卡的"开始调查"后回调给 NodeSettingView。
    /// </summary>
    public class NodeInvestigationCom : GComponent
    {
        private GList list_investigation;

        private NodeInvestigationInfo m_Info;
        private Action<InvestigationSiteInfo> m_OnConfirm;

        public override void ConstructFromXML(XML _xml)
        {
            base.ConstructFromXML(_xml);

            list_investigation = GetChild("list_investigation").asList;
            list_investigation.itemRenderer = RenderSite;
        }

        public void SetCom(NodeInvestigationInfo _info, Action<InvestigationSiteInfo> _onConfirm)
        {
            m_Info = _info;
            m_OnConfirm = _onConfirm;

            if (_info == null)
            {
                list_investigation.numItems = 0;
                return;
            }

            list_investigation.numItems = _info.m_Sites.Count;
        }

        private void RenderSite(int _index, GObject _obj)
        {
            InvestigationCom kCom = _obj as InvestigationCom;
            if (kCom == null || m_Info == null || _index < 0 || _index >= m_Info.m_Sites.Count)
            {
                return;
            }

            kCom.SetCom(m_Info.m_Sites[_index], m_OnConfirm);
        }
    }
}
