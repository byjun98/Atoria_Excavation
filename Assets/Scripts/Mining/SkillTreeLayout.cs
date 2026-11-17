using UnityEngine;

namespace Rebuild.Mining
{


    public static class SkillTreeLayout
    {
        public struct Node
        {
            public UpgradeId Id;
            public Vector2 Position;
            public UpgradeId? Parent;
            public Node(UpgradeId id, Vector2 pos, UpgradeId? parent = null)
            { Id = id; Position = pos; Parent = parent; }
        }


        public const int Cols = 22;
        public const int Rows = 9;
        public const float CellW = 100f;
        public const float CellH = 95f;

        public static readonly Node[] Nodes = new Node[]
        {
            new Node(UpgradeId.MoreTime, new Vector2(0f, 0f)),
            new Node(UpgradeId.SpawnMoreRocks, new Vector2(-180f, 320f), UpgradeId.MoreTime),
            new Node(UpgradeId.ImprovedPickaxe, new Vector2(-350f, 0f), UpgradeId.MoreTime),
            new Node(UpgradeId.MoreXP, new Vector2(-370f, 480f), UpgradeId.SpawnMoreRocks),
            new Node(UpgradeId.MaterialsWorthMore, new Vector2(350f, 0f), UpgradeId.MoreTime),
            new Node(UpgradeId.DoubleXpGoldChance, new Vector2(-560f, 600f), UpgradeId.MoreXP),
            new Node(UpgradeId.BeamChance, new Vector2(-600f, -100f), UpgradeId.ImprovedPickaxe),
            new Node(UpgradeId.PlazmaChance, new Vector2(-850f, -100f), UpgradeId.BeamChance),
            new Node(UpgradeId.DynamiteChance, new Vector2(-780f, -320f), UpgradeId.BeamChance),
            new Node(UpgradeId.LightningChance, new Vector2(-540f, -350f), UpgradeId.BeamChance),
            new Node(UpgradeId.DoubleDamageChance, new Vector2(-400f, 200f), UpgradeId.ImprovedPickaxe),
            new Node(UpgradeId.InstaMineChance, new Vector2(-550f, 320f), UpgradeId.DoubleDamageChance),
            new Node(UpgradeId.BiggerMiningArea, new Vector2(80f, -330f), UpgradeId.MoreTime),
            new Node(UpgradeId.RespawnRockChance, new Vector2(300f, -410f), UpgradeId.BiggerMiningArea),
            new Node(UpgradeId.RandomRockMineChance, new Vector2(480f, -450f), UpgradeId.RespawnRockChance),
            new Node(UpgradeId.RockBonusTimeChance, new Vector2(130f, 330f), UpgradeId.MoreTime),
            new Node(UpgradeId.TickBonusTimeChance, new Vector2(270f, 500f), UpgradeId.RockBonusTimeChance),
            new Node(UpgradeId.GoldBoost, new Vector2(450f, 230f), UpgradeId.MaterialsWorthMore),
            new Node(UpgradeId.CopperBoost, new Vector2(520f, 400f), UpgradeId.GoldBoost),
            new Node(UpgradeId.IronBoost, new Vector2(590f, 570f), UpgradeId.CopperBoost),
            new Node(UpgradeId.BeamDamage, new Vector2(-670f, 100f), UpgradeId.BeamChance),
            new Node(UpgradeId.BeamSize, new Vector2(-820f, 260f), UpgradeId.BeamDamage),
            new Node(UpgradeId.DynamiteDamage, new Vector2(-920f, -420f), UpgradeId.DynamiteChance),
            new Node(UpgradeId.DynamiteSize, new Vector2(-850f, -620f), UpgradeId.DynamiteDamage),
            new Node(UpgradeId.PlazmaDamage, new Vector2(-1040f, 10f), UpgradeId.PlazmaChance),
            new Node(UpgradeId.PlazmaSize, new Vector2(-1230f, -10f), UpgradeId.PlazmaDamage),
            new Node(UpgradeId.PlazmaLifetime, new Vector2(-1400f, -110f), UpgradeId.PlazmaSize),
            new Node(UpgradeId.LightningSize, new Vector2(-490f, -560f), UpgradeId.LightningChance),
            new Node(UpgradeId.AllProjectileDouble, new Vector2(-360f, -730f), UpgradeId.LightningSize),
            new Node(UpgradeId.AllProjectileTrigger, new Vector2(-200f, -900f), UpgradeId.AllProjectileDouble),
            new Node(UpgradeId.CascadeBeamToDyna, new Vector2(-1010f, 350f), UpgradeId.BeamSize),
            new Node(UpgradeId.CascadeBeamToBeam, new Vector2(-1200f, 400f), UpgradeId.CascadeBeamToDyna),
            new Node(UpgradeId.CascadeBeamToSplash, new Vector2(-1390f, 430f), UpgradeId.CascadeBeamToBeam),
            new Node(UpgradeId.CascadeBeamTime, new Vector2(-1570f, 430f), UpgradeId.CascadeBeamToSplash),
            new Node(UpgradeId.CascadeDynaToBeam, new Vector2(-850f, -820f), UpgradeId.DynamiteSize),
            new Node(UpgradeId.CascadeDynaToDyna, new Vector2(-700f, -950f), UpgradeId.CascadeDynaToBeam),
            new Node(UpgradeId.CascadePlazmaToDyna, new Vector2(-1510f, -280f), UpgradeId.PlazmaLifetime),
            new Node(UpgradeId.Craft2Material, new Vector2(220f, 220f), UpgradeId.MaterialsWorthMore),
            new Node(UpgradeId.CopperSpawn, new Vector2(650f, 70f), UpgradeId.MaterialsWorthMore),
            new Node(UpgradeId.CopperChunk, new Vector2(650f, 250f), UpgradeId.CopperSpawn),
            new Node(UpgradeId.CopperFull, new Vector2(650f, 430f), UpgradeId.CopperChunk),
            new Node(UpgradeId.IronSpawn, new Vector2(850f, -10f), UpgradeId.CopperSpawn),
            new Node(UpgradeId.IronChunk, new Vector2(850f, 170f), UpgradeId.IronSpawn),
            new Node(UpgradeId.IronFull, new Vector2(850f, 350f), UpgradeId.IronChunk),
            new Node(UpgradeId.CobaltSpawn, new Vector2(1050f, -90f), UpgradeId.IronSpawn),
            new Node(UpgradeId.CobaltChunk, new Vector2(1050f, 90f), UpgradeId.CobaltSpawn),
            new Node(UpgradeId.CobaltFull, new Vector2(1050f, 270f), UpgradeId.CobaltChunk),
            new Node(UpgradeId.UraniumSpawn, new Vector2(1250f, -170f), UpgradeId.CobaltSpawn),
            new Node(UpgradeId.UraniumChunk, new Vector2(1250f, 10f), UpgradeId.UraniumSpawn),
            new Node(UpgradeId.UraniumFull, new Vector2(1250f, 190f), UpgradeId.UraniumChunk),
            new Node(UpgradeId.IsmiumSpawn, new Vector2(1450f, -250f), UpgradeId.UraniumSpawn),
            new Node(UpgradeId.IsmiumChunk, new Vector2(1450f, -70f), UpgradeId.IsmiumSpawn),
            new Node(UpgradeId.IsmiumFull, new Vector2(1450f, 110f), UpgradeId.IsmiumChunk),
            new Node(UpgradeId.IridiumSpawn, new Vector2(1650f, -330f), UpgradeId.IsmiumSpawn),
            new Node(UpgradeId.IridiumChunk, new Vector2(1650f, -150f), UpgradeId.IridiumSpawn),
            new Node(UpgradeId.IridiumFull, new Vector2(1650f, 30f), UpgradeId.IridiumChunk),
            new Node(UpgradeId.PainiteSpawn, new Vector2(1850f, -410f), UpgradeId.IridiumSpawn),
            new Node(UpgradeId.PainiteChunk, new Vector2(1850f, -230f), UpgradeId.PainiteSpawn),
            new Node(UpgradeId.PainiteFull, new Vector2(1850f, -50f), UpgradeId.PainiteChunk),
            new Node(UpgradeId.FinalUpgrade, new Vector2(2050f, -80f), UpgradeId.PainiteFull),
            new Node(UpgradeId.TalentPointsPerXlevel, new Vector2(220f, -150f), UpgradeId.MaterialsWorthMore),
            new Node(UpgradeId.ChanceToSpawnRockWhenMined, new Vector2(-20f, -560f), UpgradeId.BiggerMiningArea),
            new Node(UpgradeId.SpawnXRockEveryXSecond, new Vector2(80f, -760f), UpgradeId.ChanceToSpawnRockWhenMined),
            new Node(UpgradeId.SpawnPickaxeEverySecond, new Vector2(230f, -920f), UpgradeId.SpawnXRockEveryXSecond),
            new Node(UpgradeId.ShootCircleChance, new Vector2(420f, -1020f), UpgradeId.SpawnPickaxeEverySecond),
            new Node(UpgradeId.IncreaseAndDecreaseMinignErea, new Vector2(610f, -1060f), UpgradeId.ShootCircleChance),
            new Node(UpgradeId.IncreaseSpawnChanceAllRocks, new Vector2(800f, -1050f), UpgradeId.IncreaseAndDecreaseMinignErea),
            new Node(UpgradeId.ChanceAdd1SecondEveryRockMined, new Vector2(300f, -620f), UpgradeId.BiggerMiningArea),
            new Node(UpgradeId.ChanceToAdd1SecondEverySecond, new Vector2(510f, -670f), UpgradeId.ChanceAdd1SecondEveryRockMined),
            new Node(UpgradeId.FullOreChance, new Vector2(710f, -750f), UpgradeId.ChanceToAdd1SecondEverySecond),
            new Node(UpgradeId.SpawnRockEveryXrock, new Vector2(900f, -860f), UpgradeId.FullOreChance),
            new Node(UpgradeId.MoreMaterialsFromRock, new Vector2(470f, -180f), UpgradeId.MaterialsWorthMore),
            new Node(UpgradeId.CascadePlazmaToPlasmaBall, new Vector2(-1450f, -460f), UpgradeId.CascadePlazmaToDyna),
            new Node(UpgradeId.CascadeDynaToSplash, new Vector2(-500f, -1030f), UpgradeId.CascadeDynaToDyna),
            new Node(UpgradeId.CascadePlazmaToSplash, new Vector2(-1280f, -540f), UpgradeId.CascadePlazmaToPlasmaBall),
            new Node(UpgradeId.CascadeLightningToDyna, new Vector2(-20f, -1070f), UpgradeId.AllProjectileTrigger),
            new Node(UpgradeId.PlazmaSpawnPickaxe, new Vector2(-1090f, -550f), UpgradeId.CascadePlazmaToSplash),
        };

        public static UpgradeId? GetParent(UpgradeId id)
        {
            for (int i = 0; i < Nodes.Length; i++)
                if (Nodes[i].Id == id) return Nodes[i].Parent;
            return null;
        }

        public static Vector2 GetPosition(UpgradeId id)
        {
            for (int i = 0; i < Nodes.Length; i++)
                if (Nodes[i].Id == id) return Nodes[i].Position;
            return Vector2.zero;
        }
    }
}
