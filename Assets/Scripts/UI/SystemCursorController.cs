using UnityEngine;

namespace Rebuild.UI
{






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

        }

        public bool HasNormalHand => _normalHand != null;
        public bool HasGoldHand => _goldHand != null;



        public void EnterMiningSession()
        {
            _inMining = true;
            ApplyCursor();
        }



        public void ExitMiningSession()
        {
            _inMining = false;
            ApplyCursor();
        }



        public void SetGolden(bool g)
        {
            _golden = g;
            if (_inMining) ApplyCursor();
        }



        public void ResetToSystemCursor()
        {
#if !UNITY_ANDROID && !UNITY_IOS
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
#endif
        }



        public void ApplyCursor()
        {
#if UNITY_ANDROID || UNITY_IOS

            return;
#else
            ResolveTexturesIfNeeded();
            if (_inMining)
            {
                Texture2D tex = _golden ? _goldHand : _normalHand;
                if (tex == null) tex = _normalHand;
                Cursor.SetCursor(tex, _hotspot, CursorMode.Auto);
                Cursor.visible = true;
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


            _ = Instance;
        }
    }
}
