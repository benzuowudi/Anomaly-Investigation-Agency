using UnityEngine;
using UnityEditor;
using System.IO;

namespace Framework.Script.Editor
{
    public class AssetsBundleBuidler : EditorWindow
    {
        [MenuItem("Framework/全局资源打包工具")]
        static void Init()
        {
            AssetsBundleBuidler window = (AssetsBundleBuidler)GetWindow(typeof(AssetsBundleBuidler));
            window.Show();
        }

        private void LoadAssetbundles()
        {
            if (!EditorApplication.isUpdating)
            {
                //Directory.Delete("Assets/AssetBundles", true);
                Directory.CreateDirectory("Assets/StreamingAssets/AssetBundles");

                BuildPipeline.BuildAssetBundles("Assets/StreamingAssets/AssetBundles", BuildAssetBundleOptions.ChunkBasedCompression, BuildTarget.StandaloneWindows);
                Debug.Log("资源打包完成");
            }
            else
            {
                Debug.Log("资源管理器正在刷新");
            }
        }

        public void OnGUI()
        {
            GUILayout.Label("资源打包", EditorStyles.boldLabel);
            GUILayout.TextArea("打包说明：\n" +
                "打包工具会自动读取项目中设置了AssetBundles的资源，并将其按照设置的包名进行打包\n" +
                "如果没有设置AssetBundles包名，则资源不会被打包\n" +
                "目前项目中全部的加载均通过AB包，需要在项目中加载出来的资源必须要进行打包\n" +
                "全部的 <UI预制件、模型> 需要打包为以 <.unity3d> 为后缀的AB包\n" +
                "全部的 <美术资源> 需要依照资源文件夹名称，打包为以 <.ui> 为后缀的AB包\n" +
                "全部的 <配置表文件(.json)> 需要打包为以 <.data> 为后缀的AB包\n" +
                "全部的 <模型材质>需要打包为以 <.material> 为后缀的AB包" , GUILayout.Width(500f), GUILayout.Height(120f));
            if (GUILayout.Button("打包AssetBundles资源", GUILayout.Width(500)))
            {
                LoadAssetbundles();
            }
        }

        [MenuItem("Assets/Build AssetsBundle", priority = 101)]

        static void BuildAssetsBundle()
        {
            for (int i = 0; i < Selection.count; i++)
            {
                //AssetBundleBuild[] builder = new AssetBundleBuild[1];
                //builder[0].assetBundleName = Selection.gameObjects[i].name;
                //string[] asset = new string[1] { "" };
                //builder[0].assetNames = asset;
            }
        }
    }
}