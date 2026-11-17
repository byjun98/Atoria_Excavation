using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Rebuild.Mining
{







    public sealed class ArtifactRockOverlayUI : MonoBehaviour
    {
        const float PulseDuration = 0.5f;
        const float MoveDuration = 0.4f;
        const float FinalScale = 0.35f;
        const float DefaultSize = 110f;

        Image _img;
        RectTransform _rt;
        Vector2 _startAnchored;
        Vector2 _targetAnchored;
        Coroutine _running;
        bool _initialized;



        public static ArtifactRockOverlayUI Spawn(Transform parent, Vector2 anchoredPos,
                                                    Vector2 targetAnchored, ArtifactId id)
        {
            if (parent == null) return null;
            var info = ArtifactDef.Get(id);
            var sprite = SpriteLibrary.GetArtifactSprite(id);
            bool hasCustom = sprite != null && sprite != RockSpriteFactory.GetRockSprite();

            var go = new GameObject($"ArtifactRockOverlay_{id}",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = new Vector2(DefaultSize, DefaultSize);
            rt.localScale = Vector3.zero;
            rt.SetAsLastSibling();

            var img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.color = hasCustom ? Color.white : info.Tint;
            img.preserveAspect = true;
            img.raycastTarget = false;

            var fx = go.AddComponent<ArtifactRockOverlayUI>();
            fx.Init(img, rt, anchoredPos, targetAnchored);
            return fx;
        }

        void Init(Image img, RectTransform rt, Vector2 start, Vector2 target)
        {
            _img = img;
            _rt = rt;
            _startAnchored = start;
            _targetAnchored = target;
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
            while (t < PulseDuration)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / PulseDuration);
                float s = Mathf.Sin(k * Mathf.PI) * 0.2f + Mathf.Lerp(0f, 1f, k);
                _rt.localScale = Vector3.one * s;
                if (_img != null)
                {
                    var c = _img.color; c.a = Mathf.Lerp(0f, 0.95f, k);
                    _img.color = c;
                }
                yield return null;
            }



            t = 0f;
            while (t < MoveDuration)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / MoveDuration);
                float s = Mathf.SmoothStep(0f, 1f, k);
                _rt.anchoredPosition = Vector2.LerpUnclamped(_startAnchored, _targetAnchored, s);
                float scl = Mathf.Lerp(1f, FinalScale, s);
                _rt.localScale = Vector3.one * scl;
                if (_img != null)
                {
                    var c = _img.color; c.a = Mathf.Lerp(0.95f, 0.4f, k);
                    _img.color = c;
                }
                yield return null;
            }

            Destroy(gameObject);
        }
    }
}
