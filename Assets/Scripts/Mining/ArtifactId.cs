using UnityEngine;

namespace Rebuild.Mining
{




    public enum ArtifactId
    {


        Horn          = 0,
        GoldStack     = 1,
        Meteorite     = 2,
        Bone          = 3,
        AncientDevice = 4,
        WolfClaw      = 5,

        Book          = 6,
        GoldRing      = 7,
        PurpleRing    = 8,
        AncientDice   = 9,
        Cheese        = 10,
        Axe           = 11,
        Rune          = 12,
        Skull         = 13,
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




            public readonly float SpawnChance;
            public Info(ArtifactId id, string name, string desc, Color tint, float spawnChance)
            { Id = id; Name = name; Description = desc; Tint = tint; SpawnChance = spawnChance; }
        }




        static readonly Info[] s_infos =
        {

            new Info(ArtifactId.Horn,          "불국사 백운교",             "발굴지 체력이 3% 줄어듭니다.",                         new Color(0.92f, 0.84f, 0.55f, 1f), 2.3e-05f),
            new Info(ArtifactId.GoldStack,     "불국사 대웅전",             "돌 발굴지를 25번 캘 때마다 그 발굴지의 자원을 2배로 얻습니다.",             new Color(1.00f, 0.84f, 0.10f, 1f), 0.0037f),
            new Info(ArtifactId.Meteorite,     "첨성대",                   "풍요 발굴지의 출현 확률이 1.03배가 됩니다.",                      new Color(0.55f, 0.45f, 0.85f, 1f), 1.4e-05f),
            new Info(ArtifactId.Bone,          "천마총",                   "곡괭이 피해와 채굴 속도가 각각 4% 증가합니다.",             new Color(0.90f, 0.88f, 0.78f, 1f), 0.0008f),
            new Info(ArtifactId.AncientDevice, "천마총 금관",              "자동 발굴터의 평균 산출량이 5.26% 늘어납니다.",         new Color(0.40f, 0.85f, 0.95f, 1f), 2.6e-05f),
            new Info(ArtifactId.WolfClaw,      "불국사 다보탑",             "곡괭이 피해가 6% 증가합니다.",                            new Color(0.85f, 0.35f, 0.30f, 1f), 3.5e-05f),

            new Info(ArtifactId.Book,          "동궁과 월지",               "5레벨마다 특성 포인트를 1개 더 얻습니다.",                new Color(0.70f, 0.55f, 0.35f, 1f), 0.00016f),
            new Info(ArtifactId.GoldRing,      "에밀레종(성덕대왕신종)",     "정산할 때 주괴를 10% 더 얻습니다. 1개 미만의 수량도 원석 종류별로 누적됩니다.",    new Color(1.00f, 0.78f, 0.20f, 1f), 1.2e-05f),
            new Info(ArtifactId.PurpleRing,    "경주 고선사지 삼층석탑",     "4% 확률로 경험치를 2배로 얻습니다.",                       new Color(0.65f, 0.40f, 0.95f, 1f), 8.8e-05f),
            new Info(ArtifactId.AncientDice,   "경주 봉황대",               "최대 발굴지 수가 1개 늘고, 0.7초마다 발굴지가 추가로 나타납니다.", new Color(0.95f, 0.95f, 0.95f, 1f), 0.00011f),
            new Info(ArtifactId.Cheese,        "경주 향교",                 "0.2% 확률로 돌을 1개 더 얻습니다.",                new Color(1.00f, 0.92f, 0.45f, 1f), 1.1e-05f),
            new Info(ArtifactId.Axe,           "기타 고분",                 "1% 확률로 즉시 채굴합니다. 곡괭이가 2% 확률로 두 배의 피해를 줍니다.",       new Color(0.60f, 0.55f, 0.50f, 1f), 6.9e-05f),
            new Info(ArtifactId.Rune,          "경주 석빙고",               "모든 유물의 효과가 5% 강해집니다.",                 new Color(0.45f, 0.85f, 0.55f, 1f), 1.0e-05f),
            new Info(ArtifactId.Skull,         "불국사 석가탑",             "발굴마다 발굴지가 10개 더 나타납니다.",                new Color(0.85f, 0.85f, 0.80f, 1f), 0.0015f),
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
