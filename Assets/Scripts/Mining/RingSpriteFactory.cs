using UnityEngine;

namespace Rebuild.Mining
{




    public static class RingSpriteFactory
    {
        const int Size = 256;
        const float OuterMargin = 6f;
        const int DotCount = 20;
        const float DotRadius = 7f;
        const float DotSoftEdge = 1.6f;

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


            var pixels = new Color32[Size * Size];


            var center = new Vector2(Size * 0.5f - 0.5f, Size * 0.5f - 0.5f);
            float circleR = Size * 0.5f - OuterMargin - DotRadius;



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



                        float alpha;
                        if (d <= DotRadius - DotSoftEdge) alpha = 1f;
                        else
                        {
                            float t = Mathf.Clamp01((DotRadius + DotSoftEdge - d) / (DotSoftEdge * 2f));
                            alpha = t;
                        }



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



        public static void Invalidate()
        {
            if (s_ringSprite != null) Object.DestroyImmediate(s_ringSprite);
            if (s_ringTex != null) Object.DestroyImmediate(s_ringTex);
            s_ringSprite = null;
            s_ringTex = null;
        }
    }
}
