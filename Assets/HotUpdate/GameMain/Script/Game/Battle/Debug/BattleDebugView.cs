#if UNITY_EDITOR
using System.Collections.Generic;
using cfg.GameType;
using Framework.Script.Constant;
using UnityEngine;

namespace GameMain.Script.Battle
{
    /// <summary>
    /// 开发期的战斗可视化覆盖层（IMGUI，只在编辑器编译）。
    ///
    /// 为什么是 IMGUI 而不是 FairyGUI：
    /// 战斗 HUD 的 FairyGUI 包还没做（UIProject 里目前只有 BattleSetting），
    /// 这一版的目标是"能在屏幕上看见一维站位、血条、状态和结算"，
    /// 不绑定任何美术资源。等美术包到位后，这个类整个删掉，换成
    /// View/BattleView + Component/BattleUnitBarCom 的正式实现。
    ///
    /// 它同时承担"每帧驱动"的职责：BattleSystem.Tick(Time.deltaTime)，
    /// 因为正式驱动者 BattleOperate 的预制体还没接进 operate.xlsx。
    /// </summary>
    public class BattleDebugView : MonoBehaviour
    {
        private static BattleDebugView s_Instance;

        private GUIStyle m_LabelStyle;
        private GUIStyle m_SmallStyle;
        private GUIStyle m_ButtonStyle;
        private bool m_StylesReady;

        /// <summary>进入 Play 后自动创建，不需要改场景</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (s_Instance != null)
            {
                return;
            }

            GameObject kGo = new GameObject("[BattleDebugView]");
            DontDestroyOnLoad(kGo);
            s_Instance = kGo.AddComponent<BattleDebugView>();
        }

        private void Update()
        {
            if (!GameConst.m_GameStart)
            {
                return;
            }

            BattleSystem kBattle = GetBattle();
            if (kBattle == null)
            {
                return;
            }

            if (kBattle.GetPhase() == BattlePhase.Fighting)
            {
                kBattle.Tick(Time.deltaTime);
            }
        }

        private void OnGUI()
        {
            if (!GameConst.m_GameStart)
            {
                return;
            }

            // FGUI 战斗画面正在显示时，这里只负责 Tick（Update），不画 IMGUI
            if (BattlePresentation.s_FguiActive)
            {
                return;
            }

            BattleSystem kBattle = GetBattle();
            if (kBattle == null)
            {
                return;
            }

            BattlePhase kPhase = kBattle.GetPhase();
            if (kPhase == BattlePhase.None)
            {
                return;
            }

            EnsureStyles();

            // 顶栏底板：白字压在浅色背景上会看不清，先铺一层半透明黑
            DrawBackdrop(new Rect(0f, 0f, 470f, 74f));

            float kTop = 8f;
            GUI.Label(new Rect(8f, kTop, 640f, 22f),
                "[战斗] 阶段:" + kPhase + "   耗时:" + kBattle.GetElapsed().ToString("F1") + "s"
                + "   单位:" + kBattle.GetUnits().Count,
                m_LabelStyle);
            kTop += 24f;

            DrawToolbar(kBattle, kTop);
            kTop += 30f;

            DrawField(kBattle);

            if (kPhase == BattlePhase.Finished)
            {
                DrawResult(kBattle);
            }
        }

        #region 绘制

        private void DrawToolbar(BattleSystem _battle, float _top)
        {
            float kX = 8f;
            if (GUI.Button(new Rect(kX, _top, 80f, 24f), "集火", m_ButtonStyle))
            {
                BattleUnit kEnemy = FindFirstEnemy(_battle);
                if (kEnemy != null)
                {
                    _battle.IssueCommand(BattleCommandId.Focus, kEnemy.m_UnitId);
                }
            }
            kX += 84f;
            if (GUI.Button(new Rect(kX, _top, 80f, 24f), "全体撤离", m_ButtonStyle))
            {
                _battle.IssueCommand(BattleCommandId.Retreat);
            }
            kX += 84f;
            if (GUI.Button(new Rect(kX, _top, 80f, 24f), "自由行动", m_ButtonStyle))
            {
                _battle.IssueCommand(BattleCommandId.Free);
            }
            kX += 84f;

            if (_battle.GetPhase() == BattlePhase.Fighting)
            {
                if (GUI.Button(new Rect(kX, _top, 80f, 24f), "暂停", m_ButtonStyle))
                {
                    _battle.Pause();
                }
            }
            else if (_battle.GetPhase() == BattlePhase.Paused)
            {
                if (GUI.Button(new Rect(kX, _top, 80f, 24f), "继续", m_ButtonStyle))
                {
                    _battle.Resume();
                }
            }
            kX += 84f;

            if (GUI.Button(new Rect(kX, _top, 80f, 24f), "倍速 x1", m_ButtonStyle))
            {
                float kNext = _battle.GetClock().GetScale() >= 3f ? 1f : _battle.GetClock().GetScale() + 1f;
                _battle.SetTimeScale(kNext);
            }
        }

        private void DrawField(BattleSystem _battle)
        {
            BattleField kField = _battle.GetField();
            if (kField == null)
            {
                return;
            }

            float kCenterY = Screen.height * 0.55f;
            float kLeft = 60f;
            float kRight = Screen.width - 60f;

            GUI.color = new Color(0.6f, 0.6f, 0.6f, 1f);
            GUI.DrawTexture(new Rect(kLeft, kCenterY, kRight - kLeft, 2f), Texture2D.whiteTexture);
            GUI.color = Color.white;

            IReadOnlyList<BattleUnit> kUnits = _battle.GetUnits();

            // 同阵营单位在逻辑上可能站在同一点（没有碰撞），显示时按序号错开，
            // 否则 8 个单位会叠成一两个方块。错开只影响绘制坐标，不写回 m_Pos。
            int kAllyRow = 0;
            int kEnemyRow = 0;
            for (int i = 0; i < kUnits.Count; i++)
            {
                BattleUnit kUnit = kUnits[i];
                if (kUnit == null)
                {
                    continue;
                }

                bool kAlly = kUnit.m_Faction == Faction.ally;
                int kRow = kAlly ? kAllyRow++ : kEnemyRow++;

                float kT = Mathf.Clamp01(kUnit.m_Pos / kField.Length);
                float kX = Mathf.Lerp(kLeft, kRight, kT);
                float kY = kAlly
                    ? kCenterY - 96f - kRow * 34f
                    : kCenterY + 28f + kRow * 34f;

                // 单位方块
                GUI.color = kUnit.m_LeftField
                    ? new Color(0.3f, 0.3f, 0.3f, 0.6f)
                    : (kAlly ? new Color(0.25f, 0.55f, 1f) : new Color(0.9f, 0.3f, 0.3f));
                GUI.DrawTexture(new Rect(kX - 11f, kY, 22f, 22f), Texture2D.whiteTexture);
                GUI.color = Color.white;

                // 血条
                GUI.color = new Color(0.15f, 0.15f, 0.15f, 0.9f);
                GUI.DrawTexture(new Rect(kX - 22f, kY - 9f, 44f, 6f), Texture2D.whiteTexture);
                GUI.color = new Color(0.3f, 0.9f, 0.4f, 1f);
                GUI.DrawTexture(new Rect(kX - 22f, kY - 9f, 44f * kUnit.GetHpPercent(), 6f), Texture2D.whiteTexture);
                GUI.color = Color.white;

                // 行动条
                GUI.color = new Color(0.15f, 0.15f, 0.15f, 0.9f);
                GUI.DrawTexture(new Rect(kX - 22f, kY + 25f, 44f, 4f), Texture2D.whiteTexture);
                GUI.color = new Color(0.95f, 0.8f, 0.2f, 1f);
                GUI.DrawTexture(new Rect(kX - 22f, kY + 25f, 44f * Mathf.Clamp01(kUnit.m_ActionGauge), 4f), Texture2D.whiteTexture);
                GUI.color = Color.white;

                // 右侧文字：编号 / 状态 / 血量 / 技能CD / 状态数，重伤再补倒计时
                string kText = kUnit.m_UnitId + " " + ShortState(kUnit.m_State)
                    + " " + kUnit.m_Hp + "/" + kUnit.m_MaxHp;
                if (kUnit.m_ActiveSkillId > 0)
                {
                    kText += " | 技能" + kUnit.m_ActiveSkillId + " CD" + GetSkillCd(kUnit).ToString("F0");
                    int kStates = GetStateCount(kUnit);
                    if (kStates > 0)
                    {
                        kText += " 状态" + kStates;
                    }
                }
                if (kUnit.m_State == UnitBrainState.Downed)
                {
                    kText += " " + kUnit.m_DownedRemain.ToString("F0") + "s";
                }

                Rect kTextRect = new Rect(kX + 18f, kY + 2f, 250f, 18f);
                DrawBackdrop(new Rect(kTextRect.x - 3f, kTextRect.y, 250f, 18f));
                GUI.Label(kTextRect, kText, m_SmallStyle);
            }
        }

        private void DrawResult(BattleSystem _battle)
        {
            BattleResult kResult = _battle.GetLastResult();
            if (kResult == null)
            {
                return;
            }

            float kWidth = 420f;
            float kHeight = 260f;
            Rect kRect = new Rect(Screen.width * 0.5f - kWidth * 0.5f, Screen.height * 0.5f - kHeight * 0.5f, kWidth, kHeight);

            GUI.color = new Color(0f, 0f, 0f, 0.8f);
            GUI.DrawTexture(kRect, Texture2D.whiteTexture);
            GUI.color = Color.white;

            GUI.Label(new Rect(kRect.x + 12f, kRect.y + 10f, kWidth - 24f, 24f),
                "战斗结束：" + kResult.m_Outcome + "   " + kResult.m_Duration.ToString("F1") + "s",
                m_LabelStyle);

            for (int i = 0; i < kResult.m_Units.Count; i++)
            {
                UnitResult kUnit = kResult.m_Units[i];
                GUI.Label(new Rect(kRect.x + 16f, kRect.y + 40f + i * 22f, kWidth - 32f, 20f),
                    "unit " + kUnit.m_UnitId
                    + "  hp:" + kUnit.m_Hp
                    + (kUnit.m_Dead ? "  死亡" : (kUnit.m_Downed ? "  重伤" : "")),
                    m_SmallStyle);
            }
        }

        #endregion

        #region 工具

        private static BattleSystem GetBattle()
        {
            if (!Game.Model.Has<BattleSystem>())
            {
                return null;
            }
            return Game.Model.Get<BattleSystem>();
        }

        /// <summary>半透明底板：IMGUI 的 GUI.color 是全局状态，画完必须还原</summary>
        private static void DrawBackdrop(Rect _rect)
        {
            Color kOld = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.55f);
            GUI.DrawTexture(_rect, Texture2D.whiteTexture);
            GUI.color = kOld;
        }

        /// <summary>技能剩余冷却（技能系统没注册时返回 0）</summary>
        private static float GetSkillCd(BattleUnit _unit)
        {
            if (!Game.Model.Has<GameMain.Script.Skill.SkillSystem>())
            {
                return 0f;
            }
            return Game.Model.Get<GameMain.Script.Skill.SkillSystem>().GetSkillCd(_unit);
        }

        /// <summary>身上还没消耗的技能状态数</summary>
        private static int GetStateCount(BattleUnit _unit)
        {
            if (!Game.Model.Has<GameMain.Script.Skill.SkillSystem>())
            {
                return 0;
            }
            return Game.Model.Get<GameMain.Script.Skill.SkillSystem>().Effects.CountStates(_unit.m_UnitId);
        }

        private static BattleUnit FindFirstEnemy(BattleSystem _battle)
        {
            IReadOnlyList<BattleUnit> kUnits = _battle.GetUnits();
            for (int i = 0; i < kUnits.Count; i++)
            {
                if (kUnits[i].m_Faction == Faction.enemy && BattleField.IsActive(kUnits[i]))
                {
                    return kUnits[i];
                }
            }
            return null;
        }

        private static string ShortState(UnitBrainState _state)
        {
            switch (_state)
            {
                case UnitBrainState.Idle: return "待机";
                case UnitBrainState.Move: return "移动";
                case UnitBrainState.Attack: return "攻击";
                case UnitBrainState.Reload: return "换弹";
                case UnitBrainState.Retreat: return "撤离";
                case UnitBrainState.Controlled: return "受控";
                case UnitBrainState.Downed: return "重伤";
                case UnitBrainState.Dead: return "死亡";
                default: return _state.ToString();
            }
        }

        private void EnsureStyles()
        {
            if (m_StylesReady)
            {
                return;
            }

            Font kFont = Font.CreateDynamicFontFromOSFont("Microsoft YaHei", 14);
            m_LabelStyle = new GUIStyle(GUI.skin.label);
            m_LabelStyle.font = kFont;
            m_LabelStyle.fontSize = 15;
            m_LabelStyle.normal.textColor = Color.white;

            m_SmallStyle = new GUIStyle(GUI.skin.label);
            m_SmallStyle.font = kFont;
            m_SmallStyle.fontSize = 12;
            m_SmallStyle.normal.textColor = Color.white;

            m_ButtonStyle = new GUIStyle(GUI.skin.button);
            m_ButtonStyle.font = kFont;
            m_ButtonStyle.fontSize = 13;

            m_StylesReady = true;
        }

        #endregion
    }
}
#endif
