using System;
using System.Collections;
using Rebuild.Saving;
using UnityEngine;

namespace Rebuild.Mining
{





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


        [SerializeField] bool _unlocked = false;


        public const int UnlockPrice = 500;
        public OreType UnlockCurrency => OreType.Gold;


        static readonly int[] s_speedPrices    = { 5, 12, 25, 50, 100, 200, 400, 800 };
        static readonly int[] s_materialPrices = { 8, 30, 100, 350, 1000 };


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





        public static (OreType ore, int weight)[] GetOreWeights() => s_oreWeights;

        public int SpeedLevel => speedLevel;
        public int MaterialLevel => materialLevel;
        public int MaxSpeedLevel => s_speedPrices.Length;
        public int MaxMaterialLevel => s_materialPrices.Length;
        public bool IsSpeedMaxed => speedLevel >= MaxSpeedLevel;
        public bool IsMaterialMaxed => materialLevel >= MaxMaterialLevel;
        public OreType SpeedCurrency => OreType.Gold;
        public OreType MaterialCurrency => OreType.Copper;

        public float BaseMiningTime => Mathf.Max(minMiningTime, baseMiningTime - speedLevel * speedDecreasePerLevel);
        public float CurrentMiningTime => _tickTime > 0f ? _tickTime : BaseMiningTime;

        float _tickTime;

        float GetPermanentYieldMultiplier(bool skilled)
        {

            float multiplier = skilled ? 1f / 0.95f : 1f;
            var artifacts = ArtifactsManager.Instance;
            if (artifacts != null && artifacts.HasArtifact(ArtifactId.AncientDevice))
            {
                var talents = LevelMechanics.Instance;
                float archaeology = talents != null && talents.HasTalent(TalentId.Archaeologist) ? 1.08f : 1f;
                multiplier /= 1f - 0.05f * artifacts.RuneMultiplier * archaeology;
            }
            return multiplier;
        }

        static float GetSignTargetTime(float period, SignBuffKind kind, float value, bool triple)
        {
            if (kind == SignBuffKind.FasterMine) return period * (1f - value);
            return triple ? period / 3f : period;
        }

        static float GetSignYieldMultiplier(float period, float minimum, SignBuffKind kind, float value, bool triple)
        {
            float targetTime = GetSignTargetTime(period, kind, value, triple);
            float lostTime = Mathf.Max(0f, minimum - targetTime);
            if (triple)
            {


                float meanTime = period * (1f - (2f / 3f) * value);
                return 1f + lostTime / meanTime;
            }
            return 1f + lostTime / targetTime;
        }

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

            if (_unlocked && _miningCo == null) _miningCo = StartCoroutine(MiningLoop());
        }



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

            yield return null;
            while (_unlocked)
            {
                bool skilled = LevelMechanics.Instance != null && LevelMechanics.Instance.HasTalent(TalentId.SkilledMiners);
                var sign = SignBuffMechanics.Instance;
                var kind = sign != null ? sign.ActiveBuff : SignBuffKind.None;
                float signValue = kind switch
                {
                    SignBuffKind.FasterMine => sign.FasterMineFraction,
                    SignBuffKind.Mine3XFaster => sign.Mine3XFasterChance * 0.01f,
                    SignBuffKind.MoreMaterials => sign.MoreMaterialsAmount,
                    SignBuffKind.DoubleMaterialsChance => sign.DoubleMaterialsChance * 0.01f,
                    _ => 0f,
                };
                bool triple = kind == SignBuffKind.Mine3XFaster && D100Helper.Roll(signValue);
                float period = BaseMiningTime;
                float total = Mathf.Max(minMiningTime, GetSignTargetTime(period, kind, signValue, triple));
                float yieldMultiplier = GetPermanentYieldMultiplier(skilled)
                    * GetSignYieldMultiplier(period, minMiningTime, kind, signValue, triple);
                var skills = SkillTreeManager.Instance;
                float worthMultiplier = skills != null
                    ? 1f + Rock.GetMaterialsWorthMoreBonus(skills.GetLevel(UpgradeId.MaterialsWorthMore))
                    : 1f;
                int amount = CurrentBarsMined;
                _tickTime = total;
                float elapsed = 0f;
                Progress01 = 0f;
                OnProgressChanged?.Invoke(0f);
                while (elapsed < total)
                {



                    elapsed += Time.deltaTime;
                    Progress01 = Mathf.Clamp01(elapsed / total);
                    OnProgressChanged?.Invoke(Progress01);
                    yield return null;
                }
                Progress01 = 1f;
                OnProgressChanged?.Invoke(1f);
                EmitTick(amount, skilled, kind, signValue, worthMultiplier, yieldMultiplier);
            }
            _tickTime = 0f;
            _miningCo = null;
        }

        void EmitTick(int amount, bool skilled, SignBuffKind kind, float signValue, float worthMultiplier, float yieldMultiplier)
        {
            var ore = RollOre();
            if (skilled && D100Helper.Roll(0.08f))
            {
                amount *= 2;

                Rebuild.Stats.StatsManager.Instance?.RecordTheMine2X();
            }
            if (kind == SignBuffKind.MoreMaterials) amount += (int)signValue;
            if (kind == SignBuffKind.DoubleMaterialsChance && D100Helper.Roll(signValue)) amount *= 2;

            if (PotionBuffMechanics.Instance != null
                && PotionBuffMechanics.Instance.PotionDoubleChanceIncrease > 0f
                && UnityEngine.Random.value * 100f < PotionBuffMechanics.Instance.PotionDoubleChanceIncrease)
            {
                amount *= 2;
            }

            if (PotionBuffMechanics.Instance != null
                && PotionBuffMechanics.Instance.PotionMaterialWorthMore > 0)
            {
                amount += PotionBuffMechanics.Instance.PotionMaterialWorthMore;
            }
            if (worthMultiplier != 1f)
                amount = Mathf.Max(1, Mathf.RoundToInt(amount * worthMultiplier));
            if (yieldMultiplier > 1f)
            {
                float scaledAmount = amount * yieldMultiplier;
                amount = Mathf.FloorToInt(scaledAmount);
                float remainder = scaledAmount - amount;
                if (remainder > 0f && D100Helper.Roll(remainder)) amount++;
            }
            if (GoldAndOreMechanics.Instance != null)



                GoldAndOreMechanics.Instance.AddBarsDirect(ore, amount);
            LastOre = ore;
            LastAmount = amount;
            OnTick?.Invoke(ore, amount);





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


        public void ResetAll()
        {
            speedLevel = 0;
            materialLevel = 0;
            OnUpgradeChanged?.Invoke();
        }


        public void LoadData(GameDataDTO data)
        {
            if (data == null) return;
            if (_miningCo != null) StopCoroutine(_miningCo);
            _miningCo = null;
            _tickTime = 0f;
            Progress01 = 0f;
            speedLevel = Mathf.Clamp(data.mineSpeedLevel, 0, MaxSpeedLevel);
            materialLevel = Mathf.Clamp(data.mineMaterialLevel, 0, MaxMaterialLevel);

            _unlocked = data.isTheMineUnlocked;

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
