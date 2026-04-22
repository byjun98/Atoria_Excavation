using UnityEngine;

namespace Rebuild.Stats
{
    /// <summary>milestones (6) + (10) = 16 achievements.
    /// Trigger is evaluated against <see cref="StatsManager"/> every time a relevant stat ticks
    /// (see AchievementsManager). Indices are persisted directly into the DTO so
    /// APPEND-ONLY — never reorder.</summary>
    public enum AchievementId
    {
        // ---- (0..5) — DO NOT REORDER ----
        FirstRock = 0,
        HundredRocks = 1,
        FirstCraft = 2,
        Level10 = 3,
        AllArtifactsFound = 4,
        ThousandGold = 5,

        // ---- (6..15) — append-only ----
        ExpertMiner       = 6,  // RocksDestroyed >= 10_000
        MineMaster200K    = 7,  // RocksDestroyed >= 200_000
        Talented80        = 8,  // MaxLevelReached >= 80
        LeveledUp50       = 9,  // TotalLevelUps >= 50
        Marathoner        = 10, // TimeSpentMiningSec >= 3600 (1 hour)
        MiningMarathon100 = 11, // SessionsCompleted >= 100
        LuckyStrike       = 12, // XpDoublesTriggered >= 50 (PurpleRing artifact triggered)
        BarsmithApprentice= 13, // BarsMined_TheMine >= 100
        AllPickaxe        = 14, // PickaxesCraftedCount >= 14 (all 14 pickaxes crafted)
        BankBank          = 15, // OreMined_Gold >= 1_000_000

        // ---- append-only, 6 new ----
        PotionApprentice    = 16, // PotionsDrank >= 10 (PotionDrinker / PotionChugger 사용)
        ChestHunter         = 17, // ChestsOpened + GoldenChestsOpened >= 50 (composite — watch ChestsOpened)
        BeamDynamitePlazma  = 18, // 4 projectile types all fired (composite — watch ProjectilesFired_Beam>=1)
        BeatGame            = 19, // isEndingCompleted (composite — TheEndingManager state)
        TheMineEnthusiast   = 20, // BarsMined_TheMine >= 10000
        SignSeeker          = 21, // CampfiresSpawned >= 5 (proxy — ItsASign + Camper 활용)
    }

    public readonly struct AchievementInfo
    {
        public readonly string Name;
        public readonly string Description;
        public readonly StatId Watch;
        public readonly long Threshold;
        public readonly Color Tint;

        public AchievementInfo(string name, string desc, StatId watch, long threshold, Color tint)
        {
            Name = name; Description = desc; Watch = watch; Threshold = threshold; Tint = tint;
        }
    }

    public static class AchievementDef
    {
        public const int Count = 22;

        // Tints reused on the unlock popup background + the list-cell icon.
        static readonly Color Bronze = new Color(0.80f, 0.50f, 0.25f);
        static readonly Color Silver = new Color(0.80f, 0.80f, 0.85f);
        static readonly Color Iron = new Color(0.55f, 0.55f, 0.60f);
        static readonly Color Gold = new Color(1.00f, 0.84f, 0.20f);
        static readonly Color Purple = new Color(0.65f, 0.45f, 0.90f);
        static readonly Color Yellow = new Color(0.95f, 0.85f, 0.10f);
        static readonly Color Diamond = new Color(0.70f, 0.95f, 1.00f);
        static readonly Color Emerald = new Color(0.30f, 0.80f, 0.45f);
        static readonly Color Royal = new Color(0.50f, 0.30f, 0.85f);
        static readonly Color Teal = new Color(0.20f, 0.75f, 0.75f);

        // 한국어 업적 라벨/설명. 임계값은 동일.
        public static AchievementInfo Get(AchievementId id) => id switch
        {
            AchievementId.FirstRock => new AchievementInfo(
                "첫 발굴!", "첫 발굴지를 캐낸다.", StatId.RocksDestroyed, 1, Bronze),
            AchievementId.HundredRocks => new AchievementInfo(
                "백 단위", "발굴지 100개 채굴.", StatId.RocksDestroyed, 100, Silver),
            AchievementId.FirstCraft => new AchievementInfo(
                "견습 대장장이", "곡괭이를 처음 제작.", StatId.PickaxesCraftedCount, 1, Iron),
            AchievementId.Level10 => new AchievementInfo(
                "특성 입문", "레벨 10 달성.", StatId.MaxLevelReached, 10, Purple),
            AchievementId.AllArtifactsFound => new AchievementInfo(
                "유물 수집가", "유물 14개 모두 발견.", StatId.ArtifactsFoundCount, 14, Gold),
            AchievementId.ThousandGold => new AchievementInfo(
                "돌 1,000", "돌 1,000개 채굴.", StatId.OreMined_Gold, 1000, Yellow),
            // ---- ---
            AchievementId.ExpertMiner => new AchievementInfo(
                "숙련 발굴꾼", "발굴지 10,000개 채굴.", StatId.RocksDestroyed, 10000, Diamond),
            AchievementId.MineMaster200K => new AchievementInfo(
                "발굴 명인", "발굴지 200,000개 채굴.", StatId.RocksDestroyed, 200000, Emerald),
            AchievementId.Talented80 => new AchievementInfo(
                "최고 특성", "레벨 80 달성.", StatId.MaxLevelReached, 80, Royal),
            AchievementId.LeveledUp50 => new AchievementInfo(
                "평생 학습", "총 50회 레벨업.", StatId.TotalLevelUps, 50, Purple),
            AchievementId.Marathoner => new AchievementInfo(
                "마라토너", "발굴 누적 1시간.", StatId.TimeSpentMiningSec, 3600, Teal),
            AchievementId.MiningMarathon100 => new AchievementInfo(
                "발굴 마라톤", "발굴 세션 100회 완료.", StatId.SessionsCompleted, 100, Emerald),
            AchievementId.LuckyStrike => new AchievementInfo(
                "행운의 한 방", "XP 2배 50회 발동.", StatId.XpDoublesTriggered, 50, Royal),
            AchievementId.BarsmithApprentice => new AchievementInfo(
                "자동 발굴 견습", "자동 발굴터에서\n주괴 100개 획득.", StatId.BarsMined_TheMine, 100, Bronze),
            AchievementId.AllPickaxe => new AchievementInfo(
                "곡괭이 수집가", "곡괭이 14개 모두 제작.", StatId.PickaxesCraftedCount, 14, Diamond),
            AchievementId.BankBank => new AchievementInfo(
                "돌 백만장자", "돌 1,000,000개 채굴.", StatId.OreMined_Gold, 1000000, Yellow),
            // ---- ---
            AchievementId.PotionApprentice => new AchievementInfo(
                "물약 견습", "물약 10회 마심.", StatId.PotionsDrank, 10, Royal),
            AchievementId.ChestHunter => new AchievementInfo(
                "상자 사냥꾼", "보물상자 50회 발동.", StatId.ChestsOpened, 50, Bronze),
            AchievementId.BeamDynamitePlazma => new AchievementInfo(
                "광선과 폭탄", "발사체 4종 모두\n한 번 이상 발사.", StatId.ProjectilesFired_Beam, 1, Diamond),
            AchievementId.BeatGame => new AchievementInfo(
                "엔딩 도달", "다이아몬드 곡괭이\n복원 완료.", StatId.MaxLevelReached, 1, Yellow),
            AchievementId.TheMineEnthusiast => new AchievementInfo(
                "자동 발굴 명인", "자동 발굴터에서\n주괴 10,000개 획득.", StatId.BarsMined_TheMine, 10000, Emerald),
            AchievementId.SignSeeker => new AchievementInfo(
                "징조의 추구자", "캠퍼 5회 활성.", StatId.CampfiresSpawned, 5, Silver),
            _ => new AchievementInfo(id.ToString(), "", StatId.ClicksTotal, long.MaxValue, Color.white),
        };
    }
}
