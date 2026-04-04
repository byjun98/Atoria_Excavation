namespace Rebuild.Mining
{
    /// <summary>Rock 의 3 tier (원본 게임의 일반/덩어리/완전 자원).
    /// <list type="bullet">
    /// <item><b>Normal</b> — 회색 바위 (광물 없음). 채굴 시 광석 <b>1</b> 드롭.</item>
    /// <item><b>Chunk</b> — 회색 바위 + 광물 조각. 광석 <b>3</b> 드롭.</item>
    /// <item><b>Full</b> — 광물 덩어리 (회색 바위 거의 없음). 광석 <b>7</b> 드롭.</item>
    /// </list>
    /// Tier 분포는 SkillTreeManager 의 <c>{ore}Chunk</c> / <c>{ore}Full</c> 레벨 기반 확률
    /// (RockSpawner.PickTier). SkillTree 미구매 시 모두 Normal.</summary>
    public enum RockTier
    {
        Normal = 0,
        Chunk  = 1,
        Full   = 2,
    }

    public static class RockTierDef
    {
        /// <summary>tier 별 광석 드롭 배수. Rock.FadeAndDestroy 가 이 값을 baseDrop 에 곱한다.</summary>
        public static int OreDropMultiplier(RockTier tier)
        {
            switch (tier)
            {
                case RockTier.Chunk: return 3;
                case RockTier.Full:  return 7;
                default:             return 1;
            }
        }

        /// <summary>tier 별 HP 배수. Full = 큰 덩어리이므로 더 단단함.</summary>
        public static int HpMultiplier(RockTier tier)
        {
            switch (tier)
            {
                case RockTier.Chunk: return 2;
                case RockTier.Full:  return 3;
                default:             return 1;
            }
        }
    }
}
