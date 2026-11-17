using UnityEngine;
using UnityEngine.Sprites;
using UnityEngine.UI;

namespace Rebuild.UI
{
    [RequireComponent(typeof(CanvasRenderer))]
    sealed class StartScreenRelicDrift : Graphic
    {
        const int RelicCount = 12;

        Sprite _relic;
        float _age;

        public override Texture mainTexture => _relic != null ? _relic.texture : base.mainTexture;

        public void Initialize(Sprite relic)
        {
            _relic = relic;
            raycastTarget = false;
            SetAllDirty();
        }

        void Update()
        {
            _age += Time.unscaledDeltaTime;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            if (_relic == null) return;

            var bounds = rectTransform.rect;
            var uv = DataUtility.GetOuterUV(_relic);
            for (int i = 0; i < RelicCount; i++)
            {
                float phase = Mathf.Repeat(_age / (12f + i % 5) + i * .381966f, 1f);
                float x = .13f + (i / 2) % 3 * .064f;
                if (i % 2 != 0) x = 1f - x;
                x += Mathf.Sin(phase * 7f + i * 1.9f) * .018f;
                var center = new Vector2(
                    bounds.xMin + bounds.width * x,
                    bounds.yMin + bounds.height * Mathf.Lerp(.12f, .87f, phase));

                float halfWidth = bounds.height * (.011f + i % 4 * .002f);
                float halfHeight = halfWidth * _relic.rect.height / _relic.rect.width;
                float angle = Mathf.Sin(phase * 5f + i) * .23f;
                var right = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                var up = new Vector2(-right.y, right.x);
                var a = right * halfWidth;
                var b = up * halfHeight;
                float alpha = Mathf.SmoothStep(0f, 1f, phase / .18f) *
                              (1f - Mathf.SmoothStep(0f, 1f, (phase - .70f) / .30f));
                var tint = (Color32)new Color(.98f, .91f, .75f, alpha * (.34f + i % 3 * .05f));
                int first = mesh.currentVertCount;
                mesh.AddVert(center - a - b, tint, new Vector2(uv.x, uv.y));
                mesh.AddVert(center - a + b, tint, new Vector2(uv.x, uv.w));
                mesh.AddVert(center + a + b, tint, new Vector2(uv.z, uv.w));
                mesh.AddVert(center + a - b, tint, new Vector2(uv.z, uv.y));
                mesh.AddTriangle(first, first + 1, first + 2);
                mesh.AddTriangle(first + 2, first + 3, first);
            }
        }
    }
}
