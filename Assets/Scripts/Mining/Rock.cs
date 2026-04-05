using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Rebuild.Mining
{
    /// <summary>
    /// area-trigger rock. Mirrors original <c>RockMechanics.PickaxeHit</c>: the
    /// rock is a passive damage target driven externally by the cursor. <see cref="MiningCursorUI"/>
    /// runs a mineTime-cadence Update loop and calls <see cref="ApplyExternalHit"/> on every Rock
    /// whose anchored position falls inside the area-radius ring around the mouse.
    /// <para>The cursor-centric model matches what the player sees: every rock under the
    /// ring takes damage at the equipped pickaxe's cadence.</para>
    /// </summary>
    [RequireComponent(typeof(Image))]
    public sealed class Rock : MonoBehaviour
    {
        [SerializeField] int maxHp = 10;
        [SerializeField] float punchDuration = 0.12f;
        [SerializeField] float punchScale = 0.82f;
        [SerializeField] float flashDuration = 0.08f;
        [SerializeField] float fadeDuration = 0.25f;

        Image _img;
        Color _baseColor;
        Vector3 _baseScale;
        float _hp;
        bool _isDying;
        Coroutine _feedbackRoutine;
        // 50% HP 도달 시 sprite swap. 원본 RockMechanics.DamageRock 의
        // `currentBrokenRock.SetActive(true)` 매칭. 1회만 swap (재진입 가드).
        bool _brokenSwapped;
        float _halfHpThreshold;

        public OreType Ore { get; set; }
        public RockTier Tier { get; set; }
        public int Hp => Mathf.CeilToInt(_hp);
        public bool IsDying => _isDying;

        // chest rock variant (chests_chosen / goldenChests_chosen talent 발동).
        // RockSpawner.SpawnOne 가 spawn 시 결정. 깰 때 강력한 bonus drop + (옵션) 시각 chest sprite.
        public enum ChestKind { None = 0, Chest = 1, Golden = 2 }
        public ChestKind Chest { get; set; }

        // pre-spawn artifact. RockSpawner.SpawnOne 직후 ArtifactsManager.TryRollOnRockSpawn
        // 으로 결정. 원본 Artifacts.ArtifactDropChances 의 spawn-time roll 매칭. break 시 ConfirmPreRolledArtifact
        // 으로 발견 처리. null 이면 일반 break + break-time TryRoll fallback (모델 유지 — pre-roll
        // 안 박힌 rock 도 매 break 마다 한 번씩 TryRoll). pre-roll 모델은 "이 rock 에 박힘" UX 강화.
        public ArtifactId? AssignedArtifact { get; private set; }
        // overlay child. AssignArtifact 시점에 만들고 처음에는 SetActive(false), 50% HP
        // 도달 시 SetActive(true) — 원본의 "rock scale 0.15 도달 시 artifact_icon SetActive(true)" 와 동등.
        GameObject _artifactOverlayGo;
        Image _artifactOverlayImg;
        bool _artifactOverlayRevealed;

        void Awake()
        {
            _img = GetComponent<Image>();
            _baseColor = _img.color;
            _baseScale = transform.localScale;
            _hp = maxHp;
        }

        /// <summary>.A — 원본 LocalizationScript.materialsWorthIncrease 1+2+3+9+12+4+6+18 = +56
        /// 누적 매칭. SkillTree.cs:4290 의 `materialsTotalWorth += materialsWorthIncrease` 누적 형식.
        /// lv 0 = 0 (base 1f), lv 1 = +1 (×2), lv 2 = +3 (×4), lv 8 = +56 (×57).</summary>
        static readonly float[] s_materialsWorthSteps = { 1f, 2f, 3f, 9f, 12f, 4f, 6f, 18f };
        public static float GetMaterialsWorthMoreBonus(int level)
        {
            if (level <= 0) return 0f;
            float total = 0f;
            int max = Mathf.Min(level, s_materialsWorthSteps.Length);
            for (int i = 0; i < max; i++) total += s_materialsWorthSteps[i];
            return total;
        }

        /// <summary>원본 LocalizationScript.improvedPickaxeIncrease 누적 0.031/0.034/0.04/0.045/0.054/0.063
        /// = +0.267 max 매칭. SkillTree.cs 의 `improvedPickaxeStrength += improvedPickaxeIncrease` 누적 형식.
        /// 6 lv max. Cursor area (MiningCursorUI) 도 이 multiplier 를 사용 (원본 currentMiningErea ×
        /// improvedPickaxeStrength).</summary>
        static readonly float[] s_improvedPickaxeSteps = { 0.031f, 0.034f, 0.04f, 0.045f, 0.054f, 0.063f };
        public static float GetImprovedPickaxeBonus(int level)
        {
            if (level <= 0) return 0f;
            float total = 0f;
            int max = Mathf.Min(level, s_improvedPickaxeSteps.Length);
            for (int i = 0; i < max; i++) total += s_improvedPickaxeSteps[i];
            return total;
        }

        public void Initialize(OreType ore, int hp) => Initialize(ore, hp, RockTier.Normal, ChestKind.None);

        public void Initialize(OreType ore, int hp, RockTier tier) => Initialize(ore, hp, tier, ChestKind.None);

        public void Initialize(OreType ore, int hp, RockTier tier, ChestKind chest)
        {
            Ore = ore;
            Tier = tier;
            Chest = chest;
            maxHp = hp;
            _hp = hp;
            // broken swap threshold (50% HP). 원본 halfRockHp = rockHp / 2 매칭.
            _halfHpThreshold = Mathf.Max(1f, hp * 0.5f);
            _brokenSwapped = false;
            // pre-spawn artifact 기본값 (RockSpawner.SpawnOne 직후 AssignArtifact 호출 가능).
            AssignedArtifact = null;
            _artifactOverlayRevealed = false;
            // rock no longer consumes pointer events; the cursor scans area
            // membership instead. Disabling raycastTarget skips the UGUI raycast cost entirely
            // and prevents stray hover tooltips from triggering on top of rocks.
            if (_img != null) _img.raycastTarget = false;
            // ShapeShifter talent grows every spawned rock by 6%. Apply to the
            // cached _baseScale so HitFeedback's punched/settled animations still snap to the
            // new resting size.
            if (LevelMechanics.Instance != null && LevelMechanics.Instance.HasTalent(TalentId.ShapeShifter))
            {
                _baseScale *= 1.06f;
                transform.localScale = _baseScale;
            }
        }

        /// <summary>.E — pre-spawn artifact 부착. RockSpawner.SpawnOne 가
        /// ArtifactsManager.TryRollOnRockSpawn() 의 결과로 호출. 원본 Artifacts.ArtifactDropChances 의
        /// spawn-time roll + AllArtifacts reparent + scale-reveal 모델 매칭. Chest variant 면 skip
        /// (시각 충돌 방지 — 원본도 chest + artifact 동시 등장 안 함).
        /// <para>구현 — child overlay (옵션 B) — 사용자 PNG 작업 의존도 낮음. SpriteLibrary.GetArtifactRockSprite
        /// 가 옵션 A (rock-shaped artifact PNG) 우선 시도, 없으면 GetArtifactSprite (작은 icon overlay) 로 fallback.
        /// 처음에는 SetActive(false), 50% HP 도달 시 RevealArtifactOverlayIfReady 가 SetActive(true).</para></summary>
        public void AssignArtifact(ArtifactId id)
        {
            if (Chest != ChestKind.None) return; // chest variant 면 skip.
            AssignedArtifact = id;
            EnsureArtifactOverlay(id);
        }

        void EnsureArtifactOverlay(ArtifactId id)
        {
            if (_artifactOverlayGo != null) return;
            // 옵션 A 우선 시도 — Sprites/Rock/Artifact/{id}.png (사용자 후속 PNG 작업 시 자동 swap).
            // 옵션 B fallback — Sprites/Artifacts/{id}.png 의 기존 14 sprite 재사용 (작은 child icon overlay).
            var spriteA = SpriteLibrary.GetArtifactRockSprite(id);
            var spriteB = SpriteLibrary.GetArtifactSprite(id);
            var fallbackBase = RockSpriteFactory.GetRockSprite();
            var info = ArtifactDef.Get(id);
            Sprite chosen;
            bool tint;
            float scale;
            if (spriteA != null)
            {
                // 옵션 A — rock 모양 + artifact 박힘. rock 크기 가득.
                chosen = spriteA; tint = false; scale = 1.0f;
            }
            else if (spriteB != null && spriteB != fallbackBase)
            {
                // 옵션 B — rock 안 작은 artifact icon overlay.
                chosen = spriteB; tint = false; scale = 0.45f;
            }
            else
            {
                // PNG 없음 → base rock sprite + tint (시각 hint 만).
                chosen = fallbackBase; tint = true; scale = 0.45f;
            }

            _artifactOverlayGo = new GameObject($"ArtifactOverlay_{id}",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            _artifactOverlayGo.transform.SetParent(transform, false);
            var rt = (RectTransform)_artifactOverlayGo.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            var rockRt = (RectTransform)transform;
            rt.sizeDelta = rockRt.sizeDelta * scale;
            rt.localScale = Vector3.one;

            _artifactOverlayImg = _artifactOverlayGo.GetComponent<Image>();
            _artifactOverlayImg.sprite = chosen;
            _artifactOverlayImg.color = tint ? info.Tint : Color.white;
            _artifactOverlayImg.preserveAspect = true;
            _artifactOverlayImg.raycastTarget = false;
            // 처음에는 숨김 — 50% HP 도달 시 SetActive(true) (원본 scale 0.15 reveal 매칭).
            _artifactOverlayGo.SetActive(false);
        }

        void RevealArtifactOverlayIfReady()
        {
            if (_artifactOverlayRevealed) return;
            if (_artifactOverlayGo == null) return;
            _artifactOverlayRevealed = true;
            _artifactOverlayGo.SetActive(true);
        }

        // HammerTime talent — when an insta-mine fired via HammerTime, the death drop is
        // doubled. Set right before TakeDamage triggers FadeAndDestroy; cleared after the drop applies.
        bool _hammerDoubleNextDeath;

        /// <summary>called externally by <see cref="MiningCursorUI"/> on every
        /// mineTime tick for each rock inside the cursor's area. Self-contained: rolls
        /// InstaMine / SkillTree / Anvil / Artifact bonuses, applies damage, fires the swing FX,
        /// and rolls projectile chances. No-op when already dying.</summary>
        public void ApplyExternalHit()
        {
            if (_isDying) return;
            // every external tick: SFX + click stat, just like the original
            // RockMechanics.PickaxeHit fires AudioManager.Play("Pickaxe") and increments
            // AllStats.pickaxeHits once per OnTriggerEnter2D.
            Rebuild.Audio.OverlappingSounds.Instance?.PlayPickaxe();
            Rebuild.Stats.StatsManager.Instance?.RecordClick();
            // per-tick pickaxe hit counter (vs ClicksTotal which counts UI clicks too).
            Rebuild.Stats.StatsManager.Instance?.RecordPickaxeHit();
            var sk = SkillTreeManager.Instance;
            var lm = LevelMechanics.Instance;
            // Axe artifact / InstaMineChance SkillTree / HammerTime talent
            // all roll into one combined insta-mine chance. The talent path also flags the
            // next death so FadeAndDestroy doubles the ore drop (mirrors original itsHammerTime).
            // Axe insta × (1 + runeImproveAll) 매칭.
            // 원본 RockMechanics.cs:754-770 는 별도 roll branch 였지만 Shell 의 단순화 모델 (combined roll)
            // 안에서 Rune multiplier 만 추가하여 효과 누락 해소.
            // Axe 의 2% double-damage branch 추가 (insta fail 후 별도 roll).
            // 원본 RockMechanics.cs:759-763: `if (chance < 2 × (1 + rune)) DamageRock(power × 2)`.
            // Archaeologist multi-site multiplier (archeologistIncrease=0.08).
            // 원본 RockMechanics.cs:757-758: `num7 = 1 + (rune + archeo)`, `num8 = 99 - (rune + archeo)`
            // 즉 axe 의 2x/insta chance 가 (1 + archeo) 만큼 강화 — Rune 과 같은 axis 에서 합산.
            bool hasAxe = ArtifactsManager.Instance != null
                && ArtifactsManager.Instance.HasArtifact(ArtifactId.Axe);
            float axeRuneMult = ArtifactsManager.Instance != null
                ? ArtifactsManager.Instance.RuneMultiplier : 1f;
            float axeArcheoMult = (lm != null && lm.HasTalent(TalentId.Archaeologist)) ? 1.08f : 1f;
            float instaArtifact = hasAxe ? 0.01f * axeRuneMult * axeArcheoMult : 0f;
            float axeDoubleChance = hasAxe ? 0.02f * axeRuneMult * axeArcheoMult : 0f;
            // 원본 instaMineChanceIncrease 1f/1f 매칭 (Shell 이전 1.5%/lv = 50% 강함).
            float instaSkill = sk != null ? sk.GetLevel(UpgradeId.InstaMineChance) * 0.01f : 0f;
            float instaHammer = (lm != null && lm.HasTalent(TalentId.HammerTime)) ? 0.004f : 0f;
            float instaTotal = instaArtifact + instaSkill + instaHammer;
            if (instaTotal > 0f && D100Helper.Roll(instaTotal))
            {
                Rebuild.Stats.StatsManager.Instance?.RecordInstaMine();
                // Apportion the hammer trigger stat — instaHammer fraction of the total chance.
                if (instaHammer > 0f && UnityEngine.Random.value < instaHammer / instaTotal)
                {
                    _hammerDoubleNextDeath = true;
                    Rebuild.Stats.StatsManager.Instance?.RecordHammerTrigger();
                }
                TakeDamage(int.MaxValue / 2);
                MiningCursorUI.Instance?.PlaySwingAt(transform.parent, ((RectTransform)transform).anchoredPosition);
                ProjectileLauncher.TrySpawnFromClick(this);
                return;
            }
            // Axe 의 double-damage branch (insta 와 mutually exclusive in 원본,
            // Shell 은 insta fail 후 별도 roll). dmg 계산 후 axeDouble flag 를 dmg *= 2 에 사용.
            bool axeDouble = false;
            if (axeDoubleChance > 0f && D100Helper.Roll(axeDoubleChance))
            {
                axeDouble = true;
                Rebuild.Stats.StatsManager.Instance?.RecordDoubleDamage();
            }
            // fractional damage. Pickaxe 1 = 2.5, Pickaxe 14 = 8.0.
            // Keep the original minePower as the full base damage, without the Shell-era +1.
            // 원본 currentMinePower = equippedMinePower × (improvedPickaxeStrength
            // + wolfClawBonus + potionPickaxeStats_increase) 매칭. Shell ImprovedPickaxe lv = +0.04 multiplier
            // (mineTime / area 와 동일 식). PotionPickaxeStats 도 합산.
            float dmg = 0f;
            if (AnvilManager.Instance != null)
                dmg += AnvilManager.Instance.CurrentDamageBonus;
            // multiplicative pickaxe-strength chain (원본 improvedPickaxeStrength 1f base).
            float strengthMult = 1f;
            if (sk != null)
            {
                // 원본 LocalizationScript.improvedPickaxeIncrease 누적 0.031/0.034/0.04/0.045/0.054/0.063
                // = +0.267 max (6 lv) 매칭. 이전 Shell `lv * 0.04` flat 단순화 되돌리기.
                int impLv = sk.GetLevel(UpgradeId.ImprovedPickaxe);
                if (impLv > 0) strengthMult += GetImprovedPickaxeBonus(impLv);
            }
            if (PotionBuffMechanics.Instance != null)
                strengthMult += PotionBuffMechanics.Instance.PotionPickaxeStats;
            // WolfClaw bonus — 원본은 별도 항으로 합산 (artifact multiplier 와 같이 strength chain 에 있지만
            // Shell 의 기존 artifact multiplier 블록을 그대로 두고 base strength chain 만 곱셈으로 분리.
            // 원본의 effective minePower = minePower × (improvedPickaxeStrength + wolfClaw + potion) ×
            // (1 + bone + rune) — Shell 은 Bone/WolfClaw/Rune 셋 다 artifact multiplier 로 묶음 (P3 단순화 OK).
            if (strengthMult != 1f) dmg *= strengthMult;
            // Bone/WolfClaw/Rune flat +1 → % multiplier (mirrors original
            // bonePickaxeIncrease 0.04 / wolfClawPickaxePowerIncrease 0.06 / runeImproveAll 0.05).
            // Applied as a single combined multiplier so the 3 stack additively, not multiplicatively.
            // WolfClaw 의 clawChance × (1 + RuneBonus) multiplier chain 적용
            // (원본 TheAnvil.cs:2358 `clawChance × (1 + runeImproveAll + archeologistIncrease)`).
            // Archaeologist 도 WolfClaw 강화에 추가 (원본의 `+ archeologistIncrease` 매칭).
            // Bone 은 damage 효과 그대로, mineTime reduction secondary 는 MiningCursorUI.CurrentMineTime 측에서 처리.
            // 원본 RockMechanics.cs:843-844: bonePickaxeIncrease += 0.04 + reducePickaxeMineTime -= 0.04.
            if (ArtifactsManager.Instance != null)
            {
                float artifactMult = 1f;
                if (ArtifactsManager.Instance.HasArtifact(ArtifactId.Bone)) artifactMult += 0.04f;
                if (ArtifactsManager.Instance.HasArtifact(ArtifactId.WolfClaw))
                {
                    // Rune 과 Archaeologist 모두 WolfClaw 의 contribution 을 강화.
                    float wolfArcheoMult = (lm != null && lm.HasTalent(TalentId.Archaeologist)) ? 1.08f : 1f;
                    artifactMult += 0.06f * ArtifactsManager.Instance.RuneMultiplier * wolfArcheoMult;
                }
                if (ArtifactsManager.Instance.HasArtifact(ArtifactId.Rune)) artifactMult += 0.05f;
                dmg *= artifactMult;
            }
            // Axe artifact 의 2% double-damage branch 결과 적용 (insta 와 mutually exclusive).
            if (axeDouble)
                dmg *= 2f;
            // DoubleDamageChance: 원본 doubleDamageChance 1f/2f (per-lv +1%, maxLv=2 → 2%).
            // UpgradeId 매칭 (Iron 통화, prices {56, 2000000}, maxLevel=2).
            if (sk != null)
            {
                float doubleDmgChance = sk.GetLevel(UpgradeId.DoubleDamageChance) * 0.01f;
                if (doubleDmgChance > 0f && D100Helper.Roll(doubleDmgChance))
                {
                    dmg *= 2f;
                    Rebuild.Stats.StatsManager.Instance?.RecordDoubleDamage();
                }
            }
            // Anvil DoublePowerChance: equipped pickaxe 의 2X Power Chance
            // (1.1%~6.5%) 발동. SkillTree DoubleDamageChance 와 별개 (multiplicative).
            if (AnvilManager.Instance != null)
            {
                float anvilDouble = AnvilManager.Instance.CurrentDoublePowerChance;
                if (anvilDouble > 0f && D100Helper.Roll(anvilDouble))
                {
                    dmg *= 2f;
                    Rebuild.Stats.StatsManager.Instance?.RecordDoubleDamage();
                }
            }
            TakeDamage(dmg);
            // (v2) — spawn pickaxe swing FX on this rock so the
            // visual matches the original game (pickaxe appears on the rock being struck, not
            // on the cursor).
            MiningCursorUI.Instance?.PlaySwingAt(transform.parent, ((RectTransform)transform).anchoredPosition);
            // MidasTouch talent — while a session's "golden run" flag is set, every tick
            // has a 25% chance to drop +1 Gold (independent of the rock's ore type).
            if (lm != null && lm.HasTalent(TalentId.MidasTouch)
                && MiningSessionController.Instance != null && MiningSessionController.Instance.IsMidasActiveThisSession
                && D100Helper.Roll(0.25f)
                && GoldAndOreMechanics.Instance != null)
            {
                GoldAndOreMechanics.Instance.AddOre(OreType.Gold, 1);
            }
            // sibling splashes removed. BiggerMiningArea + Anvil MiningAreaSize +
            // BiggerSplash now flow through the area radius the cursor scans against, so every
            // rock inside the ring receives ApplyExternalHit naturally. RandomRockMineChance is
            // applied at the cursor scan level (one off-ring rock per tick).
            // projectile rolls keyed to each tick. The ProjectileLauncher
            // hard-caps total simultaneous projectiles so a long hover doesn't flood the scene.
            ProjectileLauncher.TrySpawnFromClick(this);
            // ShootCircleChance 이제 per-click 이 아닌 per-second coroutine
            // (MiningSessionController.ShootCircleLoop). 원본 circleShootChance=30% per-sec 매칭.
        }

        public void TakeDamage(float amount)
        {
            if (_isDying) return;
            if (amount <= 0f) return;
            _hp -= amount;
            if (_hp <= 0)
            {
                _isDying = true;
                StartCoroutine(FadeAndDestroy());
            }
            else
            {
                // 50% HP 도달 시 broken sprite 로 swap. 원본 RockMechanics.DamageRock 의
                // `if (rockHp <= halfRockHp) { currentActiveRock.SetActive(false); currentBrokenRock.SetActive(true); }`
                // 매칭. Chest variant 면 swap skip (chest sprite 유지) — 원본도 chest 면 broken 안 보임.
                if (!_brokenSwapped && _hp <= _halfHpThreshold && Chest == ChestKind.None && _img != null)
                {
                    _brokenSwapped = true;
                    var broken = SpriteLibrary.GetBrokenRockVisual(Ore);
                    if (broken.Sprite != null) _img.sprite = broken.Sprite;
                    var newColor = broken.ShouldTint ? broken.TintColor : Color.white;
                    _img.color = newColor;
                    _baseColor = newColor; // HitFeedback 이 settle 시 이 색으로 복귀
                    _img.preserveAspect = !broken.ShouldTint;
                    // 50% HP 도달 시 pre-spawn artifact overlay 노출 (원본 scale 0.15
                    // 도달 시 artifact_icon SetActive(true) 와 동등 — Shell 의 broken swap threshold 와 묶음).
                    RevealArtifactOverlayIfReady();
                }
                if (_feedbackRoutine != null) StopCoroutine(_feedbackRoutine);
                _feedbackRoutine = StartCoroutine(HitFeedback());
            }
        }

        /// <summary>Fire-tag area kill (Camper). Mirrors original
        /// <c>RockMechanics.OnTriggerEnter2D</c> when other.tag=="Fire": 9999 damage =
        /// guaranteed FadeAndDestroy regardless of remaining HP. Skips all damage roll +
        /// HitFeedback so the rock just dies with the usual break ceremony (particle / shake /
        /// popup / ore drop / XP).</summary>
        public void InstaKill()
        {
            if (_isDying) return;
            _hp = 0;
            _isDying = true;
            StartCoroutine(FadeAndDestroy());
        }

        IEnumerator HitFeedback()
        {
            // Brief white-flash tint AND punch-down → settle scale.
            float t = 0f;
            var punched = _baseScale * punchScale;
            // Punch in (fast)
            while (t < punchDuration * 0.4f)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / (punchDuration * 0.4f));
                transform.localScale = Vector3.Lerp(_baseScale, punched, k);
                _img.color = Color.Lerp(_baseColor, Color.white, Mathf.Min(1f, k * 1.5f));
                yield return null;
            }
            // Settle back (slower)
            t = 0f;
            float settleDur = punchDuration * 0.6f;
            while (t < settleDur)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / settleDur);
                transform.localScale = Vector3.Lerp(punched, _baseScale, k);
                float flashFade = Mathf.Clamp01((t - flashDuration) / Mathf.Max(0.0001f, settleDur - flashDuration));
                _img.color = Color.Lerp(Color.white, _baseColor, flashFade);
                yield return null;
            }
            transform.localScale = _baseScale;
            _img.color = _baseColor;
            _feedbackRoutine = null;
        }

        IEnumerator FadeAndDestroy()
        {
            if (_feedbackRoutine != null) StopCoroutine(_feedbackRoutine);
            _img.raycastTarget = false;
            // rock-break overlapping SFX (single trigger on death, pitch-jittered).
            Rebuild.Audio.OverlappingSounds.Instance?.PlayRockBreak();

            // chunk burst at the rock's location. Parent under the TileRow so the
            // particle survives the rock's own fade (rock destroys itself but the row stays).
            var rockAnchored = ((RectTransform)transform).anchoredPosition;
            if (transform.parent != null)
                RockBreakParticleFx.Spawn(transform.parent, rockAnchored, 1f);

            // small screen shake on every rock break (HUD canvas is separate so the
            // timer / ore counters stay rock-steady).
            ScreenShake.Instance?.Shake();

            // rolls — base ore drop scales with MaterialsWorthMore; DoubleXpGoldChance can double both ore + XP.
            // per-ore Chunk + Full nodes add bonus ore drop for matching ore.
            // MoreMaterialsFromRock adds +1/lv to drop (stacks with MaterialsWorthMore).
            // FullOreChance lv*5% to triple the rock's drop.
            // Rock tier 적용 (Normal=×1 / Chunk=×3 / Full=×7).
            // 원본 materialsTotalWorth multiplicative 모델 복원.
            // 원본: oreDrop_final = materialAmount(count) × (materialsTotalWorth + potionMaterialWorthMore_increase).
            // Shell 은 oreDrop count(int) 에 worthMultiplier(float) 를 곱해서 round.
            // worthMultiplier = MaterialsWorthMore lv 누적 (LocalizationScript 1+2+3+9+12+4+6+18 = +56)
            //                 + PotionMaterialWorthMore (session, 1~8).
            // doubleXpAndGoldChance roll 은 count 에 *2, XP 에도 동시 *2 (원본 GiveXP 와 동일 roll).
            int oreCount = RockTierDef.OreDropMultiplier(Tier);
            int xpAmount = LevelMechanics.Instance != null ? LevelMechanics.Instance.XpPerRock : 0;
            float worthMultiplier = 1f; // base (원본 materialsTotalWorth 초기값 1f)
            if (SkillTreeManager.Instance != null)
            {
                // MaterialsWorthMore 가 multiplicative worth (원본 materialsTotalWorth).
                // 누적값은 GetMaterialsWorthMoreBonus(lv) helper 에서 계산 (lv 별 1+2+3+9+12+4+6+18).
                worthMultiplier += GetMaterialsWorthMoreBonus(
                    SkillTreeManager.Instance.GetLevel(UpgradeId.MaterialsWorthMore));
                // MoreMaterialsFromRock 는 count 자체에 flat +lv (원본 materialsFromChunkRocksIncrease 매칭, count level).
                oreCount += SkillTreeManager.Instance.GetLevel(UpgradeId.MoreMaterialsFromRock);
                // {ore}Chunk/{ore}Full SkillTree 의 효과는 이제 Rock.Tier 확률로 표현 (×3/×7).
                // 이전 GetOreDropBonus(+1/lv) flat 보너스는 중복이므로 제거.
                // doubleXpAndGoldChance roll + potionDoubleChance_increase 합산.
                float doubleChance = SkillTreeManager.Instance.GetLevel(UpgradeId.DoubleXpGoldChance) * 0.10f;
                if (PotionBuffMechanics.Instance != null
                    && PotionBuffMechanics.Instance.PotionDoubleChanceIncrease > 0f)
                {
                    doubleChance += PotionBuffMechanics.Instance.PotionDoubleChanceIncrease * 0.01f;
                }
                if (doubleChance > 0f && D100Helper.Roll(doubleChance))
                {
                    oreCount *= 2;
                    xpAmount *= 2;
                    // count distinct DoubleOre procs (vs XpDoublesTriggered which is
                    // PurpleRing only).
                    Rebuild.Stats.StatsManager.Instance?.RecordDoubleOre();
                }
                float fullOreChance = SkillTreeManager.Instance.GetLevel(UpgradeId.FullOreChance) * 0.05f;
                if (fullOreChance > 0f && D100Helper.Roll(fullOreChance))
                {
                    oreCount *= 3;
                    // full-ore proc counts as a "full rock mined" for Gold/Copper.
                    Rebuild.Stats.StatsManager.Instance?.RecordFullOreMined(Ore);
                }
            }
            // PotionMaterialWorthMore session ore drop 적용.
            // 이전엔 MineManager idle tick 만 적용 → PotionDrinker 의 MaterialWorth pick 이 session 무효.
            if (PotionBuffMechanics.Instance != null
                && PotionBuffMechanics.Instance.PotionMaterialWorthMore > 0)
            {
                worthMultiplier += PotionBuffMechanics.Instance.PotionMaterialWorthMore;
            }
            // 최종 oreDrop = count × worthMultiplier. Mathf.Max(1) 으로 0 drop 방지.
            int oreDrop = Mathf.Max(1, Mathf.RoundToInt(oreCount * worthMultiplier));

            // Sack-of-Gold artifact doubles the drop on Gold rocks (additive +oreDrop).
            // counter 모델 복원. 원본 GoldAndOreMechanics.cs:234-260 매칭. 매 gold drop
            // 마다 counter++, threshold (25, Rune 보유 시 24) 도달 시 reset + 2배. 이전 Shell 의
            // "always-double on gold rocks" 는 100% rate 였음 → 4% effective rate (1/25) 로 정확화.
            int totalOreDrop = oreDrop;
            if (ArtifactsManager.Instance != null && Ore == OreType.Gold
                && ArtifactsManager.Instance.TryGoldStackProc())
            {
                totalOreDrop += oreDrop;
                Rebuild.Stats.StatsManager.Instance?.RecordDoubleOre();
            }
            // GoldRing artifact's effect moved to GoldAndOreMechanics.CraftSessionToBars
            // (now applies as a 1.025-1.035x bonus on the session-end ore->bar conversion, mirroring
            // the original SetRockScreen.CraftOres behavior). Removed from per-rock drop.
            // HammerTime talent — the insta-mine fired by HammerTime doubles the drop.
            if (_hammerDoubleNextDeath)
            {
                totalOreDrop *= 2;
                _hammerDoubleNextDeath = false;
            }

            if (GoldAndOreMechanics.Instance != null)
            {
                GoldAndOreMechanics.Instance.AddOre(Ore, totalOreDrop);
                // popup tween from rock → top-left counter. Gameplay credit happens
                // instantly above (AddOre); the popup is purely visual feedback.
                if (MiningSessionController.Instance != null && OreCounterUI.Instance != null)
                {
                    var parent = MiningSessionController.Instance.CanvasMiningSceneTransform;
                    var target = OreCounterUI.Instance.GetEntryRT(Ore);
                    if (parent != null && target != null)
                        OreDropPopupFx.Spawn(parent, transform.position, Ore, totalOreDrop, target);
                }
                // Golden Touch talent drops +1 Gold on every rock (even non-Gold ones).
                if (LevelMechanics.Instance != null && LevelMechanics.Instance.HasTalent(TalentId.GoldenTouch)
                    && Ore != OreType.Gold)
                {
                    GoldAndOreMechanics.Instance.AddOre(OreType.Gold, 1);
                }
                // Cheese artifact: 0.2% chance per rock destroy to drop a +1 Gold bonus.
                // Cheese × (1 + runeImproveAll) 매칭.
                // base 1% → 0.2% 매칭 (원본 Artifacts.cheeseChance = 0.2f, 0.2/100 = 0.002).
                // Archaeologist 도 cheese chance 강화 (원본 RockMechanics.cs:728-730:
                // `cheeseChance × (1 + runeImproveAll + archaeologistIncrease)`).
                if (ArtifactsManager.Instance != null
                    && ArtifactsManager.Instance.HasArtifact(ArtifactId.Cheese))
                {
                    float cheeseArcheoMult = (LevelMechanics.Instance != null
                        && LevelMechanics.Instance.HasTalent(TalentId.Archaeologist)) ? 1.08f : 1f;
                    if (D100Helper.Roll(0.002f * ArtifactsManager.Instance.RuneMultiplier * cheeseArcheoMult))
                        GoldAndOreMechanics.Instance.AddOre(OreType.Gold, 1);
                }
                // Chests / GoldenChests 원본 매칭.
                // 원본 RockMechanics.cs:512~527: chest variant 만 boost 발동, 단일 path.
                // - Chest variant 부서지면 +25 ore (원본 totalChestMaterials=25)
                // - Golden variant 부서지면 +50 Gold (원본 totalGoldenChestMaterials=50)
                // 이전 Shell 의 "destroy 별도 roll 도 talent 활성 시 발동" 중복 path 제거.
                if (Chest == ChestKind.Chest)
                {
                    GoldAndOreMechanics.Instance.AddOre(Ore, 25);
                    Rebuild.Stats.StatsManager.Instance?.RecordChestOpened();
                }
                else if (Chest == ChestKind.Golden)
                {
                    GoldAndOreMechanics.Instance.AddOre(OreType.Gold, 50);
                    Rebuild.Stats.StatsManager.Instance?.RecordChestOpened();
                    Rebuild.Stats.StatsManager.Instance?.RecordGoldenChest();
                }
            }
            // record the rock + ore counts in one shot.
            Rebuild.Stats.StatsManager.Instance?.RecordRockDestroyed(Ore, totalOreDrop);
            // XP per rock (doubled by MoreXP talent — handled inside LevelMechanics).
            if (LevelMechanics.Instance != null && xpAmount > 0)
                LevelMechanics.Instance.GiveXP(xpAmount);

            // roll for a new artifact and pop up the found notice if any.
            // on hit, spawn an ArtifactRockOverlayUI at this rock's position that
            // pulses then flies toward the popup. The big "Artifact found!" UI still appears
            // (ArtifactFoundPopupUI.Show), but the in-world flourish makes the discovery feel
            // tied to the specific rock that broke.
            //
            // pre-spawn artifact priority. spawn 시점에 이미 박힌 rock 이면
            // ConfirmPreRolledArtifact (이미 _oneArtifactPreRolledThisSession flag 가 켜져 있으므로
            // TryRoll 은 어차피 null 반환) — pre-rolled artifact 가 일관성 있게 popup + flourish.
            // pre-roll 안 박힌 rock 은 기존 TryRoll fallback (모델 유지).
            if (ArtifactsManager.Instance != null)
            {
                ArtifactId? rolled;
                if (AssignedArtifact.HasValue)
                {
                    rolled = ArtifactsManager.Instance.ConfirmPreRolledArtifact(AssignedArtifact.Value);
                }
                else
                {
                    rolled = ArtifactsManager.Instance.TryRoll();
                }
                if (rolled.HasValue)
                {
                    if (ArtifactFoundPopupUI.Instance != null)
                        ArtifactFoundPopupUI.Instance.Show(rolled.Value);
                    if (transform.parent != null)
                    {
                        // Target = a bit above the rock (mimics the rock→top-center fly of the
                        // original game). Popup itself is on a separate canvas so we use a fixed
                        // upward offset rather than the popup's exact world position. `rockAnchored`
                        // is already in scope from the chunk-burst section above.
                        var artifactTarget = rockAnchored + new Vector2(0f, 260f);
                        ArtifactRockOverlayUI.Spawn(transform.parent, rockAnchored, artifactTarget, rolled.Value);
                    }
                }
            }
            // SpringSeason talent — chance to drop a clickable Flower under Canvas_Game.
            if (FlowerMechanics.Instance != null)
                FlowerMechanics.Instance.TryRollOnRockDestroy(this);
            // RespawnRockChance: lv*5% chance to immediately respawn a rock in this row.
            // ChanceToSpawnRockWhenMined: lv*5% chance to spawn a fresh rock anywhere
            //   in a random row (distinct from RespawnRockChance which targets the same row).
            // SpawnRockEveryXrock: original 6 -> 5 -> 4 -> 3 mined rocks per bonus spawn.
            // ChanceAdd1SecondEveryRockMined: original 0.075% per rock.
            if (SkillTreeManager.Instance != null)
            {
                var sk = SkillTreeManager.Instance;
                float respawnChance = sk.GetLevel(UpgradeId.RespawnRockChance) * 0.05f;
                if (respawnChance > 0f && D100Helper.Roll(respawnChance)
                    && RockSpawner.Instance != null && transform.parent != null)
                {
                    RockSpawner.Instance.SpawnOneInRow(transform.parent as RectTransform);
                }
                // fresh-rock-anywhere chance.
                float spawnRockChance = sk.GetLevel(UpgradeId.ChanceToSpawnRockWhenMined) * 0.05f;
                if (spawnRockChance > 0f && D100Helper.Roll(spawnRockChance)
                    && MiningSessionController.Instance != null)
                {
                    MiningSessionController.Instance.SpawnBonusRockRandomRow();
                }
                // 원본 chanceToAdd1SecEveryRockMined=0.075f (0.075% per rock) 매칭.
                // 이전 Shell 100% guaranteed 단순화 되돌리기.
                if (sk.GetLevel(UpgradeId.ChanceAdd1SecondEveryRockMined) > 0
                    && MiningSessionController.Instance != null
                    && D100Helper.Roll(0.00075f))
                {
                    MiningSessionController.Instance.AddTimeLeft(1f);
                }
                // SpawnRockEveryXrock counter. Period = max(3, 6 - lv).
                int xrockLv = sk.GetLevel(UpgradeId.SpawnRockEveryXrock);
                if (xrockLv > 0 && MiningSessionController.Instance != null)
                {
                    MiningSessionController.Instance.RecordRockMinedForSpawnCounter();
                    int period = Mathf.Max(3, 6 - Mathf.Clamp(xrockLv, 0, 3));
                    if (MiningSessionController.Instance.RocksMinedThisSession % period == 0)
                        MiningSessionController.Instance.SpawnBonusRockRandomRow();
                }
            }

            float t = 0f;
            var startScale = transform.localScale;
            var endScale = startScale * 1.15f;
            while (t < fadeDuration)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / fadeDuration);
                transform.localScale = Vector3.Lerp(startScale, endScale, k);
                var c = _baseColor; c.a = 1f - k;
                _img.color = c;
                yield return null;
            }
            Destroy(gameObject);
        }
    }
}
