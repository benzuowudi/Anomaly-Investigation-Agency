using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;

namespace Framework.Script.Editor
{
    /// <summary>
    /// 把旧的 AssetBundle 配置迁移到 Addressables。
    /// 约定：
    /// - Address = 资源文件名（重名时使用 "旧包名/资源文件名"）
    /// - Label   = 旧 AssetBundle 包名，例如 universal.ui、commandroom.unity3d
    /// </summary>
    public class AddressablesMigration
    {
        private const string kAddressableDataPath = "Assets/AddressableAssetsData/";

        [MenuItem("Framework/Addressables/迁移旧 AssetBundle 配置")]
        public static void Migrate()
        {
            if (!EditorUtility.DisplayDialog("迁移 AssetBundle 到 Addressables",
                    "将扫描项目中设置了 AssetBundle 名称的资源，并按旧包名加入 Addressables 分组。\n\n" +
                    "Address = 资源文件名（重名时会带旧包名前缀）\n" +
                    "Label = 旧 AssetBundle 包名\n\n" +
                    "是否继续？", "开始迁移", "取消"))
            {
                return;
            }

            string[] kAllAssetPaths = AssetDatabase.GetAllAssetPaths();
            Dictionary<string, string> kAssetBundleDic = new Dictionary<string, string>();
            Dictionary<string, int> kAddressCountDic = new Dictionary<string, int>();

            for (int i = 0; i < kAllAssetPaths.Length; i++)
            {
                string kAssetPath = kAllAssetPaths[i];
                if (!kAssetPath.StartsWith("Assets/") || kAssetPath.StartsWith(kAddressableDataPath))
                {
                    continue;
                }
                if (AssetDatabase.IsValidFolder(kAssetPath))
                {
                    continue;
                }

                AssetImporter kImporter = AssetImporter.GetAtPath(kAssetPath);
                if (kImporter == null || string.IsNullOrEmpty(kImporter.assetBundleName))
                {
                    continue;
                }

                kAssetBundleDic[kAssetPath] = kImporter.assetBundleName;

                string kFileName = Path.GetFileNameWithoutExtension(kAssetPath);
                int kCount;
                kAddressCountDic.TryGetValue(kFileName, out kCount);
                kAddressCountDic[kFileName] = kCount + 1;
            }

            if (kAssetBundleDic.Count == 0)
            {
                EditorUtility.DisplayDialog("迁移完成", "没有找到设置了 AssetBundle 名称的资源。", "确定");
                return;
            }

            AddressableAssetSettings kSettings = AddressableAssetSettingsDefaultObject.GetSettings(true);
            if (kSettings == null)
            {
                Debug.LogError("[AddressablesMigration] 无法获取 AddressableAssetSettings");
                return;
            }

            int kMigratedCount = 0;
            try
            {
                foreach (var kPair in kAssetBundleDic)
                {
                    string kAssetPath = kPair.Key;
                    string kBundleName = kPair.Value;
                    string kFileName = Path.GetFileNameWithoutExtension(kAssetPath);
                    string kAddress = kAddressCountDic[kFileName] > 1
                        ? kBundleName + "/" + kFileName
                        : kFileName;

                    AddressableAssetGroup kGroup = kSettings.FindGroup(kBundleName);
                    if (kGroup == null)
                    {
                        kGroup = kSettings.CreateGroup(kBundleName, false, false, false, null,
                            typeof(BundledAssetGroupSchema), typeof(ContentUpdateGroupSchema));
                    }

                    BundledAssetGroupSchema kSchema = kGroup.GetSchema<BundledAssetGroupSchema>();
                    if (kSchema != null)
                    {
                        kSchema.BuildPath.SetVariableByName(kSettings, AddressableAssetSettings.kLocalBuildPath);
                        kSchema.LoadPath.SetVariableByName(kSettings, AddressableAssetSettings.kLocalLoadPath);
                    }

                    string kGuid = AssetDatabase.AssetPathToGUID(kAssetPath);
                    if (string.IsNullOrEmpty(kGuid))
                    {
                        continue;
                    }

                    AddressableAssetEntry kEntry = kSettings.CreateOrMoveEntry(kGuid, kGroup, false, false);
                    if (kEntry == null)
                    {
                        continue;
                    }

                    kEntry.address = kAddress;
                    kEntry.SetLabel(kBundleName, true, true, false);
                    kMigratedCount++;
                }
            }
            finally
            {
                kSettings.SetDirty(AddressableAssetSettings.ModificationEvent.BatchModification, null, true, true);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            Debug.Log("[AddressablesMigration] 迁移完成，共处理 " + kMigratedCount + " 个资源");
            EditorUtility.DisplayDialog("迁移完成",
                "共处理 " + kMigratedCount + " 个资源。\n请打开 Addressables Groups 窗口检查分组、Address 和 Label。", "确定");
        }
    }
}
