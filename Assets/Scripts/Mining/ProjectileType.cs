using UnityEngine;

namespace Rebuild.Mining
{
    /// <summary>4 projectile categories. Each links to one SkillTree chance node
    /// (see <see cref="ProjectileDef"/>); rolling success on rock click spawns one short-lived
    /// AOE effect that hits the N closest rocks. Original game uses object pools and tag-based
    /// per-frame triggers; Shell collapses both into a one-shot AddComponent + immediate damage.</summary>
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
            public readonly UpgradeId ChanceUpgradeId;  // SkillTree chance node powering this projectile
            public readonly Color Tint;
            public readonly int Damage;                 // applied to every rock in the AOE
            public readonly int MaxTargets;             // closest-rock count affected (incl. source)
            public readonly Vector2 Size;               // UI sprite size in canvas units
            public readonly float Lifetime;             // seconds before the visual fades + destroys

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
                    /*dmg*/ 2, /*targets*/ 3, /*size*/ new Vector2(380f, 90f),  /*life*/ 0.32f),
            // Plazma Lifetime 0.42s → 1.0s 확장 + tick 3 → 5. 원본
            // plazmaBallTimer=5f 의 lingering 느낌 강화. 5 tick × 0.3× damage = 1.5× total
            // budget 유지 (balance 보존). PlazmaLifetime SkillTree 노드로 +0.1s/lv
            // 추가 연장 가능. 5초까지 안 가도 1초 lingering 으로 plazma 가 "sustained AOE" 느낌.
            new Info(ProjectileType.PlazmaBall,        "Plazma Ball",
                    UpgradeId.PlazmaChance,
                    new Color(0.78f, 0.42f, 1.00f, 0.90f),
                    /*dmg*/ 3, /*targets*/ 4, /*size*/ new Vector2(280f, 280f), /*life*/ 1.0f),
            new Info(ProjectileType.Dynamite,          "Dynamite",
                    UpgradeId.DynamiteChance,
                    new Color(1.00f, 0.55f, 0.10f, 0.95f),
                    /*dmg*/ 5, /*targets*/ 6, /*size*/ new Vector2(300f, 300f), /*life*/ 0.48f),
            new Info(ProjectileType.ElectricitySplash, "Electricity Splash",
                    UpgradeId.LightningChance,
                    new Color(0.98f, 0.95f, 0.30f, 0.95f),
                    /*dmg*/ 2, /*targets*/ 4, /*size*/ new Vector2(300f, 300f), /*life*/ 0.28f),
        };

        public static Info Get(ProjectileType t) => s_infos[(int)t];
        public static System.Collections.Generic.IReadOnlyList<Info> All => s_infos;
    }
}
