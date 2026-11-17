using Rebuild.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Rebuild.Mining
{

    [RequireComponent(typeof(Image))]
    public sealed class SkillTreeEdgeUI : MonoBehaviour
    {
        const float Thickness = 3f;
        Image _image;
        Image _segment;
        RectTransform _rect;
        bool _hasGeometry;
        bool _highlighted;

        [SerializeField] UpgradeId _parentId;
        [SerializeField] bool _hasParent;
        [SerializeField] RectTransform _fromRect;
        [SerializeField] RectTransform _toRect;

        void Awake()
        {
            EnsureRefs();
            _rect.anchorMin = _rect.anchorMax = new Vector2(0.5f, 0.5f);
            _rect.pivot = new Vector2(0.5f, 0.5f);
            _image.raycastTarget = false;
        }

        void OnEnable()
        {
            if (SkillTreeManager.Instance != null)
                SkillTreeManager.Instance.OnUpgradeChanged += HandleUpgradeChanged;
            TreeNodeUI.HoveredNodeChanged += HandleHoveredNode;
            if (_fromRect != null && _toRect != null) SetEndpoints(_fromRect, _toRect);
            ResolveParentFromEndpoint();
            RefreshFromCurrentState();
        }

        void OnDisable()
        {
            if (SkillTreeManager.Instance != null)
                SkillTreeManager.Instance.OnUpgradeChanged -= HandleUpgradeChanged;
            TreeNodeUI.HoveredNodeChanged -= HandleHoveredNode;
            _highlighted = false;
        }

        public void SetEndpoints(RectTransform from, RectTransform to)
        {
            if (from == null || to == null) return;
            EnsureRefs();
            _fromRect = from;
            _toRect = to;
            _rect.anchorMin = _rect.anchorMax = new Vector2(0.5f, 0.5f);
            _rect.pivot = new Vector2(0.5f, 0.5f);
            _rect.anchoredPosition = Vector2.zero;
            _rect.sizeDelta = Vector2.zero;
            _rect.localRotation = Quaternion.identity;
            _image.enabled = false;

            Vector2 a = _rect.InverseTransformPoint(from.TransformPoint(from.rect.center));
            Vector2 b = _rect.InverseTransformPoint(to.TransformPoint(to.rect.center));
            Vector2 delta = b - a;
            float distance = delta.magnitude;
            var segment = GetSegment();
            var rt = (RectTransform)segment.transform;
            _hasGeometry = IsFinite(a) && IsFinite(b) && distance > 0.5f;
            if (_hasGeometry)
            {
                Vector2 direction = delta / distance;
                float fromRadius = VisualRadius(from);
                float toRadius = VisualRadius(to);
                Vector2 start = a + direction * fromRadius;
                Vector2 end = b - direction * toRadius;
                float length = Mathf.Max(0f, distance - fromRadius - toRadius);
                _hasGeometry = length > 0.5f;
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = (start + end) * 0.5f;
                rt.sizeDelta = new Vector2(length, Thickness);
                rt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
            }
            else
            {
                rt.anchoredPosition = Vector2.zero;
                rt.sizeDelta = new Vector2(0f, Thickness);
                rt.localRotation = Quaternion.identity;
            }


            var oldElbow = transform.Find("Segment_1");
            if (oldElbow != null && oldElbow.TryGetComponent<Image>(out var oldImage))
                oldImage.enabled = false;
            ResolveParentFromEndpoint();
            RefreshFromCurrentState();
        }

        float VisualRadius(RectTransform endpoint)
        {
            var node = endpoint.GetComponent<TreeNodeUI>();
            RectTransform visual = node != null ? node.VisualRect : endpoint;
            Vector3 horizontal = _rect.InverseTransformVector(visual.TransformVector(Vector3.right * visual.rect.width));
            Vector3 vertical = _rect.InverseTransformVector(visual.TransformVector(Vector3.up * visual.rect.height));
            return Mathf.Min(horizontal.magnitude, vertical.magnitude) * 0.5f;
        }

        static bool IsFinite(Vector2 value) =>
            !float.IsNaN(value.x) && !float.IsInfinity(value.x)
            && !float.IsNaN(value.y) && !float.IsInfinity(value.y);

        public void SetParentId(UpgradeId parentId)
        {
            _parentId = parentId;
            _hasParent = true;
            RefreshFromCurrentState();
        }

        void ResolveParentFromEndpoint()
        {
            if (_hasParent || _fromRect == null) return;
            var node = _fromRect.GetComponent<TreeNodeUI>();
            if (node == null) return;
            _parentId = node.Id;
            _hasParent = true;
        }

        public void RefreshTint(bool unlocked)
        {
            var segment = GetSegment();
            var manager = SkillTreeManager.Instance;
            var child = _toRect != null ? _toRect.GetComponent<TreeNodeUI>() : null;
            bool owned = child != null && manager != null && manager.GetLevel(child.Id) > 0;
            bool complete = child != null && manager != null && manager.IsMaxed(child.Id);
            Color tint = complete ? MuseumUiTheme.Gold : owned ? MuseumUiTheme.Jade : MuseumUiTheme.Locked;
            tint.a = _highlighted ? 1f : complete ? 0.8f : owned ? 0.62f : 0.55f;
            if (_highlighted && !complete) tint = MuseumUiTheme.Jade;
            segment.color = tint;
            segment.enabled = _hasGeometry && (manager == null || unlocked || owned);
            var size = segment.rectTransform.sizeDelta;
            size.y = _highlighted ? Thickness + 1f : Thickness;
            segment.rectTransform.sizeDelta = size;
        }

        void HandleUpgradeChanged(UpgradeId changed, int level)
        {
            var child = _toRect != null ? _toRect.GetComponent<TreeNodeUI>() : null;
            if ((_hasParent && changed == _parentId) || (child != null && changed == child.Id))
                RefreshFromCurrentState();
        }

        void HandleHoveredNode(UpgradeId? hovered)
        {
            _highlighted = false;
            var child = _toRect != null ? _toRect.GetComponent<TreeNodeUI>() : null;
            if (child != null)
            {

                for (int i = 0; hovered.HasValue && i < SkillTreeLayout.Nodes.Length; i++)
                {
                    if (hovered.Value == child.Id) { _highlighted = true; break; }
                    hovered = SkillTreeLayout.GetParent(hovered.Value);
                }
            }
            RefreshFromCurrentState();
        }

        void RefreshFromCurrentState()
        {
            bool unlocked = _hasParent && SkillTreeManager.Instance != null
                && SkillTreeManager.Instance.GetLevel(_parentId) > 0;
            RefreshTint(unlocked);
        }

        Image GetSegment()
        {
            if (_segment != null) return _segment;
            var child = transform.Find("Segment_0");
            if (child == null)
            {
                var go = new GameObject("Segment_0", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                go.transform.SetParent(transform, false);
                child = go.transform;
            }
            _segment = child.GetComponent<Image>();
            _segment.raycastTarget = false;
            return _segment;
        }

        void EnsureRefs()
        {
            if (_image == null) _image = GetComponent<Image>();
            if (_rect == null) _rect = (RectTransform)transform;
        }
    }
}
