using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Rebuild.Mining
{
    /// <summary>left sidebar showing 8 ore bar (주괴) counts for the MainMenu tabs.
    /// Active across all 5 tabs (child of MainMenu, deactivates with it during Mining Session
    /// where Canvas_UI_MiningScene's MaterialFrame_topLeft takes over).
    /// Subscribes to GoldAndOreMechanics.OnOreChanged and updates the matching row.
    /// paradigm: GoldAndOreMechanics._oreCounts is 주괴 (bars) currency.</summary>
    public sealed class ResourcePanelUI : MonoBehaviour
    {
        [Serializable]
        public class Row
        {
            public Image icon;
            public TMP_Text countLabel;
        }

        /// <summary>MineOrePopupFx queries this for the fly-to RectTransform.</summary>
        public static ResourcePanelUI Instance { get; private set; }

        [SerializeField] Row[] rows = new Row[8];

        bool _subscribed;
        Coroutine[] _pulseRoutines;
        Vector3[] _pulseBaseScales;

        void Awake()
        {
            Instance = this;
            _pulseRoutines = new Coroutine[rows.Length];
            _pulseBaseScales = new Vector3[rows.Length];
            for (int i = 0; i < rows.Length; i++)
            {
                var icon = rows[i] != null ? rows[i].icon : null;
                _pulseBaseScales[i] = icon != null ? icon.transform.localScale : Vector3.one;
            }
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void OnEnable()
        {
            if (GoldAndOreMechanics.Instance != null)
            {
                GoldAndOreMechanics.Instance.OnOreChanged += HandleChanged;
                _subscribed = true;
            }
            RefreshAll();
        }

        void OnDisable()
        {
            if (_subscribed && GoldAndOreMechanics.Instance != null)
            {
                GoldAndOreMechanics.Instance.OnOreChanged -= HandleChanged;
                _subscribed = false;
            }
        }

        /// <summary>returns the icon RectTransform for <paramref name="ore"/>, or
        /// null if the row wasn't wired. MineOrePopupFx converts this through
        /// <c>RectTransformUtility.WorldToScreenPoint</c> + ScreenPointToLocalPointInRectangle
        /// when computing its fly-to anchored pos.</summary>
        public RectTransform GetRowIconRT(OreType ore)
        {
            int idx = (int)ore;
            if (idx < 0 || idx >= rows.Length) return null;
            var row = rows[idx];
            if (row == null || row.icon == null) return null;
            return row.icon.rectTransform;
        }

        /// <summary>short 1.25× scale punch on the row icon. Called by MineOrePopupFx
        /// at the arrival of an ore tick so the row visibly "catches" the credited bar.</summary>
        public void PulseRow(OreType ore)
        {
            int idx = (int)ore;
            if (idx < 0 || idx >= rows.Length || rows[idx] == null || rows[idx].icon == null) return;
            if (!gameObject.activeInHierarchy) return;
            if (_pulseRoutines[idx] != null) StopCoroutine(_pulseRoutines[idx]);
            _pulseRoutines[idx] = StartCoroutine(PulseRoutine(idx));
        }

        IEnumerator PulseRoutine(int idx)
        {
            var icon = rows[idx].icon;
            if (icon == null) yield break;
            var t = icon.transform;
            var basis = _pulseBaseScales[idx];
            var big = basis * 1.25f;
            float dur = 0.10f, k = 0f;
            while (k < dur)
            {
                k += Time.unscaledDeltaTime;
                t.localScale = Vector3.Lerp(basis, big, Mathf.Clamp01(k / dur));
                yield return null;
            }
            k = 0f;
            float settle = 0.14f;
            while (k < settle)
            {
                k += Time.unscaledDeltaTime;
                t.localScale = Vector3.Lerp(big, basis, Mathf.Clamp01(k / settle));
                yield return null;
            }
            t.localScale = basis;
            _pulseRoutines[idx] = null;
        }

        void HandleChanged(OreType ore, long newCount)
        {
            int idx = (int)ore;
            if (idx < 0 || idx >= rows.Length) return;
            UpdateRow(idx, newCount);
        }

        void RefreshAll()
        {
            var mech = GoldAndOreMechanics.Instance;
            for (int i = 0; i < rows.Length; i++)
            {
                long c = mech != null ? mech.GetCount((OreType)i) : 0;
                UpdateRow(i, c);
            }
        }

        void UpdateRow(int idx, long count)
        {
            var row = rows[idx];
            if (row == null) return;
            // 통일된 K/M/B/T 포맷 (100k 미만은 thousand separator).
            if (row.countLabel != null) row.countLabel.text = Rebuild.Util.FormatNumbers.Format(count);
            EnsureIconSprite(idx);
        }

        void EnsureIconSprite(int idx)
        {
            var row = rows[idx];
            if (row == null || row.icon == null) return;
            var ore = (OreType)idx;
            var bar = SpriteLibrary.GetOreBarIcon(ore);
            if (bar != null)
            {
                row.icon.sprite = bar;
                row.icon.color = Color.white;
                row.icon.preserveAspect = true;
            }
            else
            {
                if (row.icon.sprite != null) return; // keep any scene-wired fallback if bar art is absent
                var hud = SpriteLibrary.GetOreHudIcon(ore);
                if (hud != null)
                {
                    row.icon.sprite = hud;
                    row.icon.color = Color.white;
                    row.icon.preserveAspect = true;
                }
                else
                {
                    row.icon.sprite = RockSpriteFactory.GetRockSprite();
                    row.icon.color = RockSpriteFactory.GetOreColor(ore);
                    row.icon.preserveAspect = false;
                }
            }
        }

        // 자체 FormatCount 제거, Rebuild.Util.FormatNumbers.Format 으로 통일.
    }
}
