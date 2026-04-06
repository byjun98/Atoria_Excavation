using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Rebuild.Mining
{
    /// <summary>
    /// placeholder rock spawner. Picks random tile spots in each TileGrid row
    /// and instantiates simple Image-based rocks with weighted-random ore type tints.
    /// Real RockMechanics (HP, full/chunk/broken visuals, collider) lands in .
    /// </summary>
    public sealed class RockSpawner : MonoBehaviour
    {
        public static RockSpawner Instance { get; private set; }

        [Header("Spawn count")]
        [SerializeField] int baseTotalRocksToSpawn = 25;

        [Header("Rock visual")]
        [SerializeField] Vector2 rockSizeDelta = new Vector2(220f, 220f);
        [SerializeField] int rockHp = 10;

        // (type, weight). Higher weight = more common. Total can be any number; weighted-pick handles it.
        // progressive unlock. 시작 시 Gold 만 spawn (weight 50), 나머지는
        // SkillTree 의 `*Spawn` 노드 구매 시점에 활성 (+25 weight via WeightBoostFor).
        // 원본 게임 사진 2 첫 세션 = Gold 만 보이는 동작 매칭.
        static readonly (OreType type, int weight)[] OreWeights =
        {
            (OreType.Gold,    50),
            (OreType.Copper,   0),
            (OreType.Iron,     0),
            (OreType.Cobalt,   0),
            (OreType.Uranium,  0),
            (OreType.Ismium,   0),
            (OreType.Iridium,  0),
            (OreType.Painite,  0),
        };

        static readonly int[] SpawnMoreRocksIncreases = { 4, 5, 7, 8, 9, 9, 12, 15, 16 };

        readonly List<GameObject> _activeRocks = new List<GameObject>();

        // 세션당 GoldenChest 1개 제한 (원본 SetRockScreen.spawnedGoldenChest 패턴).
        // MiningSessionController.ApplyEnterSession 이 매 세션 시작 시 false 로 리셋.
        public bool GoldenChestSpawnedThisSession { get; private set; }
        public void ResetGoldenChestFlag() { GoldenChestSpawnedThisSession = false; }
        // Bonus spawns reuse the latest row offsets. Unlike the initial session layout, they do
        // not reserve tile slots: original SpawnRockCount just picked a random row position and
        // allowed rocks to overlap.
        Vector2 _cachedTileStart;
        Vector2 _cachedTileStep;
        int _cachedTilesPerRow;

        public int ActiveCount => _activeRocks.Count;

        void Awake() { Instance = this; }
        void OnDestroy() { if (Instance == this) Instance = null; }

        public void SpawnAll(RectTransform[] rowContainers, Vector2 tileStartInRow, Vector2 tileStepInRow, int tilesPerRow)
        {
            ClearAll();
            if (rowContainers == null) return;
            // Cache layout for SpawnOneInRow (RespawnRockChance hook).
            _cachedTileStart = tileStartInRow;
            _cachedTileStep = tileStepInRow;
            _cachedTilesPerRow = tilesPerRow;

            int totalRocks = ComputeTotalRocksToSpawn();
            int rowCount = rowContainers.Length;
            float perRow = rowCount > 0 ? (float)totalRocks / rowCount : 0f;
            int basePerRow = Mathf.FloorToInt(perRow);
            float fractional = perRow - basePerRow;
            int remainder = rowCount > 0 ? totalRocks % rowCount : 0;
            int extrasAdded = 0;
            var sprite = RockSpriteFactory.GetRockSprite();
            for (int i = 0; i < rowContainers.Length; i++)
            {
                var row = rowContainers[i];
                if (row == null) continue;

                int n = basePerRow;
                if (extrasAdded < remainder && Random.Range(0f, 1f) <= fractional)
                {
                    n += 1;
                    extrasAdded++;
                }
                if (i == rowContainers.Length - 1 && extrasAdded < remainder)
                    n += remainder - extrasAdded;
                n = Mathf.Clamp(n, 0, tilesPerRow);
                var picks = PickUniqueIndices(tilesPerRow, n);
                foreach (var j in picks)
                {
                    var anchored = tileStartInRow + new Vector2(tileStepInRow.x * j, tileStepInRow.y * j);
                    SpawnOne(row, anchored, sprite);
                }
            }
            // Skull artifact: 세션당 +10 bonus rocks 분산. 원본
            // RockMechanics.cs:910 `SkillTree.totalRocksToSpawn += 10` 매칭. base SpawnAll 종료 후
            // 무작위 row 에 SpawnOneInRow 10번 호출. 원본처럼 겹쳐도 spawn.
            if (ArtifactsManager.Instance != null && ArtifactsManager.Instance.HasArtifact(ArtifactId.Skull))
            {
                for (int s = 0; s < 10; s++)
                {
                    var row = rowContainers[Random.Range(0, rowContainers.Length)];
                    if (row != null) SpawnOneInRow(row);
                }
            }
        }

        int ComputeTotalRocksToSpawn()
        {
            int total = Mathf.Max(0, baseTotalRocksToSpawn);
            var sk = SkillTreeManager.Instance;
            if (sk == null) return total;

            int spawnMoreLv = Mathf.Clamp(sk.GetLevel(UpgradeId.SpawnMoreRocks), 0, SpawnMoreRocksIncreases.Length);
            for (int i = 0; i < spawnMoreLv; i++)
                total += SpawnMoreRocksIncreases[i];

            int spawnAllLv = sk.GetLevel(UpgradeId.IncreaseSpawnChanceAllRocks);
            if (spawnAllLv > 0)
                total += Mathf.CeilToInt(total * 0.5f * spawnAllLv);

            return total;
        }

        public void ClearAll()
        {
            for (int i = 0; i < _activeRocks.Count; i++)
            {
                var go = _activeRocks[i];
                if (go == null) continue;
                if (Application.isPlaying) Destroy(go); else DestroyImmediate(go);
            }
            _activeRocks.Clear();
        }

        public void DisableInteraction()
        {
            for (int i = 0; i < _activeRocks.Count; i++)
            {
                var go = _activeRocks[i];
                if (go == null) continue;
                var img = go.GetComponent<Image>();
                if (img != null) img.raycastTarget = false;
            }
        }

        public void EnableInteraction()
        {
            for (int i = 0; i < _activeRocks.Count; i++)
            {
                var go = _activeRocks[i];
                if (go == null) continue;
                var img = go.GetComponent<Image>();
                if (img != null) img.raycastTarget = true;
            }
        }

        void SpawnOne(RectTransform row, Vector2 anchored, Sprite sprite)
        {
            var go = new GameObject("Rock", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(row, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchored;
            rt.sizeDelta = rockSizeDelta;

            var ore = RollOre();
            var tier = PickTier(ore);
            // Chest variant 결정 (chests/goldenChests talent 활성 시 일정 확률).
            var chest = PickChestVariant();
            var img = go.GetComponent<Image>();
            img.raycastTarget = true; // clickable
            img.preserveAspect = false;
            // Chest variant 우선 sprite (있을 때). 없으면 ore tier visual fallback.
            Sprite chestSprite = null;
            if (chest == Rock.ChestKind.Chest) chestSprite = SpriteLibrary.GetChestRockSprite();
            else if (chest == Rock.ChestKind.Golden) chestSprite = SpriteLibrary.GetGoldenChestRockSprite();
            if (chestSprite != null)
            {
                img.sprite = chestSprite;
                img.color = Color.white;
                img.preserveAspect = true;
            }
            else
            {
                // SpriteLibrary picks the best available visual.
                // Tier 별 sprite (Sprites/Ore/{Chunk|Full}/{Ore}.png) 우선 시도, fallback Normal.
                var visual = SpriteLibrary.GetRockVisual(ore, tier);
                img.sprite = visual.Sprite != null ? visual.Sprite : sprite;
                img.color = visual.ShouldTint ? visual.TintColor : Color.white;
            }

            // artifact: Horn reduces rock maxHp by 3% (multiplier).
            // Horn 의 0.03f 에 Rune universal multiplier 체인 적용. 원본
            // RockMechanics.cs:276-296: `num2 *= 1f + runeImproveAll` (Rune 보유 시 -3.15%, 미보유 시 -3%).
            // Archaeologist HP-1 flat 제거. 사용자 결정: 원본의 archeologistIncrease
            // (0.08) multi-site multiplier 모델로 대체 (Cheese / Axe / WolfClaw / AncientDevice / PurpleRing /
            // Dice / Horn 의 +8% strength). Horn HP-decrease 도 Archaeologist 보유 시 ×(1+0.08) 추가 cut
            // — 원본 RockMechanics:279~287 매칭.
            int hp = rockHp * RockTierDef.HpMultiplier(tier); // tier 별 HP 배수.
            if (ArtifactsManager.Instance != null && ArtifactsManager.Instance.HasArtifact(ArtifactId.Horn))
            {
                float decrease = 0.03f * ArtifactsManager.Instance.RuneMultiplier;
                // Archaeologist 가 horn-decrease 를 8% 강화 (원본 archeologistIncreaseDISPLAY).
                if (LevelMechanics.Instance != null && LevelMechanics.Instance.HasTalent(TalentId.Archaeologist))
                    decrease *= 1.08f;
                hp = Mathf.Max(1, Mathf.RoundToInt(hp * (1f - decrease)));
            }
            var rock = go.AddComponent<Rock>();
            rock.Initialize(ore, hp, tier, chest);
            if (chest == Rock.ChestKind.Golden) GoldenChestSpawnedThisSession = true;

            // pre-spawn artifact roll. 원본 SetRockScreen.SpawnRockCount 직후
            // `artifactsScript.ArtifactDropChances()` 호출 매칭. ArtifactsManager 가 _oneArtifactPreRolledThisSession
            // 으로 세션당 1개만 박힘 보장. 성공 시 rock 에 AssignArtifact → child overlay sprite 부착
            // (50% HP 도달 시 노출). Chest variant 일 때는 AssignArtifact 가 자체 skip.
            if (ArtifactsManager.Instance != null)
            {
                var preRolled = ArtifactsManager.Instance.TryRollOnRockSpawn();
                if (preRolled.HasValue) rock.AssignArtifact(preRolled.Value);
            }

            _activeRocks.Add(go);
            // stat — every spawn (initial + RespawnRockChance respawn).
            Rebuild.Stats.StatsManager.Instance?.RecordRockSpawned(1);
        }

        /// <summary>Chest variant 결정. 원본 RockMechanics.cs:512~527 모델 매칭.
        /// 원본 수치 매칭:
        /// - <c>chests_chosen</c> talent + 0.6% chance (원본 rockSpawnChance=0.6f, percentage 단위)
        ///   → Chest variant → 부서지면 +25 ore (Rock.cs FadeAndDestroy)
        /// - <c>goldenChests_chosen</c> talent + 0.5% + 세션당 1회 → Golden variant → +50 Gold
        /// </summary>
        static Rock.ChestKind PickChestVariant()
        {
            var lm = LevelMechanics.Instance;
            if (lm == null) return Rock.ChestKind.None;

            // GoldenChest 가 더 희귀 — 먼저 roll.
            if (lm.HasTalent(TalentId.GoldenChests)
                && Instance != null && !Instance.GoldenChestSpawnedThisSession
                && D100Helper.Roll(0.005f))
            {
                return Rock.ChestKind.Golden;
            }
            // 0.04 → 0.006 (원본 rockSpawnChance=0.6f).
            if (lm.HasTalent(TalentId.Chests) && D100Helper.Roll(0.006f))
            {
                return Rock.ChestKind.Chest;
            }
            return Rock.ChestKind.None;
        }

        /// <summary>Tier 결정. SkillTree 의 {ore}Chunk / {ore}Full 레벨이 확률을 결정.
        /// 각 Chunk lv = +10% Chunk 확률, 각 Full lv = +5% Full 확률. 합산 80% 초과시 cap.
        /// 미구매 시 항상 Normal.
        /// <para>.D — Meteorite artifact 의 effect 가 여기로 이동: 원본 RockMechanics.cs:846-857
        /// 의 `fullXRockChance *= (1 + 0.03)` on 8 ores 매칭. Rune 의 secondary `fullXRockChance *=
        /// (1 + 0.03 × runeImproveAll)` (RockMechanics.cs:897-905, 0.03 × 0.05 = 0.0015) 도 같이 적용.</para></summary>
        static RockTier PickTier(OreType ore)
        {
            var sk = SkillTreeManager.Instance;
            if (sk == null) return RockTier.Normal;
            int chunkLv = 0, fullLv = 0;
            switch (ore)
            {
                case OreType.Gold: chunkLv = 0; fullLv = 0; break; // Gold 는 Chunk/Full 노드 없음 (base).
                case OreType.Copper:  chunkLv = sk.GetLevel(UpgradeId.CopperChunk);  fullLv = sk.GetLevel(UpgradeId.CopperFull);  break;
                case OreType.Iron:    chunkLv = sk.GetLevel(UpgradeId.IronChunk);    fullLv = sk.GetLevel(UpgradeId.IronFull);    break;
                case OreType.Cobalt:  chunkLv = sk.GetLevel(UpgradeId.CobaltChunk);  fullLv = sk.GetLevel(UpgradeId.CobaltFull);  break;
                case OreType.Uranium: chunkLv = sk.GetLevel(UpgradeId.UraniumChunk); fullLv = sk.GetLevel(UpgradeId.UraniumFull); break;
                case OreType.Ismium:  chunkLv = sk.GetLevel(UpgradeId.IsmiumChunk);  fullLv = sk.GetLevel(UpgradeId.IsmiumFull);  break;
                case OreType.Iridium: chunkLv = sk.GetLevel(UpgradeId.IridiumChunk); fullLv = sk.GetLevel(UpgradeId.IridiumFull); break;
                case OreType.Painite: chunkLv = sk.GetLevel(UpgradeId.PainiteChunk); fullLv = sk.GetLevel(UpgradeId.PainiteFull); break;
            }
            float chunkChance = Mathf.Min(0.5f, chunkLv * 0.10f);
            float fullChance  = Mathf.Min(0.3f, fullLv  * 0.05f);
            // Meteorite + Rune full-chance multiplier (원본 RockMechanics.cs:849-905).
            // Meteorite: ×(1+0.03), Rune: ×(1+0.0015). Stack multiplicatively to match origin.
            if (ArtifactsManager.Instance != null)
            {
                float fullMult = 1f;
                if (ArtifactsManager.Instance.HasArtifact(ArtifactId.Meteorite))
                    fullMult *= 1.03f;
                if (ArtifactsManager.Instance.HasArtifact(ArtifactId.Rune))
                    fullMult *= 1.0015f;
                if (fullMult > 1f)
                    fullChance = Mathf.Min(0.3f, fullChance * fullMult);
            }
            // Full 먼저 roll (드물고 보상 큰 tier 우선).
            float r = Random.value;
            if (r < fullChance) return RockTier.Full;
            if (r < fullChance + chunkChance) return RockTier.Chunk;
            return RockTier.Normal;
        }

        static OreType RollOre()
        {
            // SkillTree ore-boost nodes bump the weight of Gold / Copper / Iron.
            // Spawn nodes for Copper/Iron/Cobalt/Uranium/Ismium/Iridium/Painite each
            // add +25 weight when purchased (1 level). Base weights stay (so Gold/Copper/Iron
            // remain spawnable from session 1) — Spawn just amplifies the late-game ore presence.
            int goldBoost = 0, copperBoost = 0, ironBoost = 0;
            int copperSpawn = 0, ironSpawn = 0, cobaltSpawn = 0, uraniumSpawn = 0;
            int ismiumSpawn = 0, iridiumSpawn = 0, painiteSpawn = 0;
            if (SkillTreeManager.Instance != null)
            {
                var sk = SkillTreeManager.Instance;
                goldBoost   = sk.GetLevel(UpgradeId.GoldBoost)   * 5;
                copperBoost = sk.GetLevel(UpgradeId.CopperBoost) * 3;
                ironBoost   = sk.GetLevel(UpgradeId.IronBoost)   * 2;
                if (sk.GetLevel(UpgradeId.CopperSpawn) > 0)  copperSpawn = 25;
                if (sk.GetLevel(UpgradeId.IronSpawn) > 0)    ironSpawn = 25;
                if (sk.GetLevel(UpgradeId.CobaltSpawn) > 0)  cobaltSpawn = 25;
                if (sk.GetLevel(UpgradeId.UraniumSpawn) > 0) uraniumSpawn = 25;
                if (sk.GetLevel(UpgradeId.IsmiumSpawn) > 0)  ismiumSpawn = 25;
                if (sk.GetLevel(UpgradeId.IridiumSpawn) > 0) iridiumSpawn = 25;
                if (sk.GetLevel(UpgradeId.PainiteSpawn) > 0) painiteSpawn = 25;
            }
            int total = 0;
            for (int i = 0; i < OreWeights.Length; i++)
            {
                int w = OreWeights[i].weight + WeightBoostFor(OreWeights[i].type,
                    goldBoost, copperBoost, ironBoost,
                    copperSpawn, ironSpawn, cobaltSpawn, uraniumSpawn,
                    ismiumSpawn, iridiumSpawn, painiteSpawn);
                total += w;
            }
            int r = Random.Range(0, total);
            int acc = 0;
            for (int i = 0; i < OreWeights.Length; i++)
            {
                int w = OreWeights[i].weight + WeightBoostFor(OreWeights[i].type,
                    goldBoost, copperBoost, ironBoost,
                    copperSpawn, ironSpawn, cobaltSpawn, uraniumSpawn,
                    ismiumSpawn, iridiumSpawn, painiteSpawn);
                acc += w;
                if (r < acc) return OreWeights[i].type;
            }
            return OreType.Gold;
        }

        static int WeightBoostFor(OreType ore,
            int gold, int copper, int iron,
            int copperSpawn, int ironSpawn, int cobaltSpawn, int uraniumSpawn,
            int ismiumSpawn, int iridiumSpawn, int painiteSpawn)
        {
            switch (ore)
            {
                case OreType.Gold:    return gold;
                case OreType.Copper:  return copper + copperSpawn;
                case OreType.Iron:    return iron + ironSpawn;
                case OreType.Cobalt:  return cobaltSpawn;
                case OreType.Uranium: return uraniumSpawn;
                case OreType.Ismium:  return ismiumSpawn;
                case OreType.Iridium: return iridiumSpawn;
                case OreType.Painite: return painiteSpawn;
                default:              return 0;
            }
        }

        /// <summary>per-ore Chunk+Full bonus drop count. Each level of the
        /// matching Chunk/Full node adds +1 ore drop. Called by Rock.FadeAndDestroy.
        /// Returns 0 if SkillTreeManager / matching nodes unavailable.</summary>
        public static int GetOreDropBonus(OreType ore)
        {
            var sk = SkillTreeManager.Instance;
            if (sk == null) return 0;
            switch (ore)
            {
                case OreType.Copper:  return sk.GetLevel(UpgradeId.CopperChunk)  + sk.GetLevel(UpgradeId.CopperFull);
                case OreType.Iron:    return sk.GetLevel(UpgradeId.IronChunk)    + sk.GetLevel(UpgradeId.IronFull);
                case OreType.Cobalt:  return sk.GetLevel(UpgradeId.CobaltChunk)  + sk.GetLevel(UpgradeId.CobaltFull);
                case OreType.Uranium: return sk.GetLevel(UpgradeId.UraniumChunk) + sk.GetLevel(UpgradeId.UraniumFull);
                case OreType.Ismium:  return sk.GetLevel(UpgradeId.IsmiumChunk)  + sk.GetLevel(UpgradeId.IsmiumFull);
                case OreType.Iridium: return sk.GetLevel(UpgradeId.IridiumChunk) + sk.GetLevel(UpgradeId.IridiumFull);
                case OreType.Painite: return sk.GetLevel(UpgradeId.PainiteChunk) + sk.GetLevel(UpgradeId.PainiteFull);
                default:              return 0;
            }
        }

        /// <summary>bonus spawn. Matches original <c>SetRockScreen.SpawnRockCount(1)</c>:
        /// pick a random position in the row and spawn even if another rock already occupies it.</summary>
        public void SpawnOneInRow(RectTransform row)
        {
            if (row == null || _cachedTilesPerRow <= 0) return;
            float slot = _cachedTilesPerRow <= 1 ? 0f : Random.Range(0f, _cachedTilesPerRow - 1f);
            var anchored = _cachedTileStart + _cachedTileStep * slot;

            // Original used a small random local offset per spawned rock. Keep it subtle so the
            // isometric row still reads correctly, while stacked rocks do not land pixel-perfect.
            float jitterX = Mathf.Abs(_cachedTileStep.x) * 0.10f;
            float jitterY = Mathf.Abs(_cachedTileStep.y) * 0.10f;
            anchored += new Vector2(Random.Range(-jitterX, jitterX), Random.Range(-jitterY, jitterY));
            SpawnOne(row, anchored, RockSpriteFactory.GetRockSprite());
        }

        static List<int> PickUniqueIndices(int max, int count)
        {
            // Fisher–Yates-style partial shuffle, returning `count` unique indices in [0, max).
            var pool = new List<int>(max);
            for (int i = 0; i < max; i++) pool.Add(i);
            var result = new List<int>(count);
            for (int i = 0; i < count && pool.Count > 0; i++)
            {
                int idx = Random.Range(0, pool.Count);
                result.Add(pool[idx]);
                pool.RemoveAt(idx);
            }
            return result;
        }
    }
}
