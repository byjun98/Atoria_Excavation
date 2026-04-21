using System;
// Don't `using Rebuild.Mining;` — Assembly-CSharp has a global `OreType` that wins type
// resolution over Rebuild.Mining.OreType when both namespaces are imported. We reference
// Rebuild.Mining.OreType fully qualified everywhere instead (same pattern as + setups).
using Rebuild.Saving;
using UnityEngine;

namespace Rebuild.Stats
{
    /// <summary>counter aggregator. Singleton on MiningSessionRoot.
    /// All gameplay managers call into the RecordX helpers; no other system writes <see cref="_values"/> directly.
    /// AchievementsManager subscribes to <see cref="OnStatChanged"/> to evaluate milestones.
    /// Persists the 16-long array through <see cref="GameDataDTO.stats"/> ().</summary>
    public sealed class StatsManager : MonoBehaviour, Rebuild.Saving.IDataPersistence
    {
        public static StatsManager Instance { get; private set; }

        [SerializeField] long[] _values = new long[StatDef.Count];

        public event Action<StatId, long> OnStatChanged;

        public long Get(StatId id) => _values[(int)id];
        public long this[StatId id] => _values[(int)id];

        void Awake()
        {
            if (_values == null || _values.Length != StatDef.Count) _values = new long[StatDef.Count];
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void Add(StatId id, long delta)
        {
            if (delta == 0) return;
            int idx = (int)id;
            _values[idx] += delta;
            if (_values[idx] < 0) _values[idx] = 0;
            OnStatChanged?.Invoke(id, _values[idx]);
        }

        public void SetMax(StatId id, long candidate)
        {
            int idx = (int)id;
            if (candidate <= _values[idx]) return;
            _values[idx] = candidate;
            OnStatChanged?.Invoke(id, _values[idx]);
        }

        // ---- Recorder helpers — single hook point per gameplay event. ----
        public void RecordClick() => Add(StatId.ClicksTotal, 1);

        public void RecordRockDestroyed(Rebuild.Mining.OreType ore, long oreGained)
        {
            Add(StatId.RocksDestroyed, 1);
            if (oreGained > 0)
            {
                Add(StatDef.OreMinedStat(ore), oreGained);
                // collapsed total across all 8 ore types for the "Total Ore Mined" stat.
                Add(StatId.OresMined_Total, oreGained);
            }
        }

        public void RecordSessionCompleted() => Add(StatId.SessionsCompleted, 1);

        public void RecordProjectileFired(int spawned)
        {
            if (spawned > 0) Add(StatId.ProjectilesFired, spawned);
        }

        /// <summary>per-type breakdown of ProjectilesFired (also bumps the umbrella
        /// counter so existing UI stays consistent). Callers in ProjectileLauncher invoke this
        /// once per successful spawn after the type is known.</summary>
        public void RecordProjectileFired(Rebuild.Mining.ProjectileType type, int count = 1)
        {
            if (count <= 0) return;
            Add(StatId.ProjectilesFired, count);
            var split = type switch
            {
                Rebuild.Mining.ProjectileType.BeamOfLight       => StatId.ProjectilesFired_Beam,
                Rebuild.Mining.ProjectileType.PlazmaBall        => StatId.ProjectilesFired_Plazma,
                Rebuild.Mining.ProjectileType.Dynamite          => StatId.ProjectilesFired_Dynamite,
                Rebuild.Mining.ProjectileType.ElectricitySplash => StatId.ProjectilesFired_Lightning,
                _ => StatId.ProjectilesFired,
            };
            if (split != StatId.ProjectilesFired) Add(split, count);
        }

        public void RecordFlowerClicked() => Add(StatId.FlowersClicked, 1);

        public void RecordArtifactFound()
        {
            Add(StatId.ArtifactsFoundCount, 1);
        }

        public void RecordPickaxeCrafted() => Add(StatId.PickaxesCraftedCount, 1);

        public void RecordLevelReached(int level) => SetMax(StatId.MaxLevelReached, level);

        // ---- helpers ----
        public void RecordLevelUp() => Add(StatId.TotalLevelUps, 1);
        public void RecordXpGained(int amount) { if (amount > 0) Add(StatId.ExperienceGained, amount); }
        public void RecordXpDoubled() => Add(StatId.XpDoublesTriggered, 1);
        public void RecordTimeMined(int seconds) { if (seconds > 0) Add(StatId.TimeSpentMiningSec, seconds); }
        public void RecordRockSpawned(int count = 1) { if (count > 0) Add(StatId.TotalRocksSpawned, count); }
        public void RecordMineBarTick(int bars) { if (bars > 0) Add(StatId.BarsMined_TheMine, bars); }
        public void RecordFlowerSpawned() => Add(StatId.FlowersSpawned, 1);
        public void RecordInstaMine() => Add(StatId.InstaMineHits, 1);
        public void RecordDoubleDamage() => Add(StatId.DoubleDamageHits, 1);
        public void RecordChestOpened() => Add(StatId.ChestsOpened, 1);
        public void RecordHammerTrigger() => Add(StatId.HammerTriggers, 1);
        public void RecordZeusStrike(int count = 1) { if (count > 0) Add(StatId.ZeusStrikes, count); }
        public void RecordPickaxeSpawned() => Add(StatId.PickaxesSpawned, 1);

        // ---- 14 helpers for the new stats (enum at 33..46). ----
        public void RecordPotion() => Add(StatId.PotionsDrank, 1);
        public void RecordGoldenChest() => Add(StatId.GoldenChestsOpened, 1);
        public void RecordTheMine2X() => Add(StatId.TheMine2XTriggers, 1);
        public void RecordInflateBurst() => Add(StatId.InflateBurstTriggers, 1);
        public void RecordMidasTouchSession() => Add(StatId.MidasTouchSessions, 1);
        public void RecordEnergiDrink() => Add(StatId.EnergiDrinksDrank, 1);
        public void RecordCampfireSpawned() => Add(StatId.CampfiresSpawned, 1);
        public void RecordD100Roll() => Add(StatId.D100Rolls, 1);
        public void RecordPickaxeHit() => Add(StatId.PickaxeHits, 1);
        public void RecordDoubleOre() => Add(StatId.DoubleOreGained, 1);
        public void RecordBarsCrafted(int count = 1) { if (count > 0) Add(StatId.BarsCrafted, count); }
        /// <summary>per-ore "full rock" count. Currently we don't have a chunk vs full
        /// rock distinction in the Shell (every Rock = same HP), so this only fires when a Gold or
        /// Copper rock breaks at full HP. ore tier system may differentiate later.</summary>
        public void RecordFullOreMined(Rebuild.Mining.OreType ore)
        {
            switch (ore)
            {
                case Rebuild.Mining.OreType.Gold:   Add(StatId.FullOreMined_Gold, 1); break;
                case Rebuild.Mining.OreType.Copper: Add(StatId.FullOreMined_Copper, 1); break;
                // Other ores: no dedicated stat yet.
            }
        }

        /// <summary>Debug hard reset. Mirrors the per-manager ResetAll pattern.</summary>
        public void ResetAll()
        {
            for (int i = 0; i < _values.Length; i++) _values[i] = 0;
            for (int i = 0; i < _values.Length; i++) OnStatChanged?.Invoke((StatId)i, 0);
        }

        // ---- IDataPersistence ----
        public void LoadData(GameDataDTO data)
        {
            if (data == null) return;
            if (_values == null || _values.Length != StatDef.Count) _values = new long[StatDef.Count];
            if (data.stats != null)
            {
                int n = Mathf.Min(_values.Length, data.stats.Length);
                for (int i = 0; i < _values.Length; i++)
                    _values[i] = i < n ? data.stats[i] : 0L;
            }
            else
            {
                for (int i = 0; i < _values.Length; i++) _values[i] = 0L;
            }
            for (int i = 0; i < _values.Length; i++) OnStatChanged?.Invoke((StatId)i, _values[i]);
        }

        public void SaveData(GameDataDTO data)
        {
            if (data == null) return;
            if (data.stats == null || data.stats.Length != _values.Length)
                data.stats = new long[_values.Length];
            for (int i = 0; i < _values.Length; i++) data.stats[i] = _values[i];
        }
    }
}
