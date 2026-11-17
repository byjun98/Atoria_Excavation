using System;
using Rebuild.Saving;
using UnityEngine;

namespace Rebuild.Mining
{


























    public sealed class GoldAndOreMechanics : MonoBehaviour, Rebuild.Saving.IDataPersistence
    {
        public static GoldAndOreMechanics Instance { get; private set; }


        readonly long[] _oreCounts = new long[8];
        readonly long[] _sessionGained = new long[8];
        readonly int[] _goldRingTenths = new int[8];


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


        public long GetCount(OreType ore) => _oreCounts[(int)ore];
        public long GetSessionGained(OreType ore) => _sessionGained[(int)ore];

        public long[] CopySessionGained()
        {
            var copy = new long[_sessionGained.Length];
            Array.Copy(_sessionGained, copy, _sessionGained.Length);
            return copy;
        }



        public void AddOre(OreType ore, int amount)
        {
            if (amount <= 0) return;
            int idx = (int)ore;
            _sessionGained[idx] += amount;
            OnSessionGainedChanged?.Invoke(ore, _sessionGained[idx]);
        }




        public void AddBarsDirect(OreType ore, int amount)
        {
            if (amount <= 0) return;
            int idx = (int)ore;
            _oreCounts[idx] += amount;
            OnOreChanged?.Invoke(ore, _oreCounts[idx]);
        }



        public bool TrySpendOre(OreType ore, long amount)
        {
            if (amount <= 0) return true;
            int idx = (int)ore;
            if (_oreCounts[idx] < amount) return false;
            _oreCounts[idx] -= amount;
            OnOreChanged?.Invoke(ore, _oreCounts[idx]);
            return true;
        }









        public long[] CraftSessionToBars()
        {
            var barsCrafted = new long[_sessionGained.Length];
            int divisor = 3;
            if (SkillTreeManager.Instance != null
                && SkillTreeManager.Instance.GetLevel(UpgradeId.Craft2Material) > 0)
                divisor = 2;
            bool hasGoldRing = ArtifactsManager.Instance != null
                && ArtifactsManager.Instance.HasArtifact(ArtifactId.GoldRing);


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
                long bars = (_sessionGained[i] + divisor - 1) / divisor;
                if (d100Tripled) bars *= 3;
                if (hasGoldRing)
                {

                    int tenths = _goldRingTenths[i] + (int)(bars % 10);
                    bars += bars / 10 + tenths / 10;
                    _goldRingTenths[i] = tenths % 10;
                }
                barsCrafted[i] = bars;
                _oreCounts[i] += bars;
                OnOreChanged?.Invoke((OreType)i, _oreCounts[i]);
            }

            long totalBars = 0;
            for (int i = 0; i < barsCrafted.Length; i++) totalBars += barsCrafted[i];
            if (totalBars > 0)
                Rebuild.Stats.StatsManager.Instance?.RecordBarsCrafted((int)Mathf.Min(totalBars, int.MaxValue));
            return barsCrafted;
        }


        public void ResetSession()
        {
            for (int i = 0; i < _sessionGained.Length; i++)
            {
                _sessionGained[i] = 0;
                OnSessionGainedChanged?.Invoke((OreType)i, 0);
            }
        }


        public void ResetAll()
        {
            for (int i = 0; i < _oreCounts.Length; i++)
            {
                _oreCounts[i] = 0;
                _sessionGained[i] = 0;
                _goldRingTenths[i] = 0;
                OnOreChanged?.Invoke((OreType)i, 0);
                OnSessionGainedChanged?.Invoke((OreType)i, 0);
            }
        }


        public void LoadData(GameDataDTO data)
        {
            if (data == null) return;
            int n = Mathf.Min(_oreCounts.Length, data.oreCounts?.Length ?? 0);
            for (int i = 0; i < _oreCounts.Length; i++)
            {
                _goldRingTenths[i] = i < (data.goldRingTenths?.Length ?? 0) ? Mathf.Clamp(data.goldRingTenths[i], 0, 9) : 0;
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
            if (data.goldRingTenths == null || data.goldRingTenths.Length != _goldRingTenths.Length)
                data.goldRingTenths = new int[_goldRingTenths.Length];
            Array.Copy(_goldRingTenths, data.goldRingTenths, _goldRingTenths.Length);
        }
    }
}
