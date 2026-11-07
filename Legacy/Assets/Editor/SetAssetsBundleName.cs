using System.IO;
using UnityEditor;
using UnityEngine;

namespace Framework.Script.Editor
{
    public class SetAssetsBundleName
    {
        public static string name;
        [MenuItem("Assets/SetAssetsBundleName/资源打包(.unity3d)", priority = 100)]
        public static void InitAssat()
        {
            for (int i = 0; i < Selection.objects.Length; i++)
            {
                string kname = AssetDatabase.GetAssetPath(Selection.objects[i]);
                DirectoryInfo dirInfo = new DirectoryInfo(kname);
                if (dirInfo == null)
                {
                    Debug.LogError("******设置失败******");
                    return;
                }
                name = dirInfo.Name.Split('.')[0];

                string filePath = dirInfo.FullName.Replace('\\', '/');
                filePath = filePath.Replace(Application.dataPath, "Assets");
                AssetImporter ai = AssetImporter.GetAtPath(filePath);
                ai.assetBundleName = name + ".unity3d";

                //SetName(dirInfo);
            }
            AssetDatabase.Refresh();
            Debug.Log("******设置AssetBundle名称成功******");
        }

        [MenuItem("Assets/SetAssetsBundleName/数据打包(.data)", priority = 100)]
        public static void InitData()
        {
            for (int i = 0; i < Selection.objects.Length; i++)
            {
                string kname = AssetDatabase.GetAssetPath(Selection.objects[i]);
                DirectoryInfo dirInfo = new DirectoryInfo(kname);
                if (dirInfo == null)
                {
                    Debug.LogError("******设置失败******");
                    return;
                }
                name = dirInfo.Name.Split('.')[0];

                string filePath = dirInfo.FullName.Replace('\\', '/');
                filePath = filePath.Replace(Application.dataPath, "Assets");
                AssetImporter ai = AssetImporter.GetAtPath(filePath);
                ai.assetBundleName = name + ".data";

                //SetName(dirInfo);
            }
            AssetDatabase.Refresh();
            Debug.Log("******设置AssetBundle名称成功******");
        }

        [MenuItem("Assets/SetAssetsBundleName/UI美术资源打包(.ui)", priority = 100)]
        public static void InitUI()
        {
            for (int i = 0; i < Selection.objects.Length; i++)
            {
                string kname = AssetDatabase.GetAssetPath(Selection.objects[i]);
                DirectoryInfo dirInfo = new DirectoryInfo(kname);
                if (dirInfo == null)
                {
                    Debug.LogError("******设置失败******");
                    return;
                }
                name = dirInfo.Parent.Name;

                string filePath = dirInfo.FullName.Replace('\\', '/');
                filePath = filePath.Replace(Application.dataPath, "Assets");
                AssetImporter ai = AssetImporter.GetAtPath(filePath);
                ai.assetBundleName = string.Format("{0}{1}", name, ".ui");

                //SetName(dirInfo);
            }
            AssetDatabase.Refresh();
            Debug.Log("******设置AssetBundle名称成功******");
        }

        [MenuItem("Assets/SetAssetsBundleName/模型材质打包(.material)", priority = 100)]
        public static void InitMaterial()
        {
            for (int i = 0; i < Selection.objects.Length; i++)
            {
                string kname = AssetDatabase.GetAssetPath(Selection.objects[i]);
                DirectoryInfo dirInfo = new DirectoryInfo(kname);
                if (dirInfo == null)
                {
                    Debug.LogError("******设置失败******");
                    return;
                }
                name = dirInfo.Name.Split('.')[0];

                string filePath = dirInfo.FullName.Replace('\\', '/');
                filePath = filePath.Replace(Application.dataPath, "Assets");
                AssetImporter ai = AssetImporter.GetAtPath(filePath);
                ai.assetBundleName = string.Format("{0}{1}", name, ".material");

                //SetName(dirInfo);
            }
            AssetDatabase.Refresh();
            Debug.Log("******设置AssetBundle名称成功******");
        }

        [MenuItem("Assets/SetAssetsBundleName/热更代码(.code)", priority = 100)]
        public static void Initcode()
        {
            for (int i = 0; i < Selection.objects.Length; i++)
            {
                string kname = AssetDatabase.GetAssetPath(Selection.objects[i]);
                DirectoryInfo dirInfo = new DirectoryInfo(kname);
                if (dirInfo == null)
                {
                    Debug.LogError("******设置失败******");
                    return;
                }
                name = dirInfo.Name.Split('.')[0] + "_" + dirInfo.Name.Split('.')[1];

                string filePath = dirInfo.FullName.Replace('\\', '/');
                filePath = filePath.Replace(Application.dataPath, "Assets");
                AssetImporter ai = AssetImporter.GetAtPath(filePath);
                ai.assetBundleName = string.Format("{0}{1}", name, ".code");

                //SetName(dirInfo);
            }
            AssetDatabase.Refresh();
            Debug.Log("******设置AssetBundle名称成功******");
        }

        static void SetName(DirectoryInfo dirInfo)
        {
            FileSystemInfo[] files = dirInfo.GetFileSystemInfos();
            foreach (FileSystemInfo file in files)
            {
                if (file is FileInfo && file.Extension != ".meta" && file.Extension != ".txt")
                {
                    string filePath = file.FullName.Replace('\\', '/');
                    filePath = filePath.Replace(Application.dataPath, "Assets");
                    AssetImporter ai = AssetImporter.GetAtPath(filePath);
                    ai.assetBundleName = name;
                }
                else if (file is DirectoryInfo)
                {
                    string filePath = file.FullName.Replace('\\', '/');
                    filePath = filePath.Replace(Application.dataPath, "Assets");
                    AssetImporter ai = AssetImporter.GetAtPath(filePath);
                    ai.assetBundleName = name;
                    SetName(file as DirectoryInfo);
                }
            }
        }
    }
}