using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Rebuild.UI
{
    /// <summary>
    /// Scale-circle screen transition. Lives on Canvas_CircleTransition_andSettings.
    /// 0.4s expand (scale 0 -> 26 cover screen) -> onMidpoint swap -> 0.4s shrink reveal.
    /// Circle sprite is generated at runtime so we don't need an external asset.
    /// </summary>
    [RequireComponent(typeof(Canvas))]
    public sealed class CircleTransitionController : MonoBehaviour
    {
        public static CircleTransitionController Instance { get; private set; }

        [Header("Auto-resolved if left empty")]
        [SerializeField] RectTransform circleTransform;
        [SerializeField] Image circleImage;
        [SerializeField] GameObject transitionBlock;

        [Header("Tuning")]
        [SerializeField] float halfDuration = 0.4f;
        [SerializeField] float maxScale = 26f;
        [SerializeField] Color circleColor = new Color(0.090f, 0.463f, 0.161f, 1f); // forest green #178126

        public bool IsTransitioning { get; private set; }

        const int CircleTextureSize = 256;
        static Sprite s_runtimeCircleSprite;

        void Awake()
        {
            Instance = this;
            var canvas = GetComponent<Canvas>();
            if (canvas != null && canvas.worldCamera == null) canvas.worldCamera = Camera.main;

            AutoResolve();
            EnsureCircleSprite();
            ApplyInitialState();
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void AutoResolve()
        {
            if (circleTransform == null)
            {
                var t = transform.Find("CircleTransition");
                if (t != null) circleTransform = t as RectTransform;
            }
            if (circleImage == null && circleTransform != null)
                circleImage = circleTransform.GetComponent<Image>();
            if (transitionBlock == null)
            {
                var t = transform.Find("TransitionBlock");
                if (t != null) transitionBlock = t.gameObject;
            }
        }

        void EnsureCircleSprite()
        {
            if (circleImage == null) return;
            if (circleImage.sprite != null) return;

            if (s_runtimeCircleSprite == null)
                s_runtimeCircleSprite = CreateCircleSprite(CircleTextureSize);
            circleImage.sprite = s_runtimeCircleSprite;
            circleImage.color = circleColor;
            circleImage.preserveAspect = true;
            circleImage.raycastTarget = false;
        }

        void ApplyInitialState()
        {
            if (circleTransform != null)
            {
                circleTransform.localScale = Vector3.zero;
                circleTransform.gameObject.SetActive(false);
            }
            if (transitionBlock != null)
                transitionBlock.SetActive(false);
        }

        /// <summary>
        /// Run the full expand -> swap -> shrink sequence.
        /// </summary>
        public void PlayTransition(Action onMidpoint)
        {
            if (IsTransitioning) return;
            if (circleTransform == null) { onMidpoint?.Invoke(); return; }
            StartCoroutine(TransitionRoutine(onMidpoint));
        }

        IEnumerator TransitionRoutine(Action onMidpoint)
        {
            IsTransitioning = true;

            circleTransform.gameObject.SetActive(true);
            if (transitionBlock != null) transitionBlock.SetActive(true);

            yield return ScaleCircle(0f, maxScale, halfDuration);

            try { onMidpoint?.Invoke(); }
            catch (Exception e) { Debug.LogException(e); }

            yield return ScaleCircle(maxScale, 0f, halfDuration);

            circleTransform.localScale = Vector3.zero;
            circleTransform.gameObject.SetActive(false);
            if (transitionBlock != null) transitionBlock.SetActive(false);

            IsTransitioning = false;
        }

        IEnumerator ScaleCircle(float from, float to, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float s = Mathf.Lerp(from, to, t);
                circleTransform.localScale = new Vector3(s, s, s);
                yield return null;
            }
            circleTransform.localScale = new Vector3(to, to, to);
        }

        static Sprite CreateCircleSprite(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "Rebuild_CircleTransition_Runtime",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave,
            };
            var pixels = new Color32[size * size];
            float r = size * 0.5f;
            float r2 = r * r;
            float edge = 1.5f; // anti-alias band in pixels
            for (int y = 0; y < size; y++)
            {
                float dy = y - r + 0.5f;
                for (int x = 0; x < size; x++)
                {
                    float dx = x - r + 0.5f;
                    float d2 = dx * dx + dy * dy;
                    float d = Mathf.Sqrt(d2);
                    float a = Mathf.Clamp01((r - d) / edge);
                    byte ab = (byte)Mathf.RoundToInt(a * 255f);
                    pixels[y * size + x] = new Color32(255, 255, 255, ab);
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply(false, false);

            var sprite = Sprite.Create(
                tex,
                new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f),
                100f, 0, SpriteMeshType.FullRect);
            sprite.name = "Rebuild_CircleTransition_Runtime";
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }
    }
}
