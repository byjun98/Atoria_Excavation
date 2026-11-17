using UnityEngine;
using UnityEngine.EventSystems;

namespace Rebuild.UI
{






    [DisallowMultipleComponent]
    public sealed class HoverFx : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler,
        IPointerDownHandler, IPointerUpHandler
    {
        [Header("Hover scale (HoverOverIncreaseSize)")]
        [SerializeField] bool enableHoverScale = true;
        [SerializeField] float hoverScaleMultiplier = 1.08f;
        [SerializeField] float scaleLerpSeconds = 0.10f;

        [Header("Hover lift (UIHoverMove)")]
        [SerializeField] bool enableHoverLift = false;
        [SerializeField] float liftPx = 8f;

        [Header("Press scale (ButtonPressAnimation)")]
        [SerializeField] bool enablePressScale = true;
        [SerializeField] float pressScaleMultiplier = 0.92f;

        [Header("Hover SFX (HoverUISound)")]
        [Tooltip("AudioManager 의 sound name. 비워두면 SFX 안 재생.")]
        [SerializeField] string hoverSfxName = "HoverUI";

        RectTransform _rt;
        Vector3 _baseScale;
        Vector2 _baseAnchored;
        bool _isHovered;
        bool _isPressed;


        float _scaleT;
        float _scaleTarget;

        void Awake()
        {
            _rt = transform as RectTransform;
            CacheBasePose();
        }

        void OnEnable()
        {


            _scaleT = 0f;
            _scaleTarget = 0f;
            _isHovered = false;
            _isPressed = false;
            RestoreBasePose();
        }

        void Update()
        {
            if (Mathf.Approximately(_scaleT, _scaleTarget)) return;
            float lerpSeconds = Bad(scaleLerpSeconds) ? 0f : scaleLerpSeconds;
            float step = lerpSeconds > 0f
                ? Time.unscaledDeltaTime / lerpSeconds
                : 1f;
            _scaleT = Mathf.MoveTowards(_scaleT, _scaleTarget, step);
            ApplyScale();
        }

        void ApplyScale()
        {
            float targetMult = 1f;
            if (_isPressed && enablePressScale) targetMult = SafeMultiplier(pressScaleMultiplier, 0.92f);
            else if (_isHovered && enableHoverScale) targetMult = SafeMultiplier(hoverScaleMultiplier, 1.08f);

            float t = Bad(_scaleT) ? 0f : Mathf.Clamp01(_scaleT);
            float mult = Mathf.Lerp(1f, targetMult, t);
            var nextScale = SafeScale(_baseScale) * mult;
            transform.localScale = Bad(nextScale) ? Vector3.one : nextScale;


            if (_rt != null && enableHoverLift)
            {
                float lift = (_isHovered && !_isPressed) ? SafeFloat(liftPx, 0f) : 0f;
                var nextPos = SafeAnchored(_baseAnchored) + new Vector2(0f, Mathf.Lerp(0f, lift, t));
                _rt.anchoredPosition = Bad(nextPos) ? Vector2.zero : nextPos;
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _isHovered = true;
            _scaleTarget = 1f;
            if (!string.IsNullOrEmpty(hoverSfxName))
                Rebuild.Audio.AudioManager.Instance?.Play(hoverSfxName);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _isHovered = false;
            _isPressed = false;
            _scaleTarget = 0f;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _isPressed = true;
            _scaleTarget = 1f;
            ApplyScale();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _isPressed = false;

            _scaleTarget = _isHovered ? 1f : 0f;
            ApplyScale();
        }

        void OnDisable()
        {

            RestoreBasePose();
        }

        public void RecacheBasePose()
        {
            CacheBasePose();
            RestoreBasePose();
        }

        void CacheBasePose()
        {
            _baseScale = SafeScale(transform.localScale);
            if (Bad(transform.localScale)) transform.localScale = _baseScale;

            if (_rt != null)
            {
                _baseAnchored = SafeAnchored(_rt.anchoredPosition);
                if (Bad(_rt.anchoredPosition)) _rt.anchoredPosition = _baseAnchored;
            }
        }

        void RestoreBasePose()
        {
            transform.localScale = SafeScale(_baseScale);
            if (_rt != null) _rt.anchoredPosition = SafeAnchored(_baseAnchored);
        }

        static Vector3 SafeScale(Vector3 value) => Bad(value) ? Vector3.one : value;
        static Vector2 SafeAnchored(Vector2 value) => Bad(value) ? Vector2.zero : value;
        static float SafeMultiplier(float value, float fallback)
            => Bad(value) || value <= 0f ? fallback : value;
        static float SafeFloat(float value, float fallback) => Bad(value) ? fallback : value;
        static bool Bad(Vector2 value) => Bad(value.x) || Bad(value.y);
        static bool Bad(Vector3 value) => Bad(value.x) || Bad(value.y) || Bad(value.z);
        static bool Bad(float value) => float.IsNaN(value) || float.IsInfinity(value);
    }
}
