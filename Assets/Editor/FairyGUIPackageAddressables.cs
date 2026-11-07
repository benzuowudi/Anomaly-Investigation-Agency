using System.IO;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;

namespace Framework.Script.Editor
{
    /// <summary>
    /// 把 FairyGUI 导出的 UI 包注册进 Addressables（替代旧的 SetAssetsBundleName / AddressablesMigration）。
    /// 约定：
    ///   - 包描述：Assets/Res/UI/&lt;包名&gt;_fui.bytes   -> 地址 &lt;包名&gt;_fui
    ///   - 图集：  Assets/Res/UI/&lt;包名&gt;_atlas0[_n].png -> 地址 &lt;包名&gt;_atlas0[_n]
    ///   - 每个包一个 Addressables 分组，分组名与 Label 都是包名
    /// </summary>
    public class FairyGUIPackageAddressables
    {
        private const string kPackageRoot = "Assets/Res/UI";
        private const string kDescSuffix = "_fui";
        private const string kAtlasPrefix = "_atlas";

        [MenuItem("Framework/FairyGUI/注册 UI 包到 Addressables", priority = 200)]
        public static void RegisterAll()
        {
            if (!Directory.Exists(kPackageRoot))
            {
                Debug.LogError("[FairyGUI] 找不到 UI 包目录：" + kPackageRoot);
                return;
            }

            string[] kDescFiles = Directory.GetFiles(kPackageRoot, "*" + kDescSuffix + ".bytes", SearchOption.AllDirectories);
            if (kDescFiles.Length == 0)
            {
                Debug.LogError("[FairyGUI] 在 " + kPackageRoot + " 下没有找到 *_fui.bytes");
                return;
            }

            AddressableAssetSettings kSettings = AddressableAssetSettingsDefaultObject.GetSettings(true);
            if (kSettings == null)
            {
                Debug.LogError("[FairyGUI] 无法获取 AddressableAssetSettings");
                return;
            }

            int kPackageCount = 0;
            int kEntryCount = 0;

            for (int i = 0; i < kDescFiles.Length; i++)
            {
                string kDescPath = kDescFiles[i].Replace('\\', '/');
                string kDescName = Path.GetFileNameWithoutExtension(kDescPath); // Base_fui
                string kPackageName = kDescName.Substring(0, kDescName.Length - kDescSuffix.Length);
                if (string.IsNullOrEmpty(kPackageName))
                {
                    continue;
                }

                AddressableAssetGroup kGroup = GetOrCreateGroup(kSettings, kPackageName);
                kSettings.AddLabel(kPackageName, false);

                kEntryCount += AddEntry(kSettings, kGroup, kDescPath, kDescName, kPackageName);

                // 图集：<包名>_atlas0.png、<包名>_atlas0_1.png ...
                string kAtlasDir = Path.GetDirectoryName(kDescPath).Replace('\\', '/');
                string[] kAtlasFiles = Directory.GetFiles(kAtlasDir, kPackageName + kAtlasPrefix + "*.png");
                for (int j = 0; j < kAtlasFiles.Length; j++)
                {
                    string kAtlasPath = kAtlasFiles[j].Replace('\\', '/');
                    string kAtlasName = Path.GetFileNameWithoutExtension(kAtlasPath);
                    kEntryCount += AddEntry(kSettings, kGroup, kAtlasPath, kAtlasName, kPackageName);
                    FixAtlasImporter(kAtlasPath);
                }

                kPackageCount++;
                Debug.Log("[FairyGUI] 已注册包：" + kPackageName + "（图集 " + kAtlasFiles.Length + " 张）");
            }

            kSettings.SetDirty(AddressableAssetSettings.ModificationEvent.BatchModification, null, true, true);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[FairyGUI] 注册完成：包 " + kPackageCount + " 个，条目 " + kEntryCount + " 个");
        }

        private static AddressableAssetGroup GetOrCreateGroup(AddressableAssetSettings _settings, string _groupName)
        {
            AddressableAssetGroup kGroup = _settings.FindGroup(_groupName);
            if (kGroup == null)
            {
                kGroup = _settings.CreateGroup(_groupName, false, false, false, null,
                    typeof(BundledAssetGroupSchema), typeof(ContentUpdateGroupSchema));
            }

            BundledAssetGroupSchema kSchema = kGroup.GetSchema<BundledAssetGroupSchema>();
            if (kSchema != null)
            {
                kSchema.BuildPath.SetVariableByName(_settings, AddressableAssetSettings.kLocalBuildPath);
                kSchema.LoadPath.SetVariableByName(_settings, AddressableAssetSettings.kLocalLoadPath);
            }

            return kGroup;
        }

        private static int AddEntry(AddressableAssetSettings _settings, AddressableAssetGroup _group,
            string _assetPath, string _address, string _label)
        {
            string kGuid = AssetDatabase.AssetPathToGUID(_assetPath);
            if (string.IsNullOrEmpty(kGuid))
            {
                Debug.LogWarning("[FairyGUI] 资源没有 GUID，跳过：" + _assetPath);
                return 0;
            }

            AddressableAssetEntry kEntry = _settings.CreateOrMoveEntry(kGuid, _group, false, false);
            if (kEntry == null)
            {
                return 0;
            }

            kEntry.address = _address;
            kEntry.SetLabel(_label, true, true, false);
            return 1;
        }

        /// <summary>
        /// 图集必须是 Default 类型、不生成 Mipmap、Clamp、不缩放，否则 FairyGUI 会报采样错误
        /// </summary>
        private static void FixAtlasImporter(string _assetPath)
        {
            TextureImporter kImporter = AssetImporter.GetAtPath(_assetPath) as TextureImporter;
            if (kImporter == null)
            {
                return;
            }

            bool kDirty = false;
            if (kImporter.textureType != TextureImporterType.Default)
            {
                kImporter.textureType = TextureImporterType.Default;
                kDirty = true;
            }

            if (kImporter.mipmapEnabled)
            {
                kImporter.mipmapEnabled = false;
                kDirty = true;
            }

            if (!kImporter.alphaIsTransparency)
            {
                kImporter.alphaIsTransparency = true;
                kDirty = true;
            }

            if (kImporter.wrapMode != TextureWrapMode.Clamp)
            {
                kImporter.wrapMode = TextureWrapMode.Clamp;
                kDirty = true;
            }

            if (kImporter.npotScale != TextureImporterNPOTScale.None)
            {
                kImporter.npotScale = TextureImporterNPOTScale.None;
                kDirty = true;
            }

            if (kDirty)
            {
                kImporter.SaveAndReimport();
                Debug.Log("[FairyGUI] 已修正图集导入设置：" + _assetPath);
            }
        }
    }
}
