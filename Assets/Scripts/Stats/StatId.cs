namespace Rebuild.Stats
{






    public enum StatId
    {


        ClicksTotal = 0,
        RocksDestroyed = 1,
        SessionsCompleted = 2,
        ProjectilesFired = 3,
        FlowersClicked = 4,


        ArtifactsFoundCount = 5,
        PickaxesCraftedCount = 6,
        MaxLevelReached = 7,


        OreMined_Gold = 8,
        OreMined_Copper = 9,
        OreMined_Iron = 10,
        OreMined_Cobalt = 11,
        OreMined_Uranium = 12,
        OreMined_Ismium = 13,
        OreMined_Iridium = 14,
        OreMined_Painite = 15,


        TotalLevelUps              = 16,
        ExperienceGained           = 17,
        TimeSpentMiningSec         = 18,
        TotalRocksSpawned          = 19,
        BarsMined_TheMine          = 20,
        FlowersSpawned             = 21,
        InstaMineHits              = 22,
        XpDoublesTriggered         = 23,
        DoubleDamageHits           = 24,
        ProjectilesFired_Beam      = 25,
        ProjectilesFired_Plazma    = 26,
        ProjectilesFired_Dynamite  = 27,
        ProjectilesFired_Lightning = 28,
        OresMined_Total            = 29,
        ChestsOpened               = 30,
        HammerTriggers             = 31,
        ZeusStrikes                = 32,


        PotionsDrank               = 33,
        GoldenChestsOpened         = 34,
        TheMine2XTriggers          = 35,
        InflateBurstTriggers       = 36,
        MidasTouchSessions         = 37,
        EnergiDrinksDrank          = 38,
        CampfiresSpawned           = 39,
        D100Rolls                  = 40,
        PickaxesSpawned            = 41,
        PickaxeHits                = 42,
        DoubleOreGained            = 43,
        BarsCrafted                = 44,
        FullOreMined_Gold          = 45,
        FullOreMined_Copper        = 46,
    }



    public static class StatDef
    {
        public const int Count = 47;


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
            StatId.TotalLevelUps => "레벨 상승 횟수",
            StatId.ExperienceGained => "획득 XP",
            StatId.TimeSpentMiningSec => "발굴 시간(초)",
            StatId.TotalRocksSpawned => "발굴지 생성",
            StatId.BarsMined_TheMine => "자동 발굴터에서 얻은 주괴",
            StatId.FlowersSpawned => "꽃 생성",
            StatId.InstaMineHits => "즉시 채굴",
            StatId.XpDoublesTriggered => "XP 2배 발동",
            StatId.DoubleDamageHits => "이중 피해 발동",
            StatId.ProjectilesFired_Beam => "광선 발사",
            StatId.ProjectilesFired_Plazma => "플라즈마 발사",
            StatId.ProjectilesFired_Dynamite => "다이너마이트 발사",
            StatId.ProjectilesFired_Lightning => "번개 타격",
            StatId.OresMined_Total => "채굴한 원석 총량",
            StatId.ChestsOpened => "보물상자 개봉",
            StatId.HammerTriggers => "망치 발동",
            StatId.ZeusStrikes => "제우스 발동",

            StatId.PotionsDrank => "마신 물약",
            StatId.GoldenChestsOpened => "황금 상자 개봉",
            StatId.TheMine2XTriggers => "광부 2배 발동",
            StatId.InflateBurstTriggers => "확장 폭발 발동",
            StatId.MidasTouchSessions => "마이더스 골든런",
            StatId.EnergiDrinksDrank => "에너지 드링크",
            StatId.CampfiresSpawned => "모닥불 생성",
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
