using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Rebuild.Mining
{
    /// <summary>ore drop popup. Mirrors original
    /// <c>RockMechanics.SpawnMaterialPopUp → MineMaterialMechanics.PopUpText</c>: when a rock
    /// breaks and drops ore, one Image popup per ore unit pops out of the rock, hovers briefly,
    /// then flies to the matching ore counter in the top-left HUD where a <c>PlayPop()</c> SFX
    /// fires and the counter punches.
    /// <para>Spawned under the mining-scene Canvas (NOT as a child of the rock) so the rock's
    /// fade-out animation doesn't take the popups with it.</para>
    /// </summary>
    public sealed class OreDropPopupFx : MonoBehaviour
    {
        const float ScaleInDuration = 0.20f;
        const float MoveDurationMin = 0.28f;
        const float MoveDurationMax = 0.42f;
        const float HoverMin = 0.05f;
        const float HoverMax = 0.30f;
        const float PopupSize = 36f;
        // Cap per-rock spawn count so an area-tick that drops 20 ore from a single rock doesn't
        // flood the canvas with 20 popups (visual noise > gameplay value).
        const int MaxPopupsPerRock = 8;

        Image _img;
        RectTransform _rt;
        Vector2 _startAnchored;
        Vector2 _targetAnchored;
        Vector3 _baseScale;
        float _spawnDelay;
        bool _arrivalPlayed;

        /// <summary>Spawn <paramref name="count"/> popups originating at <paramref name="startWorldPos"/>
        /// (rock's world position), flying to <paramref name="targetRT"/> (an OreCounterUI entry).
        /// All popups parent under <paramref name="parentCanvasT"/> — typically Canvas_UI_MiningScene
        /// so they sit on top of Canvas_Game (the tile board) and survive the rock's fade.</summary>
        public static void Spawn(Transform parentCanvasT, Vector3 startWorldPos,
                                  OreType ore, int count, RectTransform targetRT)
        {
            if (parentCanvasT == null || targetRT == null || count <= 0) return;
            var canvas = parentCanvasT as RectTransform ?? parentCanvasT.GetComponent<RectTransform>();
            if (canvas == null) return;
            // Resolve sprite once — fallback to the rock silhouette so missing ore icons still
            // render something visible during early bring-up.
            var sprite = SpriteLibrary.GetOreHudIcon(ore);
            if (sprite == null) sprite = RockSpriteFactory.GetRockSprite();

            // Convert rock's world position into the parent canvas's local space.
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
                // Slight random offset on the start so multiple popups don't perfectly overlap.
                var jitter = new Vector2(Random.Range(-22f, 22f), Random.Range(-12f, 26f));
                CreateOne(canvas, startLocal + jitter, targetLocal, sprite, ore, i * 0.05f);
            }
        }

        static void CreateOne(RectTransform parentCanvas, Vector2 startAnchored, Vector2 targetAnchored,
                              Sprite sprite, OreType ore, float spawnDelay)
        {
            var go = new GameObject($"OreDropPopupFx_{ore}",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parentCanvas, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = startAnchored;
            rt.sizeDelta = new Vector2(PopupSize, PopupSize);
            rt.localScale = Vector3.zero; // scale-in
            rt.SetAsLastSibling();

            var img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.preserveAspect = true;
            img.raycastTarget = false;

            var fx = go.AddComponent<OreDropPopupFx>();
            // 🚨 AddComponent fires Awake → OnEnable synchronously. Before this fix, OnEnable
            // started the Lifecycle coroutine immediately and the scale-in block read _rt — but _rt hadn't
            // been assigned yet → NRE. Init() now drives both field setup AND coroutine start so
            // the order is unambiguous.
            fx.Init(img, rt, startAnchored, targetAnchored, spawnDelay);
        }

        Coroutine _running;
        bool _initialized;

        void Init(Image img, RectTransform rt, Vector2 startAnchored, Vector2 targetAnchored, float spawnDelay)
        {
            _img = img;
            _rt = rt;
            _startAnchored = startAnchored;
            _targetAnchored = targetAnchored;
            _baseScale = Vector3.one;
            _spawnDelay = spawnDelay;
            _initialized = true;
            if (_running != null) StopCoroutine(_running);
            _running = StartCoroutine(Lifecycle());
        }

        void OnEnable()
        {
            // Init() drives the first Lifecycle start. OnEnable only restarts when the popup was
            // disabled mid-flight and re-enabled (rare — typically the canvas re-activation path
            // after a session boundary). Skip when not yet initialised to avoid scale-in NRE.
            if (!_initialized) return;
            if (_running != null) StopCoroutine(_running);
            _running = StartCoroutine(Lifecycle());
        }

        void OnDisable()
        {
            // Session ended mid-flight — drop the popup so it doesn't leak into the next session.
            if (!_initialized) return;
            if (_running != null) StopCoroutine(_running);
            _running = null;
            if (gameObject != null) Destroy(gameObject);
        }

        IEnumerator Lifecycle()
        {
            // Pre-spawn delay so a burst of N popups doesn't all teleport at frame 0.
            float wait = 0f;
            while (wait < _spawnDelay)
            {
                wait += Time.unscaledDeltaTime;
                yield return null;
            }

            // Scale-in 0 → 1 with a small overshoot so the popup feels lively.
            float t = 0f;
            while (t < ScaleInDuration)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / ScaleInDuration);
                float overshoot = 1f + 0.20f * Mathf.Sin(k * Mathf.PI); // peak +20% at k=0.5
                _rt.localScale = _baseScale * (k * overshoot);
                yield return null;
            }
            _rt.localScale = _baseScale;

            // Random hover so popups don't move in lockstep.
            float hover = Random.Range(HoverMin, HoverMax);
            wait = 0f;
            while (wait < hover)
            {
                wait += Time.unscaledDeltaTime;
                yield return null;
            }

            // Fly to the target with an ease-out curve.
            float moveDur = Random.Range(MoveDurationMin, MoveDurationMax);
            t = 0f;
            Vector2 start = _rt.anchoredPosition;
            while (t < moveDur)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / moveDur);
                // SmoothStep for an ease-in-out feel; arrival looks decisive.
                float s = Mathf.SmoothStep(0f, 1f, k);
                _rt.anchoredPosition = Vector2.LerpUnclamped(start, _targetAnchored, s);
                // Fade slightly toward the end so popups don't punch the HUD at full opacity.
                if (_img != null)
                {
                    var c = _img.color;
                    c.a = Mathf.Lerp(1f, 0.55f, Mathf.SmoothStep(0.7f, 1f, k));
                    _img.color = c;
                }
                yield return null;
            }

            // Arrival — single pop SFX (one of N popups may share the frame; the AudioSource is
            // shared so overlapping triggers play layered, matching the original "ka-ching" feel).
            if (!_arrivalPlayed)
            {
                _arrivalPlayed = true;
                Rebuild.Audio.OverlappingSounds.Instance?.PlayPop();
            }
            Destroy(gameObject);
        }
    }
}
