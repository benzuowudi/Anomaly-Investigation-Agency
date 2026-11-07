using UnityEngine;
using UnityEditor;
using System.Diagnostics;
using System;

namespace Framework.Script.Editor
{
    public class LuBanTool : EditorWindow
    {
        [MenuItem("Framework/≈‰÷√±Ìµº»Î")]
        public static void ExcelExporter()
        {
            Process proc = null;
            try
            {
                string targetDir = Environment.CurrentDirectory + @"\Tools\LuBan\";
                proc = new Process();
                proc.StartInfo.WorkingDirectory = targetDir;
                proc.StartInfo.FileName = "gen_code_bin.bat";
                proc.StartInfo.Arguments = string.Format("10");
                proc.Start();
                proc.WaitForExit();
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.Log(ex);
            }
        }
    }
}