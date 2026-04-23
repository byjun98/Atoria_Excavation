namespace Rebuild.Mining
{
    /// <summary>SkillTree upgrade roster.
    /// Core economy upgrades cover more time, more rocks, more damage, more XP,
    /// material value, and double reward chance. Projectile gates unlock the four
    /// projectile types, then later entries add tick-time chances, ore-weight
    /// nudges, respawn / random splash, projectile stat modifiers, crafting, ore
    /// progression, ending eligibility, talent-point rewards, and spawn variety.
    ///
    /// <para><b>원본 스케일 매칭.</b> `SkillTree.Upgrades.cs` 의 _basePrices /
    /// _baseMaxLevels 그대로 매칭. 추가:
    /// • <b>BiggerMiningArea (12)</b> — 적용 site (MiningCursorUI) 추가 (이전엔 dead). 효과 = `1 + lv*0.02`.
    /// • <b>FinalUpgrade (59)</b> — TheEndingManager 의 AND 조건 추가.
    /// • <b>DynamiteChance/LightningChance (8/9)</b> — type 별 chance per level 분리 (ProjectileLauncher).
    /// • <b>PlazmaLifetime (26)</b> — `+0.1s/lv` → `+1s/lv` 원본 매칭.
    /// • <b>ShootCircleChance (64)</b> — per-click 1% → per-second 30% auto coroutine.
    /// • <b>ChanceAdd1SecondEveryRockMined/EverySecond (67/68)</b> — 100% → 0.075% / 9% 원본 확률.
    /// • <b>IncreaseSpawnChanceAllRocks (66)</b> — fixed +1 → `+ceil(maxRocks*0.5*lv)`.
    /// • <b>DoubleDamageChance/InstaMineChance (10/11)</b> — 8%/1.5% → 1%/1% 원본 매칭 + maxLevel 2.
    /// </para></summary>
    public enum UpgradeId
    {
        MoreTime             = 0, // mineSessionTime +1s per level
        SpawnMoreRocks       = 1, // rocks/row min+max +1 per level
        ImprovedPickaxe      = 2, // click damage +1 per level
        MoreXP               = 3, // XP per rock +1 per level (multiplied by talent MoreXP)
        MaterialsWorthMore   = 4, // ore drop per rock +1 per level (multiplier matches original materialsWorthIncrease 1/2/3/9/12/4/6/18)
        DoubleXpGoldChance   = 5, // chance lv*0.10 to double both XP and ore on the rock

        // each click rolls these independently. Per-type chance now matches original
        // sub-percent design (DynamiteStickChance=0.1f/0.2f, LightningChancePH=0.07f/0.08f).
        BeamChance           = 6,  // BeamS = 10%/lv (per-sec original, 매칭 유지)
        PlazmaChance         = 7,  // 4%/lv (per-spawn original plazmaBallChance 4/5)
        DynamiteChance       = 8,  // 0.1%/lv (per-hit, 원본 dynamiteStickChance 0.1/0.2)
        LightningChance      = 9,  // 0.07%/lv (per-hit, 원본 lightningTriggerChancePH 0.07/0.08)

        // ---- SkillTree (10..19) ----
        DoubleDamageChance   = 10, // Rock.ApplyOneTick: lv*1% to double the tick damage (원본 doubleDamageChance 1f/2f)
        InstaMineChance      = 11, // Rock.ApplyOneTick: lv*1% to insta-mine the rock (원본 instaMineChance 1f/1f, max 2%)
        BiggerMiningArea     = 12, // MiningCursorUI: area × (1 + lv*0.02). 원본 miningAreaIncrease=0.02f × 4lv.
        RespawnRockChance    = 13, // Rock.FadeAndDestroy: lv*5% to immediately respawn a rock in the same row
        RockBonusTimeChance  = 14, // Rock.FadeAndDestroy: lv*4% to add +1s to the session timer
        TickBonusTimeChance  = 15, // MiningSessionController.RunTimer: lv*3%/sec to add +1s
        GoldBoost            = 16, // RockSpawner.RollOre: Gold weight +5/lv
        CopperBoost          = 17, // RockSpawner.RollOre: Copper weight +3/lv
        IronBoost            = 18, // RockSpawner.RollOre: Iron weight +2/lv
        RandomRockMineChance = 19, // MiningCursorUI: lv*per-tick chance to hit a random off-ring rock (원본 1/1/2/3 % 누적)

        // ---- Projectile (20..29) ----
        BeamDamage           = 20, // ProjectileFx Beam: Damage +1/lv (원본 lightningDamageIncrease=0.2 × 1 lv)
        BeamSize             = 21, // ProjectileFx Beam: MaxTargets +1/lv (원본 lightningSizeIncrease=0.25 × 1 lv)
        DynamiteDamage       = 22, // ProjectileFx Dynamite: Damage +2/lv (원본 explosionDamagageIncrease=0.25 × 1 lv)
        DynamiteSize         = 23, // ProjectileFx Dynamite: MaxTargets +1/lv (원본 explosionSizeIncrease=0.2 × 1 lv)
        PlazmaDamage         = 24, // ProjectileFx Plazma: Damage +2/lv (원본 plazmaBallTotalDamageIncrease=0.2 × 1 lv)
        PlazmaSize           = 25, // ProjectileFx Plazma: MaxTargets +1/lv (원본 plazmaBallTotalSizeIncrease=0.2 × 1 lv)
        PlazmaLifetime       = 26, // ProjectileFx Plazma: Lifetime +1s/lv (원본 plazmaBallTimerIncrease=1f × 1 lv)
        LightningSize        = 27, // ProjectileFx ElectricitySplash: MaxTargets +1/lv (원본 lightningSizeIncrease 의 cascade)
        AllProjectileDouble  = 28, // ProjectileFx.ApplyDamage: 5% to double every projectile hit (원본 allProjectileDoubleDamageIncrease=5f × 1 lv)
        AllProjectileTrigger = 29, // ProjectileLauncher.TrySpawnFromClick: additive +15% (원본 allProjcetileTriggerChance=15f × 1 lv)

        // ---- Projectile cascade (30..36). 원본 single-level, rebuild도 1 lv 으로 매칭.
        //      10% per level chance, capped at depth 2 inside ProjectileFx so chains can't recurse forever.
        CascadeBeamToDyna    = 30, // Beam dies -> spawn a Dynamite at the source rock (원본 lightningSpawnExplosionChance=20%)
        CascadeBeamToBeam    = 31, // Beam dies -> fire another Beam (원본 triggerAnotherLighntingChance=12%)
        CascadeBeamToSplash  = 32, // Beam dies -> spawn an ElectricitySplash (원본 lightningSplashChance=17%)
        CascadeBeamTime      = 33, // Beam dies -> add +1s to the session timer (원본 lightningAddTimeChance=0.8%)
        CascadeDynaToBeam    = 34, // Dynamite dies -> spawn a Beam (원본 explosionChanceToSpawnLightning=10%)
        CascadeDynaToDyna    = 35, // Dynamite dies -> spawn another Dynamite (원본 spawn2DynamiteChance=10%)
        CascadePlazmaToDyna  = 36, // Plazma dies -> spawn a Dynamite (원본 plazmaballExplosionChance=17%)

        // ---- Crafting ----
        Craft2Material       = 37, // Session-end ore->bar conversion ratio: 3:1 default -> 2:1 when purchased

        // ============================================================================
        //  8-ore progression (Spawn / Chunk / Full).
        //  Soft-buff model: Spawn lv1 = +25 weight, Chunk lv = +1 ore drop per rock of that ore,
        //  Full lv = +1 ore drop per rock (stacks with Chunk). All gated by SkillTreeLayout
        //  prereq chain (Spawn → Chunk → Full inside its own column).
        //  Cobalt/Uranium/Ismium/Iridium/Painite already weighted in RockSpawner.OreWeights;
        //  Spawn node multiplies. Copper/Iron have their own progression chain matching
        //  the original's tiered unlocking.
        // ============================================================================
        CopperSpawn          = 38, // Copper weight +25 (top of Copper chain)
        CopperChunk          = 39, // +1 ore per Copper rock per level (3 levels — 원본 copperRockChanceIncrease 0.3/0.3/0.4)
        CopperFull           = 40, // +1 ore per Copper rock per level (3 levels — 원본 fullCopperRockChanceIncrease 0.2/0.2/0.3)
        IronSpawn            = 41, // Iron weight +25
        IronChunk            = 42, // +1 ore per Iron rock per level (2 levels — 원본 ironRockChanceIncrease 0.2/0.3)
        IronFull             = 43, // +1 ore per Iron rock per level (2 levels — 원본 fullIronRockChanceIncrease 0.2/0.2)
        CobaltSpawn          = 44, // Cobalt weight +25
        CobaltChunk          = 45, // +1 ore per Cobalt rock
        CobaltFull           = 46, // +1 ore per Cobalt rock
        UraniumSpawn         = 47, // Uranium weight +25
        UraniumChunk         = 48, // +1 ore per Uranium rock
        UraniumFull          = 49, // +1 ore per Uranium rock
        IsmiumSpawn          = 50, // Ismium weight +25
        IsmiumChunk          = 51, // +1 ore per Ismium rock
        IsmiumFull           = 52, // +1 ore per Ismium rock
        IridiumSpawn         = 53, // Iridium weight +25
        IridiumChunk         = 54, // +1 ore per Iridium rock
        IridiumFull          = 55, // +1 ore per Iridium rock
        PainiteSpawn         = 56, // Painite weight +25
        PainiteChunk         = 57, // +1 ore per Painite rock
        PainiteFull          = 58, // +1 ore per Painite rock

        // ============================================================================
        //  Final / Talent (2 IDs).
        // ============================================================================
        FinalUpgrade         = 59, // Endgame flag — ending eligibility AND gate (TheEndingManager checks this)
        TalentPointsPerXlevel= 60, // LV X 마다 talent +1 (lv1=15 lv2=10 lv3=5 — X 감소). Stacks with Book artifact (every 5).

        // ============================================================================
        //  Tick / Spawn variety (11 IDs). Original style scattered bonuses.
        // ============================================================================
        ChanceToSpawnRockWhenMined  = 61, // Rock.FadeAndDestroy: 누적 % spawn a fresh rock anywhere in row (원본 spawnWhenMinedIncrease 2/3/3/4/5/4)
        SpawnXRockEveryXSecond      = 62, // base spawn loop period. lv0=2.0s default, lv1=1.3s, lv2=0.6s, lv3=0.2s (3 levels).
        SpawnPickaxeEverySecond     = 63, // Auto-click coroutine: every (1.2 - lv*decrement)s auto-tick a random rock (원본 1.2s base, 0.2/0.3/0.3 누적)
        ShootCircleChance           = 64, // Auto-coroutine: 30%/sec small circle hit (원본 circleShootChance=30f)
        IncreaseAndDecreaseMinignErea= 65, // BiggerMiningArea +1 base (toggle bonus) — multiplier on cursor area
        IncreaseSpawnChanceAllRocks = 66, // RockSpawner.SpawnAll: rocks/row +Mathf.Ceil(maxRocks*0.5*lv) (원본 단일이지만 lv 곱셈 의도)
        ChanceAdd1SecondEveryRockMined = 67, // Rock.FadeAndDestroy: 0.075% chance per rock to add +1s (원본 chanceToAdd1SecEveryRockMined=0.075f)
        ChanceToAdd1SecondEverySecond  = 68, // MiningSessionController.RunTimer: 9%/sec chance to add +1s (원본 chanceToAdd1SecEverySec=9f)
        FullOreChance               = 69, // Rock.FadeAndDestroy: lv*5% to triple the rock's ore drop (all ore types)
        SpawnRockEveryXrock         = 70, // After every (6-lv) rocks mined, immediate +1 row rock (max lv = every 3)
        MoreMaterialsFromRock       = 71, // Adds (lv) to ore drop per rock (stacks with MaterialsWorthMore — 5 levels for richer late-game)

        // ============================================================================
        //  잔여 cascade 4종 + PlazmaSpawnPickaxe 누락 fix.
        //  원본도 single-level, rebuild 도 1 lv 으로 매칭.
        // ============================================================================
        CascadePlazmaToPlasmaBall   = 72, // Plazma dies -> spawn another Plazma (원본 plazmaBallSpawnSmallBallChance=13%)
        CascadeDynaToSplash         = 73, // Dynamite dies -> spawn an ElectricitySplash (원본 비대응, Shell 확장)
        CascadePlazmaToSplash       = 74, // Plazma dies -> spawn an ElectricitySplash (원본 비대응, Shell 확장)
        CascadeLightningToDyna      = 75, // ElectricitySplash dies -> spawn a Dynamite (원본 비대응, Shell 확장)
        PlazmaSpawnPickaxe          = 76, // Plazma dies -> auto-tick 3 random rocks (원본 plazmaBallChanceToSpawnPickaxe=4%)
    }

    public static class UpgradeDef
    {
        public const int Count = 77;

        public readonly struct Info
        {
            public readonly UpgradeId Id;
            public readonly string Name;
            public readonly string Description;
            public readonly OreType Currency;
            public readonly int[] PerLevelPrices;
            public Info(UpgradeId id, string name, string desc, OreType currency, int[] prices)
            { Id = id; Name = name; Description = desc; Currency = currency; PerLevelPrices = prices; }
            public int MaxLevel => PerLevelPrices.Length;
        }

        // 원본 SkillTree.Upgrades.cs._basePrices / _baseMaxLevels 그대로 매칭.
        // (cheaper / fewer levels) 모두 되돌림. Currency 는 후반 노드일수록 상위 광물.
        static readonly Info[] s_infos =
        {
            // ---- core (0..5) ----
            new Info(UpgradeId.MoreTime,           "여유 시간",         "세션 시간\n+1초 / 레벨.",                       OreType.Gold,
                new[] { 9, 100, 1250, 70000 }),
            new Info(UpgradeId.SpawnMoreRocks,     "발굴지 증가",       "행마다 발굴지\n+1 추가.",                       OreType.Gold,
                new[] { 3, 6, 15, 25, 350, 3800, 27000, 270000, 350000 }),
            new Info(UpgradeId.ImprovedPickaxe,    "곡괭이 강화",       "클릭마다 피해\n+1.",                            OreType.Gold,
                new[] { 11, 300, 400, 15000, 50000, 100000 }),
            new Info(UpgradeId.MoreXP,             "경험치 증가",       "발굴마다 경험치\n+1.",                          OreType.Copper,
                new[] { 12, 125, 21000, 600, 2100, 4500, 100000, 1000000 }),
            // 원본 materialsTotalWorth multiplicative 모델 복원. 누적값 = (1+2+3+9+12+4+6+18) = +56.
            // base 1f + 누적 = lv1 ×2 / lv4 ×16 / lv8 ×57 worth multiplier on ore drop count.
            new Info(UpgradeId.MaterialsWorthMore, "자원 가치",         "발굴지에서 얻는\n자원 가치 증가.",              OreType.Gold,
                new[] { 100, 2000, 3000, 44000, 110000, 5500, 300000, 9000000 }),
            new Info(UpgradeId.DoubleXpGoldChance, "XP & 자원 2배",     "발굴마다 확률\n(+10%/lv) 으로\nXP와 자원 2배.", OreType.Copper,
                new[] { 17, 10000, 2900, 300000, 1000000 }),

            // ---- projectile-chance gates (6..9). 원본 maxLevel 2 + base prices. ----
            new Info(UpgradeId.BeamChance,         "빛의 광선",         "클릭마다 확률\n으로\n광선 발동.",               OreType.Copper,
                new[] { 500, 1600 }),
            new Info(UpgradeId.PlazmaChance,       "플라즈마 구체",     "클릭마다 확률\n으로\n플라즈마 발동.",           OreType.Iron,
                new[] { 10000, 22000 }),
            new Info(UpgradeId.DynamiteChance,     "다이너마이트",      "클릭마다 확률\n으로\n폭발 발동.",               OreType.Iron,
                new[] { 1000, 3500 }),
            new Info(UpgradeId.LightningChance,    "번개 강타",         "클릭마다 확률\n으로\n번개 발동.",               OreType.Copper,
                new[] { 200, 9000 }),

            // ---- SkillTree  (10..19) ----
            new Info(UpgradeId.DoubleDamageChance, "이중 피해",         "발굴마다 확률\n(+1%/lv) 으로\n피해 2배.",       OreType.Iron,
                new[] { 56, 2000000 }),
            new Info(UpgradeId.InstaMineChance,    "즉시 채굴",         "발굴마다 확률\n(+1%/lv) 으로\n즉시 채굴.",      OreType.Iron,
                new[] { 80, 15000 }),
            new Info(UpgradeId.BiggerMiningArea,   "넓은 발굴 영역",    "발굴 영역\n+2% / 레벨.",                        OreType.Gold,
                new[] { 8, 1000, 230, 1000000 }),
            new Info(UpgradeId.RespawnRockChance,  "발굴지 재생성",     "채굴마다 확률\n(+5%/lv) 으로\n같은 행 재생성.", OreType.Gold,
                new[] { 20, 80, 350 }),
            new Info(UpgradeId.RockBonusTimeChance,"채굴마다 +시간",    "채굴마다 확률\n(+4%/lv) 으로\n세션 +1초.",      OreType.Gold,
                new[] { 18, 70, 300 }),
            new Info(UpgradeId.TickBonusTimeChance,"초마다 +시간",      "1초마다 확률\n(+3%/lv) 으로\n세션 +1초.",       OreType.Copper,
                new[] { 22, 90, 350 }),
            new Info(UpgradeId.GoldBoost,          "돌 증가",           "돌 출현 가중치\n+5 / 레벨.",                    OreType.Gold,
                new[] { 12, 50, 200 }),
            new Info(UpgradeId.CopperBoost,        "토기조각 증가",     "토기조각 출현\n가중치 +3 / 레벨.",              OreType.Gold,
                new[] { 16, 60, 220 }),
            new Info(UpgradeId.IronBoost,          "청동조각 증가",     "청동조각 출현\n가중치 +2 / 레벨.",              OreType.Copper,
                new[] { 24, 90, 320 }),
            new Info(UpgradeId.RandomRockMineChance,"무작위 채굴",      "발굴마다 확률\n(+1~3%/lv) 으로\n무작위 +1 피해.",OreType.Iron,
                new[] { 50, 350, 1000, 33000 }),

            // ---- Projectile  (20..29). 원본 single-level. ----
            new Info(UpgradeId.BeamDamage,         "광선 피해",         "빛의 광선 피해\n+1.",                           OreType.Gold,
                new[] { 1000 }),
            new Info(UpgradeId.BeamSize,           "광선 범위",         "빛의 광선 추가\n타격 +1.",                      OreType.Gold,
                new[] { 1200 }),
            new Info(UpgradeId.DynamiteDamage,     "폭발 피해",         "다이너마이트 피해\n+2.",                        OreType.Gold,
                new[] { 10000 }),
            new Info(UpgradeId.DynamiteSize,       "폭발 범위",         "다이너마이트 추가\n타격 +1.",                   OreType.Gold,
                new[] { 32000 }),
            new Info(UpgradeId.PlazmaDamage,       "플라즈마 피해",     "플라즈마 피해\n+2.",                            OreType.Gold,
                new[] { 4600000 }),
            new Info(UpgradeId.PlazmaSize,         "플라즈마 범위",     "플라즈마 추가 타격\n+1.",                       OreType.Iron,
                new[] { 30000 }),
            new Info(UpgradeId.PlazmaLifetime,     "플라즈마 지속",     "플라즈마 지속시간\n+1초.",                      OreType.Iron,
                new[] { 430000 }),
            new Info(UpgradeId.LightningSize,      "번개 범위",         "번개 추가 타격\n+1.",                           OreType.Iron,
                new[] { 20000 }),
            new Info(UpgradeId.AllProjectileDouble,"발사체 2배",        "타격마다 확률\n5% 로\n발사체 피해 2배.",        OreType.Gold,
                new[] { 1000000 }),
            new Info(UpgradeId.AllProjectileTrigger,"발사체 오라",      "모든 발사체 확률\n+15% (additive).",            OreType.Gold,
                new[] { 700000 }),

            // ---- Projectile cascade (30..36). 원본 single-level. ----
            new Info(UpgradeId.CascadeBeamToDyna,  "광선 -> 폭발",      "광선 종료 시\n+10% 폭발.",                      OreType.Gold,
                new[] { 250000 }),
            new Info(UpgradeId.CascadeBeamToBeam,  "광선 -> 광선",      "광선 종료 시\n+10% 추가 광선\n(깊이 제한).",    OreType.Copper,
                new[] { 95000 }),
            new Info(UpgradeId.CascadeBeamToSplash,"광선 -> 번개",      "광선 종료 시\n+10% 번개\n분기.",                OreType.Iron,
                new[] { 20000 }),
            new Info(UpgradeId.CascadeBeamTime,    "광선 -> +시간",     "광선 종료 시\n0.8% 으로\n세션 +1초.",           OreType.Gold,
                new[] { 500000 }),
            new Info(UpgradeId.CascadeDynaToBeam,  "폭발 -> 광선",      "폭발 종료 시\n+10% 광선\n(깊이 제한).",         OreType.Copper,
                new[] { 600000 }),
            new Info(UpgradeId.CascadeDynaToDyna,  "폭발 -> 폭발",      "폭발 종료 시\n+10% 추가 폭발\n(깊이 제한).",    OreType.Iron,
                new[] { 2500 }),
            new Info(UpgradeId.CascadePlazmaToDyna,"플라즈마 -> 폭발",  "플라즈마 종료 시\n+10% 폭발.",                  OreType.Iron,
                new[] { 610000 }),

            // ---- 원본 24,000,000 Gold (1 lv) ----
            new Info(UpgradeId.Craft2Material,     "효율 복원",         "세션 원광석 -> 주괴\n비율 3:1 -> 2:1\n(33% 증산).", OreType.Gold,
                new[] { 24000000 }),

            // ============================================================================
            //  8-ore progression (38..58). 21 nodes. 원본 base prices.
            // ============================================================================
            new Info(UpgradeId.CopperSpawn,        "토기조각 출현",     "토기조각 가중치\n+25.",                          OreType.Gold,
                new[] { 100 }),
            new Info(UpgradeId.CopperChunk,        "토기조각 덩어리",   "토기조각 발굴마다\n+1 / 레벨.",                  OreType.Copper,
                new[] { 70, 2100, 15000 }),
            new Info(UpgradeId.CopperFull,         "토기조각 풍요",     "토기조각 발굴마다\n+1 / 레벨 (누적).",           OreType.Copper,
                new[] { 90, 5500, 460000 }),
            new Info(UpgradeId.IronSpawn,          "청동조각 출현",     "청동조각 가중치\n+25.",                          OreType.Copper,
                new[] { 400 }),
            new Info(UpgradeId.IronChunk,          "청동조각 덩어리",   "청동조각 발굴마다\n+1 / 레벨.",                  OreType.Iron,
                new[] { 500, 500000 }),
            new Info(UpgradeId.IronFull,           "청동조각 풍요",     "청동조각 발굴마다\n+1 / 레벨 (누적).",           OreType.Iron,
                new[] { 920, 1000000 }),
            new Info(UpgradeId.CobaltSpawn,        "철편 출현",         "철편 가중치\n+25.",                              OreType.Iron,
                new[] { 2350 }),
            new Info(UpgradeId.CobaltChunk,        "철편 덩어리",       "철편 발굴마다\n+1.",                             OreType.Cobalt,
                new[] { 3000 }),
            new Info(UpgradeId.CobaltFull,         "철편 풍요",         "철편 발굴마다\n+1 (누적).",                      OreType.Cobalt,
                new[] { 3600 }),
            new Info(UpgradeId.UraniumSpawn,       "옥조각 출현",       "옥조각 가중치\n+25.",                            OreType.Cobalt,
                new[] { 4000 }),
            new Info(UpgradeId.UraniumChunk,       "옥조각 덩어리",     "옥조각 발굴마다\n+1.",                           OreType.Uranium,
                new[] { 2600 }),
            new Info(UpgradeId.UraniumFull,        "옥조각 풍요",       "옥조각 발굴마다\n+1 (누적).",                    OreType.Uranium,
                new[] { 3300 }),
            new Info(UpgradeId.IsmiumSpawn,        "유리구슬 출현",     "유리구슬 가중치\n+25.",                          OreType.Uranium,
                new[] { 140000 }),
            new Info(UpgradeId.IsmiumChunk,        "유리구슬 덩어리",   "유리구슬 발굴마다\n+1.",                         OreType.Ismium,
                new[] { 19000 }),
            new Info(UpgradeId.IsmiumFull,         "유리구슬 풍요",     "유리구슬 발굴마다\n+1 (누적).",                  OreType.Ismium,
                new[] { 23000 }),
            new Info(UpgradeId.IridiumSpawn,       "금조각 출현",       "금조각 가중치\n+25.",                            OreType.Ismium,
                new[] { 75000 }),
            new Info(UpgradeId.IridiumChunk,       "금조각 덩어리",     "금조각 발굴마다\n+1.",                           OreType.Iridium,
                new[] { 25000 }),
            new Info(UpgradeId.IridiumFull,        "금조각 풍요",       "금조각 발굴마다\n+1 (누적).",                    OreType.Iridium,
                new[] { 33000 }),
            new Info(UpgradeId.PainiteSpawn,       "사리 출현",         "사리 가중치\n+25.",                              OreType.Iridium,
                new[] { 1000000 }),
            new Info(UpgradeId.PainiteChunk,       "사리 덩어리",       "사리 발굴마다\n+1.",                             OreType.Painite,
                new[] { 160000 }),
            new Info(UpgradeId.PainiteFull,        "사리 풍요",         "사리 발굴마다\n+1 (누적).",                      OreType.Painite,
                new[] { 240000 }),

            // ============================================================================
            //  Final / Talent (59..60). 2 nodes.
            // ============================================================================
            new Info(UpgradeId.FinalUpgrade,       "최종 복원",         "엔딩 자격 활성화\n(곡괭이 14 + 유물 14 +\n이 노드 구매).", OreType.Painite,
                new[] { 1000000 }),
            new Info(UpgradeId.TalentPointsPerXlevel,"수련 가속",       "특정 레벨마다\n특성 +1 보너스\n(15 → 10 → 5 레벨).", OreType.Gold,
                new[] { 100, 1200, 1500000 }),

            // ============================================================================
            //  Tick / Spawn variety (61..71). 11 nodes.
            // ============================================================================
            new Info(UpgradeId.ChanceToSpawnRockWhenMined, "발굴지 추가 생성", "채굴마다 확률\n(2/3/3/4/5/4 % 누적) 으로\n행 빈자리에 추가.", OreType.Gold,
                new[] { 18, 1000, 27000, 2600, 250, 13000 }),
            new Info(UpgradeId.SpawnXRockEveryXSecond, "정기 출현 가속", "기본 2초마다\n발굴지 1개 출현 →\n레벨업 시 -0.7s/-0.7s/-0.4s.", OreType.Iron,
                new[] { 75, 420, 500000 }),
            new Info(UpgradeId.SpawnPickaxeEverySecond,"자동 곡괭이",   "(1.2 - 누적)초마다\n무작위 발굴지에\n자동 타격.", OreType.Gold,
                new[] { 4000, 8000, 6000 }),
            new Info(UpgradeId.ShootCircleChance,  "원형 파동",         "매초 30% 확률\n으로 작은 원 범위에\n자동 타격.", OreType.Copper,
                new[] { 125 }),
            new Info(UpgradeId.IncreaseAndDecreaseMinignErea,"발굴 영역 확장","발굴 영역\n+25% 토글.", OreType.Gold,
                new[] { 5000 }),
            new Info(UpgradeId.IncreaseSpawnChanceAllRocks,"발굴지 폭증","전 행 발굴지\n+ceil(maxRocks×50%×lv).",  OreType.Gold,
                new[] { 400000 }),
            new Info(UpgradeId.ChanceAdd1SecondEveryRockMined,"채굴 보너스 +초","채굴마다 0.075% 확률\n로 세션 +1초.", OreType.Gold,
                new[] { 9000 }),
            new Info(UpgradeId.ChanceToAdd1SecondEverySecond,"매초 보너스 +초","매초 9% 확률\n로 세션 +1초.", OreType.Iron,
                new[] { 800 }),
            new Info(UpgradeId.FullOreChance,      "풍요 채굴",         "채굴마다 확률\n(+5%/lv) 으로\n자원 ×3.", OreType.Gold,
                new[] { 50, 350, 1000, 33000 }),
            new Info(UpgradeId.SpawnRockEveryXrock,"누적 보너스",       "(6-lv) 발굴마다\n행 1개에 보너스 발굴지.", OreType.Copper,
                new[] { 500, 3000, 340000 }),
            new Info(UpgradeId.MoreMaterialsFromRock,"자원 대폭 증산",  "발굴마다 +1\n자원 / 레벨\n(자원 증산과 누적).", OreType.Gold,
                new[] { 350, 500000, 400000, 10000, 700000 }),

            // ============================================================================
            //  잔여 cascade 4종 + PlazmaSpawnPickaxe (72..76). 원본 single-level 매칭.
            // ============================================================================
            new Info(UpgradeId.CascadePlazmaToPlasmaBall,"플라즈마 연쇄",    "플라즈마 소멸 시\n+10% 확률로\n플라즈마 추가 발생.",       OreType.Ismium,
                new[] { 100000 }),
            new Info(UpgradeId.CascadeDynaToSplash,    "다이너마이트 → 번개","다이너마이트 소멸 시\n+10% 확률로\n번개 발생.",          OreType.Cobalt,
                new[] { 45000 }),
            new Info(UpgradeId.CascadePlazmaToSplash,  "플라즈마 → 번개",   "플라즈마 소멸 시\n+10% 확률로\n번개 발생.",             OreType.Uranium,
                new[] { 60000 }),
            new Info(UpgradeId.CascadeLightningToDyna, "번개 → 다이너마이트","번개 소멸 시\n+10% 확률로\n다이너마이트 발생.",         OreType.Iridium,
                new[] { 150000 }),
            new Info(UpgradeId.PlazmaSpawnPickaxe,     "플라즈마 곡괭이",   "플라즈마 소멸 시\n+10% 확률로\n무작위 발굴지 3개\n자동 타격.", OreType.Painite,
                new[] { 30000 }),
        };

        public static Info Get(UpgradeId id) => s_infos[(int)id];
        public static System.Collections.Generic.IReadOnlyList<Info> All => s_infos;
    }
}
