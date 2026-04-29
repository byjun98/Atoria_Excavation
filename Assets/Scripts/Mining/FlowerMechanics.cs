using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Rebuild.Mining
{
    /// <summary>SpringSeason talent system.
    /// Singleton on MiningSessionRoot. Other code calls <see cref="TryRollOnRockDestroy"/>
    /// from Rock.FadeAndDestroy; if the talent is chosen we roll a chance and, on success,
    /// spawn one Flower under Canvas_Game at a random anchored position.</summary>
    public sealed class FlowerMechanics : MonoBehaviour
    {
        public static FlowerMechanics Instance { get; private set; }

        [Header("Spawn behaviour")]
        [SerializeField] float spawnChance = 0.12f;          // 12% per destroyed rock
        // 160 → 90 으로 축소.
        // 90 → 60 추가 축소 (사용자 보고: 꽃이 행 두께를 넘어 "땅 위 아님" 느낌).
        // 원본 SetRockScreen.SetFlowers 의 localScale 0.075 small icon 모델 매칭.
        [SerializeField] Vector2 sizeDelta = new Vector2(60f, 60f);
        [SerializeField] Vector2 spawnRangeX = new Vector2(-1500f, 1500f);
        [SerializeField] Vector2 spawnRangeY = new Vector2(-360f, 360f);
        [SerializeField] int maxConcurrent = 4;
        // Y clamp 비율 (row.sizeDelta.y 의 ±15%). 원본 ±1.5f 가
        // row 두께 (~146) 대비 매우 좁은 한 줄 라인 → row 안에 머무는 효과.
        [SerializeField] float rowYClampRatio = 0.15f;

        // original SetRockScreen.flowersOnScreen is rolled ONCE at session start
        // (Random.Range(2, 18), inclusive-exclusive). LevelMechanics.GiveXP reads this for the
        // SpringSeason XP multiplier (`xp × (1 + 0.015 × budget)`). NOT alive-count: original
        // ties the bonus to a per-session allowance, decoupled from actual spawn timing.
        [Header("SpringSeason XP budget")]
        [SerializeField] Vector2Int sessionBudgetRange = new Vector2Int(2, 18);
        public int SessionFlowerBudget { get; private set; }

        Transform _canvasGameTransform;
        Transform _flowerParent;
        readonly List<Flower> _alive = new List<Flower>();

        void Awake() { Instance = this; }
        void OnDestroy() { if (Instance == this) Instance = null; }

        /// <summary>called by <see cref="MiningSessionController.StartSession"/>
        /// to roll a per-session flower budget. SpringSeason XP multiplier reads this.</summary>
        public void RollSessionBudget()
        {
            SessionFlowerBudget = UnityEngine.Random.Range(sessionBudgetRange.x, sessionBudgetRange.y);
        }

        /// <summary>원본 SetRockScreen.SetFlowers 모델 진입점. WaveThenSpawnThenTimer
        /// 가 rockSpawner.SpawnAll 후 호출 — row 들 빌드된 상태에서 N 개 꽃 일괄 spawn. SpringSeason
        /// talent 활성 시에만 작동.</summary>
        public void SpawnBatchAtStart()
        {
            if (LevelMechanics.Instance == null) return;
            if (!LevelMechanics.Instance.HasTalent(TalentId.SpringSeason)) return;
            SpawnBatch(SessionFlowerBudget);
        }

        /// <summary>원본 SetRockScreen.SetFlowers 모델. N 개 꽃을 14 row 중 random +
        /// row 안 무작위 위치에 일괄 spawn. RollSessionBudget 후 한 번만 호출.</summary>
        void SpawnBatch(int count)
        {
            if (_canvasGameTransform == null) return;
            EnsureFlowerParent();
            if (_flowerParent == null) return;

            // RockSpawner 의 row 리스트 활용 — 없으면 fallback 으로 spawnRange 전체.
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
                // Fallback — 화면 전체 spawn range.
                Spawn();
                spawned++;
            }
        }

        /// <summary>특정 row 안 무작위 가로 위치에 꽃 spawn. 원본 (Random.Range(77, -77),
        /// Random.Range(-1.5, 1.5)) 모델 — 거의 한 줄로 정렬.</summary>
        void SpawnInRow(RectTransform row)
        {
            var go = new GameObject("Flower", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(row, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = sizeDelta;
            // row width 기반 spawn range (원본 ±77px → row width 비율).
            float rowWidth = row.sizeDelta.x;
            float halfWidth = (rowWidth > 0 ? rowWidth * 0.5f : 1000f) * 0.9f;
            // Y 범위를 row 두께에 비례해서 clamp. 원본 ±1.5f 가 row 안의
            // 거의 한 줄 라인 효과 → Shell row 두께 (~146) 의 ±15% = ±21.9 가 시각적으로
            // 적당한 "땅 위 한 줄" 모양 형성. sizeDelta 60px 와 합쳐도 row 두께 안에 머묾.
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

        /// <summary>clear the per-session budget. Called at session end so a
        /// menu-time XP credit (e.g. idle TheMine) doesn't accidentally inherit the bonus.</summary>
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

        /// <summary>deprecated. 원본 모델로 변경: RollSessionBudget 시 일괄
        /// spawn (TryRollOnRockDestroy 의 per-rock 12% 폐기). Rock.FadeAndDestroy 의 호출은 no-op.</summary>
        public Flower TryRollOnRockDestroy(Rock source)
        {
            return null; // 세션 시작 시 일괄 spawn 으로 대체.
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
            // Render above rocks for clickability.
            rt.SetAsLastSibling();
            var flower = go.AddComponent<Flower>();
            flower.Initialize();
            _alive.Add(flower);
            // stat — total flowers spawned (vs FlowersClicked which counts pickups).
            Rebuild.Stats.StatsManager.Instance?.RecordFlowerSpawned();
            return flower;
        }

        void EnsureFlowerParent()
        {
            if (_flowerParent != null) return;
            if (_canvasGameTransform == null) return;
            // Reuse a single FlowerParent child to keep the hierarchy tidy.
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
