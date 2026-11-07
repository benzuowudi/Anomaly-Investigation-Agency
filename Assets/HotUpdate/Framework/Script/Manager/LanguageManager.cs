using Framework.Script.Base;
using Framework.Script.Constant;
using cfg.GameType;

namespace Framework.Script.Manager
{
    public class LanguageManager : Singleton<LanguageManager>
    {
        public string GetLanguage(int _id)
        {
            var kData = Game.Data.GetData.language.DataMap;
            string kText;
            switch (GameConst.m_Language)
            {
                case LanguageType.SimplifiedChinese:
                default:
                    kText = kData[_id].ChineseSimplified;
                    break;
                case LanguageType.TraditionalChinese:
                    kText = kData[_id].ChineseTraditional;
                    break;
                case LanguageType.English:
                    kText = kData[_id].English;
                    break;
            }
            return kText;
        }

        public void ChangeLanguage(LanguageType _type)
        {
            GameConst.m_Language = _type;
        }
    }
}
