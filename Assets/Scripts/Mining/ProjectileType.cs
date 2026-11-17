using UnityEngine;

namespace Rebuild.Mining
{




    public enum ProjectileType
    {
        BeamOfLight       = 0,
        PlazmaBall        = 1,
        Dynamite          = 2,
        ElectricitySplash = 3,
    }

    public static class ProjectileDef
    {
        public const int Count = 4;

        public readonly struct Info
        {
            public readonly ProjectileType Type;
            public readonly string Name;
            public readonly UpgradeId ChanceUpgradeId;
            public readonly Color Tint;
            public readonly int Damage;
            public readonly int MaxTargets;
            public readonly Vector2 Size;
            public readonly float Lifetime;

            public Info(ProjectileType type, string name, UpgradeId chanceId, Color tint,
                        int dmg, int maxTargets, Vector2 size, float lifetime)
            {
                Type = type; Name = name; ChanceUpgradeId = chanceId; Tint = tint;
                Damage = dmg; MaxTargets = maxTargets; Size = size; Lifetime = lifetime;
            }
        }

        static readonly Info[] s_infos =
        {
            new Info(ProjectileType.BeamOfLight,       "Beam of Light",
                    UpgradeId.BeamChance,
                    new Color(1.00f, 0.30f, 0.30f, 0.90f),
                     2,  3,  new Vector2(380f, 90f),   0.32f),




            new Info(ProjectileType.PlazmaBall,        "Plazma Ball",
                    UpgradeId.PlazmaChance,
                    new Color(0.78f, 0.42f, 1.00f, 0.90f),
                     3,  4,  new Vector2(280f, 280f),  1.0f),
            new Info(ProjectileType.Dynamite,          "Dynamite",
                    UpgradeId.DynamiteChance,
                    new Color(1.00f, 0.55f, 0.10f, 0.95f),
                     5,  6,  new Vector2(300f, 300f),  0.48f),
            new Info(ProjectileType.ElectricitySplash, "Electricity Splash",
                    UpgradeId.LightningChance,
                    new Color(0.98f, 0.95f, 0.30f, 0.95f),
                     2,  4,  new Vector2(300f, 300f),  0.28f),
        };

        public static Info Get(ProjectileType t) => s_infos[(int)t];
        public static System.Collections.Generic.IReadOnlyList<Info> All => s_infos;
    }
}
