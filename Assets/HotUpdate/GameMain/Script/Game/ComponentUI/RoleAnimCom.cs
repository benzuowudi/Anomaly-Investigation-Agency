using System;
using Framework.Script.UI.Anim;

namespace GameMain.Script.UI
{
    /// <summary>
    /// Base 包里的 RoleAnimCom（FairyGUI 侧只有一个 0,0 的 graph 挂点 "GameObjectGo"）。
    ///
    /// 真正的工作在基类 <see cref="SpineAnimCom"/> 里，这里只做角色相关的便捷封装：
    /// 角色骨架的动作名常量、待机动画兜底、播一次动作的回调。
    ///
    /// 用法：
    ///   com_role_anim.SetRole("role1");            // 加载骨架 + 播待机
    ///   com_role_anim.PlayOnce(RoleAnimCom.c_Attack);
    ///   com_role_anim.SetSkin("01");               // 换装（骨架里有 01~06 套皮肤）
    ///
    /// 动作名以 role1（chi）这套骨架为准：attack / death / dizziness / fall / float1~3 /
    /// funnystand / power / readystand / relaxstand / run / skill / walk / win。
    /// 以后新角色的动作名如果不一样，SetRole 的第二个参数可以直接指定待机动画。
    /// </summary>
    public class RoleAnimCom : SpineAnimCom
    {
        // ---- 动作名（role1 骨架的实际动画名） ----

        /// <summary>待机</summary>
        public const string c_ReadStand = "readystand";

        /// <summary>放松站姿</summary>
        public const string c_RelaxStand = "relaxstand";

        /// <summary>滑稽站姿</summary>
        public const string c_FunnyStand = "funnystand";

        /// <summary>攻击</summary>
        public const string c_Attack = "attack";

        /// <summary>技能</summary>
        public const string c_Skill = "skill";

        /// <summary>发力</summary>
        public const string c_Power = "power";

        /// <summary>眩晕</summary>
        public const string c_Dizziness = "dizziness";

        /// <summary>倒地</summary>
        public const string c_Fall = "fall";

        /// <summary>死亡</summary>
        public const string c_Death = "death";

        /// <summary>胜利</summary>
        public const string c_Win = "win";

        /// <summary>走</summary>
        public const string c_Walk = "walk";

        /// <summary>跑</summary>
        public const string c_Run = "run";

        /// <summary>漂浮 1~3</summary>
        public const string c_Float1 = "float1";
        public const string c_Float2 = "float2";
        public const string c_Float3 = "float3";

        /// <summary>
        /// 默认皮肤。role1 的 <c>default</c> 皮肤只有头/手/影子等公共部件，
        /// 完整角色在 01~06 这几套皮肤里（01~03 完整，04 有破图，05/06 更破，见 Docs/Spine动画组件.md）。
        /// </summary>
        public const string c_DefaultSkin = "01";

        /// <summary>待机动画的兜底关键词（按顺序匹配，找不到就用第一个动画）</summary>
        private static readonly string[] c_IdleKeywords = { "readystand", "idle", "relaxstand", "stand", "loop", "wait" };

        /// <summary>
        /// 加载角色骨架并播放待机动画。
        /// </summary>
        /// <param name="_address">Addressables 地址（SkeletonDataAsset）</param>
        /// <param name="_idleAnimation">指定待机动画名；留空则用 c_ReadStand，资源里没有时按关键词挑</param>
        /// <param name="_skin">皮肤（换装）；留空表示用资源默认皮肤</param>
        /// <returns>是否挂成功</returns>
        public bool SetRole(string _address, string _idleAnimation = null, string _skin = c_DefaultSkin)
        {
            if (!SetSpine(_address))
            {
                return false;
            }

            if (!string.IsNullOrEmpty(_skin) && HasSkin(_skin))
            {
                SetSkin(_skin);
            }

            string kIdle = _idleAnimation;
            if (string.IsNullOrEmpty(kIdle))
            {
                kIdle = HasAnimation(c_ReadStand) ? c_ReadStand : PickIdleAnimation();
            }

            if (!string.IsNullOrEmpty(kIdle))
            {
                Play(kIdle, true);
            }

            return true;
        }

        /// <summary>播待机（资源里没有 readystand 时按关键词兜底）</summary>
        public void PlayIdle()
        {
            string kIdle = HasAnimation(c_ReadStand) ? c_ReadStand : PickIdleAnimation();
            if (!string.IsNullOrEmpty(kIdle))
            {
                Play(kIdle, true);
            }
        }

        /// <summary>
        /// 播一次动作，播完回调。
        /// 资源里没有这个动作时（换角色骨架常见）退回待机，不会卡在空动作上。
        /// </summary>
        public void PlayOnce(string _animation, Action _onComplete = null)
        {
            if (!HasAnimation(_animation))
            {
                PlayIdle();
                return;
            }

            if (_onComplete != null)
            {
                Action<string> kHandler = null;
                kHandler = (_name) =>
                {
                    onComplete -= kHandler;
                    _onComplete();
                };
                onComplete += kHandler;
            }

            Play(_animation, false);
        }

        /// <summary>换装：骨架里 01~06 是几套皮肤，传空串回默认皮肤</summary>
        public void SetDress(string _skin)
        {
            SetSkin(_skin);
        }

        /// <summary>按关键词在资源里挑一个待机动画，挑不到就用第一个</summary>
        private string PickIdleAnimation()
        {
            string[] kAnimations = GetAnimations();
            if (kAnimations == null || kAnimations.Length == 0)
            {
                return null;
            }

            for (int i = 0; i < c_IdleKeywords.Length; i++)
            {
                string kKeyword = c_IdleKeywords[i];
                for (int j = 0; j < kAnimations.Length; j++)
                {
                    string kName = kAnimations[j];
                    if (!string.IsNullOrEmpty(kName) &&
                        kName.IndexOf(kKeyword, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return kName;
                    }
                }
            }

            return kAnimations[0];
        }
    }
}
