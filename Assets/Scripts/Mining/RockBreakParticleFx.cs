using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Rebuild.Mining
{

    public sealed class RockBreakParticleFx : MaskableGraphic
    {
        const int MaxChunks = 16;
        const int MaxIdle = 64;
        const float Lifetime = .45f;
        static readonly Stack<RockBreakParticleFx> s_idle = new Stack<RockBreakParticleFx>();
        static readonly Color RingColor = new Color(1f, .69f, .33f);
        static readonly Color Hot = new Color(1f, .93f, .70f);
        static readonly Color Stone = new Color(.42f, .32f, .25f);

        struct Chunk
        {
            public Vector2 Target;
            public float Radius;
            public float Spin;
        }

        readonly Chunk[] _chunks = new Chunk[MaxChunks];
        int _count;
        float _started;
        float _spread;

        public static RockBreakParticleFx Spawn(Transform parent, Vector2 anchoredPos, float spread = 1f)
        {
            if (parent == null) return null;
            RockBreakParticleFx fx = null;
            while (fx == null && s_idle.Count > 0) fx = s_idle.Pop();
            if (fx == null)
            {
                var go = new GameObject("RockBreakParticleFx", typeof(RectTransform), typeof(CanvasRenderer));
                go.SetActive(false);
                fx = go.AddComponent<RockBreakParticleFx>();
                fx.raycastTarget = false;
            }
            var rt = fx.rectTransform;
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = new Vector2(.5f, .5f);
            rt.pivot = new Vector2(.5f, .5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = Vector2.one * (330f * Mathf.Max(1f, spread));
            rt.SetAsLastSibling();
            fx.SetChunks(spread);
            fx._started = Time.unscaledTime - Time.unscaledDeltaTime;
            fx.gameObject.SetActive(true);
            return fx;
        }

        void SetChunks(float spread)
        {
            _count = Mathf.Clamp(Mathf.RoundToInt(7 * Mathf.Sqrt(Mathf.Max(.5f, spread))), 4, MaxChunks);
            _spread = Mathf.Max(.6f, spread);
            float size = 28f * Mathf.Max(.6f, Mathf.Sqrt(spread));
            for (int i = 0; i < _count; i++)
            {

                float angle = i * (Mathf.PI * 2f / _count) + Random.Range(-.4f, .4f);
                float distance = Random.Range(50f, 120f) * _spread;
                float upward = Mathf.Lerp(.1f, .35f, Random.value);
                var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle) + upward).normalized;
                _chunks[i] = new Chunk
                {
                    Target = direction * distance,
                    Radius = size * Random.Range(.75f, 1.25f) * .52f,
                    Spin = Random.Range(-540f, 540f) * Mathf.Deg2Rad,
                };
            }
        }

        void Update()
        {
            if (Time.unscaledTime - _started < Lifetime)
            {
                SetVerticesDirty();
                return;
            }

            gameObject.SetActive(false);
            if (s_idle.Count >= MaxIdle) { Destroy(gameObject); return; }
            transform.SetParent(ProjectileLauncher.PoolParent, false);
            s_idle.Push(this);
        }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            float age = Time.unscaledTime - _started;
            float k = Mathf.Clamp01(age / Lifetime);
            if (k >= 1f) return;

            AddImpact(mesh, age);
            float travel = 1f - Mathf.Pow(1f - k, 2.4f);
            Color shell = Color.Lerp(Hot, Stone, Mathf.Clamp01(k * 4f));
            shell.a = 1f - k;
            Color lit = Color.Lerp(Hot, RingColor, k);
            lit.a = shell.a * .78f;
            for (int i = 0; i < _count; i++)
            {
                ref var chunk = ref _chunks[i];
                Vector2 pos = chunk.Target * travel - Vector2.up * (155f * age * age * _spread);
                float angle = chunk.Spin * age;
                var axis = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (chunk.Radius * (1f - .25f * k));
                var normal = new Vector2(-axis.y, axis.x) * .69f;
                int first = mesh.currentVertCount;
                mesh.AddVert(pos + axis, lit, Vector2.zero);
                mesh.AddVert(pos + normal, shell, Vector2.zero);
                mesh.AddVert(pos - axis, shell, Vector2.zero);
                mesh.AddVert(pos - normal, lit, Vector2.zero);
                mesh.AddTriangle(first, first + 1, first + 2);
                mesh.AddTriangle(first, first + 2, first + 3);
            }
        }

        void AddImpact(VertexHelper mesh, float age)
        {
            const float duration = .18f;
            float k = Mathf.Clamp01(age / duration);
            if (k >= 1f) return;
            float radius = Mathf.Lerp(13f, 88f, 1f - (1f - k) * (1f - k)) * _spread;
            var edge = Color.Lerp(Hot, RingColor, k);
            edge.a = (1f - k) * (1f - k) * .78f;
            Color empty = new Color(edge.r, edge.g, edge.b, 0);
            int first = mesh.currentVertCount;
            const int sides = 12;
            for (int i = 0; i <= sides; i++)
            {
                float a = i * (Mathf.PI * 2 / sides);
                var dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a) * .45f);
                mesh.AddVert(dir * (radius + 4f * _spread), empty, Vector2.zero);
                mesh.AddVert(dir * radius, edge, Vector2.zero);
                mesh.AddVert(dir * (radius - 6f * _spread), empty, Vector2.zero);
                if (i == 0) continue;
                int c = first + i * 3;
                mesh.AddTriangle(c - 3, c, c + 1);
                mesh.AddTriangle(c - 3, c + 1, c - 2);
                mesh.AddTriangle(c - 2, c + 1, c + 2);
                mesh.AddTriangle(c - 2, c + 2, c - 1);
            }
        }
    }
}
