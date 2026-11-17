using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Rebuild.Saving
{





    public sealed class DataPersistenceManager : MonoBehaviour
    {
        public static DataPersistenceManager Instance { get; private set; }

        [Header("Configuration")]
        [SerializeField] string fileName = "rebuild_keeponmining.json";
        [SerializeField] float autoSaveIntervalSec = 30f;
        [SerializeField] bool loadOnStart = true;
        [SerializeField] bool autoSaveEnabled = true;

        FileDataHandler _file;
        GameDataDTO _current;
        readonly List<IDataPersistence> _all = new List<IDataPersistence>();
        Coroutine _autoSaveCo;
        bool _hasLoaded;

        public event Action OnSaved;
        public event Action OnLoaded;
        public GameDataDTO Current => _current;

        void Awake()
        {
            Instance = this;
            _file = new FileDataHandler(Application.persistentDataPath, fileName);
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Start()
        {
            CollectImplementers();
            if (loadOnStart) LoadGame();
            if (autoSaveEnabled && autoSaveIntervalSec > 0f)
                _autoSaveCo = StartCoroutine(AutoSaveLoop());
        }

        void OnApplicationPause(bool paused)
        {
            if (paused && _hasLoaded) SaveGame();
        }

        void OnApplicationQuit()
        {
            if (_hasLoaded) SaveGame();
        }

        public void CollectImplementers()
        {
            _all.Clear();

            var monos = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var mb in monos)
            {
                if (mb is IDataPersistence p) _all.Add(p);
            }
        }

        public void LoadGame()
        {
            _current = _file.Load();
            if (_current == null)
            {
                _current = new GameDataDTO();
                Debug.Log("[Rebuild.Saving] No existing save → starting fresh.");
            }
            else if (RefundRetiredMineSpeedRanks(_current))
            {


                _file.Save(_current);
            }
            for (int i = 0; i < _all.Count; i++)
            {
                try { _all[i].LoadData(_current); }
                catch (Exception e) { Debug.LogError($"[Rebuild.Saving] LoadData failed on {_all[i]}: {e}"); }
            }
            _hasLoaded = true;
            OnLoaded?.Invoke();
        }

        static bool RefundRetiredMineSpeedRanks(GameDataDTO data)
        {
            const int lastEffectiveLegacyRank = 8;
            if (data.mineSpeedLevel <= lastEffectiveLegacyRank) return false;

            int gold = (int)Rebuild.Mining.OreType.Gold;
            if (data.oreCounts == null || data.oreCounts.Length <= gold)
                Array.Resize(ref data.oreCounts, Rebuild.Mining.OreLocalization.AllNamesKr.Count);
            long refund = data.mineSpeedLevel >= 10 ? 4500L : 1500L;
            data.oreCounts[gold] = Math.Min(data.oreCounts[gold], long.MaxValue - refund) + refund;
            data.mineSpeedLevel = lastEffectiveLegacyRank;
            return true;
        }

        public void NewGame()
        {
            _current = new GameDataDTO();
            for (int i = 0; i < _all.Count; i++)
            {
                try { _all[i].LoadData(_current); }
                catch (Exception e) { Debug.LogError($"[Rebuild.Saving] LoadData(new) failed on {_all[i]}: {e}"); }
            }
            _hasLoaded = true;
            OnLoaded?.Invoke();
        }

        public bool SaveGame()
        {
            if (_current == null) _current = new GameDataDTO();
            for (int i = 0; i < _all.Count; i++)
            {
                try { _all[i].SaveData(_current); }
                catch (Exception e) { Debug.LogError($"[Rebuild.Saving] SaveData failed on {_all[i]}: {e}"); }
            }
            bool ok = _file.Save(_current);
            if (ok) OnSaved?.Invoke();
            return ok;
        }

        public void WipeSaveFile()
        {
            _file?.Delete();
        }

        IEnumerator AutoSaveLoop()
        {


            while (!_hasLoaded) yield return null;
            var wait = new WaitForSecondsRealtime(autoSaveIntervalSec);
            while (autoSaveEnabled)
            {
                yield return wait;
                if (!autoSaveEnabled) yield break;
                SaveGame();
            }
        }
    }
}
