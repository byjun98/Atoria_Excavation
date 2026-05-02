using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Rebuild.Mining
{
    /// <summary>full-screen "Artifact found!" popup on Canvas_ArtifactFound (sortingOrder 299).
    /// Lives at scene scope so Rock.FadeAndDestroy can reach it via the static instance.</summary>
    public sealed class ArtifactFoundPopupUI : MonoBehaviour
    {
        public static ArtifactFoundPopupUI Instance { get; private set; }

        [SerializeField] GameObject panelRoot;
        [SerializeField] Image bigIcon;
        [SerializeField] TMP_Text titleText;
        [SerializeField] TMP_Text descText;
        [SerializeField] Button coolButton;

        Coroutine _showCo;

        void Awake()
        {
            Instance = this;
            if (coolButton != null)
            {
                coolButton.onClick.RemoveAllListeners();
                coolButton.onClick.AddListener(Hide);
            }
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void Show(ArtifactId id)
        {
            var info = ArtifactDef.Get(id);
            if (bigIcon != null)
            {
                // same Resources lookup as ArtifactNodeUI. Custom PNG = render at
                // full saturation; placeholder rock sprite keeps the ArtifactDef tint.
                var custom = SpriteLibrary.GetArtifactSprite(id);
                bool hasCustom = custom != null && custom != RockSpriteFactory.GetRockSprite();
                bigIcon.sprite = custom;
                bigIcon.color = hasCustom ? Color.white : info.Tint;
                bigIcon.preserveAspect = false;
                bigIcon.raycastTarget = false;
            }
            if (titleText != null) titleText.text = $"Artifact found!\n{info.Name}";
            if (descText != null) descText.text = info.Description;
            if (panelRoot != null) panelRoot.SetActive(true);
            if (gameObject.activeInHierarchy)
            {
                if (_showCo != null) StopCoroutine(_showCo);
                _showCo = StartCoroutine(IconPop());
            }
        }

        public void Hide()
        {
            Rebuild.Audio.AudioManager.Instance?.Play("Click_1");
            if (panelRoot != null) panelRoot.SetActive(false);
        }

        IEnumerator IconPop()
        {
            if (bigIcon == null) yield break;
            var t = bigIcon.transform;
            t.localScale = Vector3.zero;
            float dur = 0.32f, k = 0f;
            while (k < dur)
            {
                k += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(k / dur);
                // ease-out cubic
                float e = 1f - Mathf.Pow(1f - p, 3f);
                t.localScale = Vector3.one * Mathf.Lerp(0f, 1.15f, e);
                yield return null;
            }
            // settle a touch
            k = 0f; float settle = 0.12f;
            while (k < settle)
            {
                k += Time.unscaledDeltaTime;
                t.localScale = Vector3.Lerp(Vector3.one * 1.15f, Vector3.one, Mathf.Clamp01(k / settle));
                yield return null;
            }
            t.localScale = Vector3.one;
            _showCo = null;
        }
    }
}
