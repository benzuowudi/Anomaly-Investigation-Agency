using System.Collections.Generic;
using FairyGUI;
using FairyGUI.Utils;
using UnityEngine;

namespace GameMain.Script.UI
{
    /// <summary>
    /// 通用技能/装备图标（FairyGUI 包 Base / CommonSkillIconCom）。
    /// 只负责显示图标和控制显隐，具体图标由调用方传入。
    ///
    /// 配置表里的 icon 字段写资源名即可（如 ui_skill_30001 / ui_talent_20001），
    /// 统一由 <see cref="ResolveIconUrl"/> 拼成 CommonUI 包里的 FairyGUI 地址。
    /// </summary>
    public class CommonSkillIconCom : GComponent
    {
        /// <summary>技能/天赋图标所在的 FairyGUI 包</summary>
        private const string c_IconPackage = "CommonUI";

        /// <summary>技能或天赋为空时显示的占位图标（Base 包）</summary>
        public const string PlaceholderIconUrl = "ui://Base/ui_icon_cross";

        // 已经警告过的图标地址，避免每帧/每张卡刷屏
        private static readonly HashSet<string> s_MissingIcons = new HashSet<string>();

        private GLoader load_icon;

        public override void ConstructFromXML(XML xml)
        {
            base.ConstructFromXML(xml);
            load_icon = GetChild("load_icon").asLoader;
        }

        /// <summary>
        /// 按配置里的 icon 字段显示图标，解析不到时隐藏自身。
        /// </summary>
        public void SetIcon(string _icon)
        {
            string kUrl = ResolveIconUrl(_icon);
            SetCom(kUrl, !string.IsNullOrEmpty(kUrl));
        }

        /// <summary>
        /// 技能/天赋图标：配置为空（或没有对应数据）时显示 Base 包的占位图 ui_icon_cross。
        /// </summary>
        public void SetIconOrPlaceholder(string _icon)
        {
            string kUrl = ResolveIconUrlOrPlaceholder(_icon);
            SetCom(kUrl, !string.IsNullOrEmpty(kUrl));
        }

        public void SetCom(string _iconUrl, bool _visible)
        {
            visible = _visible;
            if (!string.IsNullOrEmpty(_iconUrl))
            {
                load_icon.url = _iconUrl;
            }
        }

        /// <summary>
        /// 把配置里的 icon 解析成 FairyGUI 地址。支持三种写法：
        ///   ui_skill_30001                  —— 资源名（推荐）
        ///   Texture/skill/ui_skill_30001.png —— 带目录/后缀的路径
        ///   ui://CommonUI/ui_skill_30001     —— 完整地址
        /// 包没加载或图标不存在时返回 null，并只警告一次。
        /// </summary>
        public static string ResolveIconUrl(string _icon)
        {
            if (string.IsNullOrEmpty(_icon))
            {
                return null;
            }

            string kName = _icon.Trim();
            if (kName.Length == 0)
            {
                return null;
            }

            if (kName.StartsWith("ui://"))
            {
                if (UIPackage.GetItemByURL(kName) != null)
                {
                    return kName;
                }
                WarnMissing(kName);
                return null;
            }

            // 配置里可能写的是带目录/后缀的路径，统一取文件名再去掉扩展名
            int kSlash = kName.LastIndexOf('/');
            if (kSlash >= 0)
            {
                kName = kName.Substring(kSlash + 1);
            }
            int kDot = kName.LastIndexOf('.');
            if (kDot > 0)
            {
                kName = kName.Substring(0, kDot);
            }

            string kUrl = "ui://" + c_IconPackage + "/" + kName;
            if (UIPackage.GetItemByURL(kUrl) != null)
            {
                return kUrl;
            }

            WarnMissing(kUrl);
            return null;
        }

        /// <summary>
        /// 技能/天赋图标解析：icon 为空（没有技能/天赋，或配置没填图标）时返回占位图
        /// ui_icon_cross 的地址；配置了图标但图不存在时仍返回 null，保持隐藏，
        /// 避免把“美术资源缺失”误显示成“这个角色没有技能”。
        /// </summary>
        public static string ResolveIconUrlOrPlaceholder(string _icon)
        {
            return ResolveIconUrl(string.IsNullOrEmpty(_icon) ? PlaceholderIconUrl : _icon);
        }

        private static void WarnMissing(string _url)
        {
            if (s_MissingIcons.Add(_url))
            {
                Debug.LogWarning("[UI] 图标不存在：" + _url + "，请确认该图已导入 CommonUI 包并重新发布");
            }
        }
    }
}
