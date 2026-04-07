using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Rebuild.Mining
{
    /// <summary>pickaxe sprite that spawns on a rock during a damage tick,
    /// plays a back-swing animation, then fades + destroys. Spawned as a sibling of the rock
    /// (same TileRow) so anchored coordinates align without canvas conversion. Lives until the
    /// swing+fade routine finishes (~0.30s) then destroys itself.
    /// <para>Replaces the old "single mouse-following pickaxe" model from the
    /// original game shows the pickaxe AT the rock being struck, not on the cursor.</para>
    /// </summary>
    public sealed class PickaxeSwingFx : MonoBehaviour
    {
        const float SwingDuration = 0.18f;
        const float FadeDuration = 0.10f;
        const float SwingPeakAngle = -55f;       // back-swing peak (degrees, CCW)
        const float DefaultSize = 80f;

        Image _img;
        RectTransform _rt;

        /// <summary>Spawn a one-shot swing FX at <paramref name="anchoredPos"/> inside
        /// <paramref name="parent"/> (typically the rock's parent TileRow).</summary>
        public static PickaxeSwingFx Spawn(Transform parent, Vector2 anchoredPos,
                                            Sprite sprite, Color tint, float size = DefaultSize)
        {
            if (parent == null) return null;
            var go = new GameObject("PickaxeSwingFx",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            go.transform.SetAsLastSibling(); // render above the rock
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = new Vector2(size, size);

            var img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.color = tint;
            img.preserveAspect = true;
            img.raycastTarget = false;

            return go.AddComponent<PickaxeSwingFx>();
        }

        void Awake()
        {
            _img = GetComponent<Image>();
            _rt = (RectTransform)transform;
            StartCoroutine(SwingThenFade());
        }

        IEnumerator SwingThenFade()
        {
            // Back-swing: 0° → SwingPeakAngle (peak at k=0.5) → 0°.
            float t = 0f;
            while (t < SwingDuration)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / SwingDuration);
                float angle = SwingPeakAngle * Mathf.Sin(k * Mathf.PI);
                _rt.localRotation = Quaternion.Euler(0f, 0f, angle);
                yield return null;
            }
            _rt.localRotation = Quaternion.identity;

            // Fade out.
            t = 0f;
            Color baseColor = _img != null ? _img.color : Color.white;
            while (t < FadeDuration)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / FadeDuration);
                if (_img != null)
                    _img.color = new Color(baseColor.r, baseColor.g, baseColor.b, baseColor.a * (1f - k));
                yield return null;
            }

            Destroy(gameObject);
        }
    }
}
