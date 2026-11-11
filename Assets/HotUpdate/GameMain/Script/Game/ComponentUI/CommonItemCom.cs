using FairyGUI;
using FairyGUI.Utils;
using UnityEngine;

namespace GameMain.Script.UI
{
    /// <summary>
    /// 通用物资格（FairyGUI 包 Base / CommonItemCom，83×83）。
    ///
    /// 结构（2026-11-11 改版）：
    ///   load_bg     品质底图（state 1/3 显示，ui_itembg_1~7）
    ///   n0          空态底框
    ///   n1          加号（state 0 空态）
    ///   load_icon   物品图片（state 1/3 显示，71×70，scaleFree）
    ///   controller "state"：0 空 / 1 物品 / 3 装备
    ///
    /// 数量角标（txt_count）是代码创建的节点：FGUI 里没有对应的文本，只有仓库这类需要
    /// 显示持有数量的界面才调 SetCount()；其它界面 SetCom 后角标保持隐藏。
    ///
    /// 图片统一走 CommonSkillIconCom.ResolveIconUrl 解析，再塞进 load_icon.url。
    /// </summary>
    public class CommonItemCom : GComponent
    {
        /// <summary>空态</summary>
        public const int StateEmpty = 0;

        /// <summary>物品</summary>
        public const int StateItem = 1;

        /// <summary>角色（新结构里没有独立页，按物品页显示）</summary>
        public const int StateHero = 1;

        /// <summary>装备（对应控制器 page id 3，页索引是 2）</summary>
        public const int StateEquip = 2;

        private Controller m_State;
        private GLoader m_LoadBg;
        private GLoader m_LoadIcon;

        /// <summary>数量角标（代码创建，数量 ≤ 1 时隐藏）</summary>
        private GTextField m_TxtCount;

        public override void ConstructFromXML(XML _xml)
        {
            base.ConstructFromXML(_xml);

            m_State = GetController("state");
            m_LoadBg = GetChild("load_bg").asLoader;
            m_LoadIcon = GetChild("load_icon").asLoader;
            m_LoadBg.touchable = false;
            m_LoadIcon.fill = FillType.ScaleFree;
            m_LoadIcon.touchable = false;
        }

        /// <summary>刷新格子（默认品质 1）</summary>
        public void SetCom(int _state, string _icon = null, string _tip = null)
        {
            SetCom(_state, _icon, 1, _tip);
        }

        /// <summary>
        /// 刷新格子。
        /// </summary>
        /// <param name="_state">0 空 / 1 物品 / 3 装备</param>
        /// <param name="_icon">图标（资源名 / 带路径 / 完整 ui://；空表示不显示图片）</param>
        /// <param name="_quality">品质 1~7，决定 load_bg 用哪张 ui_itembg_*</param>
        /// <param name="_tip">tooltip 文案</param>
        public void SetCom(int _state, string _icon, int _quality, string _tip)
        {
            // 控制器页面是 0,empty / 1,item / 3,equip，selectedIndex 用的是“页索引”（0~2），
            // 不是 page id；这里再夹一次，避免外部传 3 之类直接把控制器打崩。
            int kState = _state < 0 ? 0 : (_state >= m_State.pageCount ? m_State.pageCount - 1 : _state);
            m_State.selectedIndex = kState;

            int kQuality = _quality < 1 ? 1 : (_quality > 7 ? 7 : _quality);
            m_LoadBg.url = "ui://Base/ui_itembg_" + kQuality;

            ShowIcon(_icon);
            HideCount();

            // 项目还没有 FGUI 的 tooltip 组件（UIConfig.tooltipsWin 为空），
            // 这时挂 tooltips 只会在 hover 时刷 "UIConfig.tooltipsWin not defined" 警告；
            // 等做了 tooltip 组件并配置 UIConfig.tooltipsWin 之后，这里会自动生效。
            tooltips = string.IsNullOrEmpty(UIConfig.tooltipsWin) || string.IsNullOrEmpty(_tip) ? null : _tip;
        }

        /// <summary>
        /// 显示数量角标（仓库等需要看持有数量的界面在 SetCom 之后调用）。
        /// 数量 ≤ 1 时不显示，避免每个格子都挂一个 "x1"。
        /// </summary>
        public void SetCount(int _count)
        {
            if (_count <= 1)
            {
                HideCount();
                return;
            }

            EnsureCountText();
            m_TxtCount.text = "x" + _count;
            m_TxtCount.visible = true;
        }

        private void HideCount()
        {
            if (m_TxtCount != null)
            {
                m_TxtCount.visible = false;
            }
        }

        private void EnsureCountText()
        {
            if (m_TxtCount != null)
            {
                return;
            }

            m_TxtCount = new GTextField();
            m_TxtCount.name = "txt_count";
            m_TxtCount.touchable = false;
            m_TxtCount.singleLine = true;
            m_TxtCount.SetSize(77f, 26f);
            m_TxtCount.SetXY(3f, 54f);

            TextFormat kFormat = new TextFormat();
            kFormat.font = UIConfig.defaultFont;
            kFormat.size = 20;
            kFormat.color = Color.white;
            kFormat.align = AlignType.Right;
            kFormat.outline = 2f;
            kFormat.outlineColor = new Color(0f, 0f, 0f, 0.85f);
            m_TxtCount.textFormat = kFormat;

            // 自动尺寸放在 textFormat 之后设置：套用格式时可能重算尺寸
            m_TxtCount.autoSize = AutoSizeType.None;
            m_TxtCount.verticalAlign = VertAlignType.Bottom;

            AddChild(m_TxtCount);
        }

        /// <summary>
        /// 图标显示：优先走 FairyGUI 包（CommonUI 已经发布了 ui_equip_*），
        /// 包裡没有时再退回 Addressables 的 Texture2D（Assets/Res/Icon/Equip 里的备份图）。
        /// </summary>
        private void ShowIcon(string _icon)
        {
            string kUrl = CommonSkillIconCom.ResolveIconUrl(_icon);
            if (!string.IsNullOrEmpty(kUrl))
            {
                m_LoadIcon.texture = null;
                m_LoadIcon.url = kUrl;
                m_LoadIcon.visible = true;
                return;
            }

            if (!string.IsNullOrEmpty(_icon) && _icon.StartsWith("ui_equip_"))
            {
                UnityEngine.Texture2D kTexture = Game.Res.LoadAsset<UnityEngine.Texture2D>(_icon);
                if (kTexture != null)
                {
                    m_LoadIcon.url = null;
                    m_LoadIcon.texture = new NTexture(kTexture);
                    m_LoadIcon.visible = true;
                    return;
                }
            }

            m_LoadIcon.texture = null;
            m_LoadIcon.url = null;
            m_LoadIcon.visible = false;
        }

        /// <summary>按物品 ID 显示（图标取 item.icon，名称作为 tooltip）</summary>
        public void SetItem(int _itemId, int _state = StateItem)
        {
            cfg.Tables kTables = Game.Data.GetData;
            cfg.GameData_Item.ItemData kItem = null;
            if (kTables != null && kTables.item != null && _itemId > 0)
            {
                kItem = kTables.item.GetOrDefault(_itemId);
            }

            if (kItem == null)
            {
                SetCom(StateEmpty);
                return;
            }

            SetCom(_state, kItem.Icon, kItem.Quality, kItem.Name);
        }

        /// <summary>按装备 ID 显示（图标取 equip.icon，名称作为 tooltip）</summary>
        public void SetEquip(int _equipId)
        {
            cfg.Tables kTables = Game.Data.GetData;
            cfg.GameData_Item.EquipData kEquip = null;
            if (kTables != null && kTables.equip != null && _equipId > 0)
            {
                kEquip = kTables.equip.GetOrDefault(_equipId);
            }

            if (kEquip == null)
            {
                SetCom(StateEmpty);
                return;
            }

            SetCom(StateEquip, kEquip.Icon, kEquip.Quality, kEquip.Name);
        }
    }
}
