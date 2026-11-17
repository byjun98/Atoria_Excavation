namespace Rebuild.Mining
{


















    public enum UpgradeId
    {
        MoreTime             = 0,
        SpawnMoreRocks       = 1,
        ImprovedPickaxe      = 2,
        MoreXP               = 3,
        MaterialsWorthMore   = 4,
        DoubleXpGoldChance   = 5,



        BeamChance           = 6,
        PlazmaChance         = 7,
        DynamiteChance       = 8,
        LightningChance      = 9,


        DoubleDamageChance   = 10,
        InstaMineChance      = 11,
        BiggerMiningArea     = 12,
        RespawnRockChance    = 13,
        RockBonusTimeChance  = 14,
        TickBonusTimeChance  = 15,
        GoldBoost            = 16,
        CopperBoost          = 17,
        IronBoost            = 18,
        RandomRockMineChance = 19,


        BeamDamage           = 20,
        BeamSize             = 21,
        DynamiteDamage       = 22,
        DynamiteSize         = 23,
        PlazmaDamage         = 24,
        PlazmaSize           = 25,
        PlazmaLifetime       = 26,
        LightningSize        = 27,
        AllProjectileDouble  = 28,
        AllProjectileTrigger = 29,



        CascadeBeamToDyna    = 30,
        CascadeBeamToBeam    = 31,
        CascadeBeamToSplash  = 32,
        CascadeBeamTime      = 33,
        CascadeDynaToBeam    = 34,
        CascadeDynaToDyna    = 35,
        CascadePlazmaToDyna  = 36,


        Craft2Material       = 37,










        CopperSpawn          = 38,
        CopperChunk          = 39,
        CopperFull           = 40,
        IronSpawn            = 41,
        IronChunk            = 42,
        IronFull             = 43,
        CobaltSpawn          = 44,
        CobaltChunk          = 45,
        CobaltFull           = 46,
        UraniumSpawn         = 47,
        UraniumChunk         = 48,
        UraniumFull          = 49,
        IsmiumSpawn          = 50,
        IsmiumChunk          = 51,
        IsmiumFull           = 52,
        IridiumSpawn         = 53,
        IridiumChunk         = 54,
        IridiumFull          = 55,
        PainiteSpawn         = 56,
        PainiteChunk         = 57,
        PainiteFull          = 58,




        FinalUpgrade         = 59,
        TalentPointsPerXlevel= 60,




        ChanceToSpawnRockWhenMined  = 61,
        SpawnXRockEveryXSecond      = 62,
        SpawnPickaxeEverySecond     = 63,
        ShootCircleChance           = 64,
        IncreaseAndDecreaseMinignErea= 65,
        IncreaseSpawnChanceAllRocks = 66,
        ChanceAdd1SecondEveryRockMined = 67,
        ChanceToAdd1SecondEverySecond  = 68,
        FullOreChance               = 69,
        SpawnRockEveryXrock         = 70,
        MoreMaterialsFromRock       = 71,





        CascadePlazmaToPlasmaBall   = 72,
        CascadeDynaToSplash         = 73,
        CascadePlazmaToSplash       = 74,
        CascadeLightningToDyna      = 75,
        PlazmaSpawnPickaxe          = 76,
    }

    public static class UpgradeDef
    {
        public const int Count = 77;

        static readonly int[] s_additionalRockSpawnPercent = { 2, 5, 8, 12, 17, 21 };

        public static float GetAdditionalRockSpawnChance(int level)
        {
            if (level <= 0) return 0f;
            return s_additionalRockSpawnPercent[System.Math.Min(level, s_additionalRockSpawnPercent.Length) - 1] / 100f;
        }

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



        static readonly Info[] s_infos =
        {

            new Info(UpgradeId.MoreTime,           "여유 시간",         "한 단계마다 발굴 시간이 1초 늘어납니다.",                       OreType.Gold,
                new[] { 9, 100, 1250, 70000 }),
            new Info(UpgradeId.SpawnMoreRocks,     "발굴지 증가",       "발굴을 시작할 때 발굴지가 늘어납니다.\n단계별 추가 수는 4, 5, 7, 8, 9, 9, 12, 15, 16개입니다.",                       OreType.Gold,
                new[] { 3, 6, 15, 25, 350, 3800, 27000, 270000, 350000 }),
            new Info(UpgradeId.ImprovedPickaxe,    "곡괭이 강화",       "일반 발굴 피해와 반경이 최대 26.7% 증가합니다.\n채굴 속도 계수는 단계마다 4%p 증가합니다.",                            OreType.Gold,
                new[] { 11, 300, 400, 15000, 50000, 100000 }),
            new Info(UpgradeId.MoreXP,             "경험치 증가",       "발굴이 끝나면 경험치를 더 얻습니다.\n단계별 추가 경험치는 2, 10, 46, 70, 124, 204, 314, 564입니다.",                          OreType.Copper,
                new[] { 12, 125, 21000, 600, 2100, 4500, 100000, 1000000 }),


            new Info(UpgradeId.MaterialsWorthMore, "자원 가치",         "일반 발굴과 자동 발굴에서 자원을 더 얻습니다.\n단계별 자원 배율은 2, 4, 7, 16, 28, 32, 38, 57배입니다.",              OreType.Gold,
                new[] { 100, 2000, 3000, 44000, 110000, 5500, 300000, 9000000 }),
            new Info(UpgradeId.DoubleXpGoldChance, "경험치와 자원 2배",     "발굴이 끝날 때 원석과 경험치를 2배로 얻을 수 있습니다.\n단계별 확률은 10%, 20%, 30%, 40%, 50%입니다.", OreType.Copper,
                new[] { 17, 10000, 2900, 300000, 1000000 }),


            new Info(UpgradeId.BeamChance,         "빛의 광선",         "곡괭이로 타격하면 광선이 발사될 수 있습니다.\n단계별 확률은 10%, 20%입니다.",               OreType.Copper,
                new[] { 500, 1600 }),
            new Info(UpgradeId.PlazmaChance,       "플라즈마 구체",     "곡괭이로 타격하면 플라즈마가 발사될 수 있습니다.\n단계별 확률은 4%, 8%입니다.",           OreType.Iron,
                new[] { 10000, 22000 }),
            new Info(UpgradeId.DynamiteChance,     "다이너마이트",      "곡괭이로 타격하면 다이너마이트가 폭발할 수 있습니다.\n단계별 확률은 0.1%, 0.2%입니다.",               OreType.Iron,
                new[] { 1000, 3500 }),
            new Info(UpgradeId.LightningChance,    "번개 강타",         "곡괭이로 타격하면 번개가 내리칠 수 있습니다.\n단계별 확률은 0.07%, 0.14%입니다.",               OreType.Copper,
                new[] { 200, 9000 }),


            new Info(UpgradeId.DoubleDamageChance, "이중 피해",         "곡괭이가 두 배의 피해를 줄 수 있습니다.\n단계별 확률은 1%, 2%입니다.",       OreType.Iron,
                new[] { 56, 80 }),
            new Info(UpgradeId.InstaMineChance,    "즉시 채굴",         "곡괭이로 타격한 발굴지를 즉시 캘 수 있습니다.\n단계별 확률은 1%, 2%입니다.",      OreType.Iron,
                new[] { 80, 15000 }),
            new Info(UpgradeId.BiggerMiningArea,   "넓은 발굴 영역",    "발굴 반경이 넓어집니다.\n단계별 증가율은 2%, 4%, 6%, 8%입니다.",                        OreType.Gold,
                new[] { 8, 120, 230, 2000 }),
            new Info(UpgradeId.RespawnRockChance,  "발굴지 재생성",     "발굴지를 캐면 같은 행의 빈칸에 새 발굴지가 나타날 수 있습니다.\n단계마다 확률이 5%p 높아집니다.", OreType.Gold,
                new[] { 20, 80, 350 }),
            new Info(UpgradeId.RockBonusTimeChance,"채굴 시 시간 연장",    "채굴할 때 일정 확률로 발굴 시간이 1초 늘어납니다.\n단계당 확률은 4%p, 한 번의 발굴에서 늘어나는 시간은 단계당 최대 2초입니다.", OreType.Gold,
                new[] { 18, 70, 300 }),
            new Info(UpgradeId.TickBonusTimeChance,"매초 시간 연장",      "매초 일정 확률로 발굴 시간이 1초 늘어납니다.\n단계당 확률은 3%p, 한 번의 발굴에서 늘어나는 시간은 단계당 최대 1초입니다.", OreType.Copper,
                new[] { 22, 90, 350 }),
            new Info(UpgradeId.GoldBoost,          "돌 증가",           "돌 발굴지가 나타날 가중치가 단계마다 5씩 증가합니다.",                    OreType.Gold,
                new[] { 12, 50, 200 }),
            new Info(UpgradeId.CopperBoost,        "토기조각 증가",     "토기조각 발굴지가 나타날 가중치가 단계마다 3씩 증가합니다.",              OreType.Gold,
                new[] { 16, 60, 220 }),
            new Info(UpgradeId.IronBoost,          "청동조각 증가",     "청동조각 발굴지가 나타날 가중치가 단계마다 2씩 증가합니다.",              OreType.Copper,
                new[] { 24, 90, 320 }),
            new Info(UpgradeId.RandomRockMineChance,"무작위 채굴",      "채굴 주기마다 범위 밖 발굴지 한 곳을 타격할 수 있습니다.\n단계별 확률은 3%, 6%, 9%, 12%입니다.",OreType.Iron,
                new[] { 50, 350, 1000, 33000 }),


            new Info(UpgradeId.BeamDamage,         "광선 피해",         "빛의 광선 피해가 1 증가합니다.",                           OreType.Gold,
                new[] { 1000 }),
            new Info(UpgradeId.BeamSize,           "광선 범위",         "광선이 타격하는 발굴지가 최대 3곳에서 4곳으로 늘어납니다.",                      OreType.Gold,
                new[] { 1200 }),
            new Info(UpgradeId.DynamiteDamage,     "폭발 피해",         "다이너마이트 피해가 2 증가합니다.",                        OreType.Gold,
                new[] { 10000 }),
            new Info(UpgradeId.DynamiteSize,       "폭발 범위",         "다이너마이트가 타격하는 발굴지가 최대 6곳에서 7곳으로 늘어납니다.",                   OreType.Gold,
                new[] { 32000 }),
            new Info(UpgradeId.PlazmaDamage,       "플라즈마 피해",     "플라즈마의 타격당 기본 피해가 1에서 2로 증가합니다.",                            OreType.Gold,
                new[] { 4600000 }),
            new Info(UpgradeId.PlazmaSize,         "플라즈마 범위",     "플라즈마가 한 번에 타격하는 발굴지가 최대 4곳에서 5곳으로 늘어납니다.",                       OreType.Iron,
                new[] { 30000 }),
            new Info(UpgradeId.PlazmaLifetime,     "플라즈마 지속",     "플라즈마의 지속 시간이 1초에서 2초로 늘어납니다. 최대 5회 타격합니다.",                      OreType.Iron,
                new[] { 430000 }),
            new Info(UpgradeId.LightningSize,      "번개 범위",         "번개가 타격하는 발굴지가 최대 4곳에서 5곳으로 늘어납니다.",                           OreType.Iron,
                new[] { 20000 }),
            new Info(UpgradeId.AllProjectileDouble,"발사체 2배",        "피해를 판정할 때마다 5% 확률로 해당 판정에 맞은 모든 대상에게 두 배의 피해를 줍니다.",        OreType.Gold,
                new[] { 1000000 }),
            new Info(UpgradeId.AllProjectileTrigger,"발사체 오라",      "곡괭이 타격으로 발사체가 발동할 확률이 1.15배가 됩니다.",            OreType.Gold,
                new[] { 700000 }),


            new Info(UpgradeId.CascadeBeamToDyna,  "광선의 후속 폭발",      "광선이 사라질 때 10% 확률로 폭발합니다.\n광선 계열 공통 효과로 발굴지가 생성될 확률은 24%입니다.",                      OreType.Gold,
                new[] { 250000 }),
            new Info(UpgradeId.CascadeBeamToBeam,  "광선 연쇄",      "광선이 사라질 때 10% 확률로 광선을 한 번 더 발사합니다.\n광선 계열 공통 효과로 발굴지가 생성될 확률은 24%입니다.",    OreType.Copper,
                new[] { 95000 }),
            new Info(UpgradeId.CascadeBeamToSplash,"광선의 후속 번개",      "광선이 사라질 때 10% 확률로 번개가 내리칩니다.\n광선 계열 공통 효과로 발굴지가 생성될 확률은 24%입니다.",                OreType.Iron,
                new[] { 20000 }),
            new Info(UpgradeId.CascadeBeamTime,    "광선의 시간 연장",     "광선이 사라질 때 0.8% 확률로 발굴 시간이 1초 늘어납니다.\n광선 계열 공통 효과로 발굴지가 생성될 확률은 24%입니다.",           OreType.Gold,
                new[] { 500000 }),
            new Info(UpgradeId.CascadeDynaToBeam,  "폭발의 후속 광선",      "폭발이 끝날 때 10% 확률로 광선이 발사됩니다.\n폭발 계열 공통 효과로 발굴지가 생성될 확률은 21%입니다.",         OreType.Copper,
                new[] { 600000 }),
            new Info(UpgradeId.CascadeDynaToDyna,  "연쇄 폭발",      "폭발이 끝날 때 10% 확률로 한 번 더 폭발합니다.\n폭발 계열 공통 효과로 발굴지가 생성될 확률은 21%입니다.",    OreType.Iron,
                new[] { 2500 }),
            new Info(UpgradeId.CascadePlazmaToDyna,"플라즈마의 후속 폭발",  "플라즈마가 사라질 때 10% 확률로 폭발합니다.",                  OreType.Iron,
                new[] { 610000 }),


            new Info(UpgradeId.Craft2Material,     "효율 복원",         "주괴 하나에 필요한 원석이 3개에서 2개로 줄어듭니다. 계산한 주괴 수량은 올림합니다.", OreType.Gold,
                new[] { 24000000 }),




            new Info(UpgradeId.CopperSpawn,        "토기조각 출현",     "토기조각 발굴지가 나타날 가중치가 25 증가합니다.",                          OreType.Gold,
                new[] { 100 }),
            new Info(UpgradeId.CopperChunk,        "토기조각 덩어리",   "이 자원의 덩어리 발굴지가 나타날 확률이 단계마다 10%p 높아집니다.\n원석을 기본 3개 얻으며, 발굴지 체력은 2배입니다.",                  OreType.Copper,
                new[] { 70, 2100, 15000 }),
            new Info(UpgradeId.CopperFull,         "토기조각 풍요",     "이 자원의 풍요 발굴지가 나타날 확률이 단계마다 5%p 높아집니다.\n원석을 기본 7개 얻으며, 발굴지 체력은 3배입니다.",           OreType.Copper,
                new[] { 90, 5500, 460000 }),
            new Info(UpgradeId.IronSpawn,          "청동조각 출현",     "청동조각 발굴지가 나타날 가중치가 25 증가합니다.",                          OreType.Copper,
                new[] { 400 }),
            new Info(UpgradeId.IronChunk,          "청동조각 덩어리",   "이 자원의 덩어리 발굴지가 나타날 확률이 단계마다 10%p 높아집니다.\n원석을 기본 3개 얻으며, 발굴지 체력은 2배입니다.",                  OreType.Iron,
                new[] { 500, 500000 }),
            new Info(UpgradeId.IronFull,           "청동조각 풍요",     "이 자원의 풍요 발굴지가 나타날 확률이 단계마다 5%p 높아집니다.\n원석을 기본 7개 얻으며, 발굴지 체력은 3배입니다.",           OreType.Iron,
                new[] { 920, 1000000 }),
            new Info(UpgradeId.CobaltSpawn,        "철편 출현",         "철편 발굴지가 나타날 가중치가 25 증가합니다.",                              OreType.Iron,
                new[] { 2350 }),
            new Info(UpgradeId.CobaltChunk,        "철편 덩어리",       "이 자원의 덩어리 발굴지가 나타날 확률이 단계마다 10%p 높아집니다.\n원석을 기본 3개 얻으며, 발굴지 체력은 2배입니다.",                             OreType.Cobalt,
                new[] { 3000 }),
            new Info(UpgradeId.CobaltFull,         "철편 풍요",         "이 자원의 풍요 발굴지가 나타날 확률이 단계마다 5%p 높아집니다.\n원석을 기본 7개 얻으며, 발굴지 체력은 3배입니다.",                      OreType.Cobalt,
                new[] { 3600 }),
            new Info(UpgradeId.UraniumSpawn,       "옥조각 출현",       "옥조각 발굴지가 나타날 가중치가 25 증가합니다.",                            OreType.Cobalt,
                new[] { 4000 }),
            new Info(UpgradeId.UraniumChunk,       "옥조각 덩어리",     "이 자원의 덩어리 발굴지가 나타날 확률이 단계마다 10%p 높아집니다.\n원석을 기본 3개 얻으며, 발굴지 체력은 2배입니다.",                           OreType.Uranium,
                new[] { 2600 }),
            new Info(UpgradeId.UraniumFull,        "옥조각 풍요",       "이 자원의 풍요 발굴지가 나타날 확률이 단계마다 5%p 높아집니다.\n원석을 기본 7개 얻으며, 발굴지 체력은 3배입니다.",                    OreType.Uranium,
                new[] { 3300 }),
            new Info(UpgradeId.IsmiumSpawn,        "유리구슬 출현",     "유리구슬 발굴지가 나타날 가중치가 25 증가합니다.",                          OreType.Uranium,
                new[] { 140000 }),
            new Info(UpgradeId.IsmiumChunk,        "유리구슬 덩어리",   "이 자원의 덩어리 발굴지가 나타날 확률이 단계마다 10%p 높아집니다.\n원석을 기본 3개 얻으며, 발굴지 체력은 2배입니다.",                         OreType.Ismium,
                new[] { 19000 }),
            new Info(UpgradeId.IsmiumFull,         "유리구슬 풍요",     "이 자원의 풍요 발굴지가 나타날 확률이 단계마다 5%p 높아집니다.\n원석을 기본 7개 얻으며, 발굴지 체력은 3배입니다.",                  OreType.Ismium,
                new[] { 23000 }),
            new Info(UpgradeId.IridiumSpawn,       "금조각 출현",       "금조각 발굴지가 나타날 가중치가 25 증가합니다.",                            OreType.Ismium,
                new[] { 75000 }),
            new Info(UpgradeId.IridiumChunk,       "금조각 덩어리",     "이 자원의 덩어리 발굴지가 나타날 확률이 단계마다 10%p 높아집니다.\n원석을 기본 3개 얻으며, 발굴지 체력은 2배입니다.",                           OreType.Iridium,
                new[] { 25000 }),
            new Info(UpgradeId.IridiumFull,        "금조각 풍요",       "이 자원의 풍요 발굴지가 나타날 확률이 단계마다 5%p 높아집니다.\n원석을 기본 7개 얻으며, 발굴지 체력은 3배입니다.",                    OreType.Iridium,
                new[] { 33000 }),
            new Info(UpgradeId.PainiteSpawn,       "사리 출현",         "사리 발굴지가 나타날 가중치가 25 증가합니다.",                              OreType.Iridium,
                new[] { 1000000 }),
            new Info(UpgradeId.PainiteChunk,       "사리 덩어리",       "이 자원의 덩어리 발굴지가 나타날 확률이 단계마다 10%p 높아집니다.\n원석을 기본 3개 얻으며, 발굴지 체력은 2배입니다.",                             OreType.Painite,
                new[] { 160000 }),
            new Info(UpgradeId.PainiteFull,        "사리 풍요",         "이 자원의 풍요 발굴지가 나타날 확률이 단계마다 5%p 높아집니다.\n원석을 기본 7개 얻으며, 발굴지 체력은 3배입니다.",                      OreType.Painite,
                new[] { 240000 }),




            new Info(UpgradeId.FinalUpgrade,       "최종 복원",         "곡괭이 14개를 복원하고 유물 14개를 모은 뒤 이 스킬을 구매하면 엔딩에 도전할 수 있습니다.", OreType.Painite,
                new[] { 1000000 }),
            new Info(UpgradeId.TalentPointsPerXlevel,"수련 가속",       "일정 레벨마다 특성 포인트를 1개 더 얻습니다. 포인트를 받는 간격은 단계별로 15, 10, 5레벨입니다.", OreType.Gold,
                new[] { 100, 1200, 1500000 }),




            new Info(UpgradeId.ChanceToSpawnRockWhenMined, "발굴지 추가 생성", $"새 발굴지에 바위 1개가 더 생길 수 있습니다. ×2 발굴지에서는 첫 바위를 캐면 다음 바위가 나옵니다.\n단계별 확률(%)은 {string.Join(", ", s_additionalRockSpawnPercent)}입니다.", OreType.Gold,
                new[] { 18, 1000, 27000, 2600, 250, 13000 }),
            new Info(UpgradeId.SpawnXRockEveryXSecond, "정기 출현 가속", "1단계에서는 1초마다 1개, 2단계에서는 0.45초마다 1개, 3단계에서는 0.2초마다 3개가 빈칸에 생깁니다.", OreType.Iron,
                new[] { 75, 420, 20000 }),
            new Info(UpgradeId.SpawnPickaxeEverySecond,"자동 곡괭이",   "자동 곡괭이가 무작위 발굴지를 한 번씩 타격합니다.\n단계별 타격 간격은 1.0초, 0.7초, 0.4초입니다.", OreType.Gold,
                new[] { 4000, 8000, 6000 }),
            new Info(UpgradeId.ShootCircleChance,  "원형 파동",         "매초 30% 확률로 작은 원 안의 발굴지를 자동으로 타격합니다.", OreType.Copper,
                new[] { 125 }),
            new Info(UpgradeId.IncreaseAndDecreaseMinignErea,"발굴 영역 확장","2초 주기로 발굴 반경이 최대 10%까지 넓어졌다 좁아집니다.", OreType.Gold,
                new[] { 5000 }),
            new Info(UpgradeId.IncreaseSpawnChanceAllRocks,"발굴지 폭증","발굴을 시작할 때 발굴지가 50% 더 나타납니다. 추가 수량은 올림합니다.",  OreType.Gold,
                new[] { 400000 }),
            new Info(UpgradeId.ChanceAdd1SecondEveryRockMined,"채굴 시간 보너스","채굴할 때마다 0.075% 확률로 발굴 시간이 1초 늘어납니다.", OreType.Gold,
                new[] { 9000 }),
            new Info(UpgradeId.ChanceToAdd1SecondEverySecond,"매초 시간 보너스","매초 9% 확률로 발굴 시간이 1초 늘어납니다.", OreType.Iron,
                new[] { 800 }),
            new Info(UpgradeId.FullOreChance,      "풍요 채굴",         "채굴할 때 일정 확률로 자원을 3배로 얻습니다. 단계마다 확률이 5%p 높아집니다.", OreType.Gold,
                new[] { 50, 350, 1000, 33000 }),
            new Info(UpgradeId.SpawnRockEveryXrock,"누적 보너스",       "일정 수의 발굴지를 캘 때마다 빈칸에 발굴지가 1개 생깁니다.\n단계별 필요 수량은 5개, 4개, 3개입니다.", OreType.Copper,
                new[] { 500, 3000, 340000 }),
            new Info(UpgradeId.MoreMaterialsFromRock,"자원 대폭 증산",  "발굴지마다 기본 원석 수량이 단계당 1개씩 늘어납니다. 늘어난 수량에도 자원 가치 배율이 적용됩니다.", OreType.Gold,
                new[] { 350, 500000, 400000, 10000, 700000 }),




            new Info(UpgradeId.CascadePlazmaToPlasmaBall,"플라즈마 연쇄",    "플라즈마가 사라질 때 10% 확률로 플라즈마가 한 번 더 발생합니다.",       OreType.Ismium,
                new[] { 100000 }),
            new Info(UpgradeId.CascadeDynaToSplash,    "폭발의 후속 번개","폭발이 끝날 때 10% 확률로 번개가 내리칩니다.\n폭발 계열 공통 효과로 발굴지가 생성될 확률은 21%입니다.",          OreType.Cobalt,
                new[] { 45000 }),
            new Info(UpgradeId.CascadePlazmaToSplash,  "플라즈마의 후속 번개",   "플라즈마가 사라질 때 10% 확률로 번개가 내리칩니다.",             OreType.Uranium,
                new[] { 60000 }),
            new Info(UpgradeId.CascadeLightningToDyna, "번개의 후속 폭발","번개가 사라질 때 10% 확률로 다이너마이트가 폭발합니다.",         OreType.Iridium,
                new[] { 150000 }),
            new Info(UpgradeId.PlazmaSpawnPickaxe,     "플라즈마 곡괭이",   "플라즈마가 사라질 때 10% 확률로 무작위 발굴지를 최대 3회 타격합니다. 같은 곳을 다시 타격할 수 있습니다.", OreType.Painite,
                new[] { 30000 }),
        };

        public static Info Get(UpgradeId id) => s_infos[(int)id];
        public static System.Collections.Generic.IReadOnlyList<Info> All => s_infos;
    }
}
