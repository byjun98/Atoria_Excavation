using UnityEngine;
using UnityEngine.EventSystems;

namespace Rebuild.UI
{
    /// <summary>원본 6 micro-script 통합 (HoverUISound + HoverOverIncreaseSize +
    /// UIHoverMove + ButtonAnim + ButtonPressAnimation + UiAnimations). 단일 컴포넌트로
    /// hover SFX + scale up + position lift + press scale down 한꺼번에 제공.
    /// <para>설정은 SerializeField 로 인스펙터 노출. 사용 사이트별로 켜고 끌 수 있어 prefab/animator
    /// 미사용으로도 원본 미세 인터랙션 복원. 의 <see cref="HoverCursorReset"/> 와 직교 —
    /// 둘 다 같은 GameObject 에 붙여도 안전.</para></summary>
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
        // 코루틴 없이 Update 의 Mathf.Lerp 로 매 프레임 보간. 멀티 스크립트 hover 시
        // StopCoroutine 충돌 가능성 제거.
        float _scaleT;     // 0 = base, 1 = hovered (or pressed) target
        float _scaleTarget;

        void Awake()
        {
            _rt = transform as RectTransform;
            CacheBasePose();
        }

        void OnEnable()
        {
            // 컴포넌트 비활성화/재활성화 시 보간 상태 리셋 — 안 그러면 hover 중 SetActive(false)
            // 후 다시 켜질 때 stuck 크기로 남음.
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
            // _scaleT 가 0→1 보간하므로 (1, targetMult) 사이 lerp.
            float t = Bad(_scaleT) ? 0f : Mathf.Clamp01(_scaleT);
            float mult = Mathf.Lerp(1f, targetMult, t);
            var nextScale = SafeScale(_baseScale) * mult;
            transform.localScale = Bad(nextScale) ? Vector3.one : nextScale;

            // Hover lift — scale 보간과 동일 t 사용해서 부드럽게 위로.
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
            _scaleTarget = 1f; // press scale 도 t=1 일 때 적용 (ApplyScale 가 _isPressed 우선)
            ApplyScale();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _isPressed = false;
            // hover 유지 중이면 hover scale 로 복귀, 아니면 base.
            _scaleTarget = _isHovered ? 1f : 0f;
            ApplyScale();
        }

        void OnDisable()
        {
            // hover 도중 hide 시 stuck 방지.
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
