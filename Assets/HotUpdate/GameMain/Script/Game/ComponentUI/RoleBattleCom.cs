using FairyGUI;
using FairyGUI.Utils;
using GameMain.Script.Role;
using UnityEngine;

namespace GameMain.Script.UI
{
    /// <summary>
    /// 战斗布阵/战斗画面里的单个角色卡（FairyGUI 包 Base / RoleBattleCom，135×263）。
    ///
    /// 结构（FGUI 里已经改过版）：
    ///   n11         遮罩（135×263）
    ///   n9          卡面底图
    ///   com_anim    Spine 角色（RoleAnimCom，底边中心对齐）
    ///   txt_name    角色名
    ///   com_equip / com_skill1 / com_skill2  武器、主动技能、被动天赋
    ///
    /// 缩放：
    ///   布阵界面 0.5（默认），战斗画面 0.6，由调用方通过 SetAnimScale 切换。
    ///   血条是代码加的（FGUI 里没有），只在战斗模式显示。
    /// </summary>
    public class RoleBattleCom : GComponent
    {
        /// <summary>角色骨架地址（现有资源只有 role1）</summary>
        private const string c_SpineAddress = "role1";

        /// <summary>布阵界面里的动画缩放</summary>
        public const float c_FormationScale = 0.5f;
        /// <summary>战斗画面里的动画缩放</summary>
        public const float c_BattleScale = 0.6f;

        private const float c_HpBarWidth = 119f;
        private const float c_HpBarHeight = 8f;

        private RoleAnimCom com_anim;
        private GTextField txt_name;
        private CommonItemCom com_equip;
        private CommonItemCom com_skill1;
        private CommonItemCom com_skill2;

        private GGraph m_HpBg;
        private GGraph m_HpFill;

        /// <summary>当前角色 ID（战斗时按 m_SourceId 匹配单位）</summary>
        public int RoleId { get; private set; }

        public override void ConstructFromXML(XML _xml)
        {
            base.ConstructFromXML(_xml);

            com_anim = GetChild("com_anim") as RoleAnimCom;
            // FGUI 里文本节点已从 n8 改名为 txt_name；兼容旧包/旧发布
            txt_name = GetChild("txt_name").asTextField;
            com_equip = GetChild("com_equip") as CommonItemCom;
            com_skill1 = GetChild("com_skill1") as CommonItemCom;
            com_skill2 = GetChild("com_skill2") as CommonItemCom;

            BuildHpBar();
        }

        private void BuildHpBar()
        {
            m_HpBg = new GGraph();
            m_HpBg.DrawRect(c_HpBarWidth, c_HpBarHeight, 0, Color.clear, new Color(0.08f, 0.08f, 0.1f, 0.9f));
            m_HpBg.SetXY(8f, 46f);
            m_HpBg.touchable = false;
            AddChild(m_HpBg);

            m_HpFill = new GGraph();
            m_HpFill.DrawRect(c_HpBarWidth, c_HpBarHeight, 0, Color.clear, new Color(0.35f, 0.9f, 0.45f, 1f));
            m_HpFill.SetXY(8f, 46f);
            m_HpFill.touchable = false;
            AddChild(m_HpFill);

            SetHpVisible(false);
        }

        /// <summary>填一个角色（布阵界面用）</summary>
        public void SetCom(RoleSnapshot _role, float _animScale = c_FormationScale)
        {
            if (_role == null || _role.m_Role == null)
            {
                RoleId = 0;
                txt_name.text = string.Empty;
                com_equip.SetCom(CommonItemCom.StateEmpty);
                com_skill1.SetCom(CommonItemCom.StateEmpty);
                com_skill2.SetCom(CommonItemCom.StateEmpty);
                SetHpVisible(false);
                return;
            }

            RoleId = _role.m_Role.m_Id;
            txt_name.text = _role.m_Role.GetDisplayName();

            // 骨架：role1；皮肤按角色 ID 在 01~03（完整的那三套）之间轮换
            string kSkin = ((RoleId % 3) + 1).ToString("D2");
            com_anim.SetRole(c_SpineAddress, null, kSkin);
            com_anim.SetScale(_animScale);

            cfg.GameData_Item.EquipData kEquip = PickEquip(_role);
            com_equip.SetCom(
                kEquip == null ? CommonItemCom.StateEmpty : CommonItemCom.StateEquip,
                kEquip == null ? null : kEquip.Icon,
                kEquip == null ? "未装备武器" : kEquip.Name);

            cfg.GameData_Skill.SkillData kSkill = _role.m_ActiveSkill;
            com_skill1.SetCom(
                kSkill == null ? CommonItemCom.StateEmpty : CommonItemCom.StateItem,
                kSkill == null ? null : CommonSkillIconCom.ResolveIconUrl(kSkill.Icon),
                kSkill == null ? "无主动技能" : kSkill.Name);

            cfg.GameData_Skill.TalentData kTalent = _role.m_Talent;
            com_skill2.SetCom(
                kTalent == null ? CommonItemCom.StateEmpty : CommonItemCom.StateItem,
                kTalent == null ? null : CommonSkillIconCom.ResolveIconUrl(kTalent.Icon),
                kTalent == null ? "无被动天赋" : kTalent.Name);

            SetBattleState(false, false);
            SetHpVisible(false);
        }

        /// <summary>切换骨架缩放（布阵 0.5 / 战斗 0.6）</summary>
        public void SetAnimScale(float _scale)
        {
            com_anim.SetScale(_scale);
        }

        /// <summary>血条：0~1，_visible 控制显示（布阵界面不显示）</summary>
        public void SetHp(int _current, int _max, bool _visible = true)
        {
            float kRatio = _max > 0 ? Mathf.Clamp01(_current / (float)_max) : 0f;
            m_HpFill.SetScale(kRatio, 1f);
            m_HpFill.visible = kRatio > 0.001f;
            SetHpVisible(_visible);
        }

        public void SetHpVisible(bool _visible)
        {
            m_HpBg.visible = _visible;
            m_HpFill.visible = _visible;
        }

        /// <summary>战斗状态：死亡/重伤时压暗</summary>
        public void SetBattleState(bool _dead, bool _downed)
        {
            grayed = _dead || _downed;
            alpha = _dead ? 0.25f : (_downed ? 0.5f : 1f);
        }

        /// <summary>
        /// 战斗里的单位位置：_t = 0~1（战场左端~右端），卡片**中心**对齐到 _t 的位置。
        /// </summary>
        public void SetBattlePosition(float _t, float _areaStart, float _areaWidth)
        {
            float kCenter = _areaStart + Mathf.Clamp01(_t) * _areaWidth;
            float kX = Mathf.Clamp(kCenter - width * 0.5f, _areaStart, _areaStart + _areaWidth - width);
            SetXY(kX, y);
        }

        /// <summary>与角色详情页一致：优先显示非护甲的装备，没有就退回第一件</summary>
        private static cfg.GameData_Item.EquipData PickEquip(RoleSnapshot _role)
        {
            if (_role == null || _role.m_Equips == null || _role.m_Equips.Count == 0)
            {
                return null;
            }

            for (int i = 0; i < _role.m_Equips.Count; i++)
            {
                if (!RoleTitleRule.IsArmor(_role.m_Equips[i].Type1))
                {
                    return _role.m_Equips[i];
                }
            }

            return _role.m_Equips[0];
        }
    }
}
