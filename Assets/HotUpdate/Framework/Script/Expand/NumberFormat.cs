using System;
using System.Globalization;
using cfg.GameType;
using Framework.Script.Constant;

namespace Framework.Script.Expand
{
    /// <summary>
    /// 通用数字显示格式（货币、资源、伤害等所有需要"好看的数字"的地方都可以用）。
    ///
    /// 规则：
    ///   · 绝对值 &lt; 10000：不缩写，只在千位加逗号（9,999）；
    ///   · 绝对值 &gt;= 10000：
    ///       中文（简/繁）—— 万 / 亿 / 兆，每 10000 进一级；
    ///       英文        —— k / m / b，每 1000 进一级；
    ///     最多保留两位小数，小数部分直接截断（1.2345 万 → 1.23 万），
    ///     小数末位为 0 时不保留（1.20 万 → 1.2 万，1.00 万 → 1 万）。
    ///
    /// 负数保留符号（-1.24 万）；0 显示 "0"。
    /// </summary>
    public static class NumberFormat
    {
        /// <summary>中文进位基数（万）</summary>
        private const decimal c_ChineseBase = 10000m;

        /// <summary>英文进位基数（k）</summary>
        private const decimal c_EnglishBase = 1000m;

        /// <summary>保留两位小数时用的放大倍数</summary>
        private const decimal c_RoundScale = 100m;

        private static readonly string[] c_ChineseUnits = { "", "万", "亿", "兆" };
        private static readonly string[] c_EnglishUnits = { "", "k", "m", "b" };

        /// <summary>
        /// 按当前语言格式化。
        /// </summary>
        public static string Format(long _value)
        {
            return Format(_value, GameConst.m_Language);
        }

        /// <summary>
        /// 按指定语言格式化（方便测试或需要固定语言的场景）。
        /// </summary>
        public static string Format(long _value, LanguageType _language)
        {
            bool kEnglish = _language == LanguageType.English;
            decimal kBase = kEnglish ? c_EnglishBase : c_ChineseBase;
            string[] kUnits = kEnglish ? c_EnglishUnits : c_ChineseUnits;

            decimal kAbs = Math.Abs((decimal)_value);

            // 小于 1 万：不缩写，只加千分位
            if (kAbs < 10000m)
            {
                return _value.ToString("N0", CultureInfo.InvariantCulture);
            }

            // 先找到合适的单位（如 12,345 → 1.2345 万 / 12.345 k）
            int kUnitIndex = 0;
            decimal kScaled = kAbs;
            while (kScaled >= kBase && kUnitIndex < kUnits.Length - 1)
            {
                kScaled /= kBase;
                kUnitIndex++;
            }

            // 直接截断到两位小数（不四舍五入、不向上取整）
            decimal kTruncated = decimal.Truncate(kScaled * c_RoundScale) / c_RoundScale;

            string kNumber = kTruncated.ToString("0.##", CultureInfo.InvariantCulture);
            string kSign = _value < 0 ? "-" : string.Empty;
            return kSign + kNumber + kUnits[kUnitIndex];
        }
    }
}
