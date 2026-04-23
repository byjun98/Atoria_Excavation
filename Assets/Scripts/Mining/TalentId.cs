using UnityEngine;

namespace Rebuild.Mining
{
    /// <summary>+ + = 25 full talent roster.
    /// 원본 LevelMechanics.cs (1813줄) 의 모든 talent 효과 매칭.
    /// Shell 의 의도된 nerf / 의미 재정의 풀고 원본 수치/공식 그대로:
    ///   - <see cref="Archaeologist"/> 가 8 site multi-multiplier (archeologistIncrease=0.08) 복원
    ///   - <see cref="BigMiningArea"/> = 발굴 area ×1.4 (원본 inflationBurstIncrease 매칭. Shell 의 +1/row 폐기)
    ///   - <see cref="BiggerSplash"/> = 발굴 area ×1.15 (BigMiningArea 과 동일 axis)
    ///   - <see cref="Chests"/> = 0.6% spawn → +25 ore (원본 rockSpawnChance=0.6f, totalChestMaterials=25)
    ///   - <see cref="GoldenChests"/> = 0.5% spawn (세션당 1 cap) → +50 Gold (원본 totalGoldenChestMaterials=50)
    ///   - <see cref="SkilledMiners"/> = 10% fast (mineTime÷2) + 8% double (bars×2) 별도 roll
    ///   - <see cref="Zeus"/> = 5% / 15 lightning (원본 zeusLightningAmount=15)
    ///   - <see cref="EnergiDrinker"/> = 7% / 3s / mineTime×0.5 (원본 ÷2)
    ///   - <see cref="D100"/> = 세션 종료 시 1/10/100 (=3%) 면 ore bars ×3 (원본 SetRockScreen:1021)
    /// <para>Shell 의 <see cref="ArtifactBuff"/> 는 신규 (원본 archaeologist 와 무관 — 원본은 efficacy
    /// buff 만, spawn chance 영향 X). <see cref="GoldenTouch"/> + <see cref="MidasTouch"/> 분리는
    /// Shell 디자인으로 유지 (원본은 단일 GoldenTouch 의 두 효과).</para></summary>
    public enum TalentId
    {
        // ---- ---
        Archaeologist = 0,  // archeologistIncrease=0.08 multi-site multiplier (Cheese / Axe insta / WolfClaw dmg / AncientDevice / PurpleRing / Dice spawn)
        MoreXP        = 1,  // XP gain x2 (Shell 신규)
        GoldenTouch   = 2,  // +1 Gold on every rock destroy (Shell 분리 — 원본 GoldenTouch 의 일부)
        BigMiningArea = 3,  // 발굴 area ×1.4 (원본 inflationBurstIncrease=0.4)
        MoreTime      = 4,  // mineSessionTime +5s (Shell 신규)

        // ---- ---
        Camper        = 5,  // 모닥불 activate — ON 0.06s / OFF 0.28s cycle 안 닿은 rock insta-kill
        SpringSeason  = 6,  // 세션 시작 시 flowersOnScreen=Random(2,18), XP × (1 + 0.015 × budget)

        // ---- (new — 9, simple hooks) ----
        Chests              =  7, // 0.6% chance per rock spawn → Chest variant → +25 ore (원본 매칭)
        GoldenChests        =  8, // 0.5% chance per rock spawn (세션당 1) → Golden variant → +50 Gold
        SkilledMiners       =  9, // TheMine tick: 10% fast (mineTime÷2) + 8% double (bars×2) 별도 roll
        EfficientBlacksmith = 10, // Anvil pickaxe craft cost x0.75
        SteamSale           = 11, // SkillTree purchase price x0.93
        XMarksTheSpot       = 12, // Artifact roll chance x1.4
        Explorer            = 13, // Artifact roll chance x1.5 (XMarks 와 곱셈 누적)
        ArtifactBuff        = 14, // Artifact roll chance x1.08 (Shell 신규 — 원본 archaeologist 와 무관)
        BiggerSplash        = 15, // 발굴 area ×1.15 (BigMiningArea 과 같은 axis, 더 작은 multiplier)

        // ---- (new — 4, slightly heavier hooks) ----
        Zeus                = 16, // 매 1초 5% → 15회 random rock lightning (원본 zeusLightningAmount=15)
        HammerTime          = 17, // 0.4% chance per swing → instaMine + ore drop ×2
        EnergiDrinker       = 18, // 매 1초 7% → 3초 mineTime ×0.5 (원본 ÷2)
        MidasTouch          = 19, // 세션 시작 20% chance 골든런 → 골든런 중 25%/hit +1 Gold

        // ---- (5 new — Potion/Sign/D100/ShapeShifter) ----
        PotionDrinker = 20, // 세션 시작 1/4 random potion 활성
        PotionChugger = 21, // 세션 시작 4종 동시 활성
        ItsASign      = 22, // 5분 cycle, 4종 random buff (TheMine)
        ShapeShifter  = 23, // Rock 크기 ×1.06 + 발굴 area ×1.06
        D100          = 24, // 세션 종료 시 1/10/100 (=3%) 면 ore bars ×3 (원본 매칭)
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

        // 한국어 라벨 (원본 LocalizationScript 참고 + Shell 의미차 의역).
        static readonly Info[] s_infos =
        {
            new Info(TalentId.Archaeologist,       "암석 사냥꾼",       "유물 효과 +8%\n(주사위/치즈/도끼\n늑대발톱/반지).", new Color(0.82f, 0.55f, 0.25f, 1f)),
            new Info(TalentId.MoreXP,              "경험치 2배",        "발굴 한 번마다\n경험치 2배.",                     new Color(0.45f, 0.85f, 0.40f, 1f)),
            new Info(TalentId.GoldenTouch,         "황금 손길",         "발굴 한 번마다\n돌 +1 추가.",                     new Color(1.00f, 0.84f, 0.10f, 1f)),
            new Info(TalentId.BigMiningArea,       "발굴터 확장",       "발굴 범위 ×1.4\n(인플레이션 버스트).",            new Color(0.35f, 0.70f, 0.95f, 1f)),
            new Info(TalentId.MoreTime,            "여유 시간",         "세션 시간\n+5초.",                                new Color(0.85f, 0.40f, 0.85f, 1f)),
            new Info(TalentId.Camper,              "캠퍼",              "모닥불을 피워\n가까운 발굴지를\n조금씩 캐냄.",     new Color(1.00f, 0.55f, 0.20f, 1f)),
            new Info(TalentId.SpringSeason,        "봄 시즌",           "발굴 시 가끔 꽃이\n떨어짐 — 클릭하면\n무작위 자원 +2.", new Color(1.00f, 0.55f, 0.85f, 1f)),
            new Info(TalentId.Chests,              "보물상자",          "발굴지 생성 시\n0.6% 보물상자 → 자원\n+25.",      new Color(0.55f, 0.30f, 0.15f, 1f)),
            new Info(TalentId.GoldenChests,        "황금 상자",         "발굴지 생성 시\n0.5% 황금상자 (세션당 1)\n→ 돌 +50.", new Color(1.00f, 0.80f, 0.15f, 1f)),
            new Info(TalentId.SkilledMiners,       "숙련된 광부",       "자동 발굴터: 10%\n가속(절반시간) + 8%\n산출량 2배.",  new Color(0.65f, 0.65f, 0.95f, 1f)),
            new Info(TalentId.EfficientBlacksmith, "효율적 대장간",     "곡괭이 제작 비용\n-25%.",                          new Color(0.55f, 0.40f, 0.30f, 1f)),
            new Info(TalentId.SteamSale,           "스팀 세일",         "스킬트리 가격\n-7%.",                              new Color(0.30f, 0.55f, 0.90f, 1f)),
            new Info(TalentId.XMarksTheSpot,       "X 표시된 장소",     "유물 발견 확률\n×1.4.",                            new Color(0.85f, 0.30f, 0.30f, 1f)),
            new Info(TalentId.Explorer,            "탐험가",            "유물 발견 확률\n×1.5.",                            new Color(0.70f, 0.85f, 0.30f, 1f)),
            new Info(TalentId.ArtifactBuff,        "행운의 발견",       "유물 발견 확률\n×1.08.",                           new Color(0.60f, 0.30f, 0.85f, 1f)),
            new Info(TalentId.BiggerSplash,        "넓은 충격",         "발굴 범위 ×1.15\n(작은 area 강화).",              new Color(0.40f, 0.85f, 0.85f, 1f)),
            new Info(TalentId.Zeus,                "제우스의 분노",     "1초마다 5% 확률로\n15회 무작위 발굴지\n번개 강타.", new Color(1.00f, 0.95f, 0.30f, 1f)),
            new Info(TalentId.HammerTime,          "망치 타임",         "0.4% 확률로\n즉시 채굴 + 자원 2배.",              new Color(0.55f, 0.55f, 0.55f, 1f)),
            new Info(TalentId.EnergiDrinker,       "에너지 드링커",     "1초마다 7% 확률로\n3초간 채굴 속도\n×0.5.",        new Color(0.30f, 0.95f, 0.55f, 1f)),
            new Info(TalentId.MidasTouch,          "마이더스의 손길",   "세션마다 20% 확률\n골든런: 클릭마다\n25%로 돌 +1.", new Color(1.00f, 0.65f, 0.10f, 1f)),
            // ---- ---
            new Info(TalentId.PotionDrinker,       "물약 마시기",       "세션마다 4종 물약 중\n1종 무작위 활성.",          new Color(0.55f, 0.40f, 0.95f, 1f)),
            new Info(TalentId.PotionChugger,       "물약 폭음",         "세션마다 4종 물약\n모두 동시 활성.",              new Color(0.85f, 0.30f, 0.95f, 1f)),
            new Info(TalentId.ItsASign,            "징조의 표식",       "자동 발굴터에\n5분마다 무작위\n버프 4종 중 1.",   new Color(0.85f, 0.75f, 0.30f, 1f)),
            new Info(TalentId.ShapeShifter,        "형상 변환",         "발굴지 크기 +6%.",                                new Color(0.50f, 0.85f, 0.95f, 1f)),
            new Info(TalentId.D100,                "백의 주사위",       "세션 종료 시 3%\n(1/10/100) 자원 ×3.",            new Color(0.95f, 0.95f, 0.95f, 1f)),
        };

        public static Info Get(TalentId id) => s_infos[(int)id];
        public static System.Collections.Generic.IReadOnlyList<Info> All => s_infos;
    }
}
