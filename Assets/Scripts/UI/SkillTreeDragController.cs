using UnityEngine;
using UnityEngine.EventSystems;

namespace Rebuild.UI
{
    /// <summary>drag / pan / wheel-zoom for the 72-node SkillTree container.
    /// Adapted from the original <c>SkillTreeDrag.cs</c>:
    /// <list type="bullet">
    ///   <item>Awake — caches <c>defaultZoom</c>, <c>dragThreshold = 100px</c> (desktop only).</item>
    ///   <item>Drag — translates <c>contentRoot.anchoredPosition</c> by mouse delta once the drag
    ///   threshold is crossed. Tooltip is hidden during a drag so it doesn't ride along.</item>
    ///   <item>Zoom (desktop) — mouse-wheel scaled by 0.1 multiplies <c>_targetZoom</c>, clamped to
    ///   [<see cref="minZoom"/>, <see cref="maxZoom"/>], then Lerp toward at <see cref="zoomSpeed"/>.</item>
    ///   <item>Zoom (mobile) — <see cref="mobileZoomSlider"/> drives <c>_targetZoom</c> directly.</item>
    ///   <item>Pan bounds — content stays inside <see cref="viewportRect"/>; out-of-bounds is
    ///   softly clamped each frame.</item>
    /// </list>
    /// <para>Attach to the viewport RectTransform (parent of contentRoot). Wire fields in scene
    /// setup or via <see cref="Initialize"/>.</para>
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class SkillTreeDragController : MonoBehaviour, IPointerDownHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] RectTransform contentRoot;
        [SerializeField] RectTransform viewportRect;
        [SerializeField] UnityEngine.UI.Slider mobileZoomSlider;
        [SerializeField] float defaultZoom = 0.9f;
        [SerializeField] float minZoom = 0.5f;
        [SerializeField] float maxZoom = 1.5f;
        [SerializeField] float zoomSpeed = 8f;
        [SerializeField] float dragThreshold = 100f;
        [SerializeField] float wheelMultiplier = 0.1f;

        Vector2 _mouseDownPosition;
        Vector2 _contentDownAnchoredPosition;
        bool _dragging;
        float _targetZoom;
        float _currentZoom;

        public void Initialize(RectTransform content, RectTransform viewport,
            UnityEngine.UI.Slider zoomSlider = null)
        {
            contentRoot = content;
            viewportRect = viewport;
            mobileZoomSlider = zoomSlider;
            ResetView();
        }

        public void ResetView()
        {
            _targetZoom = defaultZoom;
            _currentZoom = defaultZoom;
            if (contentRoot != null)
            {
                contentRoot.localScale = new Vector3(defaultZoom, defaultZoom, 1f);
                contentRoot.anchoredPosition = Vector2.zero;
            }
            if (mobileZoomSlider != null) mobileZoomSlider.value = defaultZoom;
        }

        void Awake()
        {
            _targetZoom = defaultZoom;
            _currentZoom = defaultZoom;
            if (contentRoot != null)
                contentRoot.localScale = new Vector3(defaultZoom, defaultZoom, 1f);
        }

        void OnEnable()
        {
            if (mobileZoomSlider != null)
                mobileZoomSlider.onValueChanged.AddListener(HandleMobileSlider);
        }

        void OnDisable()
        {
            if (mobileZoomSlider != null)
                mobileZoomSlider.onValueChanged.RemoveListener(HandleMobileSlider);
        }

        void HandleMobileSlider(float v)
        {
            _targetZoom = Mathf.Clamp(v, minZoom, maxZoom);
        }

        void Update()
        {
            if (contentRoot == null) return;

            // Desktop wheel — multiply target zoom. Only when the cursor is over our viewport
            // (so wheel doesn't fight scrollable lists outside).
            if (!Application.isMobilePlatform && IsPointerOverSelf())
            {
                float wheel = Input.mouseScrollDelta.y;
                if (Mathf.Abs(wheel) > 0.001f)
                {
                    _targetZoom *= 1f + wheel * wheelMultiplier;
                    _targetZoom = Mathf.Clamp(_targetZoom, minZoom, maxZoom);
                }
            }

            // Lerp current → target.
            _currentZoom = Mathf.Lerp(_currentZoom, _targetZoom, Time.unscaledDeltaTime * zoomSpeed);
            contentRoot.localScale = new Vector3(_currentZoom, _currentZoom, 1f);

            // Soft pan clamp.
            ClampPan();
        }

        bool IsPointerOverSelf()
        {
            if (viewportRect == null) return true;
            return RectTransformUtility.RectangleContainsScreenPoint(
                viewportRect, Input.mousePosition, null);
        }

        void ClampPan()
        {
            if (contentRoot == null || viewportRect == null) return;
            var anchored = contentRoot.anchoredPosition;
            // Width of the content at the current zoom.
            float scaledW = contentRoot.sizeDelta.x * _currentZoom;
            float scaledH = contentRoot.sizeDelta.y * _currentZoom;
            float vpW = viewportRect.rect.width;
            float vpH = viewportRect.rect.height;
            // When content > viewport, content can be panned within +-(scaled - viewport)/2
            // (centered pivot). Otherwise pin to 0.
            float maxX = Mathf.Max(0f, (scaledW - vpW) * 0.5f);
            float maxY = Mathf.Max(0f, (scaledH - vpH) * 0.5f);
            anchored.x = Mathf.Clamp(anchored.x, -maxX, maxX);
            anchored.y = Mathf.Clamp(anchored.y, -maxY, maxY);
            contentRoot.anchoredPosition = anchored;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _mouseDownPosition = eventData.position;
            _contentDownAnchoredPosition = contentRoot != null ? contentRoot.anchoredPosition : Vector2.zero;
            _dragging = false;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (contentRoot == null) return;
            Vector2 delta = eventData.position - _mouseDownPosition;
            if (!_dragging)
            {
                // On desktop, require threshold so click→tooltip doesn't trigger an accidental
                // pan. Mobile gets immediate drag (matches original behaviour where the threshold
                // is gated by isMobile).
                if (!Application.isMobilePlatform && delta.magnitude < dragThreshold) return;
                _dragging = true;
                // Hide tooltip the moment we commit to a drag.
                SkillTreeTooltipUI.Instance?.Hide();
            }
            // Apply delta in scaled space — divide by the canvas scale so the pan matches mouse motion.
            var canvas = contentRoot.GetComponentInParent<Canvas>();
            float scale = canvas != null ? canvas.scaleFactor : 1f;
            if (scale < 0.001f) scale = 1f;
            contentRoot.anchoredPosition = _contentDownAnchoredPosition + delta / scale;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            _dragging = false;
        }
    }
}
