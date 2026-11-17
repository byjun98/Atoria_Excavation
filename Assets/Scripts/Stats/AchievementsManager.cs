using System;
using Rebuild.Saving;
using UnityEngine;

namespace Rebuild.Stats
{




    public sealed class AchievementsManager : MonoBehaviour, Rebuild.Saving.IDataPersistence
    {
        public static AchievementsManager Instance { get; private set; }

        [SerializeField] bool[] _unlocked = new bool[AchievementDef.Count];
        [SerializeField] bool[] _notified = new bool[AchievementDef.Count];

        public event Action<AchievementId> OnAchievementUnlocked;

        public bool IsUnlocked(AchievementId id) => _unlocked[(int)id];
        public int UnlockedCount
        {
            get
            {
                int n = 0;
                for (int i = 0; i < _unlocked.Length; i++) if (_unlocked[i]) n++;
                return n;
            }
        }

        void Awake()
        {
            EnsureArraySizes();
            Instance = this;
        }

        void OnEnable()
        {

            if (StatsManager.Instance != null)
                StatsManager.Instance.OnStatChanged += HandleStatChanged;
        }

        void Start()
        {

            if (StatsManager.Instance != null)
            {
                StatsManager.Instance.OnStatChanged -= HandleStatChanged;
                StatsManager.Instance.OnStatChanged += HandleStatChanged;


                EvaluateAll();
            }
        }

        void OnDisable()
        {
            if (StatsManager.Instance != null)
                StatsManager.Instance.OnStatChanged -= HandleStatChanged;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void HandleStatChanged(StatId id, long _) => EvaluateAll();

        void EvaluateAll()
        {
            var stats = StatsManager.Instance;
            if (stats == null) return;
            for (int i = 0; i < AchievementDef.Count; i++)
            {
                if (_unlocked[i]) continue;
                if (EvaluateComposite((AchievementId)i, stats))
                {
                    Unlock((AchievementId)i);
                    continue;
                }
                var info = AchievementDef.Get((AchievementId)i);
                if (stats.Get(info.Watch) >= info.Threshold)
                {
                    Unlock((AchievementId)i);
                }
            }
        }

        void EnsureArraySizes()
        {
            if (_unlocked == null || _unlocked.Length != AchievementDef.Count)
                _unlocked = new bool[AchievementDef.Count];
            if (_notified == null || _notified.Length != AchievementDef.Count)
                _notified = new bool[AchievementDef.Count];
        }

        void Unlock(AchievementId id)
        {
            int i = (int)id;
            _unlocked[i] = true;
            if (_notified[i]) return;
            _notified[i] = true;
            OnAchievementUnlocked?.Invoke(id);
        }




        static bool EvaluateComposite(AchievementId id, StatsManager stats)
        {
            switch (id)
            {
                case AchievementId.BeamDynamitePlazma:

                    return stats.Get(StatId.ProjectilesFired_Beam) >= 1
                        && stats.Get(StatId.ProjectilesFired_Plazma) >= 1
                        && stats.Get(StatId.ProjectilesFired_Dynamite) >= 1
                        && stats.Get(StatId.ProjectilesFired_Lightning) >= 1;

                case AchievementId.BeatGame:

                    return Rebuild.Mining.TheEndingManager.Instance != null
                        && Rebuild.Mining.TheEndingManager.Instance.IsCompleted;

                case AchievementId.ChestHunter:

                    return stats.Get(StatId.ChestsOpened) + stats.Get(StatId.GoldenChestsOpened) >= 50;

                default:
                    return false;
            }
        }




        public void EvaluateExtra() => EvaluateAll();

        public void ResetAll()
        {
            EnsureArraySizes();
            for (int i = 0; i < _unlocked.Length; i++) _unlocked[i] = false;
            for (int i = 0; i < _notified.Length; i++) _notified[i] = false;
        }


        public void LoadData(GameDataDTO data)
        {
            if (data == null) return;
            EnsureArraySizes();
            if (data.achievementsUnlocked != null)
            {
                int n = Mathf.Min(_unlocked.Length, data.achievementsUnlocked.Length);
                for (int i = 0; i < _unlocked.Length; i++)
                    _unlocked[i] = i < n && data.achievementsUnlocked[i];
            }
            else
            {
                for (int i = 0; i < _unlocked.Length; i++) _unlocked[i] = false;
            }

            bool migrateExistingUnlocksAsNotified = data.schemaVersion < 5 || data.achievementsNotified == null;
            if (migrateExistingUnlocksAsNotified)
            {
                for (int i = 0; i < _notified.Length; i++) _notified[i] = _unlocked[i];
            }
            else
            {
                int n = Mathf.Min(_notified.Length, data.achievementsNotified.Length);
                for (int i = 0; i < _notified.Length; i++)
                    _notified[i] = i < n ? data.achievementsNotified[i] : _unlocked[i];
            }
        }

        public void SaveData(GameDataDTO data)
        {
            if (data == null) return;
            EnsureArraySizes();
            if (data.achievementsUnlocked == null || data.achievementsUnlocked.Length != _unlocked.Length)
                data.achievementsUnlocked = new bool[_unlocked.Length];
            for (int i = 0; i < _unlocked.Length; i++) data.achievementsUnlocked[i] = _unlocked[i];
            if (data.achievementsNotified == null || data.achievementsNotified.Length != _notified.Length)
                data.achievementsNotified = new bool[_notified.Length];
            for (int i = 0; i < _notified.Length; i++) data.achievementsNotified[i] = _notified[i];
        }
    }
}
