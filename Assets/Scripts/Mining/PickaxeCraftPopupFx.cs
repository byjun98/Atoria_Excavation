using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Rebuild.Mining
{







    public sealed class PickaxeCraftPopupFx : MonoBehaviour
    {
        const float PunchInDuration = 0.25f;
        const float SettleDuration = 0.25f;
        const float HoldDuration = 1.60f;
        const float FadeOutDuration = 0.25f;
        const float PeakScale = 1.6f;
        const float SettledScale = 1.0f;
        const float DefaultSize = 240f;

        Image _img;
        RectTransform _rt;
        Coroutine _running;
        bool _initialized;




        public static PickaxeCraftPopupFx Spawn(RectTransform parent, Sprite pickaxeSprite,
                                                  Color tint, float size = DefaultSize)
        {
            if (parent == null) return null;
            var go = new GameObject("PickaxeCraftPopupFx",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(size, size);
            rt.localScale = Vector3.zero;
            rt.SetAsLastSibling();

            var img = go.GetComponent<Image>();
            img.sprite = pickaxeSprite;
            img.color = tint;
            img.preserveAspect = true;
            img.raycastTarget = false;

            var fx = go.AddComponent<PickaxeCraftPopupFx>();
            fx.Init(img, rt);
            return fx;
        }

        void Init(Image img, RectTransform rt)
        {
            _img = img;
            _rt = rt;
            _initialized = true;
            if (_running != null) StopCoroutine(_running);
            _running = StartCoroutine(Lifecycle());
        }

        void OnEnable()
        {
            if (!_initialized) return;
            if (_running != null) StopCoroutine(_running);
            _running = StartCoroutine(Lifecycle());
        }

        void OnDisable()
        {

            if (!_initialized) return;
            if (_running != null) StopCoroutine(_running);
            _running = null;
            if (gameObject != null) Destroy(gameObject);
        }

        IEnumerator Lifecycle()
        {

            float t = 0f;
            while (t < PunchInDuration)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / PunchInDuration);
                float s = Mathf.Sin(k * Mathf.PI * 0.5f) * PeakScale;
                _rt.localScale = Vector3.one * s;
                yield return null;
            }
            _rt.localScale = Vector3.one * PeakScale;


            t = 0f;
            while (t < SettleDuration)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / SettleDuration));
                float s = Mathf.Lerp(PeakScale, SettledScale, k);
                _rt.localScale = Vector3.one * s;
                yield return null;
            }
            _rt.localScale = Vector3.one * SettledScale;


            float hold = 0f;
            while (hold < HoldDuration)
            {
                hold += Time.unscaledDeltaTime;
                yield return null;
            }


            t = 0f;
            Color baseColor = _img != null ? _img.color : Color.white;
            while (t < FadeOutDuration)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / FadeOutDuration);
                if (_img != null)
                    _img.color = new Color(baseColor.r, baseColor.g, baseColor.b, baseColor.a * (1f - k));
                yield return null;
            }

            Destroy(gameObject);
        }
    }
}
