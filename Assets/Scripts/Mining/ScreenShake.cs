using System.Collections;
using UnityEngine;

namespace Rebuild.Mining
{








    public sealed class ScreenShake : MonoBehaviour
    {
        static ScreenShake _instance;
        public static ScreenShake Instance
        {
            get
            {
                if (_instance != null) return _instance;


                var go = new GameObject("ScreenShakeRoot");
                _instance = go.AddComponent<ScreenShake>();
                return _instance;
            }
        }

        [SerializeField] RectTransform target;
        [SerializeField] float defaultDuration = 0.1f;
        [SerializeField] float defaultMagnitude = 4f;

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


        public void Shake() => Shake(defaultDuration, defaultMagnitude);



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



            var canvas = GameObject.Find("Canvas_Game");
            if (canvas != null)
            {
                var tiles = canvas.transform.Find("Tiles");
                if (tiles is RectTransform tilesRt) target = tilesRt;
                else if (tiles != null) target = tiles as RectTransform;


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


            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;

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
