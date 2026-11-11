using System;
using System.Collections.Generic;
using FairyGUI;
using FairyGUI.Utils;
using GameMain.Script.Explore;
using UnityEngine;

namespace GameMain.Script.UI
{
    /// <summary>
    /// 探索地图上的单个节点（FairyGUI 包 World / ExploreNodeCom，120×181）。
    ///
    /// 结构：
    ///   state       控制器：0 = noselect（普通），1 = select（金色高亮环 ui_bg_light）
    ///   load_icon   节点类型图标（World 包内 ui_node_xxx）
    ///   txt_name    节点名（出发 / 目标 / 战斗 …，文字框只有 56px，名字必须短）
    ///   txt_pro     层进度百分比
    ///
    /// 五种状态：
    ///   Locked    灰 + 半透明；
    ///   Available 高亮环（弱）；
    ///   Current   高亮环（强）；
    ///   Explored  压暗（可回退，不再触发事件）；
    ///   目标节点  图标换成 BOSS（由快照的 IconKey 决定）。
    /// </summary>
    public class ExploreNodeCom : GComponent
    {
        public const float c_Width = 120f;
        public const float c_Height = 181f;
        /// <summary>圆形底图的中心在组件内的 Y（ui_bg_node 116×116 放在 y=0，中心 58）</summary>
        public const float c_CircleCenterY = 58f;
        /// <summary>圆形底图半径（连线从圆边开始，不穿过节点名）</summary>
        public const float c_Radius = 58f;

        private const int c_PageNormal = 0;
        private const int c_PageSelect = 1;

        private Controller m_State;
        private GLoader load_icon;
        private GTextField txt_name;
        private GTextField txt_pro;

        private int m_NodeId;
        private Action<int> m_OnClick;

        /// <summary>已经提示过缺图标的类型，避免每帧刷日志</summary>
        private static readonly HashSet<int> s_WarnedMissingIconTypes = new HashSet<int>();

        public int NodeId
        {
            get { return m_NodeId; }
        }

        public override void ConstructFromXML(XML _xml)
        {
            base.ConstructFromXML(_xml);

            m_State = GetController("state");
            load_icon = GetChild("load_icon").asLoader;
            txt_name = GetChild("txt_name").asTextField;
            txt_pro = GetChild("txt_pro").asTextField;

            onClick.Add(OnClickSelf);
        }

        /// <summary>刷新一个节点；_onClick 由地图在创建时统一传入</summary>
        public void SetCom(ExploreNodeSnapshot _node, Action<int> _onClick)
        {
            m_NodeId = _node.m_Id;
            m_OnClick = _onClick;

            if (!string.IsNullOrEmpty(_node.m_IconKey))
            {
                string kUrl = "ui://World/" + _node.m_IconKey;
                if (UIPackage.GetItemByURL(kUrl) == null)
                {
                    // 表里配了 World 包还没有的图（例如 ui_node_rescue）时退回该类型的兜底图标
                    string kFallbackKey = ExploreNodeTypeRule.GetFallbackIcon(_node.m_TypeId);
                    kUrl = "ui://World/" + kFallbackKey;
                    if (s_WarnedMissingIconTypes.Add(_node.m_TypeId))
                    {
                        Game.Log.LogWarning(ExploreSystem.c_LogTag + "节点图标不存在：ui://World/"
                            + _node.m_IconKey + "，暂用 " + kFallbackKey);
                    }
                }

                load_icon.url = kUrl;
            }

            txt_name.text = _node.m_TypeName;
            txt_pro.text = Mathf.RoundToInt(_node.m_Progress * 100f) + "%";

            bool kCurrent = _node.m_State == ExploreNodeState.Current;
            bool kAvailable = _node.m_State == ExploreNodeState.Available;
            m_State.selectedIndex = kCurrent || kAvailable ? c_PageSelect : c_PageNormal;

            bool kLocked = _node.m_State == ExploreNodeState.Locked;
            bool kExplored = _node.m_State == ExploreNodeState.Explored;
            grayed = kLocked || kExplored;
            alpha = kLocked ? 0.45f : (kExplored ? 0.65f : 1f);
        }

        private void OnClickSelf()
        {
            if (m_OnClick != null)
            {
                m_OnClick(m_NodeId);
            }
        }
    }
}
