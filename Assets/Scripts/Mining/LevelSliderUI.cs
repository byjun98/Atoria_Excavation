using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Rebuild.Mining
{
    /// <summary>XP fill slider + Level number HUD (bottom-left of Canvas_UI_MiningScene).
    /// Subscribes to LevelMechanics events while enabled.</summary>
    public sealed class LevelSliderUI : MonoBehaviour
    {
        [SerializeField] Slider xpSlider;
        [SerializeField] TMP_Text levelLabel;
        [SerializeField] TMP_Text xpNeededText;

        Coroutine _punchCo;

        void OnEnable()
        {
            if (LevelMechanics.Instance != null)
            {
                LevelMechanics.Instance.OnXpChanged += HandleXpChanged;
                LevelMechanics.Instance.OnLevelUp += HandleLevelUp;
                SyncFromMechanics();
            }
        }

        void OnDisable()
        {
            if (LevelMechanics.Instance != null)
            {
                LevelMechanics.Instance.OnXpChanged -= HandleXpChanged;
                LevelMechanics.Instance.OnLevelUp -= HandleLevelUp;
            }
        }

        void Start()
        {
            // Mechanics may not have existed during OnEnable on the very first frame.
            if (LevelMechanics.Instance != null) SyncFromMechanics();
        }

        void SyncFromMechanics()
        {
            var lm = LevelMechanics.Instance;
            if (lm == null) return;
            HandleXpChanged(lm.CurrentXp, lm.XpToNextLevel);
            UpdateLevelLabel(lm.Level);
        }

        void HandleXpChanged(int currentXp, int xpToNext)
        {
            if (xpSlider != null)
            {
                xpSlider.minValue = 0f;
                xpSlider.maxValue = Mathf.Max(1, xpToNext);
                xpSlider.value = Mathf.Clamp(currentXp, 0, xpSlider.maxValue);
            }
            if (xpNeededText != null) xpNeededText.text = $"{currentXp} / {xpToNext}";
        }

        void HandleLevelUp(int newLevel, int totalTalentPoints)
        {
            UpdateLevelLabel(newLevel);
            if (gameObject.activeInHierarchy)
            {
                if (_punchCo != null) StopCoroutine(_punchCo);
                _punchCo = StartCoroutine(Punch());
            }
        }

        void UpdateLevelLabel(int newLevel)
        {
            if (levelLabel != null) levelLabel.text = $"Lv {newLevel}";
        }

        IEnumerator Punch()
        {
            var t = transform;
            var baseScale = Vector3.one;
            var bigScale = baseScale * 1.18f;
            float dur = 0.18f;
            float k = 0f;
            while (k < dur)
            {
                k += Time.unscaledDeltaTime;
                t.localScale = Vector3.Lerp(baseScale, bigScale, Mathf.Clamp01(k / dur));
                yield return null;
            }
            k = 0f;
            while (k < dur)
            {
                k += Time.unscaledDeltaTime;
                t.localScale = Vector3.Lerp(bigScale, baseScale, Mathf.Clamp01(k / dur));
                yield return null;
            }
            t.localScale = baseScale;
            _punchCo = null;
        }
    }
}
