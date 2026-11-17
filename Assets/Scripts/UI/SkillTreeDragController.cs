using Rebuild.Mining;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Rebuild.UI
{


    [RequireComponent(typeof(RectTransform))]
    public sealed class SkillTreeDragController : MonoBehaviour, IPointerDownHandler,
        IInitializePotentialDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] RectTransform contentRoot;
        [SerializeField] RectTransform viewportRect;
        [SerializeField] UnityEngine.UI.Slider mobileZoomSlider;
        [SerializeField] float defaultZoom = 1f;
        [SerializeField] float minZoom = 0.35f;
        [SerializeField] float maxZoom = 1.6f;
        [SerializeField] float zoomSpeed = 12f;
        [SerializeField] float dragThreshold = 8f;
        [SerializeField] float wheelMultiplier = 0.1f;
        [SerializeField] float edgePadding = 40f;

        Vector2 _mouseDownPosition;
        Vector2 _pointerDownLocal;
        Vector3 _contentDownLocalPosition;
        bool _dragging;
        float _targetZoom;
        float _currentZoom;
        float _fitZoom;
        Rect _contentBounds;
        Vector2 _normalCenter;
        bool _hasBounds;
        bool _isOverview = true;
        int _clickBlockedThroughFrame = -1;
        Vector2 _zoomFocusLocal;
        Vector2 _zoomFocusViewport;
        bool _hasZoomFocus;
        readonly Vector3[] _corners = new Vector3[4];
        SkillTreeManager _manager;

        public bool ShouldSuppressClick => _dragging || Time.frameCount <= _clickBlockedThroughFrame;

        Camera EventCamera
        {
            get
            {
                var canvas = GetComponentInParent<Canvas>();
                return canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            }
        }

        public void Initialize(RectTransform content, RectTransform viewport,
            UnityEngine.UI.Slider zoomSlider = null)
        {
            if (mobileZoomSlider != null) mobileZoomSlider.onValueChanged.RemoveListener(HandleMobileSlider);
            contentRoot = content;
            viewportRect = viewport;
            mobileZoomSlider = zoomSlider;
            if (isActiveAndEnabled && mobileZoomSlider != null)
                mobileZoomSlider.onValueChanged.AddListener(HandleMobileSlider);
            ResetView();
        }

        public void ResetView() => FitView();

        public void ZoomIn() => ZoomAtCenter(1.25f);
        public void ZoomOut() => ZoomAtCenter(1f / 1.25f);

        void ZoomAtCenter(float factor)
        {
            if (contentRoot == null || viewportRect == null) return;
            ZoomAt(viewportRect.rect.center, factor);
        }

        void ZoomAt(Vector2 viewportPoint, float factor)
        {
            if (!_hasBounds) RefreshContentBounds();
            SetZoomFocus(viewportPoint);
            _targetZoom = Mathf.Clamp(_targetZoom * factor, MinimumZoom, maxZoom);
            _isOverview = false;
        }

        public void FitView()
        {
            if (contentRoot == null || viewportRect == null) return;
            RefreshContentBounds();
            _isOverview = true;
            _hasZoomFocus = false;
            SetImmediateZoom(_fitZoom);
            CenterOnContentPoint(_contentBounds.center);
            ClampPan();
        }

        public void ResetNormalView()
        {
            if (contentRoot == null || viewportRect == null) return;
            RefreshContentBounds();
            _isOverview = false;
            _hasZoomFocus = false;
            SetImmediateZoom(Mathf.Clamp(defaultZoom, MinimumZoom, maxZoom));
            CenterOnContentPoint(_normalCenter);
            ClampPan();
        }

        void Awake()
        {
            if (viewportRect == null) viewportRect = (RectTransform)transform;
            _targetZoom = _currentZoom = defaultZoom;
        }

        void OnEnable()
        {
            _manager = SkillTreeManager.Instance;
            if (_manager != null) _manager.OnUpgradeChanged += InvalidateBounds;
            if (mobileZoomSlider != null) mobileZoomSlider.onValueChanged.AddListener(HandleMobileSlider);
            if (contentRoot != null) ResetView();
        }

        void OnDisable()
        {
            if (_manager != null) _manager.OnUpgradeChanged -= InvalidateBounds;
            _manager = null;
            if (mobileZoomSlider != null) mobileZoomSlider.onValueChanged.RemoveListener(HandleMobileSlider);
            _dragging = false;
            _hasZoomFocus = false;
            _clickBlockedThroughFrame = -1;
        }

        void InvalidateBounds(UpgradeId _, int __) => _hasBounds = false;

        void OnRectTransformDimensionsChange()
        {
            if (contentRoot == null || viewportRect == null || !_hasBounds) return;
            if (_isOverview) FitView();
            else ClampPan();
        }

        public void RefreshContentBounds()
        {
            if (contentRoot == null) return;
            var nodes = contentRoot.GetComponentsInChildren<TreeNodeUI>(true);
            var manager = SkillTreeManager.Instance;
            bool found = false;
            Vector2 min = Vector2.zero;
            Vector2 max = Vector2.zero;
            _normalCenter = Vector2.zero;
            for (int i = 0; i < nodes.Length; i++)
            {
                if (!TreeNodeUI.IsDiscovered(nodes[i].Id, manager)) continue;
                if (nodes[i].Id == UpgradeId.MoreTime)
                    _normalCenter = contentRoot.InverseTransformPoint(nodes[i].Rect.TransformPoint(nodes[i].Rect.rect.center));
                nodes[i].Rect.GetWorldCorners(_corners);
                for (int j = 0; j < _corners.Length; j++)
                {
                    Vector2 point = contentRoot.InverseTransformPoint(_corners[j]);
                    if (!found) { min = max = point; found = true; }
                    else { min = Vector2.Min(min, point); max = Vector2.Max(max, point); }
                }
            }
            _contentBounds = found ? Rect.MinMaxRect(min.x, min.y, max.x, max.y) : contentRoot.rect;
            _hasBounds = true;
            float width = Mathf.Max(1f, viewportRect.rect.width - edgePadding * 2f);
            float height = Mathf.Max(1f, viewportRect.rect.height - edgePadding * 2f);
            _fitZoom = Mathf.Min(Mathf.Min(defaultZoom, maxZoom), Mathf.Min(width / Mathf.Max(1f, _contentBounds.width),
                height / Mathf.Max(1f, _contentBounds.height)));
            _fitZoom = Mathf.Max(0.01f, _fitZoom);
            if (mobileZoomSlider != null)
            {
                mobileZoomSlider.minValue = MinimumZoom;
                mobileZoomSlider.maxValue = maxZoom;
            }
        }

        float MinimumZoom => Mathf.Min(minZoom, _fitZoom > 0f ? _fitZoom : minZoom);

        void SetImmediateZoom(float value)
        {
            _targetZoom = _currentZoom = value;
            contentRoot.localScale = new Vector3(value, value, 1f);
            if (mobileZoomSlider != null) mobileZoomSlider.SetValueWithoutNotify(value);
        }

        void HandleMobileSlider(float value)
        {
            SetZoomFocus(viewportRect.rect.center);
            _targetZoom = Mathf.Clamp(value, MinimumZoom, maxZoom);
            _isOverview = false;
        }

        void Update()
        {
            if (contentRoot == null || viewportRect == null) return;
            if (!_hasBounds)
            {
                if (_isOverview) FitView();
                else RefreshContentBounds();
            }

            if (!Application.isMobilePlatform && IsPointerOverSelf())
            {
                float wheel = Input.mouseScrollDelta.y;
                if (Mathf.Abs(wheel) > 0.001f)
                {
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(viewportRect,
                        Input.mousePosition, EventCamera, out var pointer);
                    ZoomAt(pointer, 1f + wheel * wheelMultiplier);
                }
            }

            if (Mathf.Abs(_currentZoom - _targetZoom) > 0.0001f)
            {
                _currentZoom = Mathf.Lerp(_currentZoom, _targetZoom, 1f - Mathf.Exp(-Time.unscaledDeltaTime * zoomSpeed));
                if (Mathf.Abs(_currentZoom - _targetZoom) < 0.0001f) _currentZoom = _targetZoom;
                contentRoot.localScale = new Vector3(_currentZoom, _currentZoom, 1f);
                if (_hasZoomFocus)
                {
                    Vector2 current = viewportRect.InverseTransformPoint(contentRoot.TransformPoint(_zoomFocusLocal));
                    TranslateInViewport(_zoomFocusViewport - current);
                }
                if (mobileZoomSlider != null) mobileZoomSlider.SetValueWithoutNotify(_currentZoom);
            }
            ClampPan();
        }

        void SetZoomFocus(Vector2 viewportPoint)
        {
            _zoomFocusViewport = viewportPoint;
            _zoomFocusLocal = contentRoot.InverseTransformPoint(viewportRect.TransformPoint(viewportPoint));
            _hasZoomFocus = true;
        }

        bool IsPointerOverSelf() => RectTransformUtility.RectangleContainsScreenPoint(
            viewportRect, Input.mousePosition, EventCamera);

        Rect BoundsInViewport()
        {
            Vector2 min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
            Vector2 max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
            for (int i = 0; i < 4; i++)
            {
                Vector2 local = new Vector2((i & 1) == 0 ? _contentBounds.xMin : _contentBounds.xMax,
                    (i & 2) == 0 ? _contentBounds.yMin : _contentBounds.yMax);
                Vector2 point = viewportRect.InverseTransformPoint(contentRoot.TransformPoint(local));
                min = Vector2.Min(min, point);
                max = Vector2.Max(max, point);
            }
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        void ClampPan()
        {
            if (contentRoot == null || viewportRect == null) return;
            if (!_hasBounds) RefreshContentBounds();
            Rect bounds = BoundsInViewport();
            Rect viewport = viewportRect.rect;
            float paddingX = Mathf.Min(edgePadding, viewport.width * 0.25f);
            float paddingY = Mathf.Min(edgePadding, viewport.height * 0.25f);
            float x = bounds.width <= viewport.width - paddingX * 2f + 0.01f
                ? viewport.center.x - bounds.center.x
                : Mathf.Clamp(0f, viewport.xMax - paddingX - bounds.xMax, viewport.xMin + paddingX - bounds.xMin);
            float y = bounds.height <= viewport.height - paddingY * 2f + 0.01f
                ? viewport.center.y - bounds.center.y
                : Mathf.Clamp(0f, viewport.yMax - paddingY - bounds.yMax, viewport.yMin + paddingY - bounds.yMin);
            TranslateInViewport(new Vector2(x, y));
        }

        void CenterOnContentPoint(Vector2 point)
        {
            Vector2 current = viewportRect.InverseTransformPoint(contentRoot.TransformPoint(point));
            TranslateInViewport(viewportRect.rect.center - current);
        }

        void TranslateInViewport(Vector2 delta)
        {
            Vector3 worldDelta = viewportRect.TransformVector(delta);
            contentRoot.localPosition += contentRoot.parent.InverseTransformVector(worldDelta);
        }

        public void OnPointerDown(PointerEventData eventData) => BeginPointer(eventData);


        public void OnInitializePotentialDrag(PointerEventData eventData)
        {
            eventData.useDragThreshold = true;
            BeginPointer(eventData);
        }

        void BeginPointer(PointerEventData eventData)
        {
            _mouseDownPosition = eventData.position;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(viewportRect,
                eventData.position, eventData.pressEventCamera, out _pointerDownLocal);
            _contentDownLocalPosition = contentRoot != null ? contentRoot.localPosition : Vector3.zero;
            _dragging = false;
            _clickBlockedThroughFrame = -1;
            _hasZoomFocus = false;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (contentRoot == null || viewportRect == null) return;
            if (!_dragging)
            {
                if ((eventData.position - _mouseDownPosition).sqrMagnitude < dragThreshold * dragThreshold) return;
                _dragging = true;
                _isOverview = false;
                SkillTreeTooltipUI.Instance?.Hide();
            }
            eventData.eligibleForClick = false;
            _clickBlockedThroughFrame = Time.frameCount + 1;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(viewportRect,
                eventData.position, eventData.pressEventCamera, out var pointer);
            Vector3 worldDelta = viewportRect.TransformVector(pointer - _pointerDownLocal);
            contentRoot.localPosition = _contentDownLocalPosition + contentRoot.parent.InverseTransformVector(worldDelta);
            ClampPan();
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (_dragging)
            {
                eventData.eligibleForClick = false;
                _clickBlockedThroughFrame = Time.frameCount + 1;
            }
            _dragging = false;
        }
    }
}
