using System;
using Rebuild.Saving;
using UnityEngine;

namespace Rebuild.Mining
{




    public sealed class SkillTreeManager : MonoBehaviour, Rebuild.Saving.IDataPersistence
    {
        public static SkillTreeManager Instance { get; private set; }

        [SerializeField] int[] _levels = new int[UpgradeDef.Count];

        public event Action<UpgradeId, int> OnUpgradeChanged;
        public event Action<UpgradeId> OnPurchaseFailed;

        void Awake()
        {
            if (_levels == null || _levels.Length != UpgradeDef.Count) _levels = new int[UpgradeDef.Count];
            Instance = this;
        }

        void Start()
        {
            RestoreProgressiveTreeObjects();
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void RestoreProgressiveTreeObjects()
        {
            var nodes = FindObjectsByType<TreeNodeUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < nodes.Length; i++)
            {
                if (nodes[i] != null && !nodes[i].gameObject.activeSelf)
                    nodes[i].gameObject.SetActive(true);
            }

            var edges = FindObjectsByType<SkillTreeEdgeUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < edges.Length; i++)
            {
                if (edges[i] != null && !edges[i].gameObject.activeSelf)
                    edges[i].gameObject.SetActive(true);
            }
        }

        public int GetLevel(UpgradeId id) => _levels[(int)id];
        public int GetMaxLevel(UpgradeId id) => UpgradeDef.Get(id).MaxLevel;
        public bool IsMaxed(UpgradeId id) => GetLevel(id) >= GetMaxLevel(id);
        public OreType GetCurrency(UpgradeId id) => UpgradeDef.Get(id).Currency;




        public bool IsPrereqMet(UpgradeId id)
        {
            var parent = SkillTreeLayout.GetParent(id);
            if (parent == null) return true;
            return GetLevel(parent.Value) > 0;
        }



        public int GetNextPrice(UpgradeId id)
        {
            var info = UpgradeDef.Get(id);
            int lv = GetLevel(id);
            if (lv >= info.PerLevelPrices.Length) return -1;
            return DiscountedPrice(info.PerLevelPrices[lv]);
        }



        public static int DiscountedPrice(int raw)
        {
            if (LevelMechanics.Instance != null && LevelMechanics.Instance.HasTalent(TalentId.SteamSale))
                return Mathf.Max(1, Mathf.CeilToInt(raw * 0.93f));
            return raw;
        }

        public bool TryPurchase(UpgradeId id)
        {

            if (!IsPrereqMet(id))
            {
                Rebuild.Audio.AudioManager.Instance?.Play("CantAfford");
                OnPurchaseFailed?.Invoke(id);
                return false;
            }
            if (IsMaxed(id))
            {
                OnPurchaseFailed?.Invoke(id);
                return false;
            }
            int price = GetNextPrice(id);
            var currency = GetCurrency(id);
            var ore = GoldAndOreMechanics.Instance;
            if (ore == null) { OnPurchaseFailed?.Invoke(id); return false; }
            if (!ore.TrySpendOre(currency, price))
            {
                Rebuild.Audio.AudioManager.Instance?.Play("CantAfford");
                OnPurchaseFailed?.Invoke(id);
                return false;
            }
            _levels[(int)id]++;
            Rebuild.Audio.AudioManager.Instance?.Play("Click_1");
            OnUpgradeChanged?.Invoke(id, _levels[(int)id]);
            return true;
        }


        public void ResetAll()
        {
            for (int i = 0; i < _levels.Length; i++)
            {
                _levels[i] = 0;
                OnUpgradeChanged?.Invoke((UpgradeId)i, 0);
            }
        }


        public void LoadData(GameDataDTO data)
        {
            if (data == null) return;
            if (_levels == null || _levels.Length != UpgradeDef.Count) _levels = new int[UpgradeDef.Count];
            if (data.skillTreeLevels != null)
            {
                int n = Mathf.Min(_levels.Length, data.skillTreeLevels.Length);
                for (int i = 0; i < _levels.Length; i++)
                    _levels[i] = i < n ? Mathf.Max(0, data.skillTreeLevels[i]) : 0;
            }
            else
            {
                for (int i = 0; i < _levels.Length; i++) _levels[i] = 0;
            }
            for (int i = 0; i < _levels.Length; i++)
                OnUpgradeChanged?.Invoke((UpgradeId)i, _levels[i]);
        }

        public void SaveData(GameDataDTO data)
        {
            if (data == null) return;
            if (data.skillTreeLevels == null || data.skillTreeLevels.Length != _levels.Length)
                data.skillTreeLevels = new int[_levels.Length];
            for (int i = 0; i < _levels.Length; i++) data.skillTreeLevels[i] = _levels[i];
        }
    }
}
