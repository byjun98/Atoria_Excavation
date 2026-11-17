namespace Rebuild.Mining
{









    public static class D100Helper
    {


        public const float D100BonusPP = 0f;



        public static float Bonus(float chance) => chance;



        public static bool Roll(float chance) => UnityEngine.Random.value < chance;
    }
}
