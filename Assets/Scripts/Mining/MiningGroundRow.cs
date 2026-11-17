using UnityEngine;
using UnityEngine.UI;

namespace Rebuild.Mining
{
    [RequireComponent(typeof(CanvasRenderer))]
    sealed class MiningGroundRow : Graphic
    {
        const float SoilUvScale = 1.85f;
        const float FurrowWidth = .057f;
        const float BankDepth = 28f;

        Texture2D _soil;
        Vector2 _corner;
        Vector2 _across;
        Vector2 _along;
        int _row;

        public override Texture mainTexture => _soil != null ? _soil : base.mainTexture;

        public void Initialize(Texture2D soil, Vector2 center, Vector2 across, Vector2 along, int row, Color tint)
        {
            _soil = soil;
            _corner = center - (across + along) * .5f;
            _across = across;
            _along = along;
            _row = row;
            color = tint;
            raycastTarget = false;
            SetAllDirty();
        }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            if (_soil == null) return;

            var down = Vector2.down * BankDepth;
            for (int col = 0; col < TileGrid.Cols; col++)
            {
                var a = _corner + _along * col;
                var b = a + _along;
                var c = b + _across;
                var d = a + _across;
                var uv = new Vector2(col / (float)TileGrid.Cols, _row / (float)TileGrid.Rows) * SoilUvScale;
                var du = Vector2.right * (SoilUvScale / TileGrid.Cols);
                var dv = Vector2.up * (SoilUvScale / TileGrid.Rows);
                var grain = .97f + ((col * 11 + _row * 17) % 9) * .007f;

                if (_row == 0)
                    AddBank(mesh, a, b, down, uv, uv + du, -dv, .77f);
                if (col == TileGrid.Cols - 1)
                    AddBank(mesh, b, c, down, uv + du, uv + du + dv, du, .67f);

                var top = Tint(grain);
                AddQuad(mesh, a, b, c, d, uv, uv + du, uv + du + dv, uv + dv, top, top);


                var furrow = Tint(grain * .51f);
                var rim = Tint(grain * 1.10f);
                if (_row != 0)
                    AddQuad(mesh, a, b, b + _across * FurrowWidth, a + _across * FurrowWidth,
                        uv, uv + du, uv + du + dv * FurrowWidth, uv + dv * FurrowWidth, furrow, rim);
                if (col != 0)
                    AddQuad(mesh, d, a, a + _along * FurrowWidth, d + _along * FurrowWidth,
                        uv + dv, uv, uv + du * FurrowWidth, uv + dv + du * FurrowWidth, furrow, rim);
            }
        }

        void AddBank(VertexHelper mesh, Vector2 a, Vector2 b, Vector2 down,
            Vector2 uvA, Vector2 uvB, Vector2 uvDown, float shade)
        {
            var rim = down * .21f;
            AddQuad(mesh, a, b, b + rim, a + rim,
                uvA, uvB, uvB + uvDown * .06f, uvA + uvDown * .06f, Tint(.94f), Tint(shade));
            AddQuad(mesh, a + rim, b + rim, b + down, a + down,
                uvA + uvDown * .06f, uvB + uvDown * .06f, uvB + uvDown * .32f, uvA + uvDown * .32f,
                Tint(shade), Tint(shade * .67f));
        }

        Color32 Tint(float factor) => new Color(color.r * factor, color.g * factor, color.b * factor, color.a);

        static void AddQuad(VertexHelper mesh, Vector2 a, Vector2 b, Vector2 c, Vector2 d,
            Vector2 uvA, Vector2 uvB, Vector2 uvC, Vector2 uvD, Color32 near, Color32 far)
        {
            int first = mesh.currentVertCount;
            mesh.AddVert(a, near, uvA);
            mesh.AddVert(b, near, uvB);
            mesh.AddVert(c, far, uvC);
            mesh.AddVert(d, far, uvD);
            mesh.AddTriangle(first, first + 1, first + 2);
            mesh.AddTriangle(first + 2, first + 3, first);
        }
    }
}
