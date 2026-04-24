using UnityEngine;

namespace Rebuild.Mining
{
    /// <summary>1 center root + 4 directional first-tier children + sub-chains.
    /// 원본 게임 사진 5 매칭: 가운데 SpawnMoreRocks + 위 MoreTime / 왼 ImprovedPickaxe /
    /// 오 MaterialsWorthMore / 아래 BiggerMiningArea. 나머지 15 root 를 4 first-tier 의 sub-chain
    /// 으로 reparent. progressive hide (TreeNodeUI.RefreshState) 가 부모 미해금 자식을
    /// SetActive(false) — 시작 시 center 1개만 visible, lv1 구매 후 4 child 활성, 단계별 fan-out.
    ///
    /// <para>좌표: 22 col × 9 row grid (cell 100 × 95) 안에 4-방향 tree 펼침. center = col 10.5,
    /// row 4. Tier 1 (4 dir) = ±3 col / ±3 row. Tier 2+ = 그 outward chain.</para>
    /// </summary>
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

        // Center = col 10.5, row 4. 4-방향 tree 펼침.
        static Vector2 G(int col, int row) => new Vector2((col - 10.5f) * CellW, (4f - row) * CellH);

        // 72 entries. Order matches UpgradeId enum index so callers can index by (int)id.
        public static readonly Node[] Nodes = new Node[]
        {
            // ====================================================================================
            //   CENTER ROOT (idx 0)
            //   MoreTime 을 center 로 — 사진 5 의 가운데 회색 돌 + 표시 (구매 가능). 시간 buff 가
            //   가장 보편적 첫 구매라 의미 매칭.
            //   ⚠️ 원래 idx 0 = MoreTime — center root 로 유지하면 자연.
            // ====================================================================================

            new Node(UpgradeId.MoreTime,             G(10, 4)),                          // idx 0 - CENTER ROOT

            // ====================================================================================
            //   TIER 1 — 4 directional first-tier children of MoreTime
            // ====================================================================================

            new Node(UpgradeId.SpawnMoreRocks,       G(10, 1), UpgradeId.MoreTime),      // idx 1 - 위 (회색 돌)
            new Node(UpgradeId.ImprovedPickaxe,      G(6, 4),  UpgradeId.MoreTime),      // idx 2 - 왼 (곡괭이)
            new Node(UpgradeId.MoreXP,               G(8, 1),  UpgradeId.SpawnMoreRocks),// idx 3 - 위-왼 (별)
            new Node(UpgradeId.MaterialsWorthMore,   G(15, 4), UpgradeId.MoreTime),      // idx 4 - 오 (광물)
            new Node(UpgradeId.DoubleXpGoldChance,   G(8, 2),  UpgradeId.MoreXP),        // idx 5 - 위-왼 chain

            // ====================================================================================
            //   TIER 2 — Projectile chance. Pickaxe 자식 아래 chain.
            // ====================================================================================

            new Node(UpgradeId.BeamChance,           G(4, 4),  UpgradeId.ImprovedPickaxe), // idx 6
            new Node(UpgradeId.PlazmaChance,         G(2, 3),  UpgradeId.BeamChance),      // idx 7
            new Node(UpgradeId.DynamiteChance,       G(2, 4),  UpgradeId.BeamChance),      // idx 8
            new Node(UpgradeId.LightningChance,      G(2, 5),  UpgradeId.BeamChance),      // idx 9

            // ====================================================================================
            //   TIER 2 — Pickaxe / Material / Spawn / Area 각 sub-chain.
            // ====================================================================================

            // Pickaxe sub-chain (왼)
            new Node(UpgradeId.DoubleDamageChance,   G(6, 3),  UpgradeId.ImprovedPickaxe), // idx 10
            new Node(UpgradeId.InstaMineChance,      G(6, 2),  UpgradeId.DoubleDamageChance), // idx 11

            // SpawnMoreRocks sub-chain (위)
            new Node(UpgradeId.BiggerMiningArea,     G(10, 7), UpgradeId.MoreTime),       // idx 12 - 아래 root
            new Node(UpgradeId.RespawnRockChance,    G(11, 7), UpgradeId.BiggerMiningArea), // idx 13
            new Node(UpgradeId.RandomRockMineChance, G(12, 7), UpgradeId.RespawnRockChance), // idx 14

            // MoreTime time-bonus chain (center, 위 쪽)
            new Node(UpgradeId.RockBonusTimeChance,  G(11, 3), UpgradeId.MoreTime),       // idx 15
            new Node(UpgradeId.TickBonusTimeChance,  G(11, 2), UpgradeId.RockBonusTimeChance), // idx 16

            // MaterialsWorthMore sub-chain (오)
            new Node(UpgradeId.GoldBoost,            G(15, 3), UpgradeId.MaterialsWorthMore), // idx 17
            new Node(UpgradeId.CopperBoost,          G(15, 2), UpgradeId.GoldBoost),         // idx 18
            new Node(UpgradeId.IronBoost,            G(15, 1), UpgradeId.CopperBoost),       // idx 19

            // ====================================================================================
            //   TIER 2 — Projectile damage / size (20..29). Projectile chance chain 아래.
            // ====================================================================================

            new Node(UpgradeId.BeamDamage,           G(4, 3),  UpgradeId.BeamChance),     // idx 20
            new Node(UpgradeId.BeamSize,             G(4, 2),  UpgradeId.BeamDamage),     // idx 21
            new Node(UpgradeId.DynamiteDamage,       G(3, 5),  UpgradeId.DynamiteChance), // idx 22
            new Node(UpgradeId.DynamiteSize,         G(3, 6),  UpgradeId.DynamiteDamage), // idx 23
            new Node(UpgradeId.PlazmaDamage,         G(3, 3),  UpgradeId.PlazmaChance),   // idx 24
            new Node(UpgradeId.PlazmaSize,           G(3, 2),  UpgradeId.PlazmaDamage),   // idx 25
            new Node(UpgradeId.PlazmaLifetime,       G(3, 1),  UpgradeId.PlazmaSize),     // idx 26
            new Node(UpgradeId.LightningSize,        G(1, 5),  UpgradeId.LightningChance),// idx 27
            new Node(UpgradeId.AllProjectileDouble,  G(1, 6),  UpgradeId.LightningSize),  // idx 28
            new Node(UpgradeId.AllProjectileTrigger, G(1, 7),  UpgradeId.AllProjectileDouble), // idx 29

            // ====================================================================================
            //   TIER 3 — Cascade tails (30..36). Projectile chain 끝에.
            // ====================================================================================

            new Node(UpgradeId.CascadeBeamToDyna,    G(4, 1),  UpgradeId.BeamSize),       // idx 30
            new Node(UpgradeId.CascadeBeamToBeam,    G(5, 1),  UpgradeId.CascadeBeamToDyna), // idx 31
            new Node(UpgradeId.CascadeBeamToSplash,  G(5, 2),  UpgradeId.CascadeBeamToBeam), // idx 32
            new Node(UpgradeId.CascadeBeamTime,      G(5, 3),  UpgradeId.CascadeBeamToSplash), // idx 33
            new Node(UpgradeId.CascadeDynaToBeam,    G(4, 6),  UpgradeId.DynamiteSize),   // idx 34
            new Node(UpgradeId.CascadeDynaToDyna,    G(5, 6),  UpgradeId.CascadeDynaToBeam), // idx 35
            new Node(UpgradeId.CascadePlazmaToDyna,  G(3, 0),  UpgradeId.PlazmaLifetime), // idx 36 (G(4,1) CascadeBeamToDyna 와 좌표 충돌 회피)

            // ====================================================================================
            //   TIER 3 — Craft / Final / Talent (37, 59, 60). MaterialsWorthMore sub-chain.
            //   ⚠️ idx 37 ~ 71 entries 은 아래쪽 동일 순서 유지. 신규 (72~76) 는 마지막에 append.
            // ====================================================================================

            // 37 Craft2Material — MaterialsWorthMore 자식
            new Node(UpgradeId.Craft2Material,       G(14, 4), UpgradeId.MaterialsWorthMore), // idx 37

            // ====================================================================================
            //   TIER 3+ — Ore progression chains (38..58). Each ore = 3 vertical nodes.
            //   Right-side MaterialsWorthMore 자식 chain 으로 reparent.
            // ====================================================================================

            // Copper col 17, row 2..4
            new Node(UpgradeId.CopperSpawn,          G(17, 4), UpgradeId.MaterialsWorthMore), // idx 38
            new Node(UpgradeId.CopperChunk,          G(17, 3), UpgradeId.CopperSpawn),    // idx 39
            new Node(UpgradeId.CopperFull,           G(17, 2), UpgradeId.CopperChunk),    // idx 40

            // Iron col 17, row 5..7
            new Node(UpgradeId.IronSpawn,            G(17, 5), UpgradeId.CopperSpawn),    // idx 41
            new Node(UpgradeId.IronChunk,            G(17, 6), UpgradeId.IronSpawn),      // idx 42
            new Node(UpgradeId.IronFull,             G(17, 7), UpgradeId.IronChunk),      // idx 43

            // Cobalt col 18
            new Node(UpgradeId.CobaltSpawn,          G(18, 4), UpgradeId.IronSpawn),      // idx 44
            new Node(UpgradeId.CobaltChunk,          G(18, 3), UpgradeId.CobaltSpawn),    // idx 45
            new Node(UpgradeId.CobaltFull,           G(18, 2), UpgradeId.CobaltChunk),    // idx 46

            // Uranium col 18 row 5..7
            new Node(UpgradeId.UraniumSpawn,         G(18, 5), UpgradeId.CobaltSpawn),    // idx 47
            new Node(UpgradeId.UraniumChunk,         G(18, 6), UpgradeId.UraniumSpawn),   // idx 48
            new Node(UpgradeId.UraniumFull,          G(18, 7), UpgradeId.UraniumChunk),   // idx 49

            // Ismium col 19
            new Node(UpgradeId.IsmiumSpawn,          G(19, 4), UpgradeId.UraniumSpawn),   // idx 50
            new Node(UpgradeId.IsmiumChunk,          G(19, 3), UpgradeId.IsmiumSpawn),    // idx 51
            new Node(UpgradeId.IsmiumFull,           G(19, 2), UpgradeId.IsmiumChunk),    // idx 52

            // Iridium col 19 row 5..7
            new Node(UpgradeId.IridiumSpawn,         G(19, 5), UpgradeId.IsmiumSpawn),    // idx 53
            new Node(UpgradeId.IridiumChunk,         G(19, 6), UpgradeId.IridiumSpawn),   // idx 54
            new Node(UpgradeId.IridiumFull,          G(19, 7), UpgradeId.IridiumChunk),   // idx 55

            // Painite col 20
            new Node(UpgradeId.PainiteSpawn,         G(20, 4), UpgradeId.IridiumSpawn),   // idx 56
            new Node(UpgradeId.PainiteChunk,         G(20, 3), UpgradeId.PainiteSpawn),   // idx 57
            new Node(UpgradeId.PainiteFull,          G(20, 2), UpgradeId.PainiteChunk),   // idx 58

            // ====================================================================================
            //   Final / Talent (59..60). Painite chain 끝 + center-아래.
            // ====================================================================================

            new Node(UpgradeId.FinalUpgrade,         G(21, 4), UpgradeId.PainiteFull),    // idx 59
            new Node(UpgradeId.TalentPointsPerXlevel,G(13, 4), UpgradeId.MaterialsWorthMore), // idx 60

            // ====================================================================================
            //   Tick / Spawn variety (61..71). BiggerMiningArea (아래) sub-chain.
            // ====================================================================================

            new Node(UpgradeId.ChanceToSpawnRockWhenMined,  G(10, 8), UpgradeId.BiggerMiningArea), // idx 61
            new Node(UpgradeId.SpawnXRockEveryXSecond,      G(9, 8),  UpgradeId.ChanceToSpawnRockWhenMined), // idx 62
            new Node(UpgradeId.SpawnPickaxeEverySecond,     G(8, 8),  UpgradeId.SpawnXRockEveryXSecond), // idx 63
            new Node(UpgradeId.ShootCircleChance,           G(8, 7),  UpgradeId.SpawnPickaxeEverySecond), // idx 64
            new Node(UpgradeId.IncreaseAndDecreaseMinignErea,G(7, 7), UpgradeId.ShootCircleChance), // idx 65
            new Node(UpgradeId.IncreaseSpawnChanceAllRocks, G(7, 6),  UpgradeId.IncreaseAndDecreaseMinignErea), // idx 66
            new Node(UpgradeId.ChanceAdd1SecondEveryRockMined,G(11, 8),UpgradeId.BiggerMiningArea), // idx 67
            new Node(UpgradeId.ChanceToAdd1SecondEverySecond,G(12, 8),UpgradeId.ChanceAdd1SecondEveryRockMined), // idx 68
            new Node(UpgradeId.FullOreChance,               G(13, 8), UpgradeId.ChanceToAdd1SecondEverySecond), // idx 69
            new Node(UpgradeId.SpawnRockEveryXrock,         G(13, 7), UpgradeId.FullOreChance), // idx 70
            new Node(UpgradeId.MoreMaterialsFromRock,       G(16, 4), UpgradeId.MaterialsWorthMore), // idx 71

            // ====================================================================================
            //   잔여 cascade 4종 + PlazmaSpawnPickaxe (72..76).
            //   Plazma chain 위 (col 4-6 row 0), Dyna chain 옆 (col 6 row 6), Lightning chain 끝 (col 0 row 7).
            // ====================================================================================

            new Node(UpgradeId.CascadePlazmaToPlasmaBall, G(4, 0), UpgradeId.CascadePlazmaToDyna),    // idx 72 - Plazma 연쇄
            new Node(UpgradeId.CascadeDynaToSplash,       G(6, 6), UpgradeId.CascadeDynaToDyna),     // idx 73 - Dyna → Splash
            new Node(UpgradeId.CascadePlazmaToSplash,     G(5, 0), UpgradeId.CascadePlazmaToPlasmaBall), // idx 74 - Plazma → Splash
            new Node(UpgradeId.CascadeLightningToDyna,    G(0, 7), UpgradeId.AllProjectileTrigger),  // idx 75 - Lightning → Dyna
            new Node(UpgradeId.PlazmaSpawnPickaxe,        G(6, 0), UpgradeId.CascadePlazmaToSplash), // idx 76 - 원본 plazmaBallSpawnPickaxeChance
        };

        /// <summary>Linear lookup — 72 entries.</summary>
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
