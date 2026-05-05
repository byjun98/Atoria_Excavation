using System;
using Rebuild.Saving;
using UnityEngine;

namespace Rebuild.Mining
{
    /// <summary>orchestrates the BigRock + Credits endgame.
    /// Singleton on MiningSessionRoot. Evaluates trigger every time AnvilManager.OnCraftStateChanged
    /// or ArtifactsManager.OnArtifactFound fires (cheap — both events are low-frequency).
    /// Original game gates the ending behind pickaxe14 craft + finalUpgrade purchase; Shell uses
    /// the two simplest conditions both fully wired today — all 5 pickaxes crafted + all 6 artifacts found.
    /// Once <see cref="IsEligible"/> flips to true we fire <see cref="OnEligible"/>; the EndingFrameUI
    /// listens and presents the trigger button. Watching the credits sets <c>isEndingCompleted</c>.</summary>
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
            // AnvilManager / ArtifactsManager are the only systems that can move the trigger needle.
            // Subscribe defensively in case they came online after our Awake.
            HookEvents();
        }

        void Start()
        {
            // After Awake order is settled, re-bind (matches AchievementsManager double-bind pattern).
            HookEvents();
            // First evaluation — handles loaded saves that already meet the threshold.
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
            // FinalUpgrade SkillTree 구매 이벤트도 watch (AND 조건 충족 즉시 OnEligible 발화).
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
        // FinalUpgrade level 변화 → 엔딩 자격 재평가.
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
                // Defensive: shouldn't happen mid-run but keeps cache truthful after ResetAll.
                _eligibleCached = false;
            }
        }

        bool EvaluateInternal()
        {
            if (_completed) return true;
            var anvil = AnvilManager.Instance;
            var artifacts = ArtifactsManager.Instance;
            if (anvil == null || artifacts == null) return false;
            // All 5 pickaxes crafted.
            for (int i = 0; i < PickaxeDef.Count; i++)
                if (!anvil.IsCrafted(i)) return false;
            // All 6 artifacts found.
            if (!artifacts.AllFound()) return false;
            // FinalUpgrade SkillTree 노드 구매 AND 조건 (원본 게이팅 매칭).
            // 이전엔 dead node (구매 가능하나 효과 0). 원본 SkillTree.PurchaseUpgrade("FinalUpgrade")
            // 가 endingButton.SetActive(true) 했던 모델 — 우리 모델에서는 IsEligible AND gate 으로 매칭.
            // SkillTreeManager 가 null 이면 skip (시스템 초기화 전 보호).
            var sk = SkillTreeManager.Instance;
            if (sk != null && sk.GetLevel(UpgradeId.FinalUpgrade) <= 0) return false;
            return true;
        }

        /// <summary>Called by EndingFrameUI when the credits finish.</summary>
        public void MarkEnded()
        {
            if (_completed) return;
            _completed = true;
            OnEnded?.Invoke();
            // BeatGame achievement composite watches IsCompleted. Trigger an
            // explicit re-evaluation since no StatsManager tick is involved in this transition.
            Rebuild.Stats.AchievementsManager.Instance?.EvaluateExtra();
        }

        public void ResetAll()
        {
            _completed = false;
            _eligibleCached = false;
        }

        // ---- IDataPersistence ----
        public void LoadData(GameDataDTO data)
        {
            if (data == null) return;
            _completed = data.isEndingCompleted;
            // Defer eligibility re-fire until other managers' LoadData populates them — Start handles it.
        }

        public void SaveData(GameDataDTO data)
        {
            if (data == null) return;
            data.isEndingCompleted = _completed;
        }
    }
}
