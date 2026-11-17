using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Profiling;

namespace Rebuild.Mining
{





    public sealed class RockSpawner : MonoBehaviour
    {
        static readonly ProfilerMarker s_spawnMarker = new ProfilerMarker("Atoria.Rock.Spawn");
        public static RockSpawner Instance { get; private set; }

        [Header("Spawn count")]
        [SerializeField] int baseTotalRocksToSpawn = 25;

        [Header("Rock visual")]
        [SerializeField] Vector2 rockSizeDelta = new Vector2(220f, 220f);
        [SerializeField] int rockHp = 10;





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
        readonly Dictionary<RectTransform, List<Rock>> _rocksByRow = new Dictionary<RectTransform, List<Rock>>();
        readonly Dictionary<(Sprite sprite, bool preserveAspect), (Vector2 pivot, float scale)> _rockFits = new();



        public bool GoldenChestSpawnedThisSession { get; private set; }
        public void ResetGoldenChestFlag() { GoldenChestSpawnedThisSession = false; }
        readonly Rock[] _cells = new Rock[TileGrid.Rows * TileGrid.Cols];
        RectTransform[] _rows;
        Vector2 _cachedTileStart;
        Vector2 _cachedTileStep;
        Vector2 _cachedTileAcross;
        int _cachedTilesPerRow;

        public int ActiveCount => _activeRocks.Count;

        void Awake() { Instance = this; }
        void OnDestroy() { if (Instance == this) Instance = null; }

        internal IReadOnlyList<Rock> RocksInRow(RectTransform row)
        {
            if (row != null && _rocksByRow.TryGetValue(row, out var rocks)) return rocks;
            return System.Array.Empty<Rock>();
        }

        internal void RemoveFromRow(Rock rock, RectTransform row)
        {
            if (row != null && _rocksByRow.TryGetValue(row, out var rocks)) rocks.Remove(rock);
            ReleaseCell(rock);
        }

        bool ReleaseCell(Rock rock)
        {
            int cell = rock.GridCell;
            if (cell < 0 || !ReferenceEquals(_cells[cell], rock)) return false;
            _cells[cell] = null;
            return true;
        }

        internal void CompleteBreak(Rock rock, bool respawnInRow)
        {
            if (!ReleaseCell(rock)) return;
            var session = MiningSessionController.Instance;
            if (session == null || !session.TimerRunning || session.ResultsShown) return;

            if (rock.HasReservedRock)
                SpawnAtCell(rock.GridCell, allowReserve: false);
            if (respawnInRow)
                SpawnOneInRow(_rows[rock.GridCell / _cachedTilesPerRow]);
        }

        public void SpawnAll(TileGrid grid)
        {
            ClearAll();
            _rows = grid.RowContainers;
            if (_rows == null) return;

            _cachedTileStart = grid.TileStartInRow;
            _cachedTileStep = grid.TileStepInRow;
            _cachedTileAcross = grid.TileAcrossInRow;
            _cachedTilesPerRow = grid.TilesPerRow;
            _rockFits.Clear();

            int rowCount = _rows.Length;
            if (rowCount == 0) return;
            int totalRocks = Mathf.Min(ComputeTotalRocksToSpawn(), rowCount * _cachedTilesPerRow);
            int basePerRow = totalRocks / rowCount;
            int remainder = totalRocks % rowCount;
            for (int i = 0; i < rowCount; i++)
            {
                int count = basePerRow;
                if (remainder > 0 && Random.Range(0, rowCount - i) < remainder)
                {
                    count++;
                    remainder--;
                }
                foreach (int column in PickUniqueIndices(_cachedTilesPerRow, count))
                    SpawnAtCell(i * _cachedTilesPerRow + column);
            }

            if (ArtifactsManager.Instance != null && ArtifactsManager.Instance.HasArtifact(ArtifactId.Skull))
                for (int i = 0; i < 10; i++) SpawnOneInEmptyCell();
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
            _rocksByRow.Clear();
            System.Array.Clear(_cells, 0, _cells.Length);
            _rows = null;
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

        Rock SpawnOne(RectTransform row, Vector2 anchored, Sprite sprite)
        {
            using var spawnSample = s_spawnMarker.Auto();
            var go = new GameObject("Rock", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(row, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = rockSizeDelta;

            var ore = RollOre();
            var tier = PickTier(ore);

            var chest = PickChestVariant();
            var img = go.GetComponent<Image>();
            img.raycastTarget = true;
            img.preserveAspect = false;

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


                var visual = SpriteLibrary.GetRockVisual(ore, tier);
                img.sprite = visual.Sprite != null ? visual.Sprite : sprite;
                img.color = visual.ShouldTint ? visual.TintColor : Color.white;
            }








            float hp = rockHp * RockTierDef.HpMultiplier(tier);
            if (ArtifactsManager.Instance != null && ArtifactsManager.Instance.HasArtifact(ArtifactId.Horn))
            {
                float decrease = 0.03f * ArtifactsManager.Instance.RuneMultiplier;

                if (LevelMechanics.Instance != null && LevelMechanics.Instance.HasTalent(TalentId.Archaeologist))
                    decrease *= 1.08f;
                hp = Mathf.Max(1f, hp * (1f - decrease));
            }
            var rock = go.AddComponent<Rock>();
            rock.Initialize(ore, hp, tier, chest);
            FitRockToCell(img);
            rt.anchoredPosition = anchored;
            if (chest == Rock.ChestKind.Golden) GoldenChestSpawnedThisSession = true;





            if (ArtifactsManager.Instance != null)
            {
                var preRolled = ArtifactsManager.Instance.TryRollOnRockSpawn();
                if (preRolled.HasValue) rock.AssignArtifact(preRolled.Value);
            }

            _activeRocks.Add(go);
            if (!_rocksByRow.TryGetValue(row, out var rocks))
            {
                rocks = new List<Rock>();
                _rocksByRow.Add(row, rocks);
            }
            rocks.Add(rock);

            Rebuild.Stats.StatsManager.Instance?.RecordRockSpawned(1);
            return rock;
        }

        void FitRockToCell(Image image)
        {
            if (image.sprite == null || _cachedTilesPerRow == 0) return;

            var key = (image.sprite, image.preserveAspect);
            if (!_rockFits.TryGetValue(key, out var fit))
            {
                var sprite = image.sprite;
                var spriteSize = sprite.rect.size;
                var drawSize = rockSizeDelta;
                if (image.preserveAspect)
                {
                    float ratio = Mathf.Min(drawSize.x / spriteSize.x, drawSize.y / spriteSize.y);
                    drawSize = spriteSize * ratio;
                }

                var along = _cachedTileStep;
                var across = _cachedTileAcross;
                float determinant = along.x * across.y - along.y * across.x;
                var min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
                var max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
                foreach (var vertex in sprite.vertices)
                {
                    var p = Vector2.Scale((vertex * sprite.pixelsPerUnit + sprite.pivot) / spriteSize, drawSize);
                    var cell = new Vector2(p.x * across.y - p.y * across.x, along.x * p.y - along.y * p.x) / determinant;
                    min = Vector2.Min(min, cell);
                    max = Vector2.Max(max, cell);
                }


                var center = (min + max) * .5f;
                var extent = (max - min) * .5f;
                fit = ((along * center.x + across * center.y) / drawSize, .435f / Mathf.Max(extent.x, extent.y));
                _rockFits.Add(key, fit);
            }

            var rt = image.rectTransform;
            rt.pivot = fit.pivot;

            rt.sizeDelta = rockSizeDelta * Mathf.Min(1f, fit.scale / rt.localScale.x);
        }







        static Rock.ChestKind PickChestVariant()
        {
            var lm = LevelMechanics.Instance;
            if (lm == null) return Rock.ChestKind.None;


            if (lm.HasTalent(TalentId.GoldenChests)
                && Instance != null && !Instance.GoldenChestSpawnedThisSession
                && D100Helper.Roll(0.005f))
            {
                return Rock.ChestKind.Golden;
            }

            if (lm.HasTalent(TalentId.Chests) && D100Helper.Roll(0.006f))
            {
                return Rock.ChestKind.Chest;
            }
            return Rock.ChestKind.None;
        }







        static RockTier PickTier(OreType ore)
        {
            var sk = SkillTreeManager.Instance;
            if (sk == null) return RockTier.Normal;
            int chunkLv = 0, fullLv = 0;
            switch (ore)
            {
                case OreType.Gold: chunkLv = 0; fullLv = 0; break;
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

            float r = Random.value;
            if (r < fullChance) return RockTier.Full;
            if (r < fullChance + chunkChance) return RockTier.Chunk;
            return RockTier.Normal;
        }

        static OreType RollOre()
        {




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

        public void SpawnOneInEmptyCell()
        {
            if (_rows != null)
                SpawnInRange(0, _rows.Length * _cachedTilesPerRow);
        }

        public void SpawnOneInRow(RectTransform row)
        {
            if (row == null || _rows == null) return;
            int index = System.Array.IndexOf(_rows, row);
            if (index >= 0) SpawnInRange(index * _cachedTilesPerRow, _cachedTilesPerRow);
        }

        void SpawnInRange(int start, int count)
        {
            int available = 0;
            int end = start + count;
            for (int i = start; i < end; i++)
                if (_cells[i] == null) available++;
            if (available == 0) return;

            int pick = Random.Range(0, available);
            for (int i = start; i < end; i++)
            {
                if (_cells[i] != null) continue;
                if (pick == 0)
                {
                    SpawnAtCell(i);
                    return;
                }
                pick--;
            }
        }

        void SpawnAtCell(int cell, bool allowReserve = true)
        {
            var row = _rows[cell / _cachedTilesPerRow];
            int column = cell % _cachedTilesPerRow;
            var rock = SpawnOne(row, _cachedTileStart + _cachedTileStep * column, RockSpriteFactory.GetRockSprite());
            rock.GridCell = cell;
            _cells[cell] = rock;

            int rank = SkillTreeManager.Instance != null
                ? SkillTreeManager.Instance.GetLevel(UpgradeId.ChanceToSpawnRockWhenMined) : 0;
            float chance = UpgradeDef.GetAdditionalRockSpawnChance(rank);
            if (allowReserve && chance > 0f && Random.value < chance)
                rock.ShowReservedRockBadge();
        }

        static List<int> PickUniqueIndices(int max, int count)
        {

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
