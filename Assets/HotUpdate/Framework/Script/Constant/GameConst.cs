using UnityEngine;
using cfg.GameType;

namespace Framework.Script.Constant
{
    public class GameConst
    {
        public static bool m_IsDev = false;
        public static bool m_GameStart = false;
        /// <summary>
        /// JSON路径
        /// </summary>
        public readonly static string m_DataJsonPath = Application.streamingAssetsPath + "/Data/{0}.json";
        /// <summary>
        /// 持久化数据路径
        /// </summary>
        public readonly static string m_PersistenceDataPath = Application.streamingAssetsPath + "/Data/{0}";
        /// <summary>
        /// 六边形瓦片边长的一半
        /// </summary>
        public readonly static float m_MapTileSize = 15f;
        /// <summary>
        /// 多语言标识，默认为简体中文
        /// </summary>
        public static LanguageType m_Language = LanguageType.SimplifiedChinese;
    }
}