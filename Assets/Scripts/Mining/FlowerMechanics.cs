using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Rebuild.Mining
{




    public sealed class FlowerMechanics : MonoBehaviour
    {
        public static FlowerMechanics Instance { get; private set; }

        [Header("Spawn behaviour")]
        [SerializeField] float spawnChance = 0.12f;



        [SerializeField] Vector2 sizeDelta = new Vector2(60f, 60f);
        [SerializeField] Vector2 spawnRangeX = new Vector2(-1500f, 1500f);
        [SerializeField] Vector2 spawnRangeY = new Vector2(-360f, 360f);
        [SerializeField] int maxConcurrent = 4;


        [SerializeField] float rowYClampRatio = 0.15f;





        [Header("SpringSeason XP budget")]
        [SerializeField] Vector2Int sessionBudgetRange = new Vector2Int(2, 18);
        public int SessionFlowerBudget { get; private set; }

        Transform _canvasGameTransform;
        Transform _flowerParent;
        readonly List<Flower> _alive = new List<Flower>();

        void Awake() { Instance = this; }
        void OnDestroy() { if (Instance == this) Instance = null; }



        public void RollSessionBudget()
        {
            SessionFlowerBudget = UnityEngine.Random.Range(sessionBudgetRange.x, sessionBudgetRange.y);
        }




        public void SpawnBatchAtStart()
        {
            if (LevelMechanics.Instance == null) return;
            if (!LevelMechanics.Instance.HasTalent(TalentId.SpringSeason)) return;
            SpawnBatch(SessionFlowerBudget);
        }



        void SpawnBatch(int count)
        {
            if (_canvasGameTransform == null) return;
            EnsureFlowerParent();
            if (_flowerParent == null) return;


            var grid = MiningSessionController.Instance != null ? MiningSessionController.Instance.TileGrid : null;
            var rows = grid != null ? grid.RowContainers : null;

            int spawned = 0;
            for (int i = 0; i < count && _alive.Count + spawned < maxConcurrent * 4; i++)
            {
                if (rows != null && rows.Length > 0)
                {
                    var row = rows[UnityEngine.Random.Range(0, rows.Length)];
                    if (row != null) { SpawnInRow(row); spawned++; continue; }
                }

                Spawn();
                spawned++;
            }
        }



        void SpawnInRow(RectTransform row)
        {
            var go = new GameObject("Flower", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(row, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = sizeDelta;

            float rowWidth = row.sizeDelta.x;
            float halfWidth = (rowWidth > 0 ? rowWidth * 0.5f : 1000f) * 0.9f;



            float rowHeight = row.sizeDelta.y;
            float halfYRange = (rowHeight > 0 ? rowHeight * rowYClampRatio : 3f);
            rt.anchoredPosition = new Vector2(
                UnityEngine.Random.Range(-halfWidth, halfWidth),
                UnityEngine.Random.Range(-halfYRange, halfYRange));
            rt.localScale = Vector3.one;
            rt.SetAsLastSibling();
            var flower = go.AddComponent<Flower>();
            flower.Initialize();
            _alive.Add(flower);
            Rebuild.Stats.StatsManager.Instance?.RecordFlowerSpawned();
        }



        public void ResetSessionBudget()
        {
            SessionFlowerBudget = 0;
        }

        public void BindCanvasGame(Transform canvasGameRoot)
        {
            _canvasGameTransform = canvasGameRoot;
            EnsureFlowerParent();
        }

        public void ClearAll()
        {
            for (int i = 0; i < _alive.Count; i++)
            {
                var f = _alive[i];
                if (f == null) continue;
                if (Application.isPlaying) Destroy(f.gameObject); else DestroyImmediate(f.gameObject);
            }
            _alive.Clear();
        }



        public Flower TryRollOnRockDestroy(Rock source)
        {
            return null;
        }

        Flower Spawn()
        {
            EnsureFlowerParent();
            if (_flowerParent == null) return null;
            var go = new GameObject("Flower", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(_flowerParent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = sizeDelta;
            rt.anchoredPosition = new Vector2(
                UnityEngine.Random.Range(spawnRangeX.x, spawnRangeX.y),
                UnityEngine.Random.Range(spawnRangeY.x, spawnRangeY.y));
            rt.localScale = Vector3.one;

            rt.SetAsLastSibling();
            var flower = go.AddComponent<Flower>();
            flower.Initialize();
            _alive.Add(flower);

            Rebuild.Stats.StatsManager.Instance?.RecordFlowerSpawned();
            return flower;
        }

        void EnsureFlowerParent()
        {
            if (_flowerParent != null) return;
            if (_canvasGameTransform == null) return;

            for (int i = 0; i < _canvasGameTransform.childCount; i++)
            {
                var c = _canvasGameTransform.GetChild(i);
                if (c != null && c.name == "FlowerParent") { _flowerParent = c; return; }
            }
            var go = new GameObject("FlowerParent", typeof(RectTransform));
            go.transform.SetParent(_canvasGameTransform, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = Vector2.zero;
            _flowerParent = go.transform;
        }
    }
}
