using System;
using Rebuild.Saving;
using UnityEngine;

namespace Rebuild.Mining
{
    /// <summary>
    /// 8-material count store + change events.
    /// <para>
    /// <c>_oreCounts</c> doubled as both mined raw count and spendable currency.
    /// </para>
    /// <para>
    /// <b></b> — separates the two concepts to mirror the original
    /// game flow:
    /// <list type="bullet">
    /// <item><c>_sessionGained</c> = raw ore mined this session. Increments on every rock break.
    /// Reset at <see cref="ResetSession"/>. Does NOT spend; not directly persisted.</item>
    /// <item><c>_oreCounts</c> = persistent <b>bar</b> count — the actual spendable currency.
    /// <see cref="TrySpendOre"/> (kept name for back-compat) deducts from this. Rocks no longer
    /// add to this; instead <see cref="CraftSessionToBars"/> runs at the end of every mining
    /// session and converts <c>_sessionGained</c> at a 3:1 (or 2:1 with Craft2Material upgrade)
    /// ratio, with optional GoldRing artifact multiplier.</item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>The Mine idle bypass</b> — <see cref="AddBarsDirect"/> credits bars without going through
    /// the session craft step, since idle TheMine production isn't tied to a mining session.
    /// </para>
    /// DTO field <c>oreCounts</c> kept under its old name so existing saves migrate seamlessly:
    /// legacy saves load with their "ore total" treated as the new bar count (effectively a
    /// fair conversion — same numeric value carries over as bars).
    /// </summary>
    public sealed class GoldAndOreMechanics : MonoBehaviour, Rebuild.Saving.IDataPersistence
    {
        public static GoldAndOreMechanics Instance { get; private set; }

        // _oreCounts is the BAR counter later (kept name for save-file compat).
        readonly long[] _oreCounts = new long[8];
        readonly long[] _sessionGained = new long[8];

        // OnOreChanged fires whenever the spendable bar count changes (purchase / session craft / mine idle).
        public event Action<OreType, long> OnOreChanged;
        public event Action<OreType, long> OnSessionGainedChanged;

        void Awake()
        {
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>Returns the persistent bar count for the given material (spendable currency).</summary>
        public long GetCount(OreType ore) => _oreCounts[(int)ore];
        public long GetSessionGained(OreType ore) => _sessionGained[(int)ore];

        public long[] CopySessionGained()
        {
            var copy = new long[_sessionGained.Length];
            Array.Copy(_sessionGained, copy, _sessionGained.Length);
            return copy;
        }

        /// <summary>adds raw ore to the current session counter. Does NOT increment the
        /// persistent bar count (that happens at session end via <see cref="CraftSessionToBars"/>).</summary>
        public void AddOre(OreType ore, int amount)
        {
            if (amount <= 0) return;
            int idx = (int)ore;
            _sessionGained[idx] += amount;
            OnSessionGainedChanged?.Invoke(ore, _sessionGained[idx]);
        }

        /// <summary>credits bars directly, bypassing the session craft step. Used by
        /// <c>MineManager.EmitTick</c> for idle background production (TheMine runs while the
        /// player is in menus, so it can't wait for a session-end craft).</summary>
        public void AddBarsDirect(OreType ore, int amount)
        {
            if (amount <= 0) return;
            int idx = (int)ore;
            _oreCounts[idx] += amount;
            OnOreChanged?.Invoke(ore, _oreCounts[idx]);
        }

        /// <summary>Atomic spend used by SkillTree / Anvil / TheMine upgrades. Affects the
        /// persistent bar count only.</summary>
        public bool TrySpendOre(OreType ore, long amount)
        {
            if (amount <= 0) return true;
            int idx = (int)ore;
            if (_oreCounts[idx] < amount) return false;
            _oreCounts[idx] -= amount;
            OnOreChanged?.Invoke(ore, _oreCounts[idx]);
            return true;
        }

        /// <summary>run by <see cref="MiningSessionController"/> after the OutOfTime
        /// banner. Converts the session's raw ore into persistent bars at a 3:1 (or 2:1 with
        /// Craft2Material SkillTree) ratio, with a small bonus multiplier when the GoldRing
        /// artifact is found. Returns the bars-crafted-per-ore table so the results panel can
        /// show the conversion alongside the raw mined totals.
        /// <para>.C (2026-05-16) — D100 talent 의 원본 ore-lottery 복원. 원본
        /// SetRockScreen.cs:1021-1039: 세션 종료 시 Random.Range(1, 101) 굴려 1/10/100 (=3%) 면
        /// session bars ×3. Shell 의 이전 +0.5pp flat bonus 모델 () 폐기.</para></summary>
        public long[] CraftSessionToBars()
        {
            var barsCrafted = new long[_sessionGained.Length];
            int divisor = 3;
            if (SkillTreeManager.Instance != null
                && SkillTreeManager.Instance.GetLevel(UpgradeId.Craft2Material) > 0)
                divisor = 2;
            bool hasGoldRing = ArtifactsManager.Instance != null
                && ArtifactsManager.Instance.HasArtifact(ArtifactId.GoldRing);
            // D100 lottery (원본 SetRockScreen:1024-1029 매칭). 세션 종료 시 1회 굴림,
            // 1/10/100 (=3% effective) 결과면 모든 ore bars ×3. D100 talent 미보유 → roll 안 함.
            bool d100Tripled = false;
            if (LevelMechanics.Instance != null && LevelMechanics.Instance.HasTalent(TalentId.D100))
            {
                int roll = UnityEngine.Random.Range(1, 101);
                if (roll == 1 || roll == 10 || roll == 100)
                {
                    d100Tripled = true;
                    Rebuild.Stats.StatsManager.Instance?.RecordD100Roll();
                }
            }
            for (int i = 0; i < _sessionGained.Length; i++)
            {
                if (_sessionGained[i] <= 0) continue;
                long bars = (_sessionGained[i] + divisor - 1) / divisor; // ceil divide
                if (hasGoldRing)
                {
                    // 원본 Artifacts.goldRingCraftChance = 1.1f 매칭 (= +10%).
                    // 이전 Shell 의 1.025~1.035 (의도적 약화 — 4× 약함) 되돌림.
                    bars = (long)Mathf.Ceil(bars * 1.1f);
                }
                // D100 hit 시 bars ×3 (원본 oresTripled).
                if (d100Tripled) bars *= 3;
                barsCrafted[i] = bars;
                _oreCounts[i] += bars;
                OnOreChanged?.Invoke((OreType)i, _oreCounts[i]);
            }
            // count total bars crafted this session (sum across 8 ores).
            long totalBars = 0;
            for (int i = 0; i < barsCrafted.Length; i++) totalBars += barsCrafted[i];
            if (totalBars > 0)
                Rebuild.Stats.StatsManager.Instance?.RecordBarsCrafted((int)Mathf.Min(totalBars, int.MaxValue));
            return barsCrafted;
        }

        /// <summary>Wipe per-session counters (called by MiningSessionController on StartSession).</summary>
        public void ResetSession()
        {
            for (int i = 0; i < _sessionGained.Length; i++)
            {
                _sessionGained[i] = 0;
                OnSessionGainedChanged?.Invoke((OreType)i, 0);
            }
        }

        /// <summary>Wipe persistent + session counters (debug / hard reset).</summary>
        public void ResetAll()
        {
            for (int i = 0; i < _oreCounts.Length; i++)
            {
                _oreCounts[i] = 0;
                _sessionGained[i] = 0;
                OnOreChanged?.Invoke((OreType)i, 0);
                OnSessionGainedChanged?.Invoke((OreType)i, 0);
            }
        }

        // ---- IDataPersistence ----
        public void LoadData(GameDataDTO data)
        {
            if (data == null || data.oreCounts == null) return;
            int n = Mathf.Min(_oreCounts.Length, data.oreCounts.Length);
            for (int i = 0; i < _oreCounts.Length; i++)
            {
                long v = i < n ? data.oreCounts[i] : 0L;
                _oreCounts[i] = v;
                OnOreChanged?.Invoke((OreType)i, v);
            }
        }

        public void SaveData(GameDataDTO data)
        {
            if (data == null) return;
            if (data.oreCounts == null || data.oreCounts.Length != _oreCounts.Length)
                data.oreCounts = new long[_oreCounts.Length];
            for (int i = 0; i < _oreCounts.Length; i++) data.oreCounts[i] = _oreCounts[i];
        }
    }
}
