namespace Rebuild.Mining
{








    public enum RockTier
    {
        Normal = 0,
        Chunk  = 1,
        Full   = 2,
    }

    public static class RockTierDef
    {

        public static int OreDropMultiplier(RockTier tier)
        {
            switch (tier)
            {
                case RockTier.Chunk: return 3;
                case RockTier.Full:  return 7;
                default:             return 1;
            }
        }


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
