using UnityEngine;

namespace Rebuild.Mining
{







    public static class ProjectileSpriteFactory
    {
        static Sprite s_radial;
        static Sprite s_bar;
        static Sprite s_square;
        static Sprite s_burst;


        static Sprite s_realBeam, s_realPlazma, s_realDynamite, s_realSplash;

        static Sprite s_realGlow, s_realRing;
        static bool s_realLoadAttempted;

        public static Sprite Get(ProjectileType type)
        {
            EnsureRealLoaded();
            switch (type)
            {
                case ProjectileType.BeamOfLight:       return s_realBeam      != null ? s_realBeam      : GetBar();
                case ProjectileType.PlazmaBall:        return s_realPlazma    != null ? s_realPlazma    : GetRadial();
                case ProjectileType.Dynamite:          return s_realDynamite  != null ? s_realDynamite  : GetSquare();
                case ProjectileType.ElectricitySplash: return s_realSplash    != null ? s_realSplash    : GetBurst();
                default:                                return GetRadial();
            }
        }

        static void EnsureRealLoaded()
        {
            if (s_realLoadAttempted) return;
            s_realLoadAttempted = true;


        }



        public static Sprite GetGlow()
        {
            EnsureRealLoaded();
            return s_realGlow;
        }



        public static Sprite GetRing()
        {
            EnsureRealLoaded();
            return s_realRing;
        }

        public static Sprite GetRadial()
        {
            if (s_radial != null) return s_radial;
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
                k = k * k;
                pixels[y * N + x] = new Color(1f, 1f, 1f, k);
            }
            tex.SetPixels32(pixels);
            tex.Apply(false);
            s_radial = NewSprite(tex, N, N);
            return s_radial;
        }

        public static Sprite GetBar()
        {
            if (s_bar != null) return s_bar;
            const int W = 256, H = 64;
            var tex = NewTex(W, H);
            float cy = (H - 1) * 0.5f;
            float vR = H * 0.42f;
            var pixels = new Color32[W * H];
            for (int y = 0; y < H; y++)
            {
                float dy = y - cy;
                float k = Mathf.Clamp01(1f - Mathf.Abs(dy) / vR);
                k = k * k;
                for (int x = 0; x < W; x++)
                {
                    float ex = Mathf.Clamp01(Mathf.Min(x, W - 1 - x) / 14f);
                    pixels[y * W + x] = new Color(1f, 1f, 1f, k * ex);
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply(false);
            s_bar = NewSprite(tex, W, H);
            return s_bar;
        }

        public static Sprite GetSquare()
        {
            if (s_square != null) return s_square;
            const int N = 192;
            var tex = NewTex(N, N);
            float cx = (N - 1) * 0.5f, cy = (N - 1) * 0.5f;
            float maxR = N * 0.48f;
            var pixels = new Color32[N * N];
            for (int y = 0; y < N; y++)
            for (int x = 0; x < N; x++)
            {
                float dxAbs = Mathf.Abs(x - cx), dyAbs = Mathf.Abs(y - cy);
                float dSquare = Mathf.Max(dxAbs, dyAbs);
                float dCircle = Mathf.Sqrt(dxAbs * dxAbs + dyAbs * dyAbs);
                float baseK = Mathf.Clamp01(1f - dSquare / (N * 0.46f)) * 0.7f;

                float armA = Mathf.Clamp01(1f - Mathf.Abs(dxAbs - dyAbs) / 10f);
                float armMask = Mathf.Clamp01(1f - dCircle / maxR);
                float arms = armA * armMask;
                float k = Mathf.Max(baseK, arms);
                pixels[y * N + x] = new Color(1f, 1f, 1f, k);
            }
            tex.SetPixels32(pixels);
            tex.Apply(false);
            s_square = NewSprite(tex, N, N);
            return s_square;
        }

        public static Sprite GetBurst()
        {
            if (s_burst != null) return s_burst;
            const int N = 192;
            var tex = NewTex(N, N);
            float cx = (N - 1) * 0.5f, cy = (N - 1) * 0.5f;
            float maxR = N * 0.48f;
            var pixels = new Color32[N * N];
            for (int y = 0; y < N; y++)
            for (int x = 0; x < N; x++)
            {
                float dx = x - cx, dy = y - cy;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                if (d > maxR) { pixels[y * N + x] = new Color(0f, 0f, 0f, 0f); continue; }
                float angle = Mathf.Atan2(dy, dx);

                float spoke = Mathf.Abs(Mathf.Sin(angle * 4f));
                float k = (1f - d / maxR) * spoke;
                k = Mathf.Clamp01(k * 1.6f);
                pixels[y * N + x] = new Color(1f, 1f, 1f, k);
            }
            tex.SetPixels32(pixels);
            tex.Apply(false);
            s_burst = NewSprite(tex, N, N);
            return s_burst;
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
