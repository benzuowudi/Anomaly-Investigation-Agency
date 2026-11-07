using Framework.Script.Base;
using System;
using System.Collections.Generic;
using FairyGUI;
using UnityEngine;

namespace Framework.Script.Manager
{
    /// <summary>
    /// FairyGUI 包加载器（Addressables 版，替代旧的 AssetBundle 加载）。
    /// 命名约定（与编辑器工具「Framework/FairyGUI/注册 UI 包到 Addressables」一致）：
    ///   &lt;包名&gt;_fui        —— 包描述（TextAsset，.bytes）
    ///   &lt;包名&gt;_atlas0[_n]  —— 图集（Texture2D）
    /// 包加载后注册进 UIPackage；界面内容由窗口预制体里的 FairyGUI.UIPanel 通过
    /// UIPackage.CreateObject 创建，UIManager 负责在实例化预制体之前把包准备好。
    /// </summary>
    public class FairyGUIManager : Singleton<FairyGUIManager>
    {
        // 包名 -> 从 Addressables 取过的资源地址，卸载时按此释放
        private readonly Dictionary<string, List<string>> m_PackageAssets = new Dictionary<string, List<string>>();

        // 正在加载的包名，供 LoadPackageAsset 记录资源归属
        private string m_LoadingPackage;

        /// <summary>
        /// 包是否已经加载
        /// </summary>
        public bool IsLoaded(string _packageName)
        {
            return !string.IsNullOrEmpty(_packageName) && UIPackage.GetByName(_packageName) != null;
        }

        /// <summary>
        /// 加载 FairyGUI 包：先取 &lt;包名&gt;_fui，再在解析过程中按需取 &lt;包名&gt;_atlas*
        /// </summary>
        public bool LoadPackage(string _packageName)
        {
            if (string.IsNullOrEmpty(_packageName))
            {
                Debug.LogError("[FairyGUI] 包名不能为空");
                return false;
            }

            if (IsLoaded(_packageName))
            {
                return true;
            }

            string kDescAddress = _packageName + "_fui";
            TextAsset kDesc = Game.Res.LoadAsset<TextAsset>(kDescAddress);
            if (kDesc == null)
            {
                Debug.LogError("[FairyGUI] 找不到包描述 " + kDescAddress +
                               "，请确认该包已加入 Addressables（菜单 Framework/FairyGUI/注册 UI 包到 Addressables）");
                return false;
            }

            m_LoadingPackage = _packageName;
            if (!m_PackageAssets.ContainsKey(_packageName))
            {
                m_PackageAssets.Add(_packageName, new List<string>());
            }

            UIPackage kPackage = null;
            try
            {
                kPackage = UIPackage.AddPackage(kDesc.bytes, _packageName, LoadPackageAsset);
            }
            catch (Exception ex)
            {
                Debug.LogError("[FairyGUI] 包加载异常：" + _packageName + "\n" + ex);
            }
            finally
            {
                m_LoadingPackage = null;
            }

            if (kPackage == null)
            {
                Debug.LogError("[FairyGUI] 包描述解析失败：" + kDescAddress);
                m_PackageAssets.Remove(_packageName);
                return false;
            }

            // 跨包引用（例如 Role 包引用 Base 的 CommonBackBtn/CommonItemCom、
            // CommonUI 的属性图标）必须先把依赖包也加载好，否则这些元件会是空白。
            LoadDependencies(kPackage);

            Debug.Log("[FairyGUI] 包已加载：" + _packageName + "（" + kPackage.name + "）");
            return true;
        }

        /// <summary>
        /// 递归加载包依赖。依赖信息由 FairyGUI 发布时写入包描述，格式为 id/name。
        /// </summary>
        private void LoadDependencies(UIPackage _package)
        {
            if (_package == null || _package.dependencies == null)
            {
                return;
            }

            for (int i = 0; i < _package.dependencies.Length; i++)
            {
                Dictionary<string, string> kDependency = _package.dependencies[i];
                if (kDependency == null)
                {
                    continue;
                }

                string kName;
                if (kDependency.TryGetValue("name", out kName) && !string.IsNullOrEmpty(kName))
                {
                    LoadPackage(kName);
                }
            }
        }

        /// <summary>
        /// 包的资源加载回调。_name 形如 "Base_atlas0"，与 Addressables 地址一致。
        /// </summary>
        private object LoadPackageAsset(string _name, string _extension, Type _type, out DestroyMethod _destroyMethod)
        {
            // 纹理由 Addressables 的句柄管理生命周期，FairyGUI 不要销毁
            _destroyMethod = DestroyMethod.None;

            object kAsset = null;
            if (_type == typeof(Texture2D) || _type == typeof(Texture))
            {
                kAsset = Game.Res.LoadAsset<Texture2D>(_name);
            }
            else if (_type == typeof(TextAsset))
            {
                kAsset = Game.Res.LoadAsset<TextAsset>(_name);
            }
            else if (_type == typeof(AudioClip))
            {
                kAsset = Game.Res.LoadAsset<AudioClip>(_name);
            }
            else
            {
                Debug.LogWarning("[FairyGUI] 暂不支持的包资源类型：" + _type + "，name=" + _name);
                return null;
            }

            if (kAsset == null)
            {
                Debug.LogError("[FairyGUI] 包资源加载失败：" + _name + "（包：" + m_LoadingPackage + "）");
                return null;
            }

            if (m_LoadingPackage != null
                && m_PackageAssets.TryGetValue(m_LoadingPackage, out List<string> kAssets)
                && !kAssets.Contains(_name))
            {
                kAssets.Add(_name);
            }

            return kAsset;
        }

        /// <summary>
        /// 卸载包（先销毁该包的窗口再调用）
        /// </summary>
        public void UnloadPackage(string _packageName)
        {
            if (!IsLoaded(_packageName))
            {
                return;
            }

            UIPackage.RemovePackage(_packageName);

            if (m_PackageAssets.TryGetValue(_packageName, out List<string> kAssets))
            {
                for (int i = 0; i < kAssets.Count; i++)
                {
                    Game.Res.Unload(kAssets[i]);
                }

                kAssets.Clear();
                m_PackageAssets.Remove(_packageName);
            }

            Game.Res.Unload(_packageName + "_fui");
            Debug.Log("[FairyGUI] 包已卸载：" + _packageName);
        }
    }
}
