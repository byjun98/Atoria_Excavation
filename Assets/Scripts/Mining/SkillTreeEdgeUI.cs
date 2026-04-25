using Rebuild.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Rebuild.Mining
{
    /// <summary>thin Image bar connecting two <see cref="TreeNodeUI"/> rects.
    /// Lives as a child under the same parent as the nodes so anchored positions align.
    /// Tint indicates whether the upstream prerequisite (parent node) is unlocked.
    /// <para>edge now subscribes to <see cref="SkillTreeManager.OnUpgradeChanged"/>
    /// itself and refreshes its tint whenever the cached parent levels up. Setup wires the
    /// parent id through <see cref="SetParentId"/>.</para>
    /// </summary>
    [RequireComponent(typeof(Image))]
    public sealed class SkillTreeEdgeUI : MonoBehaviour
    {
        const float Thickness = 6f;

        Image _image;
        RectTransform _rect;
        readonly Image[] _segments = new Image[2];
        readonly bool[] _segmentUsed = new bool[2];

        // cached parent id for live-tint updates.
        [SerializeField] UpgradeId _parentId;
        [SerializeField] bool _hasParent;

        // cache the endpoint rects so OnEnable can recompute the bar's
        // anchoredPosition/sizeDelta/rotation after a domain reload. Without this, a NaN
        // RectTransform sweep would leave the edge geometry stuck at whatever stale value
        // was on disk (often (0,0) which then propagates as Invalid AABB / IsFinite asserts).
        [SerializeField] RectTransform _fromRect;
        [SerializeField] RectTransform _toRect;

        void Awake()
        {
            _image = GetComponent<Image>();
            _rect = (RectTransform)transform;
            // Pin anchors so anchoredPosition acts as a parent-centered offset — the layout
            // script positions nodes in the same anchor space, so endpoints line up.
            _rect.anchorMin = new Vector2(0.5f, 0.5f);
            _rect.anchorMax = new Vector2(0.5f, 0.5f);
            _rect.pivot = new Vector2(0.5f, 0.5f);
            if (_image != null) _image.raycastTarget = false;
        }

        void OnEnable()
        {
            if (SkillTreeManager.Instance != null)
                SkillTreeManager.Instance.OnUpgradeChanged += HandleUpgradeChanged;

            // recompute geometry from current node positions on every
            // enable (covers domain reloads where a NaN sweep zeroed the cached layout).
            if (_fromRect != null && _toRect != null)
                SetEndpoints(_fromRect, _toRect);

            ResolveParentFromEndpoint();
            RefreshFromCurrentState();
        }

        void OnDisable()
        {
            if (SkillTreeManager.Instance != null)
                SkillTreeManager.Instance.OnUpgradeChanged -= HandleUpgradeChanged;
        }

        /// <summary>Position this rect as a rotated bar spanning <paramref name="from"/> to <paramref name="to"/>.
        /// Assumes both endpoints share this transform's parent so anchored positions are comparable.
        /// <para>caches the endpoints + guards against NaN/Infinity inputs so a
        /// corrupted RectTransform on the source nodes can't poison this edge's geometry.</para></summary>
        public void SetEndpoints(RectTransform from, RectTransform to)
        {
            if (from == null || to == null) return;
            EnsureRefs();

            // Cache for OnEnable recompute after domain reload.
            _fromRect = from;
            _toRect = to;

            Vector2 a = from.anchoredPosition;
            Vector2 b = to.anchoredPosition;

            // NaN/Infinity guard — a single bad endpoint would otherwise cascade through
            // magnitude/atan2 and write NaN into our own RectTransform (triggering Invalid
            // AABB / IsFinite asserts on the Canvas).
            if (HasBadFloat(a.x) || HasBadFloat(a.y) || HasBadFloat(b.x) || HasBadFloat(b.y))
            {
                _rect.anchoredPosition = Vector2.zero;
                _rect.sizeDelta = new Vector2(0f, Thickness);
                _rect.localRotation = Quaternion.identity;
                SetSegmentVisible(0, false);
                SetSegmentVisible(1, false);
                return;
            }

            _rect.anchoredPosition = Vector2.zero;
            _rect.sizeDelta = Vector2.zero;
            _rect.localRotation = Quaternion.identity;
            if (_image != null) _image.enabled = false;

            bool sameColumn = Mathf.Abs(a.x - b.x) < 0.5f;
            bool sameRow = Mathf.Abs(a.y - b.y) < 0.5f;
            if (sameColumn || sameRow)
            {
                SetSegment(0, a, b);
                SetSegmentVisible(1, false);
            }
            else
            {
                Vector2 corner = new Vector2(b.x, a.y);
                SetSegment(0, a, corner);
                SetSegment(1, corner, b);
            }

            ResolveParentFromEndpoint();
        }

        static bool HasBadFloat(float f) => float.IsNaN(f) || float.IsInfinity(f);

        /// <summary>cache the parent upgrade so this edge can self-refresh
        /// when its prereq is met. Setup script calls this after <see cref="SetEndpoints"/>.</summary>
        public void SetParentId(UpgradeId parentId)
        {
            _parentId = parentId;
            _hasParent = true;
            RefreshFromCurrentState();
        }

        void ResolveParentFromEndpoint()
        {
            if (_hasParent) return;
            if (_fromRect == null) return;
            var node = _fromRect.GetComponent<TreeNodeUI>();
            if (node == null) return;
            _parentId = node.Id;
            _hasParent = true;
        }

        /// <summary>Recolor based on whether the upstream prerequisite is unlocked.</summary>
        public void RefreshTint(bool unlocked)
        {
            EnsureRefs();
            Color c = unlocked ? DancheongPalette.TitleGold : DancheongPalette.Disabled;
            c.a = unlocked ? 0.85f : 0.55f;
            if (_image != null) _image.color = c;
            for (int i = 0; i < _segments.Length; i++)
                if (_segments[i] != null) _segments[i].color = c;
        }

        void HandleUpgradeChanged(UpgradeId changed, int level)
        {
            if (_hasParent && changed == _parentId) RefreshFromCurrentState();
        }

        void RefreshFromCurrentState()
        {
            if (!_hasParent) { ApplyVisibility(false); return; }
            bool unlocked = SkillTreeManager.Instance != null
                            && SkillTreeManager.Instance.GetLevel(_parentId) > 0;
            // progressive hide. 부모 미해금 시 edge 자체도 숨김 (TreeNodeUI 가 자식
            // 노드를 SetActive(false) 하므로 edge 만 보이면 화살표 끝이 허공으로 향함).
            ApplyVisibility(unlocked);
            if (!unlocked) return;
            RefreshTint(unlocked);
        }

        void ApplyVisibility(bool visible)
        {
            EnsureRefs();
            if (_image != null) _image.enabled = false;
            for (int i = 0; i < _segments.Length; i++)
                if (_segments[i] != null) _segments[i].enabled = visible && _segmentUsed[i];
        }

        void SetSegment(int index, Vector2 start, Vector2 end)
        {
            var img = GetSegment(index);
            var rt = (RectTransform)img.transform;
            Vector2 delta = end - start;
            float dist = delta.magnitude;
            if (dist < 0.5f)
            {
                img.enabled = false;
                _segmentUsed[index] = false;
                return;
            }

            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = (start + end) * 0.5f;
            rt.sizeDelta = new Vector2(dist, Thickness);
            rt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
            _segmentUsed[index] = true;
            img.enabled = true;
        }

        void SetSegmentVisible(int index, bool visible)
        {
            var img = GetSegment(index);
            _segmentUsed[index] = visible;
            img.enabled = visible;
        }

        Image GetSegment(int index)
        {
            if (_segments[index] != null) return _segments[index];

            var child = transform.Find($"Segment_{index}");
            if (child == null)
            {
                var go = new GameObject($"Segment_{index}",
                    typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                go.transform.SetParent(transform, false);
                child = go.transform;
            }

            var img = child.GetComponent<Image>();
            if (_image != null)
            {
                img.sprite = _image.sprite;
                img.type = _image.type;
                img.pixelsPerUnitMultiplier = _image.pixelsPerUnitMultiplier;
                img.color = _image.color;
            }
            img.raycastTarget = false;
            _segments[index] = img;
            return img;
        }

        void EnsureRefs()
        {
            if (_image == null) _image = GetComponent<Image>();
            if (_rect == null) _rect = (RectTransform)transform;
        }
    }
}
