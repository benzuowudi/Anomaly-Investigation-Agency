using System;
using System.Collections.Generic;
using FairyGUI;
using FairyGUI.Utils;
using GameMain.Script.Explore;
using UnityEngine;

namespace GameMain.Script.UI
{
    /// <summary>
    /// 战斗节点布阵面板（FairyGUI 包 World / NodeBattleCom，1761×723）。
    ///
    /// 只负责"布阵"：每名队员一张 RoleBattleCom（Spine 0.5），可左右拖动调整站位；
    /// 开战后由 NodeSettingView 打开 BattleView（Battle 包）显示战斗画面。
    ///
    /// 结构：
    ///   txt_name_chinese / txt_name_english     标题
    ///   list_battleField                        原 FGUI 列表（已隐藏，运行时用卡片层替代）
    ///   txt_infoname_chinese / engilsh / txt_infoDasc  战场信息
    ///   btn_battle                              开始战斗
    /// </summary>
    public class NodeBattleCom : GComponent
    {
        private GTextField txt_name_chinese;
        private GTextField txt_name_english;
        private GList list_battleField;
        private GTextField txt_infoname_chinese;
        private GTextField txt_infoname_engilsh;
        private GTextField txt_infoDasc;
        private GButton btn_battle;

        /// <summary>卡片层（替代 list_battleField 的布局，支持自由拖动）</summary>
        private GComponent m_CardLayer;

        private readonly List<RoleBattleCom> m_Cards = new List<RoleBattleCom>();

        private NodeBattleInfo m_Info;

        /// <summary>正在拖的卡片（一次只拖一张）</summary>
        private RoleBattleCom m_DragCard;
        /// <summary>按下时的触点（设计像素）</summary>
        private Vector2 m_DragStartTouch;
        private float m_DragStartX;
        private float m_DragY;
        private bool m_Dragging;

        /// <summary>点"开始战斗"的回调</summary>
        public Action onBattle;

        /// <summary>拖动卡片改变站位（角色ID, 0~1）</summary>
        public Action<int, float> onCardPositionChanged;

        public override void ConstructFromXML(XML _xml)
        {
            base.ConstructFromXML(_xml);

            txt_name_chinese = GetChild("txt_name_chinese").asTextField;
            txt_name_english = GetChild("txt_name_english").asTextField;
            list_battleField = GetChild("list_battleField").asList;
            txt_infoname_chinese = GetChild("txt_infoname_chinese").asTextField;
            txt_infoname_engilsh = GetChild("txt_infoname_engilsh").asTextField;
            txt_infoDasc = GetChild("txt_infoDasc").asTextField;
            btn_battle = GetChild("btn_battle").asButton;

            btn_battle.title = "开始战斗";
            btn_battle.onClick.Add(OnClickBattle);

            // FGUI 列表里预置了 11 个占位 item，这里不需要，清掉省资源
            list_battleField.numItems = 0;
            list_battleField.visible = false;

            CreateCardLayer();
        }

        private void CreateCardLayer()
        {
            // FGUI 的 GList 会强制按行布局，拖动卡片后会被拉回去；
            // 这里把列表隐藏，用同样位置/尺寸的自由容器代替
            m_CardLayer = new GComponent();
            m_CardLayer.SetXY(list_battleField.x, list_battleField.y);
            m_CardLayer.SetSize(list_battleField.width, list_battleField.height);
            m_CardLayer.opaque = true;
            AddChild(m_CardLayer);
        }

        public void SetCom(NodeBattleInfo _info)
        {
            m_Info = _info;
            btn_battle.visible = true;

            if (_info == null)
            {
                ClearCards();
                return;
            }

            txt_name_chinese.text = _info.m_TitleCn;
            txt_name_english.text = _info.m_TitleEn;
            txt_infoname_chinese.text = _info.m_InfoNameCn;
            txt_infoname_engilsh.text = _info.m_InfoNameEn;
            txt_infoDasc.text = _info.m_Desc;

            RebuildCards();
        }

        #region 卡片

        private void RebuildCards()
        {
            ClearCards();

            int kCount = m_Info.m_Roles.Count;
            for (int i = 0; i < kCount; i++)
            {
                RoleBattleCom kCard = UIPackage.CreateObject("Base", "RoleBattleCom") as RoleBattleCom;
                if (kCard == null)
                {
                    Game.Log.LogError(ExploreSystem.c_LogTag +
                        "角色卡创建失败：ui://Base/RoleBattleCom（检查 BaseData 的包注册）");
                    continue;
                }

                m_CardLayer.AddChild(kCard);
                kCard.SetCom(m_Info.m_Roles[i], RoleBattleCom.c_FormationScale);

                float kPosition = i < m_Info.m_Positions.Count ? m_Info.m_Positions[i] : (i + 0.5f) / kCount;
                float kY = (m_CardLayer.height - kCard.height) * 0.5f;
                float kMaxX = m_CardLayer.width - kCard.width;

                // 站位是"卡片中心"在战场宽度里的比例，而不是左边缘
                float kX = Mathf.Clamp(kPosition * m_CardLayer.width - kCard.width * 0.5f, 0f, kMaxX);
                kCard.SetXY(kX, kY);

                SetupCardDrag(kCard);
                m_Cards.Add(kCard);
            }
        }

        private void SetupCardDrag(RoleBattleCom _card)
        {
            // 不用 GObject.draggable：它内部用 GlobalToLocal 换算，而 UIPanel 的内容被
            // unitsPerPixel（约 0.00926）缩放过，1px 鼠标位移会被放大成上百像素。
            // 这里直接拿触摸事件的位移（设计像素）加到卡片局部坐标上。
            _card.draggable = false;
            _card.onTouchBegin.Add(OnCardTouchBegin);
            _card.onTouchMove.Add(OnCardTouchMove);
            _card.onTouchEnd.Add(OnCardTouchEnd);
        }

        private void OnCardTouchBegin(EventContext _context)
        {
            if (m_DragCard != null)
            {
                return;
            }

            RoleBattleCom kCard = _context.sender as RoleBattleCom;
            if (kCard == null)
            {
                return;
            }

            m_DragCard = kCard;
            m_Dragging = false;
            m_DragStartTouch = _context.inputEvent.position;
            m_DragStartX = kCard.x;
            m_DragY = kCard.y;

            // 保证拖出卡片范围后还能收到 Move/End
            _context.CaptureTouch();
        }

        private void OnCardTouchMove(EventContext _context)
        {
            if (m_DragCard == null || _context.sender != m_DragCard)
            {
                return;
            }

            Vector2 kNow = _context.inputEvent.position;
            float kDx = kNow.x - m_DragStartTouch.x;
            float kDy = kNow.y - m_DragStartTouch.y;

            // 小位移当作点击，避免轻点一下就把站位改掉
            if (!m_Dragging && Mathf.Abs(kDx) < 4f && Mathf.Abs(kDy) < 4f)
            {
                return;
            }

            m_Dragging = true;

            float kMaxX = m_CardLayer.width - m_DragCard.width;
            float kX = Mathf.Clamp(m_DragStartX + kDx, 0f, kMaxX);
            m_DragCard.SetXY(kX, m_DragY);
        }

        private void OnCardTouchEnd(EventContext _context)
        {
            if (m_DragCard == null || _context.sender != m_DragCard)
            {
                return;
            }

            RoleBattleCom kCard = m_DragCard;
            bool kDragged = m_Dragging;

            m_DragCard = null;
            m_Dragging = false;

            if (kDragged)
            {
                OnCardDragEnd(kCard);
            }
        }

        private void OnCardDragEnd(RoleBattleCom _card)
        {
            float kMaxX = m_CardLayer.width - _card.width;

            // 用卡片中心算归一化站位，和战斗里"单位位置 = 卡片中心"保持一致
            float kPosition = m_CardLayer.width > 0.001f
                ? Mathf.Clamp01((_card.x + _card.width * 0.5f) / m_CardLayer.width)
                : 0f;
            _card.SetXY(
                Mathf.Clamp(kPosition * m_CardLayer.width - _card.width * 0.5f, 0f, kMaxX),
                _card.y);

            int kIndex = m_Cards.IndexOf(_card);
            if (kIndex >= 0 && kIndex < m_Info.m_Positions.Count)
            {
                m_Info.m_Positions[kIndex] = kPosition;
            }

            if (onCardPositionChanged != null)
            {
                onCardPositionChanged(_card.RoleId, kPosition);
            }
        }

        private void ClearCards()
        {
            for (int i = 0; i < m_Cards.Count; i++)
            {
                m_Cards[i].Dispose();
            }
            m_Cards.Clear();
        }

        #endregion

        private void OnClickBattle()
        {
            if (onBattle != null)
            {
                onBattle();
            }
        }
    }
}
