using System;
using Rebuild.Saving;
using UnityEngine;

namespace Rebuild.Mining
{







    public sealed class TheEndingManager : MonoBehaviour, Rebuild.Saving.IDataPersistence
    {
        public static TheEndingManager Instance { get; private set; }

        [SerializeField] bool _completed;

        bool _eligibleCached;

        public bool IsCompleted => _completed;
        public bool IsEligible => EvaluateInternal();

        public event Action OnEligible;
        public event Action OnEnded;

        void Awake()
        {
            Instance = this;
        }

        void OnEnable()
        {


            HookEvents();
        }

        void Start()
        {

            HookEvents();

            Evaluate();
        }

        void OnDisable()
        {
            UnhookEvents();
        }

        void OnDestroy()
        {
            UnhookEvents();
            if (Instance == this) Instance = null;
        }

        void HookEvents()
        {
            if (AnvilManager.Instance != null)
            {
                AnvilManager.Instance.OnCraftStateChanged -= OnAnvilCraftChanged;
                AnvilManager.Instance.OnCraftStateChanged += OnAnvilCraftChanged;
            }
            if (ArtifactsManager.Instance != null)
            {
                ArtifactsManager.Instance.OnArtifactFound -= OnArtifactFound;
                ArtifactsManager.Instance.OnArtifactFound += OnArtifactFound;
            }

            if (SkillTreeManager.Instance != null)
            {
                SkillTreeManager.Instance.OnUpgradeChanged -= OnSkillTreeChanged;
                SkillTreeManager.Instance.OnUpgradeChanged += OnSkillTreeChanged;
            }
        }

        void UnhookEvents()
        {
            if (AnvilManager.Instance != null)
                AnvilManager.Instance.OnCraftStateChanged -= OnAnvilCraftChanged;
            if (ArtifactsManager.Instance != null)
                ArtifactsManager.Instance.OnArtifactFound -= OnArtifactFound;
            if (SkillTreeManager.Instance != null)
                SkillTreeManager.Instance.OnUpgradeChanged -= OnSkillTreeChanged;
        }

        void OnAnvilCraftChanged(int idx, bool crafted) => Evaluate();
        void OnArtifactFound(ArtifactId id) => Evaluate();

        void OnSkillTreeChanged(UpgradeId id, int level)
        {
            if (id == UpgradeId.FinalUpgrade) Evaluate();
        }

        public void Evaluate()
        {
            bool eligible = EvaluateInternal();
            if (eligible && !_eligibleCached)
            {
                _eligibleCached = true;
                OnEligible?.Invoke();
            }
            else if (!eligible && _eligibleCached)
            {

                _eligibleCached = false;
            }
        }

        bool EvaluateInternal()
        {
            if (_completed) return true;
            var anvil = AnvilManager.Instance;
            var artifacts = ArtifactsManager.Instance;
            if (anvil == null || artifacts == null) return false;

            for (int i = 0; i < PickaxeDef.Count; i++)
                if (!anvil.IsCrafted(i)) return false;

            if (!artifacts.AllFound()) return false;




            var sk = SkillTreeManager.Instance;
            if (sk != null && sk.GetLevel(UpgradeId.FinalUpgrade) <= 0) return false;
            return true;
        }


        public void MarkEnded()
        {
            if (_completed) return;
            _completed = true;
            OnEnded?.Invoke();


            Rebuild.Stats.AchievementsManager.Instance?.EvaluateExtra();
        }

        public void ResetAll()
        {
            _completed = false;
            _eligibleCached = false;
        }


        public void LoadData(GameDataDTO data)
        {
            if (data == null) return;
            _completed = data.isEndingCompleted;

        }

        public void SaveData(GameDataDTO data)
        {
            if (data == null) return;
            data.isEndingCompleted = _completed;
        }
    }
}
