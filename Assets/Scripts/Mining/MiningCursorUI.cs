using System;
using UnityEngine;
using UnityEngine.UI;

namespace Rebuild.Mining
{
    /// <summary>(2026-05-14) — mining session 의 시각 cursor.
    /// 원본 게임처럼:
    ///   • 마우스 위치에 흰색 dotted area indicator ring (RingSpriteFactory)
    ///   • 곡괭이 sprite 는 따로 — Rock 위에 PickaxeSwingFx 가 spawn → swing → fade
    ///   • 시스템 cursor 는 그대로 표시 (마우스 = 손/cursor, 곡괭이는 별도 spawn)
    /// Canvas_UI_MiningScene 안에 살므로 mining session 동안만 활성. 세션 종료 시 같이 비활성.
    /// AnvilManager.OnPickaxeChanged 구독 → 장착 변경 시 swing fx 의 sprite 도 자동 갱신.
    /// <para>v2 — pickaxeIcon SerializedField 는 더 이상 마우스 위치에 표시되지
    /// 않음 (SetActive false). PlaySwingAt(parent, pos) 가 호출되면 그 위치에 PickaxeSwingFx
    /// 를 spawn 해서 곡괭이 visual 을 보여줌. 사용자 보고: "원본은 커서 따로있고 캐지는
    /// 광물에 곡괭이 애니메이션".</para>
    /// </summary>
    public sealed class MiningCursorUI : MonoBehaviour
    {
        public static MiningCursorUI Instance { get; private set; }

        [SerializeField] Canvas hostCanvas;          // Canvas_UI_MiningScene
        [SerializeField] RectTransform cursorRoot;   // anchored to mouse each frame
        [SerializeField] Image areaCircle;           // 흰색 반투명 area indicator
        [SerializeField] Image pickaxeIcon;          // 곡괭이 sprite (swing rotates this)

        [Header("Tuning")]
        // 110 → 70 (사용자 보고: Pickaxe lv1 area 너무 큼). Pickaxe 1 의
        // MiningAreaSize=1.21 → 1.21 × 70 = 85px 반지름. 14 단계 max 1.80 → 126px.
        [SerializeField] float baseAreaRadius = 70f;  // px at MiningAreaSize=1.0
        [SerializeField] float pickaxeSize = 110f;
        [SerializeField] float swingDuration = 0.18f;
        [SerializeField] float swingPeakAngle = -50f;  // back-swing peak (degrees, CCW)
        [SerializeField] bool hideSystemCursor = false;

        Camera _camera;
        bool _subscribed;
        // area-trigger cadence. Mirrors original HandMechanics.Mine() coroutine:
        // wait mineTime + the 0.04s collider pulse, then collapse that pulse into one ScanAndHit.
        float _mineTickAccum;
        const float OriginalColliderPulseSec = 0.04f;
        const float ShootCircleRadiusMultiplier = 0.55f;
        const float ShootCircleTravelMultiplier = 2.0f;
        readonly System.Collections.Generic.List<Rock> _hitBuffer = new System.Collections.Generic.List<Rock>(32);
        bool _miningInputActive;

        /// <summary>Current ring radius in canvas-pixel coords. 원본
        /// <c>TheAnvil.SetHandColliderSize</c> 의 multiplier 체인 매칭:
        /// <c>MiningAreaSize × improvedPickaxeStrength × shapeShifterSizeIncrease × SkillTree.miningAreaSize</c>.
        /// Shell 은 architecture 차이 (Collider2D localScale → ScanAndHit radius) 가 있지만
        /// multiplier 곱셈 의미는 동등.
        /// <list type="bullet">
        /// <item><b>MiningAreaSize</b> — equipped pickaxe 의 base area (1.21 ~ 1.80).</item>
        /// <item><b>ImprovedPickaxe SkillTree</b> — lv × 0.04 multiplier (원본 누적식 +0.031~+0.063
        /// 평균치 매칭). damage 와 area 에 동일하게 적용.</item>
        /// <item><b>ShapeShifter talent</b> — ×1.06 multiplier (원본 shapeShifterSizeIncrease=0.06 매칭).
        /// 이전엔 Rock.transform.localScale 만 키웠음.</item>
        /// <item><b>IncreaseAndDecreaseMinignErea SkillTree</b> — ×1.25 single-level toggle.</item>
        /// </list>
        /// Rock.ApplySiblingSplashes / ScanAndHit / UpdateAreaSize 모두 이 값 동적 호출 →
        /// 곡괭이 변경 + skill / talent 구매 시 즉시 반영.</summary>
        public float CurrentAreaRadiusPx
        {
            get
            {
                float area = AnvilManager.Instance != null ? AnvilManager.Instance.CurrentMiningAreaSize : 1f;
                // 원본 매칭. ImprovedPickaxe 가 damage + area 둘 다 강화.
                if (SkillTreeManager.Instance != null)
                {
                    // 원본 cumulative steps (Rock.GetImprovedPickaxeBonus).
                    int lv = SkillTreeManager.Instance.GetLevel(UpgradeId.ImprovedPickaxe);
                    if (lv > 0) area *= 1f + Rock.GetImprovedPickaxeBonus(lv);
                    // BiggerMiningArea 적용 site (이전엔 dead node).
                    // 원본 LocalizationScript.miningAreaIncrease=0.02f × 4 lv 매칭 → max ×1.08.
                    int biggerLv = SkillTreeManager.Instance.GetLevel(UpgradeId.BiggerMiningArea);
                    if (biggerLv > 0) area *= 1f + biggerLv * 0.02f;
                }
                // ShapeShifter talent area boost (원본 shapeShifterSizeIncrease).
                if (LevelMechanics.Instance != null && LevelMechanics.Instance.HasTalent(TalentId.ShapeShifter))
                {
                    area *= 1.06f;
                }
                // 원본 currentMiningErea = miningErea × (improvedPickaxeStrength +
                // potionPickaxeStats_increase + shapeShifterSizeIncrease). PotionPickaxeStats consumer 추가.
                // PotionDrinker / PotionChugger pick 시 0.05 누적 → cursor area 약 5% 확대.
                if (PotionBuffMechanics.Instance != null && PotionBuffMechanics.Instance.PotionPickaxeStats > 0f)
                {
                    area *= 1f + PotionBuffMechanics.Instance.PotionPickaxeStats;
                }
                // BigMiningArea talent 원본 스케일 복원.
                // 원본 inflationBurstIncrease=0.4 → ×1.4 burst multiplier. 원본은 7% per-sec polling 으로 3초
                // 동안 active 였지만 Shell 의 area-radius 모델에는 상시 multiplier 가 architecture 친화적.
                // RockSpawner 의 +1/row 변경도 같이 제거됨 (사용자 결정 "원본 스케일 일치").
                // 이전 Shell ×1.14 averaging 단순화 (.A) → 원본 ×1.4 floor 로 끌어올림.
                if (LevelMechanics.Instance != null && LevelMechanics.Instance.HasTalent(TalentId.BigMiningArea))
                {
                    area *= 1.4f;
                }
                // BiggerSplash talent (원래 dead — 적용 site 0). 원본은 splash 시스템
                // 자체 없음 → Shell 에선 area axis 에 추가 ×1.15 multiplier 로 "넓은 충격" 의미 유지.
                // BigMiningArea (×1.4) 보다 작은 multiplier — 둘 다 같은 axis 에 multiplicative 누적
                // (둘 모두 보유 시 ×1.61).
                if (LevelMechanics.Instance != null && LevelMechanics.Instance.HasTalent(TalentId.BiggerSplash))
                {
                    area *= 1.15f;
                }
                // IncreaseAndDecreaseMinignErea. 원본 LocalizationScript.cs:2118 매칭:
                // "채굴 범위가 커졌다 작아졌다 합니다: 110%-95%". 시간 기반 sinusoidal oscillation
                // (period ~2초). 이전 Shell ×1.25 flat 단순화 되돌리기.
                if (SkillTreeManager.Instance != null
                    && SkillTreeManager.Instance.GetLevel(UpgradeId.IncreaseAndDecreaseMinignErea) > 0)
                {
                    // sin(2π · t / 2.0) → -1..+1 → 0.95..1.10 mapped via lerp(0.5+0.5*sin).
                    float k = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * Mathf.PI); // 2초 주기
                    area *= Mathf.Lerp(0.95f, 1.10f, k);
                }
                return baseAreaRadius * area;
            }
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

            // re-bind the procedural ring sprite every enable.
            // RingSpriteFactory creates the texture/sprite with HideFlags.HideAndDontSave so it
            // is NOT serialized into the scene asset. After a domain reload (or a fresh Play
            // mode entry on a saved scene), the previously-bound reference is gone and Unity's
            // Image falls back to a default white quad — the user sees a square, not a ring.
            // Calling RingSpriteFactory.GetRing() re-creates (or returns the cached) sprite and
            // re-assigns it so the visual is always the proper ring.
            EnsureRingSprite();
            // v2 — pickaxe sprite no longer follows the mouse. Hide the cursor's
            // own pickaxe Image so only the dotted ring sits at the mouse; PickaxeSwingFx spawns
            // on the rock being struck for the swing visual instead.
            if (pickaxeIcon != null) pickaxeIcon.gameObject.SetActive(false);
            UpdateAreaSize();
        }

        void EnsureRingSprite()
        {
            if (areaCircle == null) return;
            // Always re-assign — even if a stale reference survived, GetRing() returns the
            // canonical cached sprite so Image.sprite settles on the right asset.
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

            // Mouse → canvas-local coord (ScreenSpaceCamera mode requires the canvas's worldCamera).
            var canvasRT = (RectTransform)hostCanvas.transform;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRT, Input.mousePosition, _camera, out Vector2 local))
            {
                cursorRoot.anchoredPosition = local;
            }

            // Area circle stays in sync with the equipped pickaxe's MiningAreaSize stat — cheap to
            // re-evaluate per frame and lets equip changes reflect immediately even if the event
            // hook fires before the stat refresh.
            UpdateAreaSize();

            // area-trigger cadence. Mirrors original HandMechanics.Mine() coroutine
            // wait(currentMineTime) + collider toggle. Accumulate unscaledDeltaTime, then while
            // the bucket holds at least one full mineTime, fire ScanAndHit and subtract.
            float interval = CurrentMineTime();
            if (interval <= 0f) return;
            _mineTickAccum += Time.unscaledDeltaTime;
            // Cap to one ScanAndHit per frame in pathological cases (massive frame hitch) — the
            // player wouldn't see 5 simultaneous hits anyway.
            int safety = 4;
            while (_mineTickAccum >= interval && safety-- > 0)
            {
                _mineTickAccum -= interval;
                ScanAndHit();
                interval = CurrentMineTime(); // re-eval in case AnvilManager equip changed
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

        /// <summary>equipped pickaxe drives the cadence. EnergiDrinker talent
        /// halves it via the session boost flag. Mirrors original
        /// <c>HandMechanics.Mine</c>'s <c>WaitForSeconds(currentMineTime)</c>.
        /// <para>.A (2026-05-16) — 원본 <c>TheAnvil.DisplayEquippedAndSetStats</c> 의
        /// <c>mineTime / (1 + reducePickaxeMineTime + potionPickaxeStats_increase)</c> 매칭. Shell
        /// 에는 별도 <c>reducePickaxeMineTime</c> UpgradeId 가 없으므로 ImprovedPickaxe lv 를 분모로
        /// 재사용 (원본 ImprovedPickaxe lv1~6 시 reducePickaxeMineTime + 0.031~+0.063 누적, 평균 0.04
        /// 매칭). Area 와 동일 lv * 0.04 식이라 ImprovedPickaxe 가 "damage + area + cadence" 3 in 1
        /// 의미. 원본 lv6 maxed = +0.267 누적 ≈ ×1.27, Shell lv4 maxed = +0.16 ≈ ×1.16 (Shell MaxLevel
        /// 차이는 의도적 — UpgradeDef.Count 가 4 lv).</para></summary>
        float CurrentMineTime()
        {
            float t = 0.45f; // fallback when AnvilManager is missing
            if (AnvilManager.Instance != null)
            {
                int idx = AnvilManager.Instance.CurrentIdx;
                if (idx >= 0 && idx < PickaxeDef.Count)
                {
                    float candidate = PickaxeDef.Get(idx).MineTime + OriginalColliderPulseSec;
                    if (candidate > 0f) t = candidate;
                }
            }
            // 원본 매칭. ImprovedPickaxe lv (reducePickaxeMineTime 누적) +
            // PotionPickaxeStats 가 모두 분모에 합산. 작은 누적도 cadence 가 빨라지므로 의미 있음.
            // Bone artifact 의 secondary effect 복원. 원본 RockMechanics.cs:843-844:
            //   `bonePickaxeIncrease += 0.04` (damage) **AND** `reducePickaxeMineTime -= 0.04` (mineTime).
            // Shell 은 damage 측만 적용 중 → mineTime -4% 도 reduceTotal 에 합산하여 click cadence 도
            // 4% 빨라지도록 매칭. Rune 의 secondary `reducePickaxeMineTime -= 0.02 × runeImproveAll = -0.001`
            // (RockMechanics.cs:896) 도 같이 적용.
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
            // EnergiDrinker talent — boost session multiplies mineTime by 0.5.
            // 원본 매칭 (LevelMechanics.cs:1483~1497 의 ÷2). 이전 Shell
            // ×0.7 의도된 nerf () 를 풀고 원본 ×0.5 (mineTime ÷ 2) 복원.
            if (MiningSessionController.Instance != null && MiningSessionController.Instance.IsEnergiBoosted)
                t *= 0.5f;
            return t;
        }

        /// <summary>enumerate live rocks under the cursor's area ring, fire
        /// ApplyExternalHit on each. Also handles SkillTree RandomRockMineChance (one extra
        /// off-ring rock per tick at probability lv × 3%).</summary>
        void ScanAndHit()
        {
            var session = MiningSessionController.Instance;
            if (session == null || !session.IsInSession || session.ResultsShown || !session.TimerRunning) return;
            var grid = session.TileGrid;
            if (grid == null || grid.RowContainers == null) return;
            if (hostCanvas == null) return;
            var canvasRT = (RectTransform)hostCanvas.transform;

            // Cursor anchored pos (canvas-local) — already updated above this frame.
            Vector2 cursorLocal = cursorRoot.anchoredPosition;
            float radius = CurrentAreaRadiusPx;
            float radiusSqr = radius * radius;

            _hitBuffer.Clear();
            var rows = grid.RowContainers;
            for (int r = 0; r < rows.Length; r++)
            {
                var row = rows[r];
                if (row == null) continue;
                int childCount = row.childCount;
                for (int c = 0; c < childCount; c++)
                {
                    var child = row.GetChild(c);
                    var rock = child.GetComponent<Rock>();
                    if (rock == null || rock.IsDying) continue;

                    // Convert rock world pos → canvas-local through screen space (handles the
                    // nested TileRow scale + ScreenSpaceCamera projection correctly).
                    Vector2 rockScreen = RectTransformUtility.WorldToScreenPoint(_camera, rock.transform.position);
                    if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        canvasRT, rockScreen, _camera, out Vector2 rockLocal))
                        continue;

                    if ((rockLocal - cursorLocal).sqrMagnitude <= radiusSqr)
                        _hitBuffer.Add(rock);
                }
            }

            // Apply hits — each rock self-applies SFX + click stat + InstaMine/Anvil/Artifact
            // rolls. PickaxeSwingFx spawns inside ApplyExternalHit.
            for (int i = 0; i < _hitBuffer.Count; i++)
            {
                var rk = _hitBuffer[i];
                if (rk != null && !rk.IsDying) rk.ApplyExternalHit();
            }

            // InflateBurst stat. The cursor area-trigger model ()
            // replaces the original BigMiningArea per-second area-doubling roll, so we instead
            // count every tick where 2+ rocks fall inside the cursor ring (= "multi-rock burst").
            // Granular but bounded — fires at most once per mineTime tick.
            if (_hitBuffer.Count > 1)
                Rebuild.Stats.StatsManager.Instance?.RecordInflateBurst();

            // SkillTree RandomRockMineChance: roll once per tick. On success, pick one live rock
            // NOT in the area ring and apply a single hit. Mirrors original
            // RockMechanics.chanceToMineRandomRock path (kept rock-shape so artifact / talent
            // rolls inside ApplyExternalHit still fire identically).
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

        /// <summary>Original ShootCircle: once per second roll, launch a small circle from the
        /// cursor in a random direction. This applies normal pickaxe-hit logic only to rocks under
        /// that small circle, instead of damaging every live rock on the board.</summary>
        public int ShootCirclePulse()
        {
            var session = MiningSessionController.Instance;
            if (session == null || !session.IsInSession || session.ResultsShown || !session.TimerRunning) return 0;
            var grid = session.TileGrid;
            if (grid == null || grid.RowContainers == null) return 0;
            if (hostCanvas == null || cursorRoot == null) return 0;

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
                var row = rows[r];
                if (row == null) continue;
                int childCount = row.childCount;
                for (int c = 0; c < childCount; c++)
                {
                    var child = row.GetChild(c);
                    var rock = child.GetComponent<Rock>();
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
            // Reservoir-pick a single live rock outside the ring so we don't allocate a full list.
            Rock pick = null;
            int seen = 0;
            for (int r = 0; r < rows.Length; r++)
            {
                var row = rows[r];
                if (row == null) continue;
                int childCount = row.childCount;
                for (int c = 0; c < childCount; c++)
                {
                    var child = row.GetChild(c);
                    var rock = child.GetComponent<Rock>();
                    if (rock == null || rock.IsDying) continue;
                    Vector2 rockScreen = RectTransformUtility.WorldToScreenPoint(_camera, rock.transform.position);
                    if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        canvasRT, rockScreen, _camera, out Vector2 rockLocal)) continue;
                    if ((rockLocal - cursorLocal).sqrMagnitude <= radiusSqr) continue; // inside
                    seen++;
                    if (UnityEngine.Random.Range(0, seen) == 0) pick = rock;
                }
            }
            return pick;
        }

        // ── Sprite + size helpers ────────────────────────────────────────

        // v2 — equipped-pickaxe sprite is now consumed by PickaxeSwingFx.Spawn
        // at the rock's location, not painted on the cursor itself. We still listen for the
        // pickaxe change event in case future polish wants to update an off-screen preview.
        void HandlePickaxeChanged(int _) { /* future-proof hook — no-op now */ }

        void UpdateAreaSize()
        {
            if (areaCircle == null) return;
            // 시각 ring 도 CurrentAreaRadiusPx 한 곳에서 계산 (이전엔 base ×
            // MiningAreaSize 만 사용 → ImprovedPickaxe/ShapeShifter 적용 안 됨 → 시각과 hit
            // detection 불일치). 동일 multiplier 체인 거쳐서 일관성 보장.
            float diameter = CurrentAreaRadiusPx * 2f;
            areaCircle.rectTransform.sizeDelta = new Vector2(diameter, diameter);
        }

        // ── Swing animation ───────────────────────────────────────────────

        /// <summary>v2 — spawn a one-shot pickaxe swing FX at the rock's location.
        /// Called from <see cref="Rock.ApplyOneTick"/> for every successful damage tick (and from
        /// each splash sibling) so the player sees the pickaxe striking the rock being broken,
        /// matching the original game's visual.
        /// <para><paramref name="parent"/> is the rock's transform.parent (its TileRow) and
        /// <paramref name="anchoredPos"/> is the rock's anchoredPosition — that way the FX shows
        /// up exactly on top of the rock without any canvas/world conversion.</para></summary>
        public void PlaySwingAt(Transform parent, Vector2 anchoredPos)
        {
            if (!isActiveAndEnabled) return;
            if (parent == null) return;
            int idx = AnvilManager.Instance != null ? AnvilManager.Instance.CurrentIdx : 0;
            var sprite = SpriteLibrary.GetPickaxeSprite(idx);
            // Tint follows the same fallback rule used by the carousel/sidecar — full white when
            // a user PNG exists, gray→gold gradient on the rock-silhouette placeholder so the
            // pickaxe still reads at a glance.
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
