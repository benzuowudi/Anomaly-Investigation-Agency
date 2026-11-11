using System.Collections.Generic;

namespace GameMain.Script.Explore
{
    /// <summary>
    /// 物资搜索节点里的一个"调查点"（InvestigationCom 一张卡）。
    /// 目前没有专用配表，由 NodeSettingView 按节点类型临时生成；后续接掉落表后填 m_ItemIds。
    /// </summary>
    public class InvestigationSiteInfo
    {
        /// <summary>序号（从 0 开始）</summary>
        public int m_Index;
        /// <summary>调查点名称</summary>
        public string m_Title = string.Empty;
        /// <summary>说明</summary>
        public string m_Desc = string.Empty;
        /// <summary>成功率（0~100，显示在按钮上）</summary>
        public int m_SuccessRate = 75;
        /// <summary>可能获得的物品 ID（暂未接掉落表）</summary>
        public readonly List<int> m_ItemIds = new List<int>();
    }

    /// <summary>
    /// 物资搜索节点的展示数据：一个节点可以有多个调查点。
    /// </summary>
    public class NodeInvestigationInfo
    {
        public int m_NodeId;
        public int m_TypeId;

        /// <summary>节点类型全名（nodeType.name）</summary>
        public string m_NodeName = string.Empty;
        /// <summary>节点类型说明（nodeType.desc）</summary>
        public string m_NodeDesc = string.Empty;

        public readonly List<InvestigationSiteInfo> m_Sites = new List<InvestigationSiteInfo>();
    }
}
