using UnityEngine;

namespace Rebuild.Mining
{
    /// <summary>
    /// Runtime-generated isometric diamond sprite for placeholder tiles.
    /// </summary>
    public static class TileSpriteFactory
    {
        const int TexW = 256;
        const int TexH = 128;
        const float EdgeAA = 1.5f;

        static Sprite s_cachedDirt;

        public static Sprite GetDirtDiamond()
        {
            if (s_cachedDirt != null) return s_cachedDirt;
            s_cachedDirt = BuildDiamondSprite(new Color32(123, 90, 58, 255), new Color32(80, 55, 35, 255));
            return s_cachedDirt;
        }

        static Sprite BuildDiamondSprite(Color32 inner, Color32 border)
        {
            var tex = new Texture2D(TexW, TexH, TextureFormat.RGBA32, false)
            {
                name = "Rebuild_TileDiamond_Runtime",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave,
            };
            var pixels = new Color32[TexW * TexH];

            float cx = TexW * 0.5f;
            float cy = TexH * 0.5f;
            float halfW = TexW * 0.5f;
            float halfH = TexH * 0.5f;
            float borderBand = 6f; // pixels of darker border

            for (int y = 0; y < TexH; y++)
            {
                float dy = y - cy + 0.5f;
                for (int x = 0; x < TexW; x++)
                {
                    float dx = x - cx + 0.5f;
                    // Diamond equation: |dx|/halfW + |dy|/halfH <= 1
                    float dist = Mathf.Abs(dx) / halfW + Mathf.Abs(dy) / halfH;
                    // Convert to a pseudo-pixel distance from edge for AA.
                    // (1 - dist) scaled by ~half the min radius.
                    float pixDistFromEdge = (1f - dist) * Mathf.Min(halfW, halfH);
                    if (pixDistFromEdge <= 0f)
                    {
                        pixels[y * TexW + x] = new Color32(0, 0, 0, 0);
                        continue;
                    }

                    byte alpha = (byte)Mathf.RoundToInt(Mathf.Clamp01(pixDistFromEdge / EdgeAA) * 255f);
                    Color32 c;
                    if (pixDistFromEdge < borderBand)
                    {
                        float t = pixDistFromEdge / borderBand;
                        c = Lerp(border, inner, t);
                    }
                    else
                    {
                        c = inner;
                    }
                    c.a = alpha;
                    pixels[y * TexW + x] = c;
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply(false, false);

            var sprite = Sprite.Create(
                tex,
                new Rect(0f, 0f, TexW, TexH),
                new Vector2(0.5f, 0.5f),
                100f, 0, SpriteMeshType.FullRect);
            sprite.name = "Rebuild_TileDiamond_Runtime";
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }

        static Color32 Lerp(Color32 a, Color32 b, float t)
        {
            return new Color32(
                (byte)Mathf.RoundToInt(Mathf.Lerp(a.r, b.r, t)),
                (byte)Mathf.RoundToInt(Mathf.Lerp(a.g, b.g, t)),
                (byte)Mathf.RoundToInt(Mathf.Lerp(a.b, b.b, t)),
                255);
        }
    }
}
