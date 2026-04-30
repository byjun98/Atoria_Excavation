using UnityEngine;

namespace Rebuild.Mining
{
    /// <summary>PotionDrinker / PotionChugger talent hub. Original SetRockScreen
    /// rolls 1 of 4 potions (Drinker) or activates all 4 (Chugger) every mining session start.
    /// Each potion is consumed by a downstream system (XP curve, double-chance roll, ore yield,
    /// pickaxe stats) — we expose the four values and let LevelMechanics / MineManager / etc.
    /// poll them.
    /// <para>Lifecycle: <see cref="RollForSession"/> at MiningSessionController.ApplyEnterSession,
    /// <see cref="ResetForSession"/> at ApplyExitSession. In-memory only (potions reroll every
    /// session by design — original `SetRockScreen.SetRockScreen()` runs every session).</para>
    /// </summary>
    public sealed class PotionBuffMechanics : MonoBehaviour
    {
        public static PotionBuffMechanics Instance { get; private set; }

        public bool IsPotionXpActive { get; private set; }
        public float PotionXpIncrease { get; private set; }       // 0.13 ~ 0.21 (XP × (1+this))
        public float PotionDoubleChanceIncrease { get; private set; } // 0..5 (% per relevant roll)
        public int PotionMaterialWorthMore { get; private set; }   // 0..8 (flat +N bars per MineManager tick)
        public float PotionPickaxeStats { get; private set; }       // 0..0.10 (reserved — Anvil hook TBD)

        public bool IsActiveThisSession =>
            IsPotionXpActive || PotionDoubleChanceIncrease > 0f
            || PotionMaterialWorthMore > 0 || PotionPickaxeStats > 0f;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>Called by MiningSessionController.ApplyEnterSession. Rolls one or all
        /// potions based on the player's PotionDrinker / PotionChugger talents. No-op when
        /// neither is owned (all values stay 0 / inactive).</summary>
        public void RollForSession()
        {
            ResetForSession();
            if (LevelMechanics.Instance == null) return;
            bool drinker = LevelMechanics.Instance.HasTalent(TalentId.PotionDrinker);
            bool chugger = LevelMechanics.Instance.HasTalent(TalentId.PotionChugger);
            if (!drinker && !chugger) return;
            if (chugger)
            {
                ActivateXp();
                ActivateDoubleChance();
                ActivateMaterialWorth();
                ActivatePickaxeStats();
            }
            else
            {
                // 1 of 4 random (mirrors original SetRockScreen line 1564-1703 branching).
                int pick = UnityEngine.Random.Range(1, 5);
                if (pick == 1) ActivateMaterialWorth();
                if (pick == 2) ActivateXp();
                if (pick == 3) ActivatePickaxeStats();
                if (pick == 4) ActivateDoubleChance();
            }
            // count one potion activation per fresh session that rolled any buff.
            // Chugger always rolls all 4, Drinker rolls one — either way the player "drank" a potion.
            Rebuild.Stats.StatsManager.Instance?.RecordPotion();
        }

        public void ResetForSession()
        {
            IsPotionXpActive = false;
            PotionXpIncrease = 0f;
            PotionDoubleChanceIncrease = 0f;
            PotionMaterialWorthMore = 0;
            PotionPickaxeStats = 0f;
        }

        void ActivateXp()
        {
            IsPotionXpActive = true;
            PotionXpIncrease = UnityEngine.Random.Range(0.13f, 0.21f);
        }

        void ActivateDoubleChance()
        {
            // Original ranges 2~5%; we pick a single tier 5% for simplicity.
            PotionDoubleChanceIncrease = 5f;
        }

        void ActivateMaterialWorth()
        {
            // Original 1~8 random bars. Pick a mid value so the buff is meaningful but bounded.
            PotionMaterialWorthMore = UnityEngine.Random.Range(2, 6);
        }

        void ActivatePickaxeStats()
        {
            // Reserved — Anvil pickaxe stat hook (TBD ). Sets a value so
            // IsActiveThisSession reports true and UI can show the potion as active.
            PotionPickaxeStats = 0.05f;
        }
    }
}
