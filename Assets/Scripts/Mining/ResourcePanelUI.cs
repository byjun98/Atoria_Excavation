using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Rebuild.Mining
{





    public sealed class ResourcePanelUI : MonoBehaviour
    {
        [Serializable]
        public class Row
        {
            public Image icon;
            public TMP_Text countLabel;
        }


        public static ResourcePanelUI Instance { get; private set; }

        [SerializeField] Row[] rows = new Row[8];
        [SerializeField] RectTransform tooltip;
        [SerializeField] TMP_Text tooltipName;
        [SerializeField] TMP_Text tooltipDescription;
        [SerializeField] TMP_Text tooltipCount;

        static readonly string[] Descriptions =
        {
            "초반 스킬을 구매하고 곡괭이를 복원할 때 쓰입니다.",
            "발굴한 토기 파편을 정산해 얻습니다. 스킬 구매와 곡괭이 복원에 쓰입니다.",
            "발굴한 청동조각을 정산해 얻습니다. 청동조각이 필요한 스킬과 곡괭이에 쓰입니다.",
            "발굴한 철편을 정산해 얻습니다. 철편이 필요한 강화에 쓰입니다.",
            "발굴한 옥조각을 정산해 얻습니다. 옥조각이 필요한 스킬을 구매할 때 쓰입니다.",
            "발굴한 유리구슬을 정산해 얻습니다. 유리구슬이 필요한 강화에 쓰입니다.",
            "발굴한 금조각을 정산해 얻습니다. 금조각이 필요한 강화에 쓰입니다.",
            "발굴한 사리를 정산해 얻습니다. 사리가 필요한 강화에 쓰입니다."
        };

        bool _subscribed;
        int _hoveredRow = -1;
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
            HideResourceTooltip(_hoveredRow);
            if (_subscribed && GoldAndOreMechanics.Instance != null)
            {
                GoldAndOreMechanics.Instance.OnOreChanged -= HandleChanged;
                _subscribed = false;
            }
        }

        public void ConfigureTooltip(RectTransform panel, TMP_Text title, TMP_Text description, TMP_Text amount)
        {
            tooltip = panel;
            tooltipName = title;
            tooltipDescription = description;
            tooltipCount = amount;
        }

        internal void ShowResourceTooltip(int index, RectTransform row)
        {
            if (tooltip == null || index < 0 || index >= Descriptions.Length) return;
            _hoveredRow = index;
            var ore = (OreType)index;
            tooltipName.text = OreLocalization.OreNameKr(ore);
            tooltipDescription.text = Descriptions[index];
            SetTooltipCount(GoldAndOreMechanics.Instance != null ? GoldAndOreMechanics.Instance.GetCount(ore) : 0);
            tooltip.position = row.TransformPoint(new Vector3(row.rect.xMax + 16f, row.rect.center.y, 0f));
            tooltip.gameObject.SetActive(true);
        }

        internal void HideResourceTooltip(int index)
        {
            if (_hoveredRow != index) return;
            _hoveredRow = -1;
            if (tooltip != null) tooltip.gameObject.SetActive(false);
        }

        void SetTooltipCount(long count)
        {
            tooltipCount.text = $"보유 {count:N0}개";
        }





        public RectTransform GetRowIconRT(OreType ore)
        {
            int idx = (int)ore;
            if (idx < 0 || idx >= rows.Length) return null;
            var row = rows[idx];
            if (row == null || row.icon == null) return null;
            return row.icon.rectTransform;
        }



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

            if (row.countLabel != null) row.countLabel.text = Rebuild.Util.FormatNumbers.Format(count);
            if (idx == _hoveredRow) SetTooltipCount(count);
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
                if (row.icon.sprite != null) return;
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


    }
}
