using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Rebuild.Mining
{
    /// <summary>idle TheMine tick popup. Mirrors original
    /// <c>MineMaterialMechanics.PopUpText</c>: when the auto-mine ticks, one ore icon spawns at
    /// the mine entrance, hovers briefly, then lerps to the left-side ResourcePanel row for that
    /// ore. Arrival fires a one-shot SFX + row pulse so the player feels the credit landing.
    /// <para>Pattern: <see cref="PickaxeSwingFx"/> / <see cref="OreDropPopupFx"/> — one-shot
    /// component, auto-destroys at the end of its 3-stage coroutine. Counter increment is NOT
    /// gated on arrival (the bar count was already credited by <c>MineManager.EmitTick</c>); this
    /// is purely cosmetic feedback.</para>
    /// </summary>
    public sealed class MineOrePopupFx : MonoBehaviour
    {
        const float ScaleInDuration = 0.18f;
        const float HoverDuration = 0.20f;
        const float MoveDuration = 0.35f;
        const float ArrivalScale = 0.45f;   // shrink as the popup is "absorbed" into the row

        Image _img;
        RectTransform _rt;
        Vector2 _startAnchored;
        Vector2 _targetAnchored;
        float _spawnDelay;
        Action _onArrived;
        bool _initialized;
        Coroutine _running;

        /// <summary>Spawn one popup. <paramref name="parent"/> is the common ancestor of source +
        /// target (typically the MainMenu canvas RectTransform) so both endpoints share a coord
        /// space. <paramref name="spawnDelay"/> staggers bursts when a tick drops multiple ore.</summary>
        public static MineOrePopupFx Spawn(RectTransform parent,
                                            Vector2 startAnchoredPos,
                                            Vector2 targetAnchoredPos,
                                            Sprite sprite, Color tint,
                                            float size = 60f,
                                            float spawnDelay = 0f,
                                            Action onArrived = null)
        {
            if (parent == null) return null;
            var go = new GameObject("MineOrePopupFx",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = startAnchoredPos;
            rt.sizeDelta = new Vector2(size, size);
            rt.localScale = Vector3.zero;
            rt.SetAsLastSibling();

            var img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.color = tint;
            img.preserveAspect = true;
            img.raycastTarget = false;

            var fx = go.AddComponent<MineOrePopupFx>();
            fx.Init(img, rt, startAnchoredPos, targetAnchoredPos, spawnDelay, onArrived);
            return fx;
        }

        void Init(Image img, RectTransform rt, Vector2 startAnchored, Vector2 targetAnchored,
                  float spawnDelay, Action onArrived)
        {
            _img = img;
            _rt = rt;
            _startAnchored = startAnchored;
            _targetAnchored = targetAnchored;
            _spawnDelay = spawnDelay;
            _onArrived = onArrived;
            _initialized = true;
            if (_running != null) StopCoroutine(_running);
            _running = StartCoroutine(Lifecycle());
        }

        void OnEnable()
        {
            // Init() drives the first start; OnEnable only re-runs on rare canvas re-activation
            // (the popup is short-lived so this practically never fires).
            if (!_initialized) return;
            if (_running != null) StopCoroutine(_running);
            _running = StartCoroutine(Lifecycle());
        }

        void OnDisable()
        {
            // If the panel hides mid-flight (tab switch), drop the popup so it doesn't resurrect
            // when the panel re-activates and target a now-irrelevant row.
            if (!_initialized) return;
            if (_running != null) StopCoroutine(_running);
            _running = null;
            if (gameObject != null) Destroy(gameObject);
        }

        IEnumerator Lifecycle()
        {
            // Pre-spawn stagger so a tick that drops N ore doesn't show N popups at frame 0.
            float wait = 0f;
            while (wait < _spawnDelay)
            {
                wait += Time.unscaledDeltaTime;
                yield return null;
            }

            // Scale-in with ease-out (Sin(k * PI/2) is exactly 0→1 with smooth landing).
            float t = 0f;
            while (t < ScaleInDuration)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / ScaleInDuration);
                float s = Mathf.Sin(k * Mathf.PI * 0.5f);
                _rt.localScale = Vector3.one * s;
                yield return null;
            }
            _rt.localScale = Vector3.one;

            // Brief hover so the player registers what the ore is.
            wait = 0f;
            while (wait < HoverDuration)
            {
                wait += Time.unscaledDeltaTime;
                yield return null;
            }

            // Fly to target with SmoothStep ease-in-out + shrink to ArrivalScale on
            // arrival so the popup visually "merges" into the row icon.
            t = 0f;
            Vector2 startPos = _rt.anchoredPosition;
            while (t < MoveDuration)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / MoveDuration);
                float s = Mathf.SmoothStep(0f, 1f, k);
                _rt.anchoredPosition = Vector2.LerpUnclamped(startPos, _targetAnchored, s);
                float scl = Mathf.Lerp(1f, ArrivalScale, s);
                _rt.localScale = Vector3.one * scl;
                if (_img != null)
                {
                    var c = _img.color;
                    c.a = Mathf.Lerp(1f, 0.55f, Mathf.SmoothStep(0.65f, 1f, k));
                    _img.color = c;
                }
                yield return null;
            }

            // Arrival — fire the onArrived hook (SFX + row pulse), then destroy.
            try { _onArrived?.Invoke(); }
            catch (Exception e) { Debug.LogException(e); }
            Destroy(gameObject);
        }
    }
}
