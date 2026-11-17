using System;
using UnityEngine;
using UnityEngine.UI;

namespace Rebuild.Mining
{












    public sealed class MiningCursorUI : MonoBehaviour
    {
        public static MiningCursorUI Instance { get; private set; }

        [SerializeField] Canvas hostCanvas;
        [SerializeField] RectTransform cursorRoot;
        [SerializeField] Image areaCircle;
        [SerializeField] Image pickaxeIcon;

        [Header("Tuning")]


        [SerializeField] float baseAreaRadius = 70f;
        [SerializeField] float pickaxeSize = 110f;
        [SerializeField] float swingDuration = 0.18f;
        [SerializeField] float swingPeakAngle = -50f;
        [SerializeField] bool hideSystemCursor = false;

        Camera _camera;
        bool _subscribed;


        float _mineTickAccum;
        const float OriginalColliderPulseSec = 0.04f;
        const float ShootCircleRadiusMultiplier = 0.55f;
        const float ShootCircleTravelMultiplier = 2.0f;
        readonly System.Collections.Generic.List<Rock> _hitBuffer = new System.Collections.Generic.List<Rock>(32);
        bool _miningInputActive;
















        public float CurrentAreaRadiusPx
        {
            get
            {
                float area = AnvilManager.Instance != null ? AnvilManager.Instance.CurrentMiningAreaSize : 1f;

                if (SkillTreeManager.Instance != null)
                {

                    int lv = SkillTreeManager.Instance.GetLevel(UpgradeId.ImprovedPickaxe);
                    if (lv > 0) area *= 1f + Rock.GetImprovedPickaxeBonus(lv);


                    int biggerLv = SkillTreeManager.Instance.GetLevel(UpgradeId.BiggerMiningArea);
                    if (biggerLv > 0) area *= 1f + biggerLv * 0.02f;
                }

                if (LevelMechanics.Instance != null && LevelMechanics.Instance.HasTalent(TalentId.ShapeShifter))
                {
                    area *= 1.06f;
                }



                if (PotionBuffMechanics.Instance != null && PotionBuffMechanics.Instance.PotionPickaxeStats > 0f)
                {
                    area *= 1f + PotionBuffMechanics.Instance.PotionPickaxeStats;
                }





                if (LevelMechanics.Instance != null && LevelMechanics.Instance.HasTalent(TalentId.BigMiningArea))
                {
                    area *= 1.4f;
                }




                if (LevelMechanics.Instance != null && LevelMechanics.Instance.HasTalent(TalentId.BiggerSplash))
                {
                    area *= 1.15f;
                }

                if (SkillTreeManager.Instance != null
                    && SkillTreeManager.Instance.GetLevel(UpgradeId.IncreaseAndDecreaseMinignErea) > 0)
                {
                    area *= GetAreaOscillationMultiplier(Time.unscaledTime);
                }
                return baseAreaRadius * area;
            }
        }

        static float GetAreaOscillationMultiplier(float time)
        {
            float phase = 0.5f + 0.5f * Mathf.Sin(time * Mathf.PI);
            return Mathf.Lerp(1f, 1.10f, phase);
        }

        void Awake()
        {
            Instance = this;
            if (hostCanvas == null) hostCanvas = GetComponentInParent<Canvas>();
            _camera = hostCanvas != null ? hostCanvas.worldCamera : null;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void OnEnable()
        {
            if (hideSystemCursor) Cursor.visible = false;

            if (AnvilManager.Instance != null)
            {
                AnvilManager.Instance.OnPickaxeChanged += HandlePickaxeChanged;
                _subscribed = true;
            }








            EnsureRingSprite();



            if (pickaxeIcon != null) pickaxeIcon.gameObject.SetActive(false);
            UpdateAreaSize();
        }

        void EnsureRingSprite()
        {
            if (areaCircle == null) return;


            areaCircle.sprite = RingSpriteFactory.GetRing();
            areaCircle.color = new Color(1f, 1f, 1f, 0.85f);
            areaCircle.preserveAspect = true;
            areaCircle.raycastTarget = false;
            areaCircle.type = Image.Type.Simple;
        }

        void OnDisable()
        {
            if (hideSystemCursor) Cursor.visible = true;

            if (_subscribed && AnvilManager.Instance != null)
            {
                AnvilManager.Instance.OnPickaxeChanged -= HandlePickaxeChanged;
                _subscribed = false;
            }
        }

        void Update()
        {
            if (cursorRoot == null || hostCanvas == null) return;

            if (!CanMineNow())
            {
                SetMiningInputActive(false);
                return;
            }

            SetMiningInputActive(true);


            var canvasRT = (RectTransform)hostCanvas.transform;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRT, Input.mousePosition, _camera, out Vector2 local))
            {
                cursorRoot.anchoredPosition = local;
            }




            UpdateAreaSize();




            float interval = CurrentMineTime();
            if (interval <= 0f) return;
            _mineTickAccum += Time.unscaledDeltaTime;


            int safety = 4;
            while (_mineTickAccum >= interval && safety-- > 0)
            {
                _mineTickAccum -= interval;
                ScanAndHit();
                interval = CurrentMineTime();
                if (interval <= 0f) break;
            }
        }

        bool CanMineNow()
        {
            var session = MiningSessionController.Instance;
            return session != null
                && session.IsInSession
                && !session.ResultsShown
                && session.TimerRunning;
        }

        void SetMiningInputActive(bool active)
        {
            if (cursorRoot != null && cursorRoot.gameObject.activeSelf != active)
                cursorRoot.gameObject.SetActive(active);

            if (_miningInputActive == active) return;
            _miningInputActive = active;
            if (!active) ResetMineCadence();
        }

        public void ResetMineCadence()
        {
            _mineTickAccum = 0f;
            _hitBuffer.Clear();
        }











        float CurrentMineTime()
        {
            float t = 0.45f;
            if (AnvilManager.Instance != null)
            {
                int idx = AnvilManager.Instance.CurrentIdx;
                if (idx >= 0 && idx < PickaxeDef.Count)
                {
                    float candidate = PickaxeDef.Get(idx).MineTime + OriginalColliderPulseSec;
                    if (candidate > 0f) t = candidate;
                }
            }







            float reduceTotal = 0f;
            if (SkillTreeManager.Instance != null)
            {
                int lv = SkillTreeManager.Instance.GetLevel(UpgradeId.ImprovedPickaxe);
                if (lv > 0) reduceTotal += lv * 0.04f;
            }
            if (PotionBuffMechanics.Instance != null)
            {
                reduceTotal += PotionBuffMechanics.Instance.PotionPickaxeStats;
            }
            if (ArtifactsManager.Instance != null)
            {
                if (ArtifactsManager.Instance.HasArtifact(ArtifactId.Bone))
                    reduceTotal += 0.04f;
                if (ArtifactsManager.Instance.HasArtifact(ArtifactId.Rune))
                    reduceTotal += 0.001f;
            }
            if (reduceTotal > 0f) t /= (1f + reduceTotal);



            if (MiningSessionController.Instance != null && MiningSessionController.Instance.IsEnergiBoosted)
                t *= 0.5f;
            return t;
        }




        void ScanAndHit()
        {
            var session = MiningSessionController.Instance;
            if (session == null || !session.IsInSession || session.ResultsShown || !session.TimerRunning) return;
            var grid = session.TileGrid;
            if (grid == null || grid.RowContainers == null) return;
            if (hostCanvas == null) return;
            var spawner = RockSpawner.Instance;
            if (spawner == null) return;
            var canvasRT = (RectTransform)hostCanvas.transform;


            Vector2 cursorLocal = cursorRoot.anchoredPosition;
            float radius = CurrentAreaRadiusPx;
            float radiusSqr = radius * radius;

            _hitBuffer.Clear();
            var rows = grid.RowContainers;
            for (int r = 0; r < rows.Length; r++)
            {
                var rocks = spawner.RocksInRow(rows[r]);
                for (int c = 0; c < rocks.Count; c++)
                {
                    var rock = rocks[c];
                    if (rock == null || rock.IsDying) continue;



                    Vector2 rockScreen = RectTransformUtility.WorldToScreenPoint(_camera, rock.transform.position);
                    if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        canvasRT, rockScreen, _camera, out Vector2 rockLocal))
                        continue;

                    if ((rockLocal - cursorLocal).sqrMagnitude <= radiusSqr)
                        _hitBuffer.Add(rock);
                }
            }



            for (int i = 0; i < _hitBuffer.Count; i++)
            {
                var rk = _hitBuffer[i];
                if (rk != null && !rk.IsDying) rk.ApplyExternalHit();
            }





            if (_hitBuffer.Count > 1)
                Rebuild.Stats.StatsManager.Instance?.RecordInflateBurst();





            var sk = SkillTreeManager.Instance;
            if (sk != null)
            {
                int level = sk.GetLevel(UpgradeId.RandomRockMineChance);
                if (level > 0 && UnityEngine.Random.value < level * 0.03f)
                {
                    var outsider = PickRandomOutsider(rows, canvasRT, cursorLocal, radiusSqr);
                    if (outsider != null) outsider.ApplyExternalHit();
                }
            }
        }




        public int ShootCirclePulse()
        {
            var session = MiningSessionController.Instance;
            if (session == null || !session.IsInSession || session.ResultsShown || !session.TimerRunning) return 0;
            var grid = session.TileGrid;
            if (grid == null || grid.RowContainers == null) return 0;
            if (hostCanvas == null || cursorRoot == null) return 0;
            var spawner = RockSpawner.Instance;
            if (spawner == null) return 0;

            Vector2 dir = UnityEngine.Random.insideUnitCircle;
            if (dir.sqrMagnitude < 0.0001f) dir = Vector2.right;
            else dir.Normalize();

            float cursorRadius = CurrentAreaRadiusPx;
            float pulseRadius = Mathf.Max(32f, cursorRadius * ShootCircleRadiusMultiplier);
            Vector2 centerLocal = cursorRoot.anchoredPosition
                + dir * Mathf.Max(pulseRadius, cursorRadius * ShootCircleTravelMultiplier);
            float radiusSqr = pulseRadius * pulseRadius;

            _hitBuffer.Clear();
            var rows = grid.RowContainers;
            var canvasRT = (RectTransform)hostCanvas.transform;
            for (int r = 0; r < rows.Length; r++)
            {
                var rocks = spawner.RocksInRow(rows[r]);
                for (int c = 0; c < rocks.Count; c++)
                {
                    var rock = rocks[c];
                    if (rock == null || rock.IsDying) continue;

                    Vector2 rockScreen = RectTransformUtility.WorldToScreenPoint(_camera, rock.transform.position);
                    if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        canvasRT, rockScreen, _camera, out Vector2 rockLocal))
                        continue;

                    if ((rockLocal - centerLocal).sqrMagnitude <= radiusSqr)
                        _hitBuffer.Add(rock);
                }
            }

            int hits = _hitBuffer.Count;
            for (int i = 0; i < _hitBuffer.Count; i++)
            {
                var rk = _hitBuffer[i];
                if (rk != null && !rk.IsDying) rk.ApplyExternalHit();
            }
            if (hits > 1)
                Rebuild.Stats.StatsManager.Instance?.RecordInflateBurst();
            return hits;
        }

        Rock PickRandomOutsider(RectTransform[] rows, RectTransform canvasRT,
                                 Vector2 cursorLocal, float radiusSqr)
        {

            var spawner = RockSpawner.Instance;
            if (spawner == null) return null;
            Rock pick = null;
            int seen = 0;
            for (int r = 0; r < rows.Length; r++)
            {
                var rocks = spawner.RocksInRow(rows[r]);
                for (int c = 0; c < rocks.Count; c++)
                {
                    var rock = rocks[c];
                    if (rock == null || rock.IsDying) continue;
                    Vector2 rockScreen = RectTransformUtility.WorldToScreenPoint(_camera, rock.transform.position);
                    if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        canvasRT, rockScreen, _camera, out Vector2 rockLocal)) continue;
                    if ((rockLocal - cursorLocal).sqrMagnitude <= radiusSqr) continue;
                    seen++;
                    if (UnityEngine.Random.Range(0, seen) == 0) pick = rock;
                }
            }
            return pick;
        }






        void HandlePickaxeChanged(int _) {  }

        void UpdateAreaSize()
        {
            if (areaCircle == null) return;



            float diameter = CurrentAreaRadiusPx * 2f;
            areaCircle.rectTransform.sizeDelta = new Vector2(diameter, diameter);
        }










        public void PlaySwingAt(Transform parent, Vector2 anchoredPos)
        {
            if (!isActiveAndEnabled) return;
            if (parent == null) return;
            int idx = AnvilManager.Instance != null ? AnvilManager.Instance.CurrentIdx : 0;
            var sprite = SpriteLibrary.GetPickaxeSprite(idx);



            bool hasCustom = sprite != null && sprite != RockSpriteFactory.GetRockSprite();
            Color tint;
            if (hasCustom)
            {
                tint = Color.white;
            }
            else
            {
                float t = idx / Mathf.Max(1f, (float)(PickaxeDef.Count - 1));
                tint = Color.Lerp(new Color(0.55f, 0.50f, 0.45f, 1f),
                                    new Color(1.00f, 0.84f, 0.18f, 1f), t);
            }
            PickaxeSwingFx.Spawn(parent, anchoredPos, sprite, tint, pickaxeSize);
        }
    }
}
