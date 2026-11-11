using System.Collections.Generic;
using FairyGUI;
using Framework.Script.UI;
using GameMain.Script.Battle;
using GameMain.Script.Explore;
using GameMain.Script.Role;
using UnityEngine;

namespace GameMain.Script.UI
{
    /// <summary>
    /// 战斗界面（FairyGUI 包 Battle / BattleView，预制体 BattleView）。
    ///
    /// BattleView 目前是空外壳（1920×1080），这里先用代码搭最小的一维战场：
    ///   - 己方：RoleBattleCom 角色卡（Spine 0.6），按 BattleUnit.m_Pos 横向移动、显示血条；
    ///   - 敌方：红色细条 + 血条占位（还没有敌人 Spine 资源）；
    ///   - 顶部一行状态文字。
    /// 后续在 FGUI 的 BattleView 里加细节（背景、血条样式、结算面板等），
    /// 代码这边只要保留 BuildUnits / Refresh 的数据绑定即可。
    ///
    /// 战斗逻辑仍由 BattleSystem + BattleDebugView（只 Tick、不绘制）驱动。
    /// </summary>
    public class BattleView : UIWindow
    {
        private const float c_Margin = 80f;
        /// <summary>战场线 Y（单位脚底大致落在这条线上）</summary>
        private const float c_LineY = 760f;
        /// <summary>己方卡片顶部 Y（卡片 135×263，脚底在卡片内 y≈214）</summary>
        private const float c_AllyCardY = 500f;
        /// <summary>敌方占位顶部 Y（战场线上方）</summary>
        private const float c_EnemyY = 300f;

        private GComponent m_Field;
        private GTextField txt_phase;
        private GList list_hp;

        private readonly Dictionary<int, RoleBattleCom> m_AllyCards = new Dictionary<int, RoleBattleCom>();
        private readonly Dictionary<int, EnemyBar> m_Enemies = new Dictionary<int, EnemyBar>();
        /// <summary>出战角色（左上血条列表按这个顺序排列）</summary>
        private readonly List<BattleUnit> m_AllyUnits = new List<BattleUnit>();

        private bool m_Subscribed;
        private bool m_Ready;
        /// <summary>这场战斗里调查结束的消息（全灭 / 目标完成），用于战斗结束后弹结算</summary>
        private ExploreRunEndedMsg m_RunEndedMsg;

        private static BattleSystem Battle
        {
            get { return Game.Model.Get<BattleSystem>(); }
        }

        private static RoleSystem Role
        {
            get { return Game.Model.Get<RoleSystem>(); }
        }

        protected internal override void Init(object o)
        {
            list_hp = ui.GetChild("list_hp").asList;
            list_hp.itemRenderer = RenderHpBar;

            BuildShell();

            // 血条列表放在最上层，避免被代码搭的卡片/战场线挡住
            ui.SetChildIndex(list_hp, ui.numChildren - 1);

            base.Init(o);
        }

        protected internal override void Enter(object o)
        {
            base.Enter(o);

            Subscribe();
            BattlePresentation.s_FguiActive = true;
            m_RunEndedMsg = null;

            BuildUnits();
            Refresh();
        }

        protected internal override void Leave()
        {
            UnSubscribe();
            BattlePresentation.s_FguiActive = false;
            base.Leave();
        }

        protected internal override void Destroy()
        {
            UnSubscribe();
            BattlePresentation.s_FguiActive = false;
            base.Destroy();
        }

        private void Update()
        {
            if (m_Ready)
            {
                Refresh();
            }
        }

        #region 外壳

        private void BuildShell()
        {
            m_Field = new GComponent();
            m_Field.SetSize(1920f, 1080f);
            m_Field.opaque = false;
            ui.AddChild(m_Field);

            // 一维战场线
            GGraph kLine = new GGraph();
            kLine.DrawRect(1920f - c_Margin * 2f, 2f, 0, Color.clear, new Color(1f, 1f, 1f, 0.22f));
            kLine.SetXY(c_Margin, c_LineY);
            kLine.touchable = false;
            m_Field.AddChild(kLine);

            txt_phase = CreateText(26, new Color(0.88f, 0.87f, 0.91f, 1f));
            txt_phase.SetXY(c_Margin, 40f);
            txt_phase.SetSize(1200f, 36f);
            txt_phase.text = "自动战斗中";
            m_Field.AddChild(txt_phase);
        }

        private static GTextField CreateText(int _fontSize, Color _color)
        {
            GTextField kText = new GTextField();
            TextFormat kFormat = new TextFormat();
            kFormat.font = UIConfig.defaultFont;
            kFormat.size = _fontSize;
            kFormat.color = _color;
            kFormat.align = AlignType.Left;
            kText.textFormat = kFormat;
            kText.verticalAlign = VertAlignType.Middle;
            kText.singleLine = true;
            kText.autoSize = AutoSizeType.None;
            kText.touchable = false;
            return kText;
        }

        #endregion

        #region 建单位

        private void BuildUnits()
        {
            ClearUnits();

            BattleSystem kBattle = Battle;
            IReadOnlyList<BattleUnit> kUnits = kBattle == null ? null : kBattle.GetUnits();
            if (kUnits == null)
            {
                m_Ready = false;
                return;
            }

            for (int i = 0; i < kUnits.Count; i++)
            {
                BattleUnit kUnit = kUnits[i];
                if (kUnit == null)
                {
                    continue;
                }

                if (kUnit.m_Faction == cfg.GameType.Faction.ally)
                {
                    m_AllyUnits.Add(kUnit);

                    RoleSnapshot kRole = Role.GetRoleData(kUnit.m_SourceId);
                    if (kRole == null || kRole.m_Role == null)
                    {
                        continue;
                    }

                    RoleBattleCom kCard = UIPackage.CreateObject("Base", "RoleBattleCom") as RoleBattleCom;
                    if (kCard == null)
                    {
                        Game.Log.LogError("[战斗] 角色卡创建失败：ui://Base/RoleBattleCom");
                        continue;
                    }

                    kCard.SetCom(kRole, RoleBattleCom.c_BattleScale);
                    kCard.SetHpVisible(true);
                    kCard.SetXY(0f, c_AllyCardY);
                    m_Field.AddChild(kCard);
                    m_AllyCards[kUnit.m_UnitId] = kCard;
                }
                else
                {
                    EnemyBar kBar = new EnemyBar();
                    kBar.Build(m_Field);
                    m_Enemies[kUnit.m_UnitId] = kBar;
                }
            }

            if (list_hp != null)
            {
                list_hp.numItems = m_AllyUnits.Count;
            }

            m_Ready = true;
        }

        private void ClearUnits()
        {
            foreach (KeyValuePair<int, RoleBattleCom> kPair in m_AllyCards)
            {
                kPair.Value.Dispose();
            }
            m_AllyCards.Clear();

            foreach (KeyValuePair<int, EnemyBar> kPair in m_Enemies)
            {
                kPair.Value.Dispose();
            }
            m_Enemies.Clear();
            m_AllyUnits.Clear();

            if (list_hp != null)
            {
                list_hp.numItems = 0;
            }

            m_Ready = false;
        }

        #endregion

        #region 每帧刷新

        private void Refresh()
        {
            BattleSystem kBattle = Battle;
            if (kBattle == null)
            {
                return;
            }

            BattlePhase kPhase = kBattle.GetPhase();
            if (kPhase == BattlePhase.None)
            {
                return;
            }

            BattleField kField = kBattle.GetField();
            IReadOnlyList<BattleUnit> kUnits = kBattle.GetUnits();
            if (kField == null || kUnits == null)
            {
                return;
            }

            float kLength = Mathf.Max(0.001f, kField.Length);
            float kAreaWidth = 1920f - c_Margin * 2f;

            int kAliveEnemies = 0;
            for (int i = 0; i < kUnits.Count; i++)
            {
                BattleUnit kUnit = kUnits[i];
                if (kUnit == null)
                {
                    continue;
                }

                float kT = Mathf.Clamp01(kUnit.m_Pos / kLength);

                RoleBattleCom kCard;
                if (m_AllyCards.TryGetValue(kUnit.m_UnitId, out kCard))
                {
                    // 卡片中心对齐单位位置（ApplyPositions 把站位写成了 m_Pos，单位位置就是中心）
                    kCard.SetBattlePosition(kT, c_Margin, kAreaWidth);

                    // 同一位置附近会叠在一起：按队伍序号上下错开一点
                    int kRow = kUnit.m_TeamIndex % 3 - 1;
                    kCard.SetXY(kCard.x, c_AllyCardY + kRow * 18f);

                    kCard.SetHp(kUnit.m_Hp, kUnit.m_MaxHp);
                    kCard.SetBattleState(
                        kUnit.m_State == UnitBrainState.Dead,
                        kUnit.m_State == UnitBrainState.Downed);
                }
                else
                {
                    EnemyBar kBar;
                    if (m_Enemies.TryGetValue(kUnit.m_UnitId, out kBar))
                    {
                        // 敌人细条也按中心对齐
                        float kEnemyOffset = ((i % 3) - 1) * 8f;
                        float kCenter = c_Margin + kT * kAreaWidth;
                        float kX = Mathf.Clamp(kCenter - EnemyBar.c_Width * 0.5f,
                            c_Margin, c_Margin + kAreaWidth - EnemyBar.c_Width);
                        kBar.SetPosition(kX, c_EnemyY + kEnemyOffset);
                        kBar.SetHp(kUnit.m_Hp, kUnit.m_MaxHp);
                        kBar.SetDead(kUnit.m_State == UnitBrainState.Dead);
                        if (BattleField.IsActive(kUnit))
                        {
                            kAliveEnemies++;
                        }
                    }
                }
            }

            if (txt_phase != null)
            {
                txt_phase.text = kPhase == BattlePhase.Finished
                    ? "战斗结束"
                    : "自动战斗中 · 敌方存活 " + kAliveEnemies;
            }

            RefreshHpBars();
        }

        /// <summary>左侧出战角色血条：按 m_AllyUnits 顺序刷新</summary>
        private void RefreshHpBars()
        {
            if (list_hp == null)
            {
                return;
            }

            int kCount = Mathf.Min(list_hp.numChildren, m_AllyUnits.Count);
            for (int i = 0; i < kCount; i++)
            {
                GProgressBar kBar = list_hp.GetChildAt(i).asProgress;
                BattleUnit kUnit = m_AllyUnits[i];
                if (kBar == null || kUnit == null)
                {
                    continue;
                }

                kBar.max = Mathf.Max(1, kUnit.m_MaxHp);
                kBar.value = kUnit.m_Hp;
                kBar.alpha = kUnit.m_State == UnitBrainState.Dead
                    ? 0.35f
                    : (kUnit.m_State == UnitBrainState.Downed ? 0.7f : 1f);
            }
        }

        private void RenderHpBar(int _index, GObject _obj)
        {
            GProgressBar kBar = _obj.asProgress;
            if (kBar == null || _index < 0 || _index >= m_AllyUnits.Count)
            {
                return;
            }

            BattleUnit kUnit = m_AllyUnits[_index];
            kBar.max = Mathf.Max(1, kUnit.m_MaxHp);
            kBar.value = kUnit.m_Hp;

            GTextField kName = kBar.GetChild("txt_name").asTextField;
            kName.text = GetAllyName(kUnit);
        }

        private static string GetAllyName(BattleUnit _unit)
        {
            RoleRecord kRole = Role.GetRole(_unit.m_SourceId);
            return kRole == null ? "调查员" : kRole.GetDisplayName();
        }

        #endregion

        #region 事件

        private void Subscribe()
        {
            if (m_Subscribed)
            {
                return;
            }

            Game.Event.Subscribe(BattleEvent.OnBattleEnded, OnBattleEnded);
            Game.Event.Subscribe(ExploreEvent.OnRunEnded, OnRunEnded);
            m_Subscribed = true;
        }

        private void UnSubscribe()
        {
            if (!m_Subscribed)
            {
                return;
            }

            Game.Event.UnSubscribe(BattleEvent.OnBattleEnded, OnBattleEnded);
            Game.Event.UnSubscribe(ExploreEvent.OnRunEnded, OnRunEnded);
            m_Subscribed = false;
        }

        private void OnRunEnded(object _msg)
        {
            // 全灭 / 目标节点完成会在这之后触发 OnBattleEnded，先记下来
            m_RunEndedMsg = _msg as ExploreRunEndedMsg;
        }

        private void OnBattleEnded(object _msg)
        {
            m_Ready = false;
            BattlePresentation.s_FguiActive = false;

            // 探索层已经在这场战斗的事件里写好结果，这里只收尾
            Battle.ClearBattle();

            if (m_RunEndedMsg != null)
            {
                ExploreRunEndedMsg kResult = m_RunEndedMsg;
                m_RunEndedMsg = null;
                ExploreSettlement.Show(kResult);
                return;
            }

            Game.UI.ShowWindow(UIDefine.Explore);
        }

        #endregion

        /// <summary>敌人占位：红色细条 + 血条（没有敌人 Spine 资源）</summary>
        private class EnemyBar
        {
            public const float c_Width = 40f;
            private const float c_Height = 96f;
            private const float c_HpWidth = 32f;

            private GComponent m_Root;
            private GGraph m_HpFill;

            public void Build(GComponent _parent)
            {
                m_Root = new GComponent();
                m_Root.SetSize(c_Width, c_Height);
                m_Root.SetXY(0f, 300f);
                _parent.AddChild(m_Root);

                GGraph kBody = new GGraph();
                kBody.DrawRect(c_Width, c_Height, 0, Color.clear, new Color(0.45f, 0.16f, 0.16f, 0.92f));
                kBody.SetXY(0f, 0f);
                kBody.touchable = false;
                m_Root.AddChild(kBody);

                GTextField kName = CreateText(18, new Color(0.95f, 0.9f, 0.9f, 1f), AlignType.Center);
                kName.text = "敌";
                kName.SetXY(0f, 6f);
                kName.SetSize(c_Width, 24f);
                m_Root.AddChild(kName);

                GGraph kHpBg = new GGraph();
                kHpBg.DrawRect(c_HpWidth, 6f, 0, Color.clear, new Color(0.08f, 0.08f, 0.08f, 0.9f));
                kHpBg.SetXY(4f, c_Height - 12f);
                kHpBg.touchable = false;
                m_Root.AddChild(kHpBg);

                m_HpFill = new GGraph();
                m_HpFill.DrawRect(c_HpWidth, 6f, 0, Color.clear, new Color(0.9f, 0.3f, 0.3f, 1f));
                m_HpFill.SetXY(4f, c_Height - 12f);
                m_HpFill.touchable = false;
                m_Root.AddChild(m_HpFill);
            }

            public void SetPosition(float _x, float _y)
            {
                m_Root.SetXY(_x, _y);
            }

            public void SetHp(int _current, int _max)
            {
                float kRatio = _max > 0 ? Mathf.Clamp01(_current / (float)_max) : 0f;
                m_HpFill.SetScale(kRatio, 1f);
                m_HpFill.visible = kRatio > 0.001f;
            }

            public void SetDead(bool _dead)
            {
                m_Root.alpha = _dead ? 0.25f : 1f;
            }

            public void Dispose()
            {
                if (m_Root != null)
                {
                    m_Root.Dispose();
                    m_Root = null;
                }
            }

            private static GTextField CreateText(int _fontSize, Color _color, AlignType _align)
            {
                GTextField kText = new GTextField();
                TextFormat kFormat = new TextFormat();
                kFormat.font = UIConfig.defaultFont;
                kFormat.size = _fontSize;
                kFormat.color = _color;
                kFormat.align = _align;
                kText.textFormat = kFormat;
                kText.verticalAlign = VertAlignType.Middle;
                kText.singleLine = true;
                kText.autoSize = AutoSizeType.None;
                kText.touchable = false;
                return kText;
            }
        }
    }
}
