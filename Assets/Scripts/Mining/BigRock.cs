using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Rebuild.Mining
{
    /// <summary>the endgame BigRock + hover-channel refit.
    /// Lives under Canvas_Ending. Hover-tick (same model as Rock.cs) channels damage every
    /// <c>MineTime</c> seconds while the pointer is over the rock. HP defaults to 200 (original
    /// 3500 + 3 broken sprite stages — ). Damage borrows the same multipliers
    /// Rock.cs uses so endgame upgrades still feel meaningful.</summary>
    [RequireComponent(typeof(Image))]
    public sealed class BigRock : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        // Original BigRock HP = 3500. Three
        // sprite stages swap as HP drops past 66.7% / 33.3% thresholds. Long ceremony lets the
        // 14-stage pickaxe roster contribute meaningfully even at +8 minePower (~440 hits).
        [SerializeField] int maxHp = 3500;
        [SerializeField] float punchDuration = 0.10f;
        [SerializeField] float punchScale = 0.92f;
        [SerializeField] float fadeDuration = 0.6f;

        [Tooltip("Fallback mine-time if no pickaxe is equipped.")]
        [SerializeField] float defaultMineTime = 0.45f;

        // sprite swap thresholds. Above stage1 = stage 0, between = stage 1,
        // below stage2 = stage 2. Matches original 1/3 / 2/3 transitions.
        const float kStage1Threshold = 0.667f;
        const float kStage2Threshold = 0.333f;

        Image _img;
        Color _baseColor;
        Vector3 _baseScale;
        int _hp;
        bool _isDying;
        bool _isHovered;
        float _tickAccum;
        int _spriteStage = -1;
        Coroutine _feedbackRoutine;

        public int MaxHp => maxHp;
        public int Hp => _hp;
        public bool IsDying => _isDying;

        public event Action<int, int> OnHpChanged;
        public event Action OnDestroyed;

        void Awake()
        {
            _img = GetComponent<Image>();
            _baseColor = _img.color;
            _baseScale = transform.localScale;
            _hp = maxHp;
        }

        public void Initialize(int hp)
        {
            maxHp = Mathf.Max(1, hp);
            _hp = maxHp;
            _isDying = false;
            _isHovered = false;
            _tickAccum = 0f;
            _spriteStage = -1;
            if (_img != null) _img.raycastTarget = true;
            UpdateSpriteStage();
            OnHpChanged?.Invoke(_hp, maxHp);
        }

        /// <summary>swap to the appropriate damaged-stage sprite. Sprite 0 = full,
        /// 1 = ~1/3 damaged, 2 = ~2/3 damaged. SpriteLibrary loads from Sprites/Ending/BigRock_1..3
        /// (user PNGs). Silently no-ops when the slot is empty so missing art still leaves
        /// the original BigRock sprite visible.</summary>
        void UpdateSpriteStage()
        {
            if (_img == null) return;
            float hpRatio = maxHp > 0 ? (float)_hp / maxHp : 0f;
            int newStage = hpRatio > kStage1Threshold ? 0 : (hpRatio > kStage2Threshold ? 1 : 2);
            if (newStage == _spriteStage) return;
            _spriteStage = newStage;
            var s = SpriteLibrary.GetBigRockStage(newStage);
            if (s != null) _img.sprite = s;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_isDying) return;
            _isHovered = true;
            Rebuild.Audio.OverlappingSounds.Instance?.PlayPickaxe();
            Rebuild.Stats.StatsManager.Instance?.RecordClick();
            ApplyOneTick();
            _tickAccum = 0f;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _isHovered = false;
            _tickAccum = 0f;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            // Click is a no-op channel-style — Enter already paid the first damage. Kept for
            // accessibility and mobile tap fallback (Enter+Click+Exit in same frame).
        }

        void Update()
        {
            if (_isDying || !_isHovered) return;
            _tickAccum += Time.unscaledDeltaTime;
            float interval = CurrentMineTime();
            while (_tickAccum >= interval && _isHovered && !_isDying)
            {
                _tickAccum -= interval;
                Rebuild.Audio.OverlappingSounds.Instance?.PlayPickaxe();
                Rebuild.Stats.StatsManager.Instance?.RecordClick();
                ApplyOneTick();
                interval = CurrentMineTime();
            }
        }

        float CurrentMineTime()
        {
            float t = defaultMineTime;
            if (AnvilManager.Instance != null)
            {
                int idx = AnvilManager.Instance.CurrentIdx;
                if (idx >= 0 && idx < PickaxeDef.Count)
                {
                    float candidate = PickaxeDef.Get(idx).MineTime;
                    if (candidate > 0f) t = candidate;
                }
            }
            // Bone artifact 의 secondary effect 복원 (원본 RockMechanics.cs:843-844).
            // bonePickaxeIncrease=0.04 → damage +4% AND reducePickaxeMineTime -= 0.04 → click cadence -4%.
            // MiningCursorUI.CurrentMineTime 과 동일 식 (분모에 합산). Rune 의 secondary -0.001 도 포함.
            float reduceTotal = 0f;
            if (ArtifactsManager.Instance != null)
            {
                if (ArtifactsManager.Instance.HasArtifact(ArtifactId.Bone)) reduceTotal += 0.04f;
                if (ArtifactsManager.Instance.HasArtifact(ArtifactId.Rune)) reduceTotal += 0.001f;
            }
            if (reduceTotal > 0f) t /= (1f + reduceTotal);
            // EnergiDrinker boost. 원본은 모든 hand collider mining 에
            // mineTime ÷ 2 적용 → BigRock 도 같은 boost 받음.
            if (MiningSessionController.Instance != null && MiningSessionController.Instance.IsEnergiBoosted)
                t *= 0.5f;
            return t;
        }

        void ApplyOneTick()
        {
            if (_isDying) return;
            // fractional pickaxe damage (mirrors Rock.ApplyOneTick).
            // Rock.ApplyExternalHit 의 multiplier 체인 매칭. BigRock 은 endgame
            // boulder 인데 두 큰 multiplier (DoubleDamageChance SkillTree + Anvil DoublePowerChance) 가
            // 누락이었음 → BigRock 진행도 24%+6.5% 약화.
            float dmg = 1f;
            if (AnvilManager.Instance != null)
                dmg += AnvilManager.Instance.CurrentDamageBonus;
            // Rock 과 동일한 multiplicative pickaxe-strength chain (원본 매칭).
            float strengthMult = 1f;
            var sk = SkillTreeManager.Instance;
            if (sk != null)
            {
                int impLv = sk.GetLevel(UpgradeId.ImprovedPickaxe);
                if (impLv > 0) strengthMult += impLv * 0.04f;
            }
            if (PotionBuffMechanics.Instance != null)
                strengthMult += PotionBuffMechanics.Instance.PotionPickaxeStats;
            if (strengthMult != 1f) dmg *= strengthMult;
            // same multiplier model as Rock.ApplyExternalHit (Bone +4 / WolfClaw +6 / Rune +5 %).
            // WolfClaw 의 clawChance × (1 + RuneBonus) multiplier chain 적용
            // (원본 TheAnvil.cs:2358 `clawChance × (1 + runeImproveAll + archeologistIncrease)`).
            // Archaeologist multi-site multiplier 매칭 (Rock.ApplyExternalHit 와 동일).
            if (ArtifactsManager.Instance != null)
            {
                float artifactMult = 1f;
                if (ArtifactsManager.Instance.HasArtifact(ArtifactId.Bone)) artifactMult += 0.04f;
                if (ArtifactsManager.Instance.HasArtifact(ArtifactId.WolfClaw))
                {
                    float wolfArcheoMult = (LevelMechanics.Instance != null
                        && LevelMechanics.Instance.HasTalent(TalentId.Archaeologist)) ? 1.08f : 1f;
                    artifactMult += 0.06f * ArtifactsManager.Instance.RuneMultiplier * wolfArcheoMult;
                }
                if (ArtifactsManager.Instance.HasArtifact(ArtifactId.Rune)) artifactMult += 0.05f;
                dmg *= artifactMult;
            }
            // DoubleDamageChance (SkillTree) roll. Rock 의 동일 패턴 복사.
            // rate 는 *0.01f 매칭 (원본 doubleDamageChance 1f/2f, maxLv=2).
            if (sk != null)
            {
                float doubleDmgChance = sk.GetLevel(UpgradeId.DoubleDamageChance) * 0.01f;
                if (doubleDmgChance > 0f && D100Helper.Roll(doubleDmgChance))
                {
                    dmg *= 2f;
                    Rebuild.Stats.StatsManager.Instance?.RecordDoubleDamage();
                }
            }
            // Anvil DoublePowerChance: equipped pickaxe 의 2X Power Chance 발동
            // (Pickaxe14 의 6.5% buff 등). SkillTree DoubleDamageChance 와 별개. 원본 Rock 과 동일.
            if (AnvilManager.Instance != null)
            {
                float anvilDouble = AnvilManager.Instance.CurrentDoublePowerChance;
                if (anvilDouble > 0f && D100Helper.Roll(anvilDouble))
                {
                    dmg *= 2f;
                    Rebuild.Stats.StatsManager.Instance?.RecordDoubleDamage();
                }
            }
            TakeDamage(Mathf.RoundToInt(dmg));
            // (v2) — spawn pickaxe swing fx ON the BigRock itself
            // (sits as a sibling so anchored coords align without conversion).
            var rt = transform as RectTransform;
            if (rt != null)
                MiningCursorUI.Instance?.PlaySwingAt(transform.parent, rt.anchoredPosition);
        }

        public void TakeDamage(int amount)
        {
            if (_isDying) return;
            _hp -= amount;
            if (_hp <= 0)
            {
                _hp = 0;
                _isDying = true;
                _isHovered = false;
                OnHpChanged?.Invoke(_hp, maxHp);
                OnDestroyed?.Invoke();
                StartCoroutine(FadeOut());
            }
            else
            {
                OnHpChanged?.Invoke(_hp, maxHp);
                UpdateSpriteStage();
                if (_feedbackRoutine != null) StopCoroutine(_feedbackRoutine);
                _feedbackRoutine = StartCoroutine(HitFeedback());
            }
        }

        IEnumerator HitFeedback()
        {
            float t = 0f;
            var punched = _baseScale * punchScale;
            while (t < punchDuration * 0.4f)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / (punchDuration * 0.4f));
                transform.localScale = Vector3.Lerp(_baseScale, punched, k);
                _img.color = Color.Lerp(_baseColor, Color.white, Mathf.Min(1f, k * 1.5f));
                yield return null;
            }
            t = 0f;
            float settleDur = punchDuration * 0.6f;
            while (t < settleDur)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / settleDur);
                transform.localScale = Vector3.Lerp(punched, _baseScale, k);
                _img.color = Color.Lerp(Color.white, _baseColor, k);
                yield return null;
            }
            transform.localScale = _baseScale;
            _img.color = _baseColor;
            _feedbackRoutine = null;
        }

        IEnumerator FadeOut()
        {
            if (_feedbackRoutine != null) StopCoroutine(_feedbackRoutine);
            _img.raycastTarget = false;
            Rebuild.Audio.OverlappingSounds.Instance?.PlayRockBreak();

            // much bigger chunk burst (BigRock is the endgame boulder). Original game
            // uses localScale=10 vs 2.6 — we pass spread=3 so chunk count + travel distance scale.
            var rtSelf = transform as RectTransform;
            if (rtSelf != null && transform.parent != null)
                RockBreakParticleFx.Spawn(transform.parent, rtSelf.anchoredPosition, 3f);

            // heavier shake for the BigRock break (0.2s vs 0.1s, 8px vs 4px).
            ScreenShake.Instance?.Shake(0.20f, 8f);

            float t = 0f;
            var startScale = transform.localScale;
            var endScale = startScale * 1.4f;
            while (t < fadeDuration)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / fadeDuration);
                transform.localScale = Vector3.Lerp(startScale, endScale, k);
                var c = _baseColor; c.a = 1f - k;
                _img.color = c;
                yield return null;
            }
            gameObject.SetActive(false);
        }
    }
}
