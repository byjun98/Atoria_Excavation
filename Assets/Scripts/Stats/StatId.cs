namespace Rebuild.Stats
{
    /// <summary>+ (17) = 33 active stat counters.
    /// Indices are persisted directly into <see cref="Rebuild.Saving.GameDataDTO.stats"/>,
    /// so APPEND-ONLY — never reorder. A future enum extension grows DTO arrays via
    /// Mathf.Min migration without breaking older saves.
    /// Original AllStats tracks 47 fields; the remaining 9 (potions / chests / talent-specific
    /// triggers / D100 / bars crafted) come back as their dependent systems land (P2).</summary>
    public enum StatId
    {
        // ---- (0..15) — DO NOT REORDER ----
        // Activity counters
        ClicksTotal = 0,
        RocksDestroyed = 1,
        SessionsCompleted = 2,
        ProjectilesFired = 3,
        FlowersClicked = 4,

        // Progression milestones
        ArtifactsFoundCount = 5,
        PickaxesCraftedCount = 6,
        MaxLevelReached = 7,

        // Per-ore mined total (mirrors OreType order — Gold..Painite)
        OreMined_Gold = 8,
        OreMined_Copper = 9,
        OreMined_Iron = 10,
        OreMined_Cobalt = 11,
        OreMined_Uranium = 12,
        OreMined_Ismium = 13,
        OreMined_Iridium = 14,
        OreMined_Painite = 15,

        // ---- (16..32) — append-only ----
        TotalLevelUps              = 16, // Cumulative level-ups (vs MaxLevelReached which is a peak)
        ExperienceGained           = 17, // Lifetime XP awarded (before doubles)
        TimeSpentMiningSec         = 18, // Seconds counted by the session timer loop
        TotalRocksSpawned          = 19, // Counts RockSpawner.SpawnOne calls (incl. respawns)
        BarsMined_TheMine          = 20, // Idle bars accumulated by MineManager.EmitTick
        FlowersSpawned             = 21, // Flower instances spawned (vs FlowersClicked)
        InstaMineHits              = 22, // Times an insta-mine fired (Axe / InstaMineChance / HammerTime)
        XpDoublesTriggered         = 23, // PurpleRing artifact triggered XP x2
        DoubleDamageHits           = 24, // DoubleDamageChance SkillTree procced
        ProjectilesFired_Beam      = 25, // Per-type breakdown of ProjectilesFired (split, additive)
        ProjectilesFired_Plazma    = 26,
        ProjectilesFired_Dynamite  = 27,
        ProjectilesFired_Lightning = 28,
        OresMined_Total            = 29, // Sum of every AddOre quantity (8 ores collapsed)
        ChestsOpened               = 30, // Chests talent procced (also covers GoldenChests)
        HammerTriggers             = 31, // HammerTime talent procced
        ZeusStrikes                = 32, // Zeus talent fired a beam

        // ---- append-only, 14 new ----
        PotionsDrank               = 33, // PotionDrinker / PotionChugger session activation
        GoldenChestsOpened         = 34, // GoldenChests talent procced (distinct from regular Chests)
        TheMine2XTriggers          = 35, // SkilledMiners talent doubled a Mine tick
        InflateBurstTriggers       = 36, // BigMiningArea inflate-burst (후 hook)
        MidasTouchSessions         = 37, // Sessions where MidasTouch's golden run was rolled
        EnergiDrinksDrank          = 38, // EnergiDrinker boost windows triggered
        CampfiresSpawned           = 39, // Sessions where Camper talent activated
        D100Rolls                  = 40, // D100 stub triggers (reserved — )
        PickaxesSpawned            = 41, // ProjectileLauncher Pickaxe spawns (SkillTree hook)
        PickaxeHits                = 42, // Every Rock.ApplyExternalHit (per-tick stat)
        DoubleOreGained            = 43, // DoubleOreChance procced
        BarsCrafted                = 44, // Anvil craft completions (vs PickaxesCraftedCount which counts pickaxes)
        FullOreMined_Gold          = 45, // Per-ore "full" rock mined (vs chunk — chunk = the basic small rock)
        FullOreMined_Copper        = 46,
    }

    /// <summary>Display-only metadata for the StatsPanel UI. Kept out of the manager so the
    /// runtime stays a thin counter and the strings can swap with localization later.</summary>
    public static class StatDef
    {
        public const int Count = 47;

        // 한국어 라벨.
        public static string GetName(StatId id) => id switch
        {
            StatId.ClicksTotal => "클릭 횟수",
            StatId.RocksDestroyed => "발굴지 채굴",
            StatId.SessionsCompleted => "세션 완료",
            StatId.ProjectilesFired => "발사체 발사",
            StatId.FlowersClicked => "꽃 클릭",
            StatId.ArtifactsFoundCount => "유물 발견",
            StatId.PickaxesCraftedCount => "곡괭이 복원",
            StatId.MaxLevelReached => "최고 레벨",
            StatId.OreMined_Gold => "돌 채굴",
            StatId.OreMined_Copper => "토기조각 채굴",
            StatId.OreMined_Iron => "청동조각 채굴",
            StatId.OreMined_Cobalt => "철편 채굴",
            StatId.OreMined_Uranium => "옥조각 채굴",
            StatId.OreMined_Ismium => "유리구슬 채굴",
            StatId.OreMined_Iridium => "금조각 채굴",
            StatId.OreMined_Painite => "사리 채굴",
            StatId.TotalLevelUps => "총 레벨업",
            StatId.ExperienceGained => "획득 XP",
            StatId.TimeSpentMiningSec => "발굴 시간(초)",
            StatId.TotalRocksSpawned => "발굴지 생성",
            StatId.BarsMined_TheMine => "주괴 (자동 발굴터)",
            StatId.FlowersSpawned => "꽃 생성",
            StatId.InstaMineHits => "즉시 채굴",
            StatId.XpDoublesTriggered => "XP 2배 발동",
            StatId.DoubleDamageHits => "이중 피해 발동",
            StatId.ProjectilesFired_Beam => "광선 발사",
            StatId.ProjectilesFired_Plazma => "플라즈마 발사",
            StatId.ProjectilesFired_Dynamite => "다이너마이트 발사",
            StatId.ProjectilesFired_Lightning => "번개 발사",
            StatId.OresMined_Total => "총 자원 채굴",
            StatId.ChestsOpened => "보물상자 발동",
            StatId.HammerTriggers => "망치 발동",
            StatId.ZeusStrikes => "제우스 발동",
            // 추가 14
            StatId.PotionsDrank => "물약 마심",
            StatId.GoldenChestsOpened => "황금 상자 발동",
            StatId.TheMine2XTriggers => "광부 2배 발동",
            StatId.InflateBurstTriggers => "확장 폭발 발동",
            StatId.MidasTouchSessions => "마이더스 골든런",
            StatId.EnergiDrinksDrank => "에너지 드링크",
            StatId.CampfiresSpawned => "캠퍼 활성",
            StatId.D100Rolls => "주사위 굴림",
            StatId.PickaxesSpawned => "곡괭이 생성",
            StatId.PickaxeHits => "곡괭이 명중",
            StatId.DoubleOreGained => "자원 2배 발동",
            StatId.BarsCrafted => "주괴 제작",
            StatId.FullOreMined_Gold => "큰 돌 채굴",
            StatId.FullOreMined_Copper => "큰 토기 채굴",
            _ => id.ToString()
        };

        public static StatId OreMinedStat(Rebuild.Mining.OreType ore) =>
            (StatId)((int)StatId.OreMined_Gold + (int)ore);
    }
}
