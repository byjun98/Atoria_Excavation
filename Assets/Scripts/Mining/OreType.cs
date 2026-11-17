namespace Rebuild.Mining
{




    public enum OreType
    {
        Gold = 0,
        Copper = 1,
        Iron = 2,
        Cobalt = 3,
        Uranium = 4,
        Ismium = 5,
        Iridium = 6,
        Painite = 7,
    }




    public static class OreLocalization
    {
        static readonly string[] s_namesKr =
        {
            "돌",
            "토기조각",
            "청동조각",
            "철편",
            "옥조각",
            "유리구슬",
            "금조각",
            "사리",
        };

        public static string OreNameKr(OreType ore)
        {
            int idx = (int)ore;
            return (idx >= 0 && idx < s_namesKr.Length) ? s_namesKr[idx] : ore.ToString();
        }

        public static System.Collections.Generic.IReadOnlyList<string> AllNamesKr => s_namesKr;
    }
}
