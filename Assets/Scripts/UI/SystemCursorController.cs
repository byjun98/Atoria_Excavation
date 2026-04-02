using UnityEngine;

namespace Rebuild.UI
{
    /// <summary>Keeps cursor state coordinated while a mining session is active.
    /// MidasTouch / GoldenHand talents can flip to a gold variant when textures are available.
    /// <para>Self-instantiating singleton — <see cref="Instance"/> spawns the holder on first
    /// access so MiningSessionController doesn't need a setup step. Missing textures intentionally
    /// leave the system cursor alone.</para>
    /// </summary>
    public sealed class SystemCursorController : MonoBehaviour
    {
        static SystemCursorController _instance;
        public static SystemCursorController Instance
        {
            get
            {
                if (_instance != null) return _instance;
                var go = new GameObject("SystemCursorRoot");
                _instance = go.AddComponent<SystemCursorController>();
                return _instance;
            }
        }

        Texture2D _normalHand;
        Texture2D _goldHand;
        bool _texturesResolved;
        bool _inMining;
        bool _golden;
        readonly Vector2 _hotspot = Vector2.zero;

        void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            ResolveTexturesIfNeeded();
        }

        void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        void ResolveTexturesIfNeeded()
        {
            if (_texturesResolved) return;
            _texturesResolved = true;
            // Public build ships without custom cursor textures; keep the OS cursor as fallback.
        }

        public bool HasNormalHand => _normalHand != null;
        public bool HasGoldHand => _goldHand != null;

        /// <summary>Called by MiningSessionController on session start. Switches the OS cursor
        /// to the normal/gold hand based on <see cref="SetGolden"/>'s last call.</summary>
        public void EnterMiningSession()
        {
            _inMining = true;
            ApplyCursor();
        }

        /// <summary>Called by MiningSessionController on session end. Restores the OS cursor
        /// (Texture2D=null = default arrow).</summary>
        public void ExitMiningSession()
        {
            _inMining = false;
            ApplyCursor();
        }

        /// <summary>MidasTouch / GoldenHand active → gold variant. Idempotent — call before
        /// EnterMiningSession so the very first cursor frame already uses the right sprite.</summary>
        public void SetGolden(bool g)
        {
            _golden = g;
            if (_inMining) ApplyCursor();
        }

        /// <summary>Helper for HoverCursorReset — restore the OS cursor briefly (e.g. while the
        /// pointer is over the EndSession button). Pair with <see cref="ApplyCursor"/> on exit.</summary>
        public void ResetToSystemCursor()
        {
#if !UNITY_ANDROID && !UNITY_IOS
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
#endif
        }

        /// <summary>Reapply the cursor based on current state. HoverCursorReset uses this when
        /// the pointer leaves a "reset zone" and the mining cursor should take over again.</summary>
        public void ApplyCursor()
        {
#if UNITY_ANDROID || UNITY_IOS
            // Mobile devices don't have an OS cursor — Cursor.SetCursor is a no-op anyway. Skip.
            return;
#else
            ResolveTexturesIfNeeded();
            if (_inMining)
            {
                Texture2D tex = _golden ? _goldHand : _normalHand;
                if (tex == null) tex = _normalHand; // gold falls back to normal if missing
                Cursor.SetCursor(tex, _hotspot, CursorMode.Auto);
                Cursor.visible = true; // the hand IS the cursor
            }
            else
            {
                Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
                Cursor.visible = true;
            }
#endif
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoSpawn()
        {
            // Touch Instance to trigger lazy-spawn so EnterMiningSession() works even on the
            // very first session before MiningSessionController has a chance to call it.
            _ = Instance;
        }
    }
}
