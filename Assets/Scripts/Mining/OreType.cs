namespace Rebuild.Mining
{
    /// <summary>
    /// 8-ore enum used across the mining system (rocks, popups, HUD, skill tree gates).
    /// Order matches the original SampleScene exactly so save data and skill tree indices stay in sync.
    /// </summary>
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

    /// <summary>아토리아 발굴단 localization. Enum names stay English (save/DTO
    /// compatibility), but every UI label routes through <see cref="OreNameKr(OreType)"/>.
    /// 8 ore 매핑은 돌→사리 계층 (사용자 확정 2026-05-13).</summary>
    public static class OreLocalization
    {
        static readonly string[] s_namesKr =
        {
            "돌",       // Gold
            "토기조각", // Copper
            "청동조각", // Iron
            "철편",     // Cobalt
            "옥조각",   // Uranium
            "유리구슬", // Ismium
            "금조각",   // Iridium
            "사리",     // Painite
        };

        public static string OreNameKr(OreType ore)
        {
            int idx = (int)ore;
            return (idx >= 0 && idx < s_namesKr.Length) ? s_namesKr[idx] : ore.ToString();
        }

        public static System.Collections.Generic.IReadOnlyList<string> AllNamesKr => s_namesKr;
    }
}
