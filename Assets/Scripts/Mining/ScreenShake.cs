using System.Collections;
using UnityEngine;

namespace Rebuild.Mining
{
    /// <summary>mirror of original <c>ScreenShake.cs</c>: when a rock breaks, jitter
    /// the tile grid's RectTransform for ~0.1s so the camera "feels" the impact. Mirrors
    /// <c>RockMechanics.DamageRock</c> calling <c>screenShakeScript.ShakeTheScreen()</c> on the
    /// break path. Self-instantiating singleton — first call to <see cref="Instance"/> spawns the
    /// holder GameObject + auto-binds the shake target (Canvas_Game / Tiles).
    /// <para>Only the in-game tile layer shakes — HUD (Canvas_UI_MiningScene) is a separate Canvas
    /// so timer / ore counters / EndSession button stay rock-steady, matching the original.</para>
    /// </summary>
    public sealed class ScreenShake : MonoBehaviour
    {
        static ScreenShake _instance;
        public static ScreenShake Instance
        {
            get
            {
                if (_instance != null) return _instance;
                // Lazy-spawn so Rock.cs / BigRock.cs can call Shake() without a Setup menu step.
                // Hidden holder so the inspector doesn't get a stray root.
                var go = new GameObject("ScreenShakeRoot");
                _instance = go.AddComponent<ScreenShake>();
                return _instance;
            }
        }

        [SerializeField] RectTransform target;
        [SerializeField] float defaultDuration = 0.1f;
        [SerializeField] float defaultMagnitude = 4f;   // px in canvas-local coords (UGUI)

        Vector2 _origin;
        bool _hasOrigin;
        Coroutine _routine;

        void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
        }

        void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        /// <summary>Shake using the default tuning (0.1s, 4px). Suitable for regular Rock break.</summary>
        public void Shake() => Shake(defaultDuration, defaultMagnitude);

        /// <summary>Shake the bound target for <paramref name="duration"/> seconds with a max
        /// per-frame offset of <paramref name="magnitude"/> pixels.</summary>
        public void Shake(float duration, float magnitude)
        {
            if (!isActiveAndEnabled) return;
            if (!EnsureTarget()) return;
            if (_routine != null) StopCoroutine(_routine);
            _routine = StartCoroutine(ShakeRoutine(duration, magnitude));
        }

        bool EnsureTarget()
        {
            if (target != null) return true;
            // Try the obvious candidates in the active scene. Canvas_Game's "Tiles" child is the
            // tile-grid wrapper TileGrid builds at runtime — that's exactly what the original
            // game shook (the visual rock board, not the HUD).
            var canvas = GameObject.Find("Canvas_Game");
            if (canvas != null)
            {
                var tiles = canvas.transform.Find("Tiles");
                if (tiles is RectTransform tilesRt) target = tilesRt;
                else if (tiles != null) target = tiles as RectTransform;
                // Fallback: shake the whole Canvas_Game RectTransform (HUD lives on a separate
                // Canvas so this still keeps HUD steady).
                if (target == null && canvas.transform is RectTransform crt) target = crt;
            }
            return target != null;
        }

        IEnumerator ShakeRoutine(float duration, float magnitude)
        {
            if (!_hasOrigin)
            {
                _origin = target.anchoredPosition;
                _hasOrigin = true;
            }
            // If we re-trigger mid-shake, _origin already points at the resting pos so layered
            // shakes don't drift the target away from where it started.
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                // Random.insideUnitCircle is a uniform disc — multiply by magnitude to get px.
                Vector2 off = Random.insideUnitCircle * magnitude;
                target.anchoredPosition = _origin + off;
                yield return null;
            }
            target.anchoredPosition = _origin;
            _hasOrigin = false;
            _routine = null;
        }
    }
}
