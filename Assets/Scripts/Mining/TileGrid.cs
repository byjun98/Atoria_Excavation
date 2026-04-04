using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Rebuild.Mining
{
    /// <summary>
    /// 14x14 = 196 isometric diamond tile grid. Builds the grid on demand and runs the
    /// per-row wave entry animation. Rock spawning lives in (TileGrid will hand off
    /// row references to SpawnRocks at the end of each TileRowUpDown).
    /// </summary>
    public sealed class TileGrid : MonoBehaviour
    {
        public const int Rows = 14;
        public const int Cols = 14;

        [Header("Grid layout (match original SampleScene exactly)")]
        [SerializeField] Vector2 tilesAnchoredPos = new Vector2(-212f, 153f);
        [SerializeField] float tilesScale = 0.963f;
        [SerializeField] Vector2 rowOriginStart = new Vector2(600f, -293f);
        [SerializeField] Vector2 rowOriginStep = new Vector2(-70f, 34.6f);
        // Each row is scaled down so its child tile spacing visually matches the
        // (row_x_step, row_y_step) above — without this, tiles spread out 2.2× too far.
        // Value taken from original SetRockScreen.SetAllTiles: tileRow[N].localScale = 0.45562.
        [SerializeField] float rowScale = 0.45562f;
        [SerializeField] Vector2 tileStartInRow = new Vector2(1160f, 330f);
        [SerializeField] Vector2 tileStepInRow = new Vector2(-153.5f, -76.4f);
        // 2x the row spacing so adjacent diamonds meet edge-to-edge (no gaps between cells).
        // After rowScale (0.45562) is applied, visual = (140, 69.2) ≈ 2 * (row x_step 70, row y_step 34.6).
        [SerializeField] Vector2 tileSize = new Vector2(307f, 151.88f);

        [Header("Wave animation")]
        [SerializeField] float rowStagger = 0.065f;
        [SerializeField] float upHalfDuration = 0.105f;
        [SerializeField] float upDeltaY = 63f;

        [Header("Tile prefab fields")]
        [SerializeField] Color tileColor = Color.white;

        RectTransform _tilesRoot;
        RectTransform[] _rowContainers;
        Vector2[] _rowRestPos;
        bool _isBuilt;

        public RectTransform[] RowContainers => _rowContainers;
        public Vector2 TileStartInRow => tileStartInRow;
        public Vector2 TileStepInRow => tileStepInRow;
        public int TilesPerRow => Cols;

        public void BuildIfNeeded()
        {
            if (_isBuilt) return;

            _tilesRoot = GetOrCreateChildRect(transform, "Tiles");
            _tilesRoot.anchorMin = _tilesRoot.anchorMax = new Vector2(0.5f, 0.5f);
            _tilesRoot.pivot = new Vector2(0.5f, 0.5f);
            _tilesRoot.anchoredPosition = tilesAnchoredPos;
            _tilesRoot.localScale = new Vector3(tilesScale, tilesScale, 1f);

            _rowContainers = new RectTransform[Rows];
            _rowRestPos = new Vector2[Rows];

            // prefer the user-authored Tile PNG (Sprites/World/Tile) over the runtime
            // diamond. Falls back to the dirt generator when the slot is empty *or* when
            // the user PNG isn't isometric.
            //
            // 🚨 자동 reject 규칙: 사용자가 isometric (가로:세로 ≈ 2:1) 다이아몬드 대신
            //    1:1 비율의 정사각형 (rotated square) 또는 일반 사각형 tile 을 그리는 경우가
            //    흔함. alpha-trimmed aspect ratio < 1.6 이면 isometric 이 아니라고 판단해서
            //    fallback runtime diamond 를 사용. (이상적인 isometric 다이아몬드 = 2.0)
            var userTile = SpriteLibrary.GetTileSprite();
            if (userTile != null && userTile.textureRect.height > 0f)
            {
                float aspect = userTile.textureRect.width / userTile.textureRect.height;
                if (aspect < 1.6f)
                {
                    Debug.LogWarning(
                        $"[Rebuild][TileGrid] User Tile.png alpha aspect = {aspect:F2} (need ≥ 1.6 for isometric). "
                      + "PNG 안의 다이아몬드가 2:1 비율이 아니라 fallback runtime tile 을 사용합니다. "
                      + "256×128 PNG 영역을 가득 채우는 isometric 다이아몬드 "
                      + "(양옆 끝까지 닿는 ◇ 모양) 로 다시 만들어 주세요.");
                    userTile = null;
                }
            }
            var dirtSprite = userTile != null ? userTile : TileSpriteFactory.GetDirtDiamond();
            Vector2 cellSize = tileSize;
            if (userTile != null)
            {
                float wRatio = userTile.textureRect.width  > 0 ? userTile.rect.width  / userTile.textureRect.width  : 1f;
                float hRatio = userTile.textureRect.height > 0 ? userTile.rect.height / userTile.textureRect.height : 1f;
                cellSize = new Vector2(tileSize.x * wRatio, tileSize.y * hRatio);
            }

            for (int i = 0; i < Rows; i++)
            {
                var row = GetOrCreateChildRect(_tilesRoot, $"TileRow_{i + 1}");
                row.anchorMin = row.anchorMax = new Vector2(0.5f, 0.5f);
                row.pivot = new Vector2(0.5f, 0.5f);
                var rowPos = rowOriginStart + new Vector2(rowOriginStep.x * i, rowOriginStep.y * i);
                row.anchoredPosition = rowPos;
                row.localScale = new Vector3(rowScale, rowScale, 1f);
                _rowContainers[i] = row;
                _rowRestPos[i] = rowPos;

                for (int j = 0; j < Cols; j++)
                {
                    var tileName = $"Tile_{i + 1}_{j + 1}";
                    var tileT = row.Find(tileName);
                    RectTransform tile;
                    if (tileT == null)
                    {
                        var go = new GameObject(tileName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                        go.transform.SetParent(row, false);
                        tile = (RectTransform)go.transform;
                        var img = go.GetComponent<Image>();
                        img.sprite = dirtSprite;
                        img.color = tileColor;
                        img.raycastTarget = false;
                        img.preserveAspect = false;
                    }
                    else
                    {
                        tile = (RectTransform)tileT;
                    }
                    tile.anchorMin = tile.anchorMax = new Vector2(0.5f, 0.5f);
                    tile.pivot = new Vector2(0.5f, 0.5f);
                    tile.anchoredPosition = tileStartInRow + new Vector2(tileStepInRow.x * j, tileStepInRow.y * j);
                    tile.sizeDelta = cellSize;
                    tile.localScale = Vector3.one;
                }
            }
            _isBuilt = true;
        }

        public void ResetRowsToRest()
        {
            if (_rowContainers == null) return;
            for (int i = 0; i < _rowContainers.Length; i++)
            {
                if (_rowContainers[i] != null)
                    _rowContainers[i].anchoredPosition = _rowRestPos[i];
            }
        }

        /// <summary>Plays the row-by-row wave entry. Returns when all rows have settled.</summary>
        public IEnumerator PlayWaveAnim()
        {
            BuildIfNeeded();
            ResetRowsToRest();

            for (int i = 0; i < Rows; i++)
            {
                StartCoroutine(TileRowUpDown(i));
                yield return new WaitForSecondsRealtime(rowStagger);
            }
            // wait for the last row's full bounce so the caller knows the wave is done
            yield return new WaitForSecondsRealtime(upHalfDuration * 2f);
        }

        IEnumerator TileRowUpDown(int rowIndex)
        {
            if (rowIndex < 0 || rowIndex >= _rowContainers.Length) yield break;
            var rt = _rowContainers[rowIndex];
            if (rt == null) yield break;
            var rest = _rowRestPos[rowIndex];
            var up = rest + new Vector2(0f, upDeltaY);

            float t = 0f;
            while (t < upHalfDuration)
            {
                t += Time.unscaledDeltaTime;
                rt.anchoredPosition = Vector2.Lerp(rest, up, Mathf.Clamp01(t / upHalfDuration));
                yield return null;
            }
            rt.anchoredPosition = up;
            t = 0f;
            while (t < upHalfDuration)
            {
                t += Time.unscaledDeltaTime;
                rt.anchoredPosition = Vector2.Lerp(up, rest, Mathf.Clamp01(t / upHalfDuration));
                yield return null;
            }
            rt.anchoredPosition = rest;
        }

        static RectTransform GetOrCreateChildRect(Transform parent, string name)
        {
            var t = parent.Find(name);
            if (t != null) return (RectTransform)t;
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }
    }
}
