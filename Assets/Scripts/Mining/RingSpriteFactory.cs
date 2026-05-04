using UnityEngine;

namespace Rebuild.Mining
{
    /// <summary>generates a **dotted** white ring sprite at runtime (matches the
    /// original game's mining-area indicator: ~20 small dots evenly spaced around the
    /// circumference, transparent inside). Used by <see cref="MiningCursorUI"/> for the mining-area
    /// indicator. Cached statically with <see cref="HideFlags.HideAndDontSave"/> — runtime-only.</summary>
    public static class RingSpriteFactory
    {
        const int Size = 256;
        const float OuterMargin = 6f;     // distance from texture edge to ring centerline
        const int DotCount = 20;          // dots evenly spaced around the circle
        const float DotRadius = 7f;       // each dot's radius in px (at 256-square base)
        const float DotSoftEdge = 1.6f;   // anti-alias band on the dot edge

        static Sprite s_ringSprite;
        static Texture2D s_ringTex;

        public static Sprite GetRing()
        {
            if (s_ringSprite != null) return s_ringSprite;

            s_ringTex = new Texture2D(Size, Size, TextureFormat.RGBA32, mipChain: false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave,
                name = "MiningAreaRing_runtime",
            };

            // Start fully transparent — we'll only paint the dots.
            var pixels = new Color32[Size * Size];
            // (Color32 default is (0,0,0,0) which is what we want for an empty texture.)

            var center = new Vector2(Size * 0.5f - 0.5f, Size * 0.5f - 0.5f);
            float circleR = Size * 0.5f - OuterMargin - DotRadius; // dot CENTER orbit radius

            // Plot DotCount dots evenly around the circumference. Only iterate within each dot's
            // bounding box for efficiency (DotCount × ~225 pixels instead of full Size² scan).
            for (int i = 0; i < DotCount; i++)
            {
                float angle = (i / (float)DotCount) * Mathf.PI * 2f;
                Vector2 dotCenter = new Vector2(
                    center.x + Mathf.Cos(angle) * circleR,
                    center.y + Mathf.Sin(angle) * circleR);

                int x0 = Mathf.Max(0, Mathf.FloorToInt(dotCenter.x - DotRadius - 1));
                int x1 = Mathf.Min(Size, Mathf.CeilToInt(dotCenter.x + DotRadius + 1));
                int y0 = Mathf.Max(0, Mathf.FloorToInt(dotCenter.y - DotRadius - 1));
                int y1 = Mathf.Min(Size, Mathf.CeilToInt(dotCenter.y + DotRadius + 1));

                for (int y = y0; y < y1; y++)
                {
                    for (int x = x0; x < x1; x++)
                    {
                        float d = Vector2.Distance(new Vector2(x, y), dotCenter);
                        if (d > DotRadius + DotSoftEdge) continue;

                        // Soft edge: full alpha when inside (d < DotRadius - SoftEdge),
                        // fading linearly to 0 at d = DotRadius + SoftEdge.
                        float alpha;
                        if (d <= DotRadius - DotSoftEdge) alpha = 1f;
                        else
                        {
                            float t = Mathf.Clamp01((DotRadius + DotSoftEdge - d) / (DotSoftEdge * 2f));
                            alpha = t;
                        }

                        // Take max so overlapping dots don't darken each other (dots are spaced
                        // far enough apart at DotCount=20 to not overlap, but max keeps it safe).
                        int idx = y * Size + x;
                        byte a = (byte)Mathf.RoundToInt(alpha * 255f);
                        if (a > pixels[idx].a)
                        {
                            pixels[idx] = new Color32(255, 255, 255, a);
                        }
                    }
                }
            }

            s_ringTex.SetPixels32(pixels);
            s_ringTex.Apply(updateMipmaps: false, makeNoLongerReadable: true);

            s_ringSprite = Sprite.Create(s_ringTex,
                new Rect(0f, 0f, Size, Size),
                new Vector2(0.5f, 0.5f),
                pixelsPerUnit: 100f);
            s_ringSprite.name = "MiningAreaRing_runtime";
            s_ringSprite.hideFlags = HideFlags.HideAndDontSave;
            return s_ringSprite;
        }

        /// <summary>Drop the cached sprite so the next <see cref="GetRing"/> regenerates the
        /// texture. Useful when tuning DotCount/DotRadius constants in the editor.</summary>
        public static void Invalidate()
        {
            if (s_ringSprite != null) Object.DestroyImmediate(s_ringSprite);
            if (s_ringTex != null) Object.DestroyImmediate(s_ringTex);
            s_ringSprite = null;
            s_ringTex = null;
        }
    }
}
