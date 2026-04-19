namespace Rebuild.Mining
{
    /// <summary>14 full pickaxe roster.
    /// Idx 0..4 use Gold/Copper/Iron only; idx 5..13 unlock the rarer ores
    /// (Cobalt / Uranium / Ismium / Iridium / Painite) via TheMine idle yields and
    /// SkillTree rolls. Each pickaxe contributes a flat <see cref="Info.DamageBonus"/>
    /// on click and sets the hover-channel <see cref="Info.MineTime"/> cadence.
    /// Cost values mirror the restored anvil balance table.
    /// </summary>
    public static class PickaxeDef
    {
        public const int Count = 14;

        public readonly struct Cost
        {
            public readonly OreType[] Currencies;
            public readonly int[] Amounts;
            public Cost(OreType[] currencies, int[] amounts) { Currencies = currencies; Amounts = amounts; }
            public bool IsFree => Amounts == null || Amounts.Length == 0;
        }

        public readonly struct Info
        {
            public readonly int Idx;
            public readonly string Name;
            /// <summary>채굴 위력 (Mine Power). 한 번 클릭 시 flat +N.N 데미지. 원본 TheAnvil 의
            /// minePower 값과 정확 매칭 — Pickaxe 1 = +2.5, Pickaxe 14 = +8.0 ().</summary>
            public readonly float DamageBonus;
            /// <summary>채굴 시간 (Mine Time, 초). hover 채굴 cadence — 낮을수록 빠름. 원본
            /// TheAnvil mineTime: Pickaxe 1 = 0.34s, Pickaxe 14 = 0.20s ().</summary>
            public readonly float MineTime;
            /// <summary>위력 2배 발동 확률 (0..1). 1.1%~6.5%.</summary>
            public readonly float DoublePowerChance;
            /// <summary>발굴 영역 크기 (splash 반경 multiplier). 인접 발굴지 추가 데미지 (1 미만 = no splash).</summary>
            public readonly float MiningAreaSize;
            public readonly Cost Cost;
            public Info(int idx, string name, float dmg, float mineTime, float doublePowerChance, float miningAreaSize, Cost cost)
            { Idx = idx; Name = name; DamageBonus = dmg; MineTime = mineTime;
              DoublePowerChance = doublePowerChance; MiningAreaSize = miningAreaSize; Cost = cost; }
        }

        // 14 행 stats 원본 TheAnvil.cs:1312~1367 표 정확 매칭.
        // mineTime / minePower / 2XChance / areaSize 모두 원본 그대로.
        // Cost arrays stay separate from the stat table so economy tuning remains local.
        static readonly Info[] s_infos =
        {
            new Info(0,  "돌날 곡괭이",       2.5f, 0.34f,  0.011f, 1.21f, new Cost(new OreType[0], new int[0])),
            new Info(1,  "동빛 곡괭이",       2.9f, 0.33f,  0.014f, 1.30f, new Cost(new[] { OreType.Gold }, new[] { 65 })),
            new Info(2,  "녹청 장식 곡괭이",  3.3f, 0.32f,  0.019f, 1.40f, new Cost(new[] { OreType.Gold, OreType.Copper }, new[] { 370, 140 })),
            new Info(3,  "흑철 곡괭이",       3.7f, 0.31f,  0.021f, 1.43f, new Cost(new[] { OreType.Copper, OreType.Iron }, new[] { 650, 400 })),
            new Info(4,  "은날 곡괭이",       3.9f, 0.30f,  0.023f, 1.47f, new Cost(new[] { OreType.Gold, OreType.Iron }, new[] { 4000, 1800 })),
            new Info(5,  "흑요석 곡괭이",     4.3f, 0.30f,  0.026f, 1.50f, new Cost(new[] { OreType.Gold, OreType.Cobalt }, new[] { 10000, 3000 })),
            new Info(6,  "비취 곡괭이",       4.7f, 0.285f, 0.029f, 1.52f, new Cost(new[] { OreType.Gold, OreType.Copper, OreType.Iron }, new[] { 25000, 6000, 5000 })),
            new Info(7,  "청유리 곡괭이",     5.0f, 0.28f,  0.031f, 1.60f, new Cost(new[] { OreType.Cobalt, OreType.Uranium }, new[] { 12000, 8000 })),
            new Info(8,  "금장 곡괭이",       5.2f, 0.27f,  0.038f, 1.60f, new Cost(new[] { OreType.Gold, OreType.Uranium, OreType.Ismium }, new[] { 300000, 40000, 25000 })),
            new Info(9,  "은문양 곡괭이",     5.6f, 0.26f,  0.041f, 1.67f, new Cost(new[] { OreType.Gold, OreType.Copper, OreType.Iridium }, new[] { 750000, 550000, 40000 })),
            new Info(10, "흑수정 곡괭이",     6.1f, 0.25f,  0.046f, 1.70f, new Cost(new[] { OreType.Cobalt, OreType.Uranium, OreType.Ismium }, new[] { 1000000, 650000, 235000 })),
            new Info(11, "황금결정 곡괭이",   6.6f, 0.24f,  0.051f, 1.70f, new Cost(new[] { OreType.Gold, OreType.Iridium }, new[] { 9000000, 700000 })),
            new Info(12, "백옥 곡괭이",       7.4f, 0.23f,  0.057f, 1.76f, new Cost(new[] { OreType.Gold, OreType.Iron, OreType.Painite }, new[] { 30000000, 10000000, 700000 })),
            new Info(13, "왕가 금장곡괭이",   8.0f, 0.20f,  0.065f, 1.80f, new Cost(new[] { OreType.Gold, OreType.Painite }, new[] { 50000000, 2500000 })),
        };

        public static Info Get(int idx) => s_infos[idx];
        public static System.Collections.Generic.IReadOnlyList<Info> All => s_infos;
    }
}
