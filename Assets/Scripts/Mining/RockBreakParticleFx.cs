using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Rebuild.Mining
{
    /// <summary>burst of small rock chunks that fly outward + fade when a rock breaks.
    /// Mirrors the original <c>RockMechanics.RockBreakParticle()</c> (pooled ParticleSystem prefab,
    /// localScale=2.6 for rocks / 10 for BigRock) using UGUI Image children since the rest of our
    /// mining scene is canvas-based.
    /// <para>Spawns one root GameObject under <paramref name="parent"/> (typically the rock's
    /// TileRow) which fans out N chunks at random angles, lerps them outward 50–120 px while
    /// fading alpha 1→0 over <see cref="Lifetime"/> seconds, then destroys itself. Lives as a
    /// sibling of the rock so the rock's own fade animation doesn't take the chunks with it.</para>
    /// </summary>
    public sealed class RockBreakParticleFx : MonoBehaviour
    {
        const float Lifetime = 0.45f;
        const int DefaultChunkCount = 7;
        const float DefaultChunkSize = 28f;
        const float MinTravel = 50f;
        const float MaxTravel = 120f;

        struct Chunk
        {
            public RectTransform rt;
            public Image img;
            public Vector2 start;
            public Vector2 target;
            public float rotSpeed; // deg/sec
        }

        Chunk[] _chunks;

        /// <summary>Spawn a one-shot particle burst at <paramref name="anchoredPos"/> inside
        /// <paramref name="parent"/>. <paramref name="spread"/> scales the chunk count + travel
        /// distance for bigger explosions (BigRock passes ~3f, regular rocks pass 1f).</summary>
        public static RockBreakParticleFx Spawn(Transform parent, Vector2 anchoredPos, float spread = 1f)
        {
            if (parent == null) return null;
            var go = new GameObject("RockBreakParticleFx", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = Vector2.zero;
            rt.SetAsLastSibling();

            var fx = go.AddComponent<RockBreakParticleFx>();
            fx.Init(spread);
            return fx;
        }

        void Init(float spread)
        {
            int chunkCount = Mathf.Clamp(Mathf.RoundToInt(DefaultChunkCount * Mathf.Sqrt(Mathf.Max(0.5f, spread))), 4, 16);
            var sprite = RockSpriteFactory.GetRockSprite();
            float baseSize = DefaultChunkSize * Mathf.Max(0.6f, Mathf.Sqrt(spread));
            float travelScale = Mathf.Max(0.6f, spread);

            _chunks = new Chunk[chunkCount];
            for (int i = 0; i < chunkCount; i++)
            {
                // Random angle around the origin so the burst looks omni-directional rather than
                // a tidy ring (we still bias slightly upward to feel like a rock cracking).
                float angle = (i * (Mathf.PI * 2f / chunkCount)) + Random.Range(-0.4f, 0.4f);
                float dist = Random.Range(MinTravel, MaxTravel) * travelScale;
                float upwardBias = Mathf.Lerp(0.1f, 0.35f, Random.value);
                Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle) + upwardBias).normalized;
                Vector2 target = dir * dist;

                var cgo = new GameObject($"chunk_{i}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                cgo.transform.SetParent(transform, false);
                var crt = (RectTransform)cgo.transform;
                crt.anchorMin = crt.anchorMax = new Vector2(0.5f, 0.5f);
                crt.pivot = new Vector2(0.5f, 0.5f);
                crt.anchoredPosition = Vector2.zero;
                float sz = baseSize * Random.Range(0.75f, 1.25f);
                crt.sizeDelta = new Vector2(sz, sz);

                var img = cgo.GetComponent<Image>();
                img.sprite = sprite;
                img.preserveAspect = true;
                img.raycastTarget = false;
                // Stone-grey base with a hint of warmth so chunks read against the dirt tile.
                img.color = new Color(0.42f, 0.36f, 0.32f, 1f);

                _chunks[i] = new Chunk
                {
                    rt = crt,
                    img = img,
                    start = Vector2.zero,
                    target = target,
                    rotSpeed = Random.Range(-540f, 540f),
                };
            }

            StartCoroutine(Lifecycle());
        }

        IEnumerator Lifecycle()
        {
            float t = 0f;
            while (t < Lifetime)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / Lifetime);
                // Ease-out so chunks fly fast at the start, slow as they fade.
                float pos = 1f - Mathf.Pow(1f - k, 2.4f);
                float alpha = 1f - k;
                for (int i = 0; i < _chunks.Length; i++)
                {
                    var c = _chunks[i];
                    if (c.rt == null) continue;
                    c.rt.anchoredPosition = Vector2.LerpUnclamped(c.start, c.target, pos);
                    c.rt.localRotation = Quaternion.Euler(0f, 0f, c.rotSpeed * t);
                    if (c.img != null)
                    {
                        var col = c.img.color; col.a = alpha;
                        c.img.color = col;
                    }
                }
                yield return null;
            }
            Destroy(gameObject);
        }
    }
}
