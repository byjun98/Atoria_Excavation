using UnityEngine;

namespace Rebuild.Mining
{



    public static class FireSpriteFactory
    {
        static Sprite s_flame;
        static Sprite s_glow;
        static Sprite s_flower;

        public static Sprite GetFlame()
        {
            if (s_flame != null) return s_flame;
            const int W = 128, H = 192;
            var tex = NewTex(W, H);
            float cx = (W - 1) * 0.5f;
            var pixels = new Color32[W * H];
            for (int y = 0; y < H; y++)
            {

                float yN = y / (float)(H - 1);
                float pivot = 0.35f;
                float widthFactor;
                if (yN < pivot) widthFactor = Mathf.Lerp(0.75f, 1.0f, yN / pivot);
                else            widthFactor = Mathf.Lerp(1.0f, 0.0f, (yN - pivot) / (1f - pivot));
                float halfW = W * 0.42f * widthFactor;
                for (int x = 0; x < W; x++)
                {
                    float dx = Mathf.Abs(x - cx);
                    if (dx > halfW || halfW < 0.5f) { pixels[y * W + x] = new Color32(0, 0, 0, 0); continue; }
                    float k = Mathf.Clamp01(1f - dx / halfW);
                    k = k * k;

                    float bottom = Mathf.Clamp01(yN * 6f);
                    pixels[y * W + x] = new Color(1f, 1f, 1f, k * bottom);
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply(false);
            s_flame = NewSprite(tex, W, H);
            return s_flame;
        }

        public static Sprite GetGlow()
        {
            if (s_glow != null) return s_glow;
            const int N = 192;
            var tex = NewTex(N, N);
            float cx = (N - 1) * 0.5f, cy = (N - 1) * 0.5f;
            float r = N * 0.48f;
            var pixels = new Color32[N * N];
            for (int y = 0; y < N; y++)
            for (int x = 0; x < N; x++)
            {
                float dx = x - cx, dy = y - cy;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float k = Mathf.Clamp01(1f - d / r);
                pixels[y * N + x] = new Color(1f, 1f, 1f, k * k);
            }
            tex.SetPixels32(pixels);
            tex.Apply(false);
            s_glow = NewSprite(tex, N, N);
            return s_glow;
        }

        public static Sprite GetFlower()
        {
            if (s_flower != null) return s_flower;
            const int N = 128;
            var tex = NewTex(N, N);
            float cx = (N - 1) * 0.5f, cy = (N - 1) * 0.5f;
            float petalR = N * 0.34f;
            float petalRadius = N * 0.18f;
            float coreRadius = N * 0.14f;
            var pixels = new Color32[N * N];

            const int petalCount = 5;
            var petalCenters = new Vector2[petalCount];
            for (int i = 0; i < petalCount; i++)
            {
                float a = (Mathf.PI * 2f * i) / petalCount + Mathf.PI / 2f;
                petalCenters[i] = new Vector2(cx + Mathf.Cos(a) * petalR, cy + Mathf.Sin(a) * petalR);
            }
            for (int y = 0; y < N; y++)
            for (int x = 0; x < N; x++)
            {
                float dxC = x - cx, dyC = y - cy;
                float dC = Mathf.Sqrt(dxC * dxC + dyC * dyC);
                float kCore = Mathf.Clamp01(1f - dC / coreRadius);
                kCore = kCore * kCore;
                float kPetal = 0f;
                for (int i = 0; i < petalCount; i++)
                {
                    float dx = x - petalCenters[i].x;
                    float dy = y - petalCenters[i].y;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float kk = Mathf.Clamp01(1f - d / petalRadius);
                    if (kk > kPetal) kPetal = kk;
                }
                float k = Mathf.Max(kCore, kPetal * 0.95f);
                pixels[y * N + x] = new Color(1f, 1f, 1f, k);
            }
            tex.SetPixels32(pixels);
            tex.Apply(false);
            s_flower = NewSprite(tex, N, N);
            return s_flower;
        }

        static Texture2D NewTex(int w, int h)
        {
            return new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };
        }

        static Sprite NewSprite(Texture2D tex, int w, int h)
        {
            var s = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
            s.hideFlags = HideFlags.HideAndDontSave;
            return s;
        }
    }
}
