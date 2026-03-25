using System.Globalization;

namespace Rebuild.Util
{
    /// <summary>큰 숫자 (KMBT) 표시 헬퍼. 원본 FormatNumbers.cs (50 magnitude buckets,
    /// 1E+3~1E+90) 를 game 규모에 맞게 K/M/B/T 4 단계로 축약. 100k 이상부터 자동 축약.
    /// <para>
    /// 예: 1234 → "1,234", 12345 → "12,345", 123456 → "123K", 1234567 → "1.2M",
    /// 12345678 → "12M", 1234567890 → "1.2B", 1234567890123 → "1.2T".
    /// </para>
    /// 1k~99k 는 culture comma 그대로 표시 (1,234) — 너무 일찍 축약하면 가독성 떨어짐.</summary>
    public static class FormatNumbers
    {
        // 100k 미만이면 그대로 (또는 thousand separator). 그 이상은 축약.
        const long kThresholdK = 100_000L;
        const long kThresholdM = 1_000_000L;
        const long kThresholdB = 1_000_000_000L;
        const long kThresholdT = 1_000_000_000_000L;

        public static string Format(long value)
        {
            if (value < 0L) return "-" + FormatPositive(-value);
            return FormatPositive(value);
        }

        public static string Format(int value) => Format((long)value);

        public static string Format(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value)) return "—";
            if (value < 0d) return "-" + FormatPositive((long)System.Math.Round(-value));
            return FormatPositive((long)System.Math.Round(value));
        }

        static string FormatPositive(long v)
        {
            if (v < kThresholdK) return v.ToString("N0", CultureInfo.InvariantCulture);
            if (v < kThresholdM) return Trim((double)v / 1_000d) + "K";
            if (v < kThresholdB) return Trim((double)v / 1_000_000d) + "M";
            if (v < kThresholdT) return Trim((double)v / 1_000_000_000d) + "B";
            return Trim((double)v / 1_000_000_000_000d) + "T";
        }

        /// <summary>10 이상이면 소숫점 X (예: "12M"), 10 미만이면 소수 1자리 (예: "1.2M").
        /// 100 이상도 정수 (예: "123K"). 너무 긴 소수 자릿수는 시각적 노이즈만 추가.</summary>
        static string Trim(double v)
        {
            if (v >= 100d) return ((long)v).ToString(CultureInfo.InvariantCulture);
            if (v >= 10d) return ((long)v).ToString(CultureInfo.InvariantCulture);
            // 한 자릿수: "1.2" 형식. 끝의 .0 제거.
            string s = v.ToString("F1", CultureInfo.InvariantCulture);
            if (s.EndsWith(".0")) s = s.Substring(0, s.Length - 2);
            return s;
        }
    }
}
