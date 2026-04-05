using UnityEngine;

namespace Rebuild.Mining
{
    /// <summary>
    /// Runtime-generated rock + ore-tint palette for placeholders.
    /// Single ellipse sprite (shared across all 8 ore types) + per-ore Color tint
    /// applied via Image.color. Swap for real art in a later polish pass.
    /// </summary>
    public static class RockSpriteFactory
    {
        const int TexW = 192;
        const int TexH = 160;
        const float EdgeAA = 1.5f;

        static Sprite s_cachedRock;

        public static Sprite GetRockSprite()
        {
            if (s_cachedRock != null) return s_cachedRock;
            s_cachedRock = BuildEllipseSprite();
            return s_cachedRock;
        }

        public static Color GetOreColor(OreType ore)
        {
            switch (ore)
            {
                case OreType.Gold:    return new Color(1.00f, 0.84f, 0.10f, 1f);  // #FFD61A
                case OreType.Copper:  return new Color(0.85f, 0.43f, 0.18f, 1f);  // #D96E2E
                case OreType.Iron:    return new Color(0.78f, 0.80f, 0.84f, 1f);  // #C7CCD6
                case OreType.Cobalt:  return new Color(0.20f, 0.40f, 0.85f, 1f);  // #3366D9
                case OreType.Uranium: return new Color(0.42f, 0.95f, 0.40f, 1f);  // #6BF266
                case OreType.Ismium:  return new Color(0.65f, 0.40f, 0.95f, 1f);  // #A666F2
                case OreType.Iridium: return new Color(0.00f, 0.80f, 0.82f, 1f);  // #00CCD1
                case OreType.Painite: return new Color(0.85f, 0.20f, 0.45f, 1f);  // #D93373
                default: return Color.gray;
            }
        }

        static Sprite BuildEllipseSprite()
        {
            var tex = new Texture2D(TexW, TexH, TextureFormat.RGBA32, false)
            {
                name = "Rebuild_RockEllipse_Runtime",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave,
            };
            var pixels = new Color32[TexW * TexH];

            float cx = TexW * 0.5f;
            float cy = TexH * 0.5f;
            float a = TexW * 0.5f - 2f;
            float b = TexH * 0.5f - 2f;
            // Centre highlight (lighter), edge (darker) for a hint of volume.
            Color32 baseColor   = new Color32(220, 220, 220, 255);
            Color32 borderColor = new Color32(140, 140, 140, 255);

            for (int y = 0; y < TexH; y++)
            {
                float dy = y - cy + 0.5f;
                for (int x = 0; x < TexW; x++)
                {
                    float dx = x - cx + 0.5f;
                    // Ellipse equation (dx/a)^2 + (dy/b)^2 <= 1 → inside; convert to pixel distance from edge.
                    float n = (dx * dx) / (a * a) + (dy * dy) / (b * b);
                    if (n >= 1f) { pixels[y * TexW + x] = new Color32(0, 0, 0, 0); continue; }
                    float d = (1f - Mathf.Sqrt(n)) * Mathf.Min(a, b); // approx pixels-from-edge
                    byte alpha = (byte)Mathf.RoundToInt(Mathf.Clamp01(d / EdgeAA) * 255f);
                    float tBorder = Mathf.Clamp01(d / 12f); // 12-pixel falloff into base
                    Color32 c = Lerp(borderColor, baseColor, tBorder);
                    c.a = alpha;
                    pixels[y * TexW + x] = c;
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply(false, false);

            var sprite = Sprite.Create(
                tex, new Rect(0f, 0f, TexW, TexH), new Vector2(0.5f, 0.5f),
                100f, 0, SpriteMeshType.FullRect);
            sprite.name = "Rebuild_RockEllipse_Runtime";
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
