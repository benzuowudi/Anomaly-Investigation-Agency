using FairyGUI;
using FairyGUI.Utils;

namespace GameMain.Script.UI
{
    /// <summary>
    /// 通用物资格（FairyGUI 包 Base / CommonItemCom）。
    ///
    /// 结构：n0 底框、n1 加号；controller "state"：0 空 / 1 物品 / 2 角色 / 3 装备。
    ///
    /// 说明：包里的 CommonItemCom 目前只有底框和加号、没有图标节点，
    /// 这里在 ConstructFromXML 里补一个 load_icon；美术之后在 FairyGUI 里加了同名节点，
    /// 这段兜底会自动失效（优先用包里的节点）。
    /// </summary>
    public class CommonItemCom : GComponent
    {
        /// <summary>空态</summary>
        public const int StateEmpty = 0;

        /// <summary>物品</summary>
        public const int StateItem = 1;

        /// <summary>角色</summary>
        public const int StateHero = 2;

        /// <summary>装备</summary>
        public const int StateEquip = 3;

        private Controller m_State;
        private GLoader m_LoadIcon;

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);

            m_State = GetController("state");
            m_LoadIcon = GetChild("load_icon") as GLoader;
            if (m_LoadIcon == null)
            {
                m_LoadIcon = new GLoader();
                m_LoadIcon.SetSize(this.width, this.height);
                m_LoadIcon.align = AlignType.Center;
                m_LoadIcon.verticalAlign = VertAlignType.Middle;
                m_LoadIcon.fill = FillType.ScaleFree;
                m_LoadIcon.touchable = false;
                AddChild(m_LoadIcon);
            }

            m_LoadIcon.visible = false;
        }

        /// <summary>
        /// 刷新格子。
        /// </summary>
        /// <param name="_state">0 空 / 1 物品 / 2 角色 / 3 装备</param>
        /// <param name="_iconUrl">图标地址（ui://…），空表示不显示图标</param>
        /// <param name="_tip">tooltip 文案</param>
        public void SetCom(int _state, string _iconUrl = null, string _tip = null)
        {
            m_State.selectedIndex = _state;

            if (string.IsNullOrEmpty(_iconUrl))
            {
                m_LoadIcon.visible = false;
            }
            else
            {
                m_LoadIcon.url = _iconUrl;
                m_LoadIcon.visible = true;
            }

            tooltips = string.IsNullOrEmpty(_tip) ? null : _tip;
        }
    }
}
