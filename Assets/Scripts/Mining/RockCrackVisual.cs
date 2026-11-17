using UnityEngine;
using UnityEngine.UI;

namespace Rebuild.Mining
{

    public sealed class RockCrackVisual : MaskableGraphic
    {
        static readonly Vector2[] s_main =
        {
            new Vector2(-.18f, .075f), new Vector2(-.116f, .033f), new Vector2(-.143f, -.016f),
            new Vector2(-.057f, -.059f), new Vector2(-.081f, -.122f), new Vector2(.013f, -.19f),
        };
        static readonly Vector2[] s_right =
        {
            new Vector2(-.057f, -.059f), new Vector2(.006f, -.033f), new Vector2(.059f, -.076f),
            new Vector2(.117f, -.054f), new Vector2(.196f, -.113f),
        };
        static readonly Vector2[] s_upper =
        {
            new Vector2(.059f, -.076f), new Vector2(.087f, -.020f), new Vector2(.158f, .014f),
            new Vector2(.194f, -.012f),
        };
        static readonly Vector2[] s_lower =
        {
            new Vector2(-.081f, -.122f), new Vector2(-.155f, -.148f), new Vector2(-.198f, -.181f),
        };
        static readonly Color Ink = new Color(.11f, .080f, .067f, .89f);
        static readonly Color Rim = new Color(.91f, .76f, .59f, .49f);

        float _opacity = 1f;
        float _mirror = 1f;
        public float Progress { get; private set; }

        public static RockCrackVisual Create(RectTransform rock)
        {
            var go = new GameObject("Rock Cracks", typeof(RectTransform), typeof(CanvasRenderer));
            var rt = (RectTransform)go.transform;
            rt.SetParent(rock, false);
            rt.SetAsFirstSibling();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            var visual = go.AddComponent<RockCrackVisual>();
            visual.raycastTarget = false;
            visual._mirror = (rock.GetInstanceID() & 2) == 0 ? 1f : -1f;
            return visual;
        }

        public void SetProgress(float progress)
        {
            if (Mathf.Approximately(Progress, progress)) return;
            Progress = progress;
            SetVerticesDirty();
        }

        public void SetOpacity(float opacity)
        {
            if (Mathf.Approximately(_opacity, opacity)) return;
            _opacity = opacity;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            if (Progress <= 0 || _opacity <= 0) return;
            var rect = rectTransform.rect;
            float alpha = _opacity * Mathf.Clamp01(.25f + Progress * 2.7f);
            DrawFissure(mesh, rect, s_main, Mathf.InverseLerp(0, .72f, Progress), alpha, 1f);
            DrawFissure(mesh, rect, s_right, Mathf.InverseLerp(.29f, .90f, Progress), alpha, .85f);
            DrawFissure(mesh, rect, s_upper, Mathf.InverseLerp(.64f, 1f, Progress), alpha, .67f);
            DrawFissure(mesh, rect, s_lower, Mathf.InverseLerp(.66f, 1f, Progress), alpha, .61f);
        }

        void DrawFissure(VertexHelper mesh, Rect rect, Vector2[] points, float growth, float alpha, float width)
        {
            float segments = growth * (points.Length - 1);
            var scale = new Vector2(rect.width * _mirror, rect.height);
            for (int i = 0; i < points.Length - 1 && i < segments; i++)
            {
                var from = rect.center + Vector2.Scale(points[i], scale);
                var to = rect.center + Vector2.Scale(Vector2.Lerp(points[i], points[i + 1], Mathf.Clamp01(segments - i)), scale);
                float taper = i == points.Length - 2 ? .47f : 1f;
                float thickness = Mathf.Lerp(.012f, .033f, Progress) * rect.width * width * taper;
                var edge = new Vector2(.28f, -.6f) * thickness;
                Stroke(mesh, from + edge, to + edge, thickness * .83f, Rim, alpha);
                Stroke(mesh, from, to, thickness, Ink, alpha);
            }
        }

        static void Stroke(VertexHelper mesh, Vector2 a, Vector2 b, float width, Color color, float alpha)
        {
            var delta = b - a;
            var side = new Vector2(-delta.y, delta.x).normalized * (width * .5f);
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
