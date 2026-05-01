using UnityEngine;

namespace Rebuild.Mining
{
    /// <summary>14 full artifact roster.
    /// Idx 0..5 are the Shell starters (simple multiplicative or flat boosts).
    /// Idx 6..13 extend the roster with later-game artifact effects.
    /// Each effect hook is queried by name (HasArtifact) in the matching manager.</summary>
    public enum ArtifactId
    {
        // 14 artifact 원본 매칭. 의 click-damage 단순화 모델 (Bone/WolfClaw/Rune
        // 셋 다 +damage) 을 풀고 원본의 multi-site multiplier 모델 복원.
        Horn          = 0,  // Rock maxHp × (1 - 0.03 × (1 + RuneBonus)) — multi-site multiplier
        GoldStack     = 1,  // Gold rock counter — 25번에 1번 (Rune 24) ore drop ×2
        Meteorite     = 2,  // RockSpawner.PickTier: fullXRockChance × 1.03 on 8 ores (Rune +0.0015)
        Bone          = 3,  // +4% click damage + click cadence -4% (mineTime 분모에 합산, secondary)
        AncientDevice = 4,  // MineManager.CurrentMiningTime × (1 - 0.05 × (1 + RuneBonus))
        WolfClaw      = 5,  // +6% × (1 + RuneBonus) click damage (clawChance 매칭)
        // Extended artifact effects.
        Book          = 6,  // LevelMechanics.GiveXP: every 5 levels, +1 talentPoint bonus
        GoldRing      = 7,  // GoldAndOreMechanics.CraftSessionToBars: 1.025~1.035× bar bonus
        PurpleRing    = 8,  // LevelMechanics.GiveXP: 4% × (1 + RuneBonus) chance XP×2
        AncientDice   = 9,  // RockSpawner.SpawnAll: maxRocks +1 + 0.7s dice loop bonus spawn (Rune -0.05s)
        Cheese        = 10, // Rock.FadeAndDestroy: 0.2% × (1 + RuneBonus) chance +1 Gold
        Axe           = 11, // Rock.ApplyExternalHit: 1% insta + 2% 2× damage branch (둘 다 × (1 + RuneBonus))
        Rune          = 12, // universal multiplier × (1 + 0.05): 위 다른 12+ site 의 booster + 자체 +5% damage
        Skull         = 13, // RockSpawner.SpawnAll: 세션당 +10 bonus rocks (분산 spawn)
    }

    public static class ArtifactDef
    {
        public const int Count = 14;

        public readonly struct Info
        {
            public readonly ArtifactId Id;
            public readonly string Name;
            public readonly string Description;
            public readonly Color Tint;
            /// <summary>original Artifacts.SetArtifactChances base spawnChance.
            /// 14 unique values ranging Skull/GoldStack=common (0.0015~0.0037, ~270-666 destroys
            /// per find) to Rune=rarest (1e-05, ~100,000 destroys). Replaces our former single
            /// 0.01 roll where every artifact was equally likely.</summary>
            public readonly float SpawnChance;
            public Info(ArtifactId id, string name, string desc, Color tint, float spawnChance)
            { Id = id; Name = name; Description = desc; Tint = tint; SpawnChance = spawnChance; }
        }

        // Display names are Korean heritage artifacts; enum names stay stable because gameplay
        // systems key their effects by ArtifactId.
        // SpawnChance per-artifact (Artifacts.cs:361-431 표 그대로).
        static readonly Info[] s_infos =
        {
            // 원본 multi-site multiplier 모델 매칭. 설명 텍스트는 효과 핵심만 표시.
            new Info(ArtifactId.Horn,          "불국사 백운교",             "발굴지 체력 -3%.",                         new Color(0.92f, 0.84f, 0.55f, 1f), 2.3e-05f),
            new Info(ArtifactId.GoldStack,     "불국사 대웅전",             "25번째 금 채굴마다\n자원 2배.",             new Color(1.00f, 0.84f, 0.10f, 1f), 0.0037f),
            new Info(ArtifactId.Meteorite,     "첨성대",                   "Full 광물 확률 +3%.",                      new Color(0.55f, 0.45f, 0.85f, 1f), 1.4e-05f),
            new Info(ArtifactId.Bone,          "천마총",                   "클릭 피해 +4%\n채굴 속도 +4%.",             new Color(0.90f, 0.88f, 0.78f, 1f), 0.0008f),
            new Info(ArtifactId.AncientDevice, "천마총 금관",              "자동 발굴터\n주기 5% 감소.",                new Color(0.40f, 0.85f, 0.95f, 1f), 2.6e-05f),
            new Info(ArtifactId.WolfClaw,      "불국사 다보탑",             "클릭 피해 +6%.",                            new Color(0.85f, 0.35f, 0.30f, 1f), 3.5e-05f),
            // Extended artifact effects.
            new Info(ArtifactId.Book,          "동궁과 월지",               "5레벨마다\n특성 포인트 +1.",                new Color(0.70f, 0.55f, 0.35f, 1f), 0.00016f),
            new Info(ArtifactId.GoldRing,      "에밀레종(성덕대왕신종)",     "주괴 변환\n+2.5~3.5% 보너스.",              new Color(1.00f, 0.78f, 0.20f, 1f), 1.2e-05f),
            new Info(ArtifactId.PurpleRing,    "경주 고선사지 삼층석탑",     "4% 확률로\nXP 2배.",                       new Color(0.65f, 0.40f, 0.95f, 1f), 8.8e-05f),
            new Info(ArtifactId.AncientDice,   "경주 봉황대",               "최대 발굴지 +1,\n0.7초마다 추가\n발굴지 등장.", new Color(0.95f, 0.95f, 0.95f, 1f), 0.00011f),
            new Info(ArtifactId.Cheese,        "경주 향교",                 "0.2% 확률로\n금 +1 보너스.",                new Color(1.00f, 0.92f, 0.45f, 1f), 1.1e-05f),
            new Info(ArtifactId.Axe,           "기타 고분",                 "1% 즉시 채굴 +\n2% 확률로 2배 피해.",       new Color(0.60f, 0.55f, 0.50f, 1f), 6.9e-05f),
            new Info(ArtifactId.Rune,          "경주 석빙고",               "모든 유물 효과\n+5% 강화.",                 new Color(0.45f, 0.85f, 0.55f, 1f), 1.0e-05f),
            new Info(ArtifactId.Skull,         "불국사 석가탑",             "세션마다 발굴지\n+10 추가.",                new Color(0.85f, 0.85f, 0.80f, 1f), 0.0015f),
        };

        static readonly string[] s_spriteResourceNames =
        {
            "불국사 백운교",
            "불국사 대웅전",
            "첨성대",
            "천마총",
            "천마총 금관",
            "불국사 다보탑",
            "동궁과 월지",
            "에밀레종(성덕대왕신종)",
            "경주 고선사지 삼층석탑",
            "경주 봉황대",
            "경주 향교",
            "기타 고분",
            "경주 석빙고",
            "불국사 석가탑",
        };

        public static Info Get(ArtifactId id) => s_infos[(int)id];
        public static string GetSpriteResourceName(ArtifactId id) => s_spriteResourceNames[(int)id];
        public static System.Collections.Generic.IReadOnlyList<Info> All => s_infos;
    }
}
