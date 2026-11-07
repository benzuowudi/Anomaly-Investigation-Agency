using UnityEngine;
using UnityEditor;
using System.Diagnostics;

namespace Framework.Script.Editor
{
    public class Excel2Xml : EditorWindow
    {
        //[MenuItem("Framework/Exporter Exel")]
        public static void ExcelExporter()
        {
            UnityEngine.Debug.Log("方法暂时弃用，后去需要xml读取时重新启用");
            //Process proc = null;
            //try
            //{
            //    string targetDir = string.Format(@"D:\unityproject_x\Tools\ReadData\ReadData\ReadData\bin\Debug\netcoreapp3.1\");
            //    proc = new Process();
            //    proc.StartInfo.WorkingDirectory = targetDir;
            //    proc.StartInfo.FileName = "ReadData.exe";
            //    proc.StartInfo.Arguments = string.Format("10");
            //    proc.Start();
            //    proc.WaitForExit();
            //    UnityEngine.Debug.Log("xml加载完成");
            //}
            //catch
            //{

            //}
        }
    }
}