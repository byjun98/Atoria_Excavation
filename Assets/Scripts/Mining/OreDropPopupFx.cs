using System.Collections;
using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.UI;

namespace Rebuild.Mining
{








    public sealed class OreDropPopupFx : MonoBehaviour
    {
        const float ScaleInDuration = 0.20f;
        const float MoveDurationMin = 0.28f;
        const float MoveDurationMax = 0.42f;
        const float HoverMin = 0.05f;
        const float HoverMax = 0.30f;
        const float PopupSize = 36f;


        const int MaxPopupsPerRock = 8;
        const int MaxIdlePopups = 640;
        const int MaxVisiblePopups = 256;
        const int ReservedPerOre = 8;
        const int OreCount = 8;
        static readonly Stack<OreDropPopupFx> s_idle = new Stack<OreDropPopupFx>();
        static readonly ProfilerMarker s_stepMarker = new ProfilerMarker("Atoria.OreFlight.Step");
        static readonly int[] s_visibleByOre = new int[OreCount];
        static int s_visible;

        Image _img;
        RectTransform _rt;
        Vector2 _targetAnchored;
        Vector3 _baseScale;
        float _spawnDelay;
        bool _arrivalPlayed;
        bool _leased;
        bool _visible;
        int _oreIndex;
        Coroutine _running;





        public static void Spawn(Transform parentCanvasT, Vector3 startWorldPos,
                                  OreType ore, int count, RectTransform targetRT)
        {
            if (parentCanvasT == null || targetRT == null || count <= 0) return;
            var canvas = parentCanvasT as RectTransform ?? parentCanvasT.GetComponent<RectTransform>();
            if (canvas == null || !canvas.gameObject.activeInHierarchy) return;


            var sprite = SpriteLibrary.GetOreHudIcon(ore);
            if (sprite == null) sprite = RockSpriteFactory.GetRockSprite();


            var parentCanvasComp = parentCanvasT.GetComponentInParent<Canvas>();
            Camera cam = parentCanvasComp != null ? parentCanvasComp.worldCamera : null;
            Vector2 startLocal;
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(cam, startWorldPos);
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas, screen, cam, out startLocal))
                startLocal = Vector2.zero;
            Vector2 targetScreen = RectTransformUtility.WorldToScreenPoint(cam, targetRT.position);
            Vector2 targetLocal;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas, targetScreen, cam, out targetLocal))
                targetLocal = Vector2.zero;

            int spawnCount = Mathf.Min(count, MaxPopupsPerRock);
            for (int i = 0; i < spawnCount; i++)
            {

                var jitter = new Vector2(Random.Range(-22f, 22f), Random.Range(-12f, 26f));
                CreateOne(canvas, startLocal + jitter, targetLocal, sprite, (int)ore, i * 0.05f);
            }
        }

        static void CreateOne(RectTransform parentCanvas, Vector2 startAnchored, Vector2 targetAnchored,
                              Sprite sprite, int oreIndex, float spawnDelay)
        {
            OreDropPopupFx fx = null;
            while (fx == null && s_idle.Count > 0) fx = s_idle.Pop();
            if (fx == null)
            {
                var go = new GameObject("OreDropPopupFx",
                    typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                go.SetActive(false);
                fx = go.AddComponent<OreDropPopupFx>();
                fx._rt = (RectTransform)go.transform;
                fx._img = go.GetComponent<Image>();
                fx._img.preserveAspect = true;
                fx._img.raycastTarget = false;
            }
            var rt = fx._rt;
            if (rt.parent != parentCanvas) rt.SetParent(parentCanvas, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = startAnchored;
            rt.sizeDelta = new Vector2(PopupSize, PopupSize);
            rt.localScale = Vector3.zero;
            rt.SetAsLastSibling();

            fx._oreIndex = oreIndex;

            const int shared = MaxVisiblePopups - ReservedPerOre * OreCount;
            fx._visible = s_visible < shared || (s_visible < MaxVisiblePopups && s_visibleByOre[oreIndex] < ReservedPerOre);
            if (fx._visible)
            {
                s_visible++;
                s_visibleByOre[oreIndex]++;
            }
            fx._img.sprite = sprite;
            fx._img.color = Color.white;
            fx._img.enabled = fx._visible;
            fx._targetAnchored = targetAnchored;
            fx._baseScale = Vector3.one;
            fx._spawnDelay = spawnDelay;
            fx._arrivalPlayed = false;
            fx._leased = true;
            fx.gameObject.SetActive(true);
            fx._running = fx.StartCoroutine(fx.Lifecycle());
        }

        void OnDisable()
        {
            if (!_leased) return;
            _leased = false;
            ReleaseVisibility();
            if (_running != null) StopCoroutine(_running);
            _running = null;
            _img.enabled = false;
            var session = MiningSessionController.Instance;
            if (session != null && session.isActiveAndEnabled) session.StartCoroutine(ParkNextFrame());
            else Destroy(gameObject);
        }

        IEnumerator ParkNextFrame()
        {

            yield return null;
            if (this != null) ReturnToPool();
        }

        void ReturnToPool()
        {
            _leased = false;
            ReleaseVisibility();
            _running = null;
            gameObject.SetActive(false);
            if (s_idle.Count >= MaxIdlePopups)
            {
                Destroy(gameObject);
                return;
            }

            s_idle.Push(this);
        }

        void ReleaseVisibility()
        {
            if (!_visible) return;
            _visible = false;
            s_visible--;
            s_visibleByOre[_oreIndex]--;
        }

        IEnumerator Lifecycle()
        {

            float wait = 0f;
            while (wait < _spawnDelay)
            {
                using (s_stepMarker.Auto()) wait += Time.unscaledDeltaTime;
                yield return null;
            }


            float t = 0f;
            while (t < ScaleInDuration)
            {
                s_stepMarker.Begin();
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / ScaleInDuration);
                float overshoot = 1f + 0.20f * Mathf.Sin(k * Mathf.PI);
                if (_visible) _rt.localScale = _baseScale * (k * overshoot);
                s_stepMarker.End();
                yield return null;
            }
            if (_visible) _rt.localScale = _baseScale;


            float hover = Random.Range(HoverMin, HoverMax);
            wait = 0f;
            while (wait < hover)
            {
                using (s_stepMarker.Auto()) wait += Time.unscaledDeltaTime;
                yield return null;
            }


            float moveDur = Random.Range(MoveDurationMin, MoveDurationMax);
            t = 0f;
            Vector2 start = _rt.anchoredPosition;
            while (t < moveDur)
            {
                s_stepMarker.Begin();
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / moveDur);

                float s = Mathf.SmoothStep(0f, 1f, k);

                if (_visible)
                {
                    _rt.anchoredPosition = Vector2.LerpUnclamped(start, _targetAnchored, s);
                    var c = _img.color;
                    c.a = Mathf.Lerp(1f, 0.55f, Mathf.SmoothStep(0.7f, 1f, k));
                    _img.color = c;
                }
                s_stepMarker.End();
                yield return null;
            }



            if (!_arrivalPlayed)
            {
                _arrivalPlayed = true;
                Rebuild.Audio.OverlappingSounds.Instance?.PlayPop();
            }
            ReturnToPool();
        }
    }
}
