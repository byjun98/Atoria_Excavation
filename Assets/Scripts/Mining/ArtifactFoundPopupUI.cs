using System.Collections;
using Rebuild.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Rebuild.Mining
{
    public sealed class ArtifactFoundPopupUI : MonoBehaviour
    {
        public const string Heading = "유물 발굴";
        public static ArtifactFoundPopupUI Instance { get; private set; }

        [SerializeField] GameObject panelRoot;
        [SerializeField] Image bigIcon;
        [SerializeField] TMP_Text titleText;
        [SerializeField] TMP_Text nameText;
        [SerializeField] TMP_Text descText;
        [SerializeField] Button coolButton;
        [SerializeField] ArtifactDiscoveryPreview preview;
        [SerializeField] RectTransform showcase;
        [SerializeField] RawImage halo;
        [SerializeField] RawImage[] sparks;

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
            if (panelRoot != null) panelRoot.SetActive(true);
            bool showingModel = preview != null && preview.Show(id);
            if (bigIcon != null)
            {
                bigIcon.enabled = !showingModel;
                if (!showingModel)
                {
                    var sprite = SpriteLibrary.GetArtifactSprite(id);
                    bigIcon.sprite = sprite;
                    bigIcon.color = sprite != null && sprite != RockSpriteFactory.GetRockSprite() ? Color.white : info.Tint;
                }
            }

            if (titleText != null) titleText.text = nameText != null ? Heading : $"{Heading}\n{info.Name}";
            if (nameText != null) nameText.text = info.Name;

            if (descText != null)
                descText.text = descText.richText
                    ? "<nobr>" + info.Description.Replace(" ", "</nobr> <nobr>") + "</nobr>"
                    : info.Description;
            if (!isActiveAndEnabled) return;
            if (_showCo != null) StopCoroutine(_showCo);
            _showCo = StartCoroutine(PlayReveal());
        }

        public void Hide()
        {
            Rebuild.Audio.AudioManager.Instance?.Play("Click_1");
            if (_showCo != null)
            {
                StopCoroutine(_showCo);
                _showCo = null;
            }
            if (panelRoot != null) panelRoot.SetActive(false);
        }

        IEnumerator PlayReveal()
        {
            const float duration = 1.05f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                float open = Mathf.Clamp01(elapsed / .38f);
                float ease = 1f - Mathf.Pow(1f - open, 3f);
                if (showcase != null)
                    showcase.localScale = Vector3.one * (.84f + .16f * ease + .032f * Mathf.Sin(Mathf.PI * ease));
                float progress = elapsed / duration;
                float amplitude = 3f * (1f - progress);
                ApplyTextWave(titleText, elapsed, amplitude);
                ApplyTextWave(nameText, elapsed + .16f, amplitude);
                var gold = MuseumUiTheme.Gold;
                if (halo != null)
                {
                    halo.rectTransform.localScale = Vector3.one * (.66f + .34f * ease);
                    gold.a = Mathf.Lerp(.42f, .16f, progress);
                    halo.color = gold;
                }

                for (int i = 0; sparks != null && i < sparks.Length; i++)
                {
                    float life = Mathf.Clamp01((elapsed - (i % 4) * .046f) / .86f);
                    var spark = sparks[i];
                    if (spark == null) continue;
                    float angle = (i + .5f) * Mathf.PI * 2f / sparks.Length;
                    float spread = Mathf.Lerp(114f, 229f + (i % 3) * 11f, 1f - (1f - life) * (1f - life));
                    spark.rectTransform.anchoredPosition = new Vector2(Mathf.Cos(angle) * spread, Mathf.Sin(angle) * spread * .78f + life * 21f);
                    spark.rectTransform.localScale = Vector3.one * Mathf.Lerp(1.18f, .56f, life);
                    gold.a = Mathf.Sin(Mathf.PI * life) * .88f;
                    spark.color = gold;
                    spark.enabled = gold.a > .001f;
                }
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            if (titleText != null) titleText.ForceMeshUpdate();
            if (nameText != null) nameText.ForceMeshUpdate();
            if (showcase != null) showcase.localScale = Vector3.one;
            if (halo != null)
            {
                halo.rectTransform.localScale = Vector3.one;
                var fadedGold = MuseumUiTheme.Gold;
                fadedGold.a = .16f;
                halo.color = fadedGold;
            }
            if (sparks != null)
            {
                foreach (var spark in sparks)
                    if (spark != null) spark.enabled = false;
            }
            _showCo = null;
        }

        static void ApplyTextWave(TMP_Text text, float time, float amplitude)
        {
            if (text == null || !text.isActiveAndEnabled) return;
            text.ForceMeshUpdate();
            var info = text.textInfo;
            for (int i = 0; i < info.characterCount; i++)
            {
                var character = info.characterInfo[i];
                if (!character.isVisible) continue;
                var vertices = info.meshInfo[character.materialReferenceIndex].vertices;
                float offset = Mathf.Sin(time * 11f - i * .55f) * amplitude;
                for (int corner = 0; corner < 4; corner++)
                    vertices[character.vertexIndex + corner].y += offset;
            }
            text.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices);
        }
    }
}
