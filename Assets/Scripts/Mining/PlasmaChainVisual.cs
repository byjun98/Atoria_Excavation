using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Rebuild.Mining
{

    public sealed class PlasmaChainVisual : MaskableGraphic
    {
        const string OrbSheetPath = "AtoriaFX/PlasmaOrb";
        const int FrameSize = 64;
        const int FrameCount = 12;
        const int OrbFramesPerSecond = 24;
        const float ArcDuration = .17f;
        const float LinkStagger = .012f;

        static readonly Color ArcEdge = new Color(.38f, .18f, .61f, .85f);
        static readonly Color ArcBody = new Color(.72f, .41f, .99f, 1f);
        static readonly Color ArcCore = new Color(1f, .91f, 1f, 1f);
        static readonly Color LightningEdge = new Color(.12f, .45f, .98f, .88f);
        static readonly Color LightningBody = new Color(.36f, .83f, 1f, 1f);
        static readonly Color LightningCore = new Color(.91f, .99f, 1f, 1f);
        static Sprite[] s_orbFrames;

        readonly List<Vector2> _hitPoints = new List<Vector2>(5);
        Image _orb;
        Vector2 _origin;
        float _orbSize;
        float _hitAt;
        int _arcFrame;
        int _pulse;
        bool _singleStrike;
        Color Edge => _singleStrike ? LightningEdge : ArcEdge;
        Color Body => _singleStrike ? LightningBody : ArcBody;
        Color Core => _singleStrike ? LightningCore : ArcCore;

        public static bool TryBegin(RectTransform board, Vector3 worldOrigin, float sizeScale,
            bool singleStrike, ref PlasmaChainVisual visual)
        {
            if (board == null || (!singleStrike && !LoadFrames())) return false;
            if (visual == null)
            {
                var root = new GameObject("Plasma Chain", typeof(RectTransform), typeof(CanvasRenderer));
                root.SetActive(false);
                visual = root.AddComponent<PlasmaChainVisual>();
                visual.raycastTarget = false;

            }
            if (!singleStrike && visual._orb == null)
            {
                var orb = new GameObject("PixelFX Plasma Orb", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                orb.transform.SetParent(visual.transform, false);
                visual._orb = orb.GetComponent<Image>();
                visual._orb.raycastTarget = false;
            }

            var rect = visual.rectTransform;
            rect.SetParent(board, false);
            rect.SetAsLastSibling();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;

            visual._origin = rect.InverseTransformPoint(worldOrigin);
            visual._singleStrike = singleStrike;
            visual._orbSize = 104f * sizeScale;
            if (visual._orb != null)
            {
                visual._orb.gameObject.SetActive(!singleStrike);
                visual._orb.rectTransform.anchoredPosition = visual.OrbCenter;
            }
            visual._hitPoints.Clear();
            visual._hitAt = float.NegativeInfinity;
            visual._arcFrame = -1;
            visual._pulse = 0;
            visual.gameObject.SetActive(true);
            visual.Advance(0, 1);
            return true;
        }

        static bool LoadFrames()
        {
            if (s_orbFrames != null) return true;
            var sheet = Resources.Load<Texture2D>(OrbSheetPath);
            const int columns = 4;
            const int rows = FrameCount / columns;
            if (sheet == null || sheet.width != FrameSize * columns || sheet.height != FrameSize * rows)
                return false;

            s_orbFrames = new Sprite[FrameCount];
            for (int i = 0; i < FrameCount; i++)
            {
                var rect = new Rect((i % columns) * FrameSize, (rows - 1 - i / columns) * FrameSize,
                    FrameSize, FrameSize);
                s_orbFrames[i] = Sprite.Create(sheet, rect, new Vector2(.5f, .5f), 100, 0, SpriteMeshType.FullRect);
            }
            return true;
        }

        Vector2 OrbCenter => _origin + new Vector2(0, _singleStrike ? 0 : _orbSize * .21f);

        public void BeginHit()
        {
            _hitPoints.Clear();
            _hitAt = Time.unscaledTime;
            _arcFrame = -1;
            _pulse++;
        }

        public void RecordHit(Vector3 worldPosition)
        {
            _hitPoints.Add(rectTransform.InverseTransformPoint(worldPosition));
        }

        public void Advance(float time, float life)
        {
            float sinceHit = Time.unscaledTime - _hitAt;
            if (!_singleStrike)
            {
                var sprite = s_orbFrames[Mathf.FloorToInt(time * OrbFramesPerSecond) % FrameCount];
                if (_orb.sprite != sprite) _orb.sprite = sprite;
                float surge = Mathf.Clamp01(1 - sinceHit / .12f) * .22f;
                float scale = Mathf.Lerp(.58f, 1f, Mathf.Clamp01(time / .10f)) + surge;
                _orb.rectTransform.sizeDelta = Vector2.one * (_orbSize * scale);
                float alpha = Mathf.Clamp01((life - time) / .14f);
                _orb.color = new Color(1, 1, 1, alpha);
            }
            float duration = _singleStrike ? Mathf.Min(.45f, .25f + .026f * _hitPoints.Count) : ArcDuration;
            int frame = sinceHit < duration ? Mathf.FloorToInt(sinceHit * (_singleStrike ? 70 : 35)) : -1;
            if (frame == _arcFrame) return;
            _arcFrame = frame;
            SetVerticesDirty();
        }

        public void Clear()
        {
            _hitPoints.Clear();
            gameObject.SetActive(false);
        }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            if (_arcFrame < 0) return;

            float age = _arcFrame / (_singleStrike ? 70f : 35f);
            var from = OrbCenter;
            int link = 0;
            for (int i = 0; i < _hitPoints.Count; i++)
            {
                var point = _hitPoints[i];
                bool originHit = (point - _origin).sqrMagnitude < .01f;
                float linkAge = age - (_singleStrike ? .026f : LinkStagger) * link;
                if (linkAge < 0) break;
                float strength = Mathf.Clamp01(1f - linkAge / (_singleStrike ? .24f : .14f));
                if (_singleStrike)
                    strength *= _arcFrame % 5 == 3 ? .68f : 1f;
                if (!originHit)
                {
                    if (strength > 0) DrawArc(mesh, from, point, link, strength);
                    from = point;
                    link++;
                }
                DrawContact(mesh, point, strength, i);
            }
        }

        void DrawArc(VertexHelper mesh, Vector2 from, Vector2 to, int link, float strength)
        {
            var delta = to - from;
            float length = delta.magnitude;
            if (length < 1f) return;
            var normal = new Vector2(-delta.y, delta.x) / length;
            int steps = Mathf.Clamp(Mathf.CeilToInt(length / 22f), 4, 12);
            float jitter = Mathf.Min(_singleStrike ? 14f : 9f, length * .15f);
            float fade = Mathf.Lerp(.8f, 1f, strength) * (_singleStrike ? 1.55f : 1f);
            var prior = from;
            for (int i = 1; i <= steps; i++)
            {
                float u = (float)i / steps;
                float bend = i == steps ? 0 : Jitter(link, i) * Mathf.Sin(u * Mathf.PI) * jitter;
                var point = from + delta * u + normal * bend;
                Stroke(mesh, prior, point, 9f * fade, Edge, strength * .76f);
                Stroke(mesh, prior, point, 5f * fade, Body, strength);
                Stroke(mesh, prior, point, 2.35f * fade, Core, strength);
                if (i == steps / 2 && strength > .35f)
                {
                    var branch = point + normal * Jitter(link, i + 29) * 17f + delta.normalized * 10f;
                    Stroke(mesh, point, branch, 1.8f * fade, Body, strength * .7f);
                }
                prior = point;
            }
        }

        void DrawContact(VertexHelper mesh, Vector2 point, float strength, int index)
        {
            if (strength <= 0) return;
            Quad(mesh, point, _singleStrike ? 14f : 8f, Edge, strength * .75f);
            Quad(mesh, point, _singleStrike ? 6f : 4.2f, Core, strength);
            for (int i = 0; i < 3; i++)
            {
                float angle = i * 2.094f + Jitter(index, i + 37);
                var dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                float reach = 7f + 12f * (1 - strength);
                Stroke(mesh, point + dir * 4f, point + dir * reach, 1.8f, Body, strength);
                Quad(mesh, point + dir * (reach + 3f), 1.2f, Core, strength * .75f);
            }
        }

        float Jitter(int link, int step)
        {

            int shapeFrame = _arcFrame / 2;
            uint bits = (uint)(_pulse * 6719 + shapeFrame * 1291 + link * 2179 + step * 1151);
            bits = unchecked((bits ^ (bits >> 15)) * 2246822519u);
            return (bits & 1023u) / 511.5f - 1f;
        }

        static void Quad(VertexHelper mesh, Vector2 center, float radius, Color color, float alpha)
        {
            int first = mesh.currentVertCount;
            color.a *= alpha;
            mesh.AddVert(center + Vector2.up * radius, color, Vector2.zero);
            mesh.AddVert(center + Vector2.right * radius, color, Vector2.zero);
            mesh.AddVert(center + Vector2.down * radius, color, Vector2.zero);
            mesh.AddVert(center + Vector2.left * radius, color, Vector2.zero);
            mesh.AddTriangle(first, first + 1, first + 2);
            mesh.AddTriangle(first, first + 2, first + 3);
        }

        static void Stroke(VertexHelper mesh, Vector2 a, Vector2 b, float width, Color color, float alpha)
        {
            var axis = b - a;
            var side = new Vector2(-axis.y, axis.x).normalized * (width * .5f);
            int first = mesh.currentVertCount;
            color.a *= alpha;
            mesh.AddVert(a - side, color, Vector2.zero);
            mesh.AddVert(a + side, color, Vector2.zero);
            mesh.AddVert(b + side, color, Vector2.zero);
            mesh.AddVert(b - side, color, Vector2.zero);
            mesh.AddTriangle(first, first + 1, first + 2);
            mesh.AddTriangle(first, first + 2, first + 3);
        }
    }
}
