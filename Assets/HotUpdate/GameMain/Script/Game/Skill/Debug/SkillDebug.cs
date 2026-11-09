#if UNITY_EDITOR
using cfg.GameType;
using Framework.Script.Constant;
using GameMain.Script.Battle;
using UnityEditor;
using UnityEngine;

namespace GameMain.Script.Skill
{
    /// <summary>
    /// 技能系统的编辑器调试入口（只在编辑器里编译，打包时整段剔除）。
    ///
    /// 用法：先 Play → 跑一场 4v4 → 再用这里的菜单。
    /// "换技能"只改运行时单位的 m_ActiveSkillId，不动 role.xlsx 的随机结果。
    /// </summary>
    public static class SkillDebug
    {
        private const string c_Tag = "[技能] ";

        private static SkillSystem System
        {
            get { return Game.Model.Get<SkillSystem>(); }
        }

        private static bool EnsureReady()
        {
            if (!GameConst.m_GameStart)
            {
                Debug.LogError(c_Tag + "请先运行游戏再点这个菜单（GameConst.m_GameStart = false）");
                return false;
            }

            SkillSystem kSystem = System;
            if (kSystem == null || kSystem.GetBattle() == null)
            {
                Debug.LogError(c_Tag + "没有进行中的战斗，先点 Framework/战斗/跑一场 4v4");
                return false;
            }
            return true;
        }

        #region 打印

        [MenuItem("Framework/技能/打印技能冷却", false, 300)]
        public static void DumpSkillState()
        {
            if (!EnsureReady())
            {
                return;
            }
            Debug.Log(System.DumpSkillState());
        }

        [MenuItem("Framework/技能/打印效果实例", false, 301)]
        public static void DumpEffects()
        {
            if (!EnsureReady())
            {
                return;
            }
            Debug.Log(System.DumpEffects());
        }

        #endregion

        #region 换技能

        [MenuItem("Framework/技能/调试-全队换成 极速突击(30001)", false, 310)]
        public static void Assign30001()
        {
            Assign(30001);
        }

        [MenuItem("Framework/技能/调试-全队换成 炽热刀刃(30004)", false, 311)]
        public static void Assign30004()
        {
            Assign(30004);
        }

        [MenuItem("Framework/技能/调试-全队换成 应急补给包(30007)", false, 312)]
        public static void Assign30007()
        {
            Assign(30007);
        }

        [MenuItem("Framework/技能/调试-全队换成 振奋人心(30012)", false, 313)]
        public static void Assign30012()
        {
            Assign(30012);
        }

        [MenuItem("Framework/技能/调试-全队换成 纵火犯(30013)", false, 314)]
        public static void Assign30013()
        {
            Assign(30013);
        }

        [MenuItem("Framework/技能/调试-全队换成 战斗兴奋剂(30015)", false, 315)]
        public static void Assign30015()
        {
            Assign(30015);
        }

        private static void Assign(int _skillId)
        {
            if (!EnsureReady())
            {
                return;
            }

            int kCount = System.DebugAssignSkill(_skillId, Faction.ally);
            Debug.Log(c_Tag + "调试：我方 " + kCount + " 个单位换成技能 " + _skillId);
        }

        #endregion

        #region 强制施放

        [MenuItem("Framework/技能/调试-立刻施放 unit 1", false, 320)]
        public static void ForceCastUnitOne()
        {
            if (!EnsureReady())
            {
                return;
            }

            bool kCast = System.DebugForceCast(1);
            Debug.Log(c_Tag + "强制施放 unit 1：" + (kCast ? "成功" : "失败（无技能/无效果）"));
        }

        [MenuItem("Framework/技能/调试-清掉所有我方冷却", false, 321)]
        public static void ClearCooldown()
        {
            if (!EnsureReady())
            {
                return;
            }

            BattleSystem kBattle = System.GetBattle();
            for (int i = 0; i < kBattle.GetUnits().Count; i++)
            {
                System.ClearSkillCooldown(kBattle.GetUnits()[i]);
            }
            Debug.Log(c_Tag + "已清空全部单位技能冷却");
        }

        #endregion

        #region 配置校验

        [MenuItem("Framework/技能/校验技能配置", false, 330)]
        public static void ValidateConfig()
        {
            if (!GameConst.m_GameStart)
            {
                Debug.LogError(c_Tag + "请先运行游戏再点这个菜单（GameConst.m_GameStart = false）");
                return;
            }

            cfg.Tables kTables = Game.Data.GetData;
            if (kTables == null || kTables.skill == null)
            {
                Debug.LogError(c_Tag + "配置表未加载");
                return;
            }

            int kSkillCount = 0;
            int kWithEffect = 0;
            int kError = 0;
            for (int i = 0; i < kTables.skill.DataList.Count; i++)
            {
                cfg.GameData_Skill.SkillData kSkill = kTables.skill.DataList[i];
                kSkillCount++;

                if (kSkill.Cd < 1)
                {
                    Debug.LogError(c_Tag + "技能 " + kSkill.ID + " 的 cd 非法：" + kSkill.Cd);
                    kError++;
                }

                if (kSkill.Effect == null || kSkill.Effect.Count == 0)
                {
                    continue;
                }
                kWithEffect++;

                for (int e = 0; e < kSkill.Effect.Count; e++)
                {
                    cfg.GameData_Skill.EffectData kEffect = SkillRule.GetEffect(kSkill.Effect[e]);
                    if (kEffect == null)
                    {
                        Debug.LogError(c_Tag + "技能 " + kSkill.ID + " 引用了不存在的效果 " + kSkill.Effect[e]);
                        kError++;
                        continue;
                    }
                    if (kEffect.Type == EffectType.attrMod && kEffect.AttrID <= 0
                        && kEffect.ValueType != AttrModType.setTo)
                    {
                        Debug.LogError(c_Tag + "效果 " + kEffect.ID + " 是 attrMod 但 attrID 为 0");
                        kError++;
                    }
                    if (kEffect.Type == EffectType.addBuff && kEffect.BuffID <= 0)
                    {
                        Debug.LogError(c_Tag + "效果 " + kEffect.ID + " 是 addBuff 但 buffID 为 0");
                        kError++;
                    }
                    if (kEffect.Type == EffectType.addState
                        && kEffect.StateTrigger == StateTriggerCondition.immediate)
                    {
                        Debug.LogError(c_Tag + "效果 " + kEffect.ID + " 是 addState 但没配 stateTrigger");
                        kError++;
                    }
                }
            }

            if (kTables.effect != null)
            {
                for (int i = 0; i < kTables.effect.DataList.Count; i++)
                {
                    cfg.GameData_Skill.EffectData kEffect = kTables.effect.DataList[i];
                    if (kEffect.Type == EffectType.addItem && kEffect.ValueSourceParam <= 0)
                    {
                        Debug.LogError(c_Tag + "效果 " + kEffect.ID + " 是 addItem 但 valueSourceParam（itemID）为 0");
                        kError++;
                    }
                    if (kEffect.Type == EffectType.addState && kEffect.TickEffectID <= 0)
                    {
                        Debug.LogWarning(c_Tag + "效果 " + kEffect.ID
                            + " 是 addState 但没配 payload（tickEffectID），触发后不会有任何结算");
                    }
                }
            }

            Debug.Log(c_Tag + "校验完成：技能 " + kSkillCount + " 条，其中已配效果 " + kWithEffect
                + " 条，错误 " + kError + " 个");
        }

        #endregion
    }
}
#endif
