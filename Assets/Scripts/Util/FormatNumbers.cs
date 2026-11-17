using System.Globalization;

namespace Rebuild.Util
{







    public static class FormatNumbers
    {

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
            if (double.IsNaN(value) || double.IsInfinity(value)) return "-";
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



        static string Trim(double v)
        {
            if (v >= 100d) return ((long)v).ToString(CultureInfo.InvariantCulture);
            if (v >= 10d) return ((long)v).ToString(CultureInfo.InvariantCulture);

            string s = v.ToString("F1", CultureInfo.InvariantCulture);
            if (s.EndsWith(".0")) s = s.Substring(0, s.Length - 2);
            return s;
        }
    }
}
