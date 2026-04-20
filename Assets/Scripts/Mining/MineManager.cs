using System;
using System.Collections;
using Rebuild.Saving;
using UnityEngine;

namespace Rebuild.Mining
{
    /// <summary>passive idle mining.
    /// Singleton on MiningSessionRoot. Runs an infinite-recurring tick (always on regardless
    /// of which screen is showing). Two upgrade slots (speed / barsMined) buy down the tick
    /// time and bump bars-per-tick. persists <c>speedLevel</c>, <c>materialLevel</c>.
    /// </summary>
    public sealed class MineManager : MonoBehaviour, Rebuild.Saving.IDataPersistence
    {
        public static MineManager Instance { get; private set; }

        [Header("Curve")]
        [SerializeField] float baseMiningTime = 5f;
        [SerializeField] int baseBarsMined = 1;
        [SerializeField] float speedDecreasePerLevel = 0.5f;
        [SerializeField] float minMiningTime = 1.0f;

        [Header("State (persistable)")]
        [SerializeField] int speedLevel = 0;
        [SerializeField] int materialLevel = 0;
        // TheMine 잠금 상태. 원본 TheMine.isTheMineUnlocked 와 동일 의미.
        // 시작 시 false → 사용자가 PurchaseUnlock() (500 Gold bar) 호출 후 true → MiningLoop 코루틴 시작.
        [SerializeField] bool _unlocked = false;

        // TheMine 첫 구매 가격 (원본 theMinePrice = 500.0).
        public const int UnlockPrice = 500;
        public OreType UnlockCurrency => OreType.Gold;

        // Per-level prices: speed (Gold), material (Copper).
        static readonly int[] s_speedPrices    = { 5, 12, 25, 50, 100, 200, 400, 800, 1500, 3000 };
        static readonly int[] s_materialPrices = { 8, 30, 100, 350, 1000 };

        // Same weighted distribution as the RockSpawner placeholder roll ().
        static readonly (OreType ore, int weight)[] s_oreWeights =
        {
            (OreType.Gold,    35),
            (OreType.Copper,  25),
            (OreType.Iron,    15),
            (OreType.Cobalt,  10),
            (OreType.Uranium,  7),
            (OreType.Ismium,   4),
            (OreType.Iridium,  3),
            (OreType.Painite,  1),
        };

        /// <summary>exposed for MineChanceHudUI. Returns the live oreWeights array
        /// (read-only — callers must not mutate). When introduces SkillTree-gated
        /// weights this becomes an instance method that folds SpawnXxx upgrades; current
        /// static implementation is unaffected by SkillTree.</summary>
        public static (OreType ore, int weight)[] GetOreWeights() => s_oreWeights;

        public int SpeedLevel => speedLevel;
        public int MaterialLevel => materialLevel;
        public int MaxSpeedLevel => s_speedPrices.Length;
        public int MaxMaterialLevel => s_materialPrices.Length;
        public bool IsSpeedMaxed => speedLevel >= MaxSpeedLevel;
        public bool IsMaterialMaxed => materialLevel >= MaxMaterialLevel;
        public OreType SpeedCurrency => OreType.Gold;
        public OreType MaterialCurrency => OreType.Copper;

        public float CurrentMiningTime
        {
            get
            {
                float t = baseMiningTime - speedLevel * speedDecreasePerLevel;
                // artifact: Ancient Device multiplies the tick time by 0.95.
                // 원본 TheMine.cs:285-303 의 multiplier chain 복원:
                //   `num2 = ancientDeviceTimeReduction × (1 + runeImproveAll)` → `t *= (1 - num2)`.
                // Rune 보유 시 -5.25%, 미보유 시 -5%.
                // Archaeologist multi-site multiplier 매칭. AncientDevice
                // 의 cut 이 Archaeologist 보유 시 추가 ×1.08 (원본 archeologistIncreaseDISPLAY=0.08).
                if (ArtifactsManager.Instance != null && ArtifactsManager.Instance.HasArtifact(ArtifactId.AncientDevice))
                {
                    float archeoMult = (LevelMechanics.Instance != null
                        && LevelMechanics.Instance.HasTalent(TalentId.Archaeologist)) ? 1.08f : 1f;
                    float reduce = 0.05f * ArtifactsManager.Instance.RuneMultiplier * archeoMult;
                    t *= (1f - reduce);
                }
                // ItsASign FasterMine buff slices mineTime by 4~7%.
                if (SignBuffMechanics.Instance != null
                    && SignBuffMechanics.Instance.ActiveBuff == SignBuffKind.FasterMine
                    && SignBuffMechanics.Instance.FasterMineFraction > 0f)
                {
                    t *= (1f - SignBuffMechanics.Instance.FasterMineFraction);
                }
                // SkilledMiners "fast" 효과 (10% chance, mineTime÷2). 원본
                // TheMine.cs:311-316 매칭. tick 시작 시 RollSkilledMinersFast() 가 결과를
                // _skilledMinersFastThisTick 에 저장 — CurrentMiningTime 이 같은 tick 내에서
                // 항상 같은 값을 돌려주도록 보장 (mineSlider progress 안 깨짐). EmitTick 끝에 reset.
                if (_skilledMinersFastThisTick) t /= 2f;
                return Mathf.Max(minMiningTime, t);
            }
        }

        // SkilledMiners fast 의 per-tick flag (CurrentMiningTime 이 한 tick 안에서 안정값을 줘야 하므로
        // tick 시작에 1회 roll, tick 끝 EmitTick 에 reset). 원본 TheMine.Mining 의 flag bool 패턴 매칭.
        bool _skilledMinersFastThisTick;

        public int CurrentBarsMined => baseBarsMined + materialLevel;

        public int GetSpeedNextPrice() => IsSpeedMaxed ? -1 : s_speedPrices[speedLevel];
        public int GetMaterialNextPrice() => IsMaterialMaxed ? -1 : s_materialPrices[materialLevel];

        public float Progress01 { get; private set; }
        public OreType LastOre { get; private set; }
        public int LastAmount { get; private set; }

        public event Action<float> OnProgressChanged;
        public event Action<OreType, int> OnTick;
        public event Action OnUpgradeChanged;
        public event Action OnUpgradeFailed;
        // TheMine unlock 상태 변경 시 fire. TheMineUI 가 잠금/활성 view 토글.
        public event Action OnUnlockChanged;

        public bool IsUnlocked => _unlocked;

        Coroutine _miningCo;

        void Awake()
        {
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Start()
        {
            // TheMine 잠금 상태에선 MiningLoop 미시작. PurchaseUnlock() 후 시작.
            if (_unlocked && _miningCo == null) _miningCo = StartCoroutine(MiningLoop());
        }

        /// <summary>TheMine 첫 잠금 해제. 500 Gold bar 지불 후 MiningLoop 시작.
        /// 원본 TheMine.PurchaseTheMine 과 동일.</summary>
        public bool TryPurchaseUnlock()
        {
            if (_unlocked) return false;
            var ore = GoldAndOreMechanics.Instance;
            if (ore == null) return false;
            if (!ore.TrySpendOre(UnlockCurrency, UnlockPrice))
            {
                Rebuild.Audio.AudioManager.Instance?.Play("CantAfford");
                return false;
            }
            _unlocked = true;
            Rebuild.Audio.AudioManager.Instance?.Play("MinePurchase");
            if (_miningCo == null) _miningCo = StartCoroutine(MiningLoop());
            OnUnlockChanged?.Invoke();
            return true;
        }

        IEnumerator MiningLoop()
        {
            // Wait one frame so other managers (GoldAndOreMechanics) Awake first.
            yield return null;
            while (true)
            {
                // SkilledMiners fast roll (per-tick, 10% chance). 원본 TheMine.cs:311-316
                // 매칭. CurrentMiningTime 이 같은 tick 동안 안정된 값을 돌려주도록 tick 시작 시 set.
                _skilledMinersFastThisTick = LevelMechanics.Instance != null
                    && LevelMechanics.Instance.HasTalent(TalentId.SkilledMiners)
                    && D100Helper.Roll(0.10f);
                float elapsed = 0f;
                float total = CurrentMiningTime;
                Progress01 = 0f;
                OnProgressChanged?.Invoke(0f);
                while (elapsed < total)
                {
                    // switched to Time.deltaTime so menu/transition
                    // timeScale=0 pauses idle progression — matches the original game's behaviour
                    // where talent/circle-wipe pauses freeze the mine tick.
                    elapsed += Time.deltaTime;
                    total = CurrentMiningTime; // re-read in case the upgrade changed mid-tick
                    Progress01 = Mathf.Clamp01(elapsed / total);
                    OnProgressChanged?.Invoke(Progress01);
                    yield return null;
                }
                Progress01 = 1f;
                OnProgressChanged?.Invoke(1f);
                EmitTick();
            }
        }

        void EmitTick()
        {
            var ore = RollOre();
            int amount = CurrentBarsMined;
            // SkilledMiners talent — 원본 TheMine.cs:335-339 매칭: 별도 8% chance per tick
            // 로 bars × 2 (skilledMinersDoubleChance=8). fast (10%, mineTime÷2) 는 CurrentMiningTime
            // 측에서 별도 roll. 두 효과 독립 발동.
            // 원본 매칭: 0.10 → 0.08 (skilledMinersDoubleChance).
            if (LevelMechanics.Instance != null && LevelMechanics.Instance.HasTalent(TalentId.SkilledMiners)
                && D100Helper.Roll(0.08f))
            {
                amount *= 2;
                // count SkilledMiners 2X procs (double).
                Rebuild.Stats.StatsManager.Instance?.RecordTheMine2X();
            }
            // fast roll 결과 stat 기록 (fast 효과는 CurrentMiningTime 측 처리).
            if (_skilledMinersFastThisTick)
                Rebuild.Stats.StatsManager.Instance?.RecordTheMine2X();
            _skilledMinersFastThisTick = false;
            // ItsASign buffs to the tick yield.
            if (SignBuffMechanics.Instance != null)
            {
                switch (SignBuffMechanics.Instance.ActiveBuff)
                {
                    case SignBuffKind.MoreMaterials:
                        amount += SignBuffMechanics.Instance.MoreMaterialsAmount;
                        break;
                    case SignBuffKind.DoubleMaterialsChance:
                        if (UnityEngine.Random.value * 100f < SignBuffMechanics.Instance.DoubleMaterialsChance)
                            amount *= 2;
                        break;
                    // Mine3XFaster is handled per-tick by the speed branch below — apply only when ON.
                    case SignBuffKind.Mine3XFaster:
                        // No yield change; the cadence-side roll is folded into CurrentMiningTime
                        // by triggering a one-off ×0.33 burst during the next tick coroutine.
                        break;
                }
            }
            // PotionDoubleChance buff: extra ×2 yield roll (adds to SkilledMiners).
            if (PotionBuffMechanics.Instance != null
                && PotionBuffMechanics.Instance.PotionDoubleChanceIncrease > 0f
                && UnityEngine.Random.value * 100f < PotionBuffMechanics.Instance.PotionDoubleChanceIncrease)
            {
                amount *= 2;
            }
            // PotionMaterialWorthMore: flat additive yield (+1~8 bars).
            if (PotionBuffMechanics.Instance != null
                && PotionBuffMechanics.Instance.PotionMaterialWorthMore > 0)
            {
                amount += PotionBuffMechanics.Instance.PotionMaterialWorthMore;
            }
            // 원본 GoldAndOreMechanics.GiveGoldBar 의 worthMultiplier 매칭.
            // TheMine idle tick 도 session ore drop 과 동일한 materialsTotalWorth 배수 (LocalizationScript
            // 1+2+3+9+12+4+6+18 누적). amount(count) × worthMultiplier 곱셈.
            if (SkillTreeManager.Instance != null)
            {
                float worthMultiplier = 1f
                    + Rock.GetMaterialsWorthMoreBonus(SkillTreeManager.Instance.GetLevel(UpgradeId.MaterialsWorthMore));
                if (worthMultiplier != 1f)
                    amount = Mathf.Max(1, Mathf.RoundToInt(amount * worthMultiplier));
            }
            if (GoldAndOreMechanics.Instance != null)
                // idle production bypasses the session craft step (AddBarsDirect
                // credits bars immediately; AddOre would feed _sessionGained that only flushes
                // at a mining-session end, but TheMine ticks while in menus too).
                GoldAndOreMechanics.Instance.AddBarsDirect(ore, amount);
            LastOre = ore;
            LastAmount = amount;
            OnTick?.Invoke(ore, amount);
            // sounded like background music to the user (every tick = a constant
            // loop while the menus stay open). Removed; MinePurchase still fires on speed/material
            // upgrade clicks so the tab isn't completely silent.
            // Rebuild.Audio.AudioManager.Instance?.Play("MineCollect");
            // stat — total idle bars produced by The Mine (counts the doubled amount).
            Rebuild.Stats.StatsManager.Instance?.RecordMineBarTick(amount);
        }

        static OreType RollOre()
        {
            int total = 0;
            for (int i = 0; i < s_oreWeights.Length; i++) total += s_oreWeights[i].weight;
            int r = UnityEngine.Random.Range(0, total);
            int acc = 0;
            for (int i = 0; i < s_oreWeights.Length; i++)
            {
                acc += s_oreWeights[i].weight;
                if (r < acc) return s_oreWeights[i].ore;
            }
            return OreType.Gold;
        }

        public bool TryUpgradeSpeed()
        {
            if (IsSpeedMaxed) { OnUpgradeFailed?.Invoke(); return false; }
            int price = GetSpeedNextPrice();
            var ore = GoldAndOreMechanics.Instance;
            if (ore == null || !ore.TrySpendOre(SpeedCurrency, price))
            {
                Rebuild.Audio.AudioManager.Instance?.Play("CantAfford");
                OnUpgradeFailed?.Invoke();
                return false;
            }
            speedLevel++;
            Rebuild.Audio.AudioManager.Instance?.Play("MinePurchase");
            OnUpgradeChanged?.Invoke();
            return true;
        }

        public bool TryUpgradeMaterial()
        {
            if (IsMaterialMaxed) { OnUpgradeFailed?.Invoke(); return false; }
            int price = GetMaterialNextPrice();
            var ore = GoldAndOreMechanics.Instance;
            if (ore == null || !ore.TrySpendOre(MaterialCurrency, price))
            {
                Rebuild.Audio.AudioManager.Instance?.Play("CantAfford");
                OnUpgradeFailed?.Invoke();
                return false;
            }
            materialLevel++;
            Rebuild.Audio.AudioManager.Instance?.Play("MinePurchase");
            OnUpgradeChanged?.Invoke();
            return true;
        }

        /// <summary>Debug hard reset.</summary>
        public void ResetAll()
        {
            speedLevel = 0;
            materialLevel = 0;
            OnUpgradeChanged?.Invoke();
        }

        // ---- IDataPersistence ----
        public void LoadData(GameDataDTO data)
        {
            if (data == null) return;
            speedLevel = Mathf.Clamp(data.mineSpeedLevel, 0, MaxSpeedLevel);
            materialLevel = Mathf.Clamp(data.mineMaterialLevel, 0, MaxMaterialLevel);
            // TheMine unlock 상태. 기존 save (DTO field 없음) = false default.
            _unlocked = data.isTheMineUnlocked;
            // 잠금 해제된 save 라면 Start 가 못 잡은 경우 대비 코루틴 시작.
            if (_unlocked && _miningCo == null && isActiveAndEnabled)
                _miningCo = StartCoroutine(MiningLoop());
            OnUpgradeChanged?.Invoke();
            OnUnlockChanged?.Invoke();
        }

        public void SaveData(GameDataDTO data)
        {
            if (data == null) return;
            data.mineSpeedLevel = speedLevel;
            data.mineMaterialLevel = materialLevel;
            data.isTheMineUnlocked = _unlocked;
        }
    }
}
