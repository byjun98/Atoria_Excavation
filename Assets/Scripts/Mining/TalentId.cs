using UnityEngine;

namespace Rebuild.Mining
{















    public enum TalentId
    {

        Archaeologist = 0,
        MoreXP        = 1,
        GoldenTouch   = 2,
        BigMiningArea = 3,
        MoreTime      = 4,


        Camper        = 5,
        SpringSeason  = 6,


        Chests              =  7,
        GoldenChests        =  8,
        SkilledMiners       =  9,
        EfficientBlacksmith = 10,
        SteamSale           = 11,
        XMarksTheSpot       = 12,
        Explorer            = 13,
        ArtifactBuff        = 14,
        BiggerSplash        = 15,


        Zeus                = 16,
        HammerTime          = 17,
        EnergiDrinker       = 18,
        MidasTouch          = 19,


        PotionDrinker = 20,
        PotionChugger = 21,
        ItsASign      = 22,
        ShapeShifter  = 23,
        D100          = 24,
    }

    public static class TalentDef
    {
        public const int Count = 25;

        public readonly struct Info
        {
            public readonly TalentId Id;
            public readonly string Name;
            public readonly string Description;
            public readonly Color Tint;
            public Info(TalentId id, string name, string desc, Color tint) { Id = id; Name = name; Description = desc; Tint = tint; }
        }


        static readonly Info[] s_infos =
        {
            new Info(TalentId.Archaeologist,       "암석 사냥꾼",       "불국사 백운교와 천마총 금관 등 일부 유물의 효과가 8% 강해집니다.", new Color(0.82f, 0.55f, 0.25f, 1f)),
            new Info(TalentId.MoreXP,              "경험치 2배",        "발굴할 때 경험치를 2배로 얻습니다.",                     new Color(0.45f, 0.85f, 0.40f, 1f)),
            new Info(TalentId.GoldenTouch,         "황금 손길",         "발굴할 때마다 돌을 1개 더 얻습니다.",                     new Color(1.00f, 0.84f, 0.10f, 1f)),
            new Info(TalentId.BigMiningArea,       "발굴터 확장",       "발굴 범위가 1.4배로 넓어집니다.",            new Color(0.35f, 0.70f, 0.95f, 1f)),
            new Info(TalentId.MoreTime,            "여유 시간",         "발굴 시간이 5초 늘어납니다.",                                new Color(0.85f, 0.40f, 0.85f, 1f)),
            new Info(TalentId.Camper,              "캠퍼",              "모닥불이 주변 발굴지를 조금씩 캐냅니다.",     new Color(1.00f, 0.55f, 0.20f, 1f)),
            new Info(TalentId.SpringSeason,        "봄 시즌",           "꽃을 클릭하면 무작위 자원을 2개 얻습니다.", new Color(1.00f, 0.55f, 0.85f, 1f)),
            new Info(TalentId.Chests,              "보물상자",          "발굴지 생성 시 0.6% 확률로 등장합니다. 열면 자원을 25개 얻습니다.",      new Color(0.55f, 0.30f, 0.15f, 1f)),
            new Info(TalentId.GoldenChests,        "황금 상자",         "발굴지 생성 시 0.5% 확률로 등장합니다. 한 발굴에 최대 1개가 나오며, 열면 돌 50개를 얻습니다.", new Color(1.00f, 0.80f, 0.15f, 1f)),
            new Info(TalentId.SkilledMiners,       "숙련된 광부",       "자동 발굴 평균 산출량이 5.26% 늘어납니다. 별도로 8% 확률로 산출량이 2배가 됩니다.",  new Color(0.65f, 0.65f, 0.95f, 1f)),
            new Info(TalentId.EfficientBlacksmith, "효율적 대장간",     "곡괭이 복원 비용이 25% 줄어듭니다.",                          new Color(0.55f, 0.40f, 0.30f, 1f)),
            new Info(TalentId.SteamSale,           "스팀 세일",         "스킬 구매 비용이 7% 줄어듭니다.",                              new Color(0.30f, 0.55f, 0.90f, 1f)),
            new Info(TalentId.XMarksTheSpot,       "X 표시된 장소",     "유물 발견 확률이 1.4배가 됩니다.",                            new Color(0.85f, 0.30f, 0.30f, 1f)),
            new Info(TalentId.Explorer,            "탐험가",            "유물 발견 확률이 1.5배가 됩니다.",                            new Color(0.70f, 0.85f, 0.30f, 1f)),
            new Info(TalentId.ArtifactBuff,        "행운의 발견",       "유물 발견 확률이 1.08배가 됩니다.",                           new Color(0.60f, 0.30f, 0.85f, 1f)),
            new Info(TalentId.BiggerSplash,        "넓은 충격",         "발굴 범위가 1.15배로 넓어집니다.",              new Color(0.40f, 0.85f, 0.85f, 1f)),
            new Info(TalentId.Zeus,                "제우스의 분노",     "매초 5% 확률로 번개가 무작위 발굴지를 15회 타격합니다.", new Color(1.00f, 0.95f, 0.30f, 1f)),
            new Info(TalentId.HammerTime,          "망치 타임",         "0.4% 확률로 즉시 채굴하고 자원을 2배로 얻습니다.",              new Color(0.55f, 0.55f, 0.55f, 1f)),
            new Info(TalentId.EnergiDrinker,       "에너지 드링커",     "매초 7% 확률로 3초간 채굴 간격이 절반으로 줄어듭니다.",        new Color(0.30f, 0.95f, 0.55f, 1f)),
            new Info(TalentId.MidasTouch,          "마이더스의 손길",   "발굴 시작 시 20% 확률로 발동합니다. 발동 중 타격마다 25% 확률로 돌을 1개 더 얻습니다.", new Color(1.00f, 0.65f, 0.10f, 1f)),

            new Info(TalentId.PotionDrinker,       "물약 마시기",       "발굴마다 물약 1종을 마십니다. 물약 폭음도 보유하면 1종이 무작위로 강화됩니다.",      new Color(0.55f, 0.40f, 0.95f, 1f)),
            new Info(TalentId.PotionChugger,       "물약 폭음",         "발굴마다 물약 4종을 마십니다. 물약 마시기도 보유하면 1종이 무작위로 강화됩니다.",    new Color(0.85f, 0.30f, 0.95f, 1f)),
            new Info(TalentId.ItsASign,            "징조의 표식",       "자동 발굴터에 5분마다 강화 효과 4종 중 하나가 무작위로 적용됩니다.",   new Color(0.85f, 0.75f, 0.30f, 1f)),
            new Info(TalentId.ShapeShifter,        "형상 변환",         "발굴지 크기와 발굴 반경이 6% 늘어납니다.",                                new Color(0.50f, 0.85f, 0.95f, 1f)),
            new Info(TalentId.D100,                "백의 주사위",       "발굴이 끝날 때 주사위가 1, 10 또는 100이면 자원을 3배로 얻습니다 (확률 3%).",            new Color(0.95f, 0.95f, 0.95f, 1f)),
        };

        public static Info Get(TalentId id) => s_infos[(int)id];
        public static System.Collections.Generic.IReadOnlyList<Info> All => s_infos;
    }
}
