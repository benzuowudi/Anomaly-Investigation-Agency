using cfg.GameType;

namespace GameMain.Script.Explore
{
    /// <summary>
    /// 节点类型的展示/规则视图（nodeType 表的一行，缺表时用 ExploreConst 兜底）。
    /// </summary>
    public class ExploreNodeTypeView
    {
        public int m_TypeId;
        public string m_Name = string.Empty;
        /// <summary>World 包内的图标资源名（不含 ui:// 前缀）</summary>
        public string m_Icon = string.Empty;
        public string m_Desc = string.Empty;
        public bool m_CanEvacuate;
    }

    /// <summary>
    /// 节点类型的兜底规则：表为空时也能把 7 种节点显示对、把"能不能撤离"判对。
    /// 纯函数（只依赖入参和配置表），不需要注册。
    /// </summary>
    public static class ExploreNodeTypeRule
    {
        /// <summary>取类型视图；_typeId 非法或表里没有时按 1~7 的兜底表返回</summary>
        public static ExploreNodeTypeView Get(cfg.Tables _tables, int _typeId)
        {
            ExploreNodeTypeView kView = new ExploreNodeTypeView();
            kView.m_TypeId = _typeId;

            cfg.GameData_Explore.NodeTypeData kRow = null;
            if (_tables != null && _tables.nodeType != null && _typeId > 0)
            {
                kRow = _tables.nodeType.GetOrDefault(_typeId);
            }

            if (kRow != null)
            {
                kView.m_Name = kRow.Name;
                kView.m_Icon = kRow.Icon;
                kView.m_Desc = kRow.Desc;
                kView.m_CanEvacuate = kRow.CanEvacuate;
            }

            if (string.IsNullOrEmpty(kView.m_Name))
            {
                kView.m_Name = GetFallbackName(_typeId);
            }
            if (string.IsNullOrEmpty(kView.m_Icon))
            {
                kView.m_Icon = GetFallbackIcon(_typeId);
            }

            // 表里没配 canEvacuate 时，按文档 8：只有露营地能撤离（目标节点的直升机由 ExploreSystem 单独判断）
            if (kRow == null)
            {
                kView.m_CanEvacuate = _typeId == (int)NodeType.camp;
            }

            return kView;
        }

        public static string GetFallbackName(int _typeId)
        {
            if (_typeId > 0 && _typeId < ExploreConst.NodeTypeNames.Length)
            {
                return ExploreConst.NodeTypeNames[_typeId];
            }
            return "未知节点";
        }

        public static string GetFallbackIcon(int _typeId)
        {
            if (_typeId > 0 && _typeId < ExploreConst.NodeTypeIcons.Length)
            {
                return ExploreConst.NodeTypeIcons[_typeId];
            }
            return ExploreConst.NodeTypeIcons[(int)NodeType.battle];
        }
    }
}
