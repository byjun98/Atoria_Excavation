using System.Collections.Generic;
using Rebuild.Stats;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Rebuild.UI
{




    public sealed class StatsPanelUI : MonoBehaviour
    {
        public static StatsPanelUI Instance { get; private set; }

        [SerializeField] GameObject panelRoot;
        [SerializeField] Button closeButton;
        [SerializeField] ScrollRect scrollRect;
        [SerializeField] TMP_Text achievementSummary;

        [System.Serializable]
        public struct Row { public StatId stat; public TMP_Text valueLabel; }
        [SerializeField] Row[] rows;

        [System.Serializable]
        public struct AchievementRow { public AchievementId id; public TMP_Text titleLabel; public Image swatch; public GameObject lockedOverlay; }
        [SerializeField] AchievementRow[] achievementRows;

        readonly Dictionary<StatId, TMP_Text> _statLabels = new Dictionary<StatId, TMP_Text>();

        void Awake()
        {
            Instance = this;
            if (closeButton != null)
            {
                closeButton.onClick.RemoveAllListeners();
                closeButton.onClick.AddListener(Hide);
            }
            if (scrollRect == null && panelRoot != null)
                scrollRect = panelRoot.GetComponentInChildren<ScrollRect>(includeInactive: true);
            if (rows != null)
            {
                for (int i = 0; i < rows.Length; i++)
                    if (rows[i].valueLabel != null) _statLabels[rows[i].stat] = rows[i].valueLabel;
            }
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void OnEnable()
        {
            if (StatsManager.Instance != null)
                StatsManager.Instance.OnStatChanged += HandleStatChanged;
            if (AchievementsManager.Instance != null)
                AchievementsManager.Instance.OnAchievementUnlocked += HandleAchievementUnlocked;
            RefreshAll();
        }

        void OnDisable()
        {
            if (StatsManager.Instance != null)
                StatsManager.Instance.OnStatChanged -= HandleStatChanged;
            if (AchievementsManager.Instance != null)
                AchievementsManager.Instance.OnAchievementUnlocked -= HandleAchievementUnlocked;
        }

        public void Show()
        {
            Rebuild.Audio.AudioManager.Instance?.Play("Click_1");
            if (panelRoot != null) panelRoot.SetActive(true);
            if (scrollRect != null)
            {
                scrollRect.horizontalNormalizedPosition = 0f;
                scrollRect.verticalNormalizedPosition = 1f;
            }
            RefreshAll();
        }

        public void Hide()
        {
            Rebuild.Audio.AudioManager.Instance?.Play("Click_1");
            if (panelRoot != null) panelRoot.SetActive(false);
        }

        void HandleStatChanged(StatId id, long value)
        {

            if (_statLabels.TryGetValue(id, out var label) && label != null)
                label.text = Rebuild.Util.FormatNumbers.Format(value);
        }

        void HandleAchievementUnlocked(AchievementId id) => RefreshAchievements();

        void RefreshAll()
        {
            var stats = StatsManager.Instance;
            if (stats != null && rows != null)
            {
                for (int i = 0; i < rows.Length; i++)
                {
                    if (rows[i].valueLabel != null)
                        rows[i].valueLabel.text = Rebuild.Util.FormatNumbers.Format(stats.Get(rows[i].stat));
                }
            }
            RefreshAchievements();
        }

        void RefreshAchievements()
        {
            var ach = AchievementsManager.Instance;
            if (achievementSummary != null)
            {
                int unlocked = ach != null ? ach.UnlockedCount : 0;
                achievementSummary.text = $"업적: {unlocked} / {AchievementDef.Count}";
            }
            if (achievementRows == null) return;
            for (int i = 0; i < achievementRows.Length; i++)
            {
                var row = achievementRows[i];
                var info = AchievementDef.Get(row.id);
                bool unlocked = ach != null && ach.IsUnlocked(row.id);
                if (row.titleLabel != null) row.titleLabel.text = unlocked ? info.Name : "???";
                if (row.swatch != null)
                {
                    row.swatch.sprite = Rebuild.Mining.SpriteLibrary.GetAchievementSprite(row.id);
                    row.swatch.preserveAspect = true;
                    row.swatch.color = unlocked ? info.Tint : new Color(0.3f, 0.3f, 0.3f, 1f);
                }
                if (row.lockedOverlay != null) row.lockedOverlay.SetActive(!unlocked);
            }
        }
    }
}
