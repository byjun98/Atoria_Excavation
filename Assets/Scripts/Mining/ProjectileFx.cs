using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Rebuild.Mining
{
    /// <summary>+ + short-lived AOE projectile.
    /// Spawned by <see cref="ProjectileLauncher"/> after a rock click rolls a SkillTree chance.
    /// <para>
    /// <b>7-tag damage variants &amp; per-type animation flair:</b>
    /// </para>
    /// <list type="bullet">
    /// <item><b>Beam_PH (50%) / Beam_S (50%)</b> — Beam coin-flips on spawn. PH stays at base
    /// damage with the red tint; S adds a sharper cyan tint + 2x base damage to mirror
    /// the original <c>Beam_S</c> tag (dmg x3 vs Beam_PH dmg x2 → relative 1.5x).</item>
    /// <item><b>Plazma multi-tick</b> — splits its damage into 5 ticks across the
    /// 1.0s Lifetime (was 3 × 0.42s = 0.42s 단축). 각 tick re-resolves 최근접 N rocks. Total damage
    /// budget 5 × 0.3 = 1.5× single-shot (의 1.5× 유지). 원본 PlazmaBallCollider 가
    /// plazmaBallTimer=5f lingering 동안 충돌마다 damage 였던 "sustained AOE" 느낌 매칭.</item>
    /// <item><b>Dynamite shockwave</b> — two rapid scale punches over Animate so the explosion
    /// feels staccato (original 3 explosion child sprites).</item>
    /// <item><b>ElectricitySplash rotation</b> — 1 full spin over Lifetime so the 8-arm sprite
    /// reads as energy + a brighter glow during the first half.</item>
    /// </list>
    /// <para><b>verified design lineage vs 원본:</b></para>
    /// <list type="bullet">
    /// <item><b>beamHitPos</b> 매칭 — 원본의 <c>RockMechanics.beamHitPos</c> static + <c>LightningAftermath</c>
    /// (이전 beam hit 위치에 secondary spawn) 는 Shell 의 per-projectile <c>_sourceRock</c> field +
    /// <c>FindLivingAnchor()</c> fallback 으로 동등. cascade 가 source rock anchor 를 그대로 받음.</item>
    /// <item><b>OnlyExplosion</b> tag 의도된 차이 — 원본은 cascade-spawned dynamite 에 <c>OnlyExplosion</c>
    /// tag 를 박아 <i>damage 0</i> (visual only). Shell 은 <c>MaxCascadeDepth=2</c> 로 chain length 만
    /// 제한, cascade dynamite 도 full damage. → cascade 더 강력 but 깊이 제한.</item>
    /// <item><b>Plazma timer</b> 부분 매칭 — 원본 <c>plazmaBallTimer = 5f</c> lingering ball, 충돌마다
    /// damage. Shell 은 로 Lifetime <b>1.0s</b> + 5 evenly-spaced tick × 0.3× damage
    /// = 예측 가능한 fixed budget (1.5× single-shot). 1초 lingering 으로 sustained AOE 느낌 보존하면서
    /// 5초까지의 화면 혼잡도는 회피. PlazmaLifetime SkillTree 노드로 추가 연장 가능.</item>
    /// <item><b>AllProjectileTrigger</b> 확장 매칭 — 원본 single-level node (+15%, Beam 제외) → Shell
    /// multi-level (+5%/lv, Beam 포함). 의 의도된 enhancement.</item>
    /// </list>
    /// Cascade trigger + pool release stay unchanged.</summary>
    [RequireComponent(typeof(Image), typeof(RectTransform))]
    public sealed class ProjectileFx : MonoBehaviour
    {
        public const int MaxCascadeDepth = 2;
        const float CascadeChancePerLevel = 0.10f;
        const float ProjectileAddTimeChance = 0.008f;
        const float BeamSpawnRockChance = 0.24f;
        const float DynamiteSpawnRockChance = 0.21f;
        const int ParticleSortingOrder = 250;

        static readonly string[] ParticlePrefabPaths = new string[ProjectileDef.Count]
        {
            null,
            null,
            null,
            null,
        };

        static readonly GameObject[] s_particlePrefabs = new GameObject[ProjectileDef.Count];

        Image _img;
        RectTransform _rt;
        ProjectileType _type;
        bool _counted;
        bool _active;          // true while we hold a Spawned slot — Reset clears this for pool reuse
        int _cascadeDepth;
        Rock _sourceRock;      // captured for cascade spawn position (source rock may already be dead)
        GameObject _particleRoot;
        bool _usingParticleVisual;

        // Beam_S variant flag. Only meaningful when _type == BeamOfLight.
        bool _beamIsSVariant;

        // auxiliary layers built once per pool instance, then re-purposed per spawn.
        // GlowChild renders BEHIND the main sprite (set-as-first-sibling) as a soft halo.
        // RingChild renders IN FRONT (set-as-last-sibling) for Dynamite shockwaves.
        Image _glowChild;
        RectTransform _glowChildRt;
        Image _ringChild;
        RectTransform _ringChildRt;

        void Awake()
        {
            EnsureChildLayers();
        }

        void OnDestroy()
        {
            // Only fire the despawn callback if we were still counted as "alive" — for pool-released
            // instances Animate already cleared _counted before yielding back to the pool.
            if (_counted) ProjectileLauncher.OnProjectileDespawned(_type);
        }

        /// <summary>lazy-create the back glow + front ring child Images. Called from
        /// Awake (runs once on first pool activation) and re-checked from Initialize so pool entries
        /// created before this method existed still get the layers retrofitted on the next spawn.</summary>
        void EnsureChildLayers()
        {
            if (_glowChild == null)
            {
                var glowGo = new GameObject("Glow", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                glowGo.transform.SetParent(transform, false);
                glowGo.transform.SetAsFirstSibling();
                _glowChildRt = (RectTransform)glowGo.transform;
                _glowChildRt.anchorMin = _glowChildRt.anchorMax = new Vector2(0.5f, 0.5f);
                _glowChildRt.pivot = new Vector2(0.5f, 0.5f);
                _glowChildRt.anchoredPosition = Vector2.zero;
                _glowChild = glowGo.GetComponent<Image>();
                _glowChild.raycastTarget = false;
                _glowChild.preserveAspect = false;
                glowGo.SetActive(false);
            }
            if (_ringChild == null)
            {
                var ringGo = new GameObject("Ring", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                ringGo.transform.SetParent(transform, false);
                ringGo.transform.SetAsLastSibling();
                _ringChildRt = (RectTransform)ringGo.transform;
                _ringChildRt.anchorMin = _ringChildRt.anchorMax = new Vector2(0.5f, 0.5f);
                _ringChildRt.pivot = new Vector2(0.5f, 0.5f);
                _ringChildRt.anchoredPosition = Vector2.zero;
                _ringChild = ringGo.GetComponent<Image>();
                _ringChild.raycastTarget = false;
                _ringChild.preserveAspect = false;
                ringGo.SetActive(false);
            }
        }

        /// <summary>Reset for fresh use from the pool. Called by <see cref="ProjectileLauncher"/>
        /// before <see cref="Initialize"/>.</summary>
        public void ResetForReuse()
        {
            _counted = false;
            _active = false;
            _cascadeDepth = 0;
            _sourceRock = null;
            _beamIsSVariant = false;
            _usingParticleVisual = false;
            DestroyParticleRoot();
            if (_img != null) _img.enabled = true;
            // Reset transform side-effects from previous spawn (Dynamite shockwave / Lightning rotation).
            transform.localRotation = Quaternion.identity;
            // Hide both decoration layers — Initialize re-enables what each type needs.
            if (_glowChild != null) _glowChild.gameObject.SetActive(false);
            if (_ringChild != null)
            {
                _ringChild.gameObject.SetActive(false);
                if (_ringChildRt != null) _ringChildRt.localScale = Vector3.one;
            }
        }

        public void Initialize(ProjectileType type, Rock source, int cascadeDepth = 0)
        {
            _img = GetComponent<Image>();
            _rt = (RectTransform)transform;
            _type = type;
            _counted = true;
            _active = true;
            _cascadeDepth = cascadeDepth;
            _sourceRock = source;
            // Beam_PH (default) vs Beam_S coin-flip. S = sharper cyan + double base dmg.
            _beamIsSVariant = (type == ProjectileType.BeamOfLight) && UnityEngine.Random.value < 0.5f;
            ProjectileLauncher.OnProjectileSpawned(type);
            // SkillTree stat mods (BeamDamage / *Size / PlazmaLifetime) are folded
            // into the per-spawn Info struct so the visuals (sizeDelta, lifetime) reflect them too.
            var info = ApplyStatMods(ProjectileDef.Get(type));

            _img.sprite = ProjectileSpriteFactory.Get(type);
            // Beam_S overrides the warm red tint with a cool cyan so the variant
            // reads at a glance. Other types keep their tint.
            var mainColor = (type == ProjectileType.BeamOfLight && _beamIsSVariant)
                ? new Color(0.30f, 0.95f, 1.00f, 0.95f) // sharper cyan
                : info.Tint;
            _img.color = mainColor;
            _img.raycastTarget = false;
            _img.preserveAspect = false;
            _rt.sizeDelta = info.Size;
            _rt.localScale = Vector3.one * 0.4f; // starts small — Animate scales up
            _rt.localRotation = Quaternion.identity;
            // light up the decoration layers per type.
            EnsureChildLayers();
            SetupLayers(type, info, mainColor);

            _usingParticleVisual = TrySetupParticleVisual(info);
            if (_usingParticleVisual)
            {
                _img.enabled = false;
                _img.raycastTarget = false;
                _rt.localScale = Vector3.one;
                if (_glowChild != null) _glowChild.gameObject.SetActive(false);
                if (_ringChild != null) _ringChild.gameObject.SetActive(false);
            }

            // projectile firing SFX (beam-type overlapping). Same source for all 4
            // projectile types in Shell; per-type SFX is a polish item.
            Rebuild.Audio.OverlappingSounds.Instance?.PlayBeam();

            // Plazma applies its damage over time inside Animate; everyone else
            // resolves once up-front so the AOE punch lands on Initialize.
            if (type != ProjectileType.PlazmaBall)
                ApplyDamage(info, source, dmgMultiplier: 1f);

            StartCoroutine(Animate(info));
        }

        /// <summary>apply SkillTree stat mods. Each level adds a fixed amount to
        /// the per-type Damage / MaxTargets / Lifetime / visual Size. Mods are computed once at
        /// spawn so cascading projectiles can reuse the base mechanic without recomputing.</summary>
        static ProjectileDef.Info ApplyStatMods(ProjectileDef.Info info)
        {
            var sk = SkillTreeManager.Instance;
            if (sk == null) return info;
            int dmgMod = 0, targetMod = 0;
            float lifeMod = 0f;
            float sizeMult = 1f;
            switch (info.Type)
            {
                case ProjectileType.BeamOfLight:
                    dmgMod    = sk.GetLevel(UpgradeId.BeamDamage);
                    targetMod = sk.GetLevel(UpgradeId.BeamSize);
                    sizeMult  = 1f + targetMod * 0.10f;
                    break;
                case ProjectileType.Dynamite:
                    dmgMod    = sk.GetLevel(UpgradeId.DynamiteDamage) * 2;
                    targetMod = sk.GetLevel(UpgradeId.DynamiteSize);
                    sizeMult  = 1f + targetMod * 0.12f;
                    break;
                case ProjectileType.PlazmaBall:
                    dmgMod    = sk.GetLevel(UpgradeId.PlazmaDamage) * 2;
                    targetMod = sk.GetLevel(UpgradeId.PlazmaSize);
                    // 원본 plazmaBallTimerIncrease=1f 매칭 (Shell 이전 0.1f = 10배 약함).
                    // 단일 lv 노드(MaxLevel=1) → +1초 fixed.
                    lifeMod   = sk.GetLevel(UpgradeId.PlazmaLifetime) * 1f;
                    sizeMult  = 1f + targetMod * 0.12f;
                    break;
                case ProjectileType.ElectricitySplash:
                    targetMod = sk.GetLevel(UpgradeId.LightningSize);
                    sizeMult  = 1f + targetMod * 0.10f;
                    break;
            }
            return new ProjectileDef.Info(
                info.Type, info.Name, info.ChanceUpgradeId, info.Tint,
                info.Damage + dmgMod,
                info.MaxTargets + targetMod,
                info.Size * sizeMult,
                info.Lifetime + lifeMod);
        }

        static GameObject LoadParticlePrefab(ProjectileType type)
        {
            int i = (int)type;
            if (i < 0 || i >= s_particlePrefabs.Length) return null;
            if (string.IsNullOrEmpty(ParticlePrefabPaths[i])) return null;
            if (s_particlePrefabs[i] == null)
                s_particlePrefabs[i] = Resources.Load<GameObject>(ParticlePrefabPaths[i]);
            return s_particlePrefabs[i];
        }

        bool TrySetupParticleVisual(ProjectileDef.Info info)
        {
            var prefab = LoadParticlePrefab(info.Type);
            if (prefab == null) return false;

            DestroyParticleRoot();
            _particleRoot = Instantiate(prefab, transform);
            _particleRoot.name = prefab.name + "_Runtime";
            _particleRoot.transform.localPosition = Vector3.zero;
            _particleRoot.transform.localRotation = ParticleRotationFor(info.Type);
            _particleRoot.transform.localScale = ParticleScaleFor(info);

            var renderers = _particleRoot.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                renderers[i].sortingOrder = ParticleSortingOrder + ParticleSortingOffset(info.Type);
            }

            var systems = _particleRoot.GetComponentsInChildren<ParticleSystem>(true);
            if (systems.Length == 0)
            {
                DestroyParticleRoot();
                return false;
            }
            for (int i = 0; i < systems.Length; i++)
            {
                systems[i].Clear(true);
                systems[i].Play(true);
            }

            return true;
        }

        void DestroyParticleRoot()
        {
            if (_particleRoot == null) return;
            _particleRoot.SetActive(false);
            if (Application.isPlaying) Destroy(_particleRoot);
            else DestroyImmediate(_particleRoot);
            _particleRoot = null;
        }

        static Quaternion ParticleRotationFor(ProjectileType type)
        {
            if (type == ProjectileType.ElectricitySplash)
                return Quaternion.Euler(0f, 0f, UnityEngine.Random.Range(0f, 360f));
            return Quaternion.identity;
        }

        static Vector3 ParticleScaleFor(ProjectileDef.Info info)
        {
            switch (info.Type)
            {
                case ProjectileType.BeamOfLight:
                    return Vector3.one * Mathf.Clamp(info.Size.x / 14f, 18f, 42f);
                case ProjectileType.PlazmaBall:
                    return Vector3.one * Mathf.Clamp(info.Size.x / 35f, 6f, 14f);
                case ProjectileType.Dynamite:
                    return Vector3.one * Mathf.Clamp(info.Size.x / 28f, 8f, 18f);
                case ProjectileType.ElectricitySplash:
                    return Vector3.one * Mathf.Clamp(info.Size.x / 32f, 7f, 16f);
                default:
                    return Vector3.one;
            }
        }

        static int ParticleSortingOffset(ProjectileType type)
        {
            switch (type)
            {
                case ProjectileType.Dynamite: return 3;
                case ProjectileType.BeamOfLight: return 2;
                case ProjectileType.PlazmaBall: return 1;
                default: return 0;
            }
        }

        static float ParticleVisualLifetime(ProjectileDef.Info info)
        {
            switch (info.Type)
            {
                case ProjectileType.BeamOfLight: return Mathf.Max(info.Lifetime, 0.75f);
                case ProjectileType.Dynamite: return Mathf.Max(info.Lifetime, 0.90f);
                case ProjectileType.ElectricitySplash: return Mathf.Max(info.Lifetime, 0.50f);
                default: return info.Lifetime;
            }
        }

        void ApplyDamage(ProjectileDef.Info info, Rock source, float dmgMultiplier)
        {
            if (source == null) return;
            // Find every Rock in the scene, sort by world-distance to the source rock,
            // and damage the closest MaxTargets that aren't already dying.
            var rocks = Object.FindObjectsByType<Rock>(FindObjectsSortMode.None);
            if (rocks == null || rocks.Length == 0) return;

            Vector3 origin = source.transform.position;
            System.Array.Sort(rocks, (a, b) =>
            {
                if (a == null && b == null) return 0;
                if (a == null) return 1;
                if (b == null) return -1;
                float da = (a.transform.position - origin).sqrMagnitude;
                float db = (b.transform.position - origin).sqrMagnitude;
                return da.CompareTo(db);
            });

            // 7-tag damage — Beam_S variant doubles the per-hit damage so the cyan beams
            // feel meaningfully different from the red Beam_PH.
            int dmg = Mathf.Max(1, Mathf.RoundToInt(info.Damage * dmgMultiplier));
            if (_type == ProjectileType.BeamOfLight && _beamIsSVariant) dmg *= 2;

            // AllProjectileDouble — lv*5% chance per hit instance to double the damage.
            var sk = SkillTreeManager.Instance;
            if (sk != null)
            {
                float doubleChance = sk.GetLevel(UpgradeId.AllProjectileDouble) * 0.05f;
                if (doubleChance > 0f && D100Helper.Roll(doubleChance)) dmg *= 2;
            }

            int hit = 0;
            for (int i = 0; i < rocks.Length && hit < info.MaxTargets; i++)
            {
                var r = rocks[i];
                if (r == null || r.IsDying) continue;
                r.TakeDamage(dmg);
                hit++;
            }
        }

        IEnumerator Animate(ProjectileDef.Info info)
        {
            float life = Mathf.Max(0.05f, _usingParticleVisual ? ParticleVisualLifetime(info) : info.Lifetime);
            var startScale = Vector3.one * 0.4f;
            var endScale = Vector3.one * 1.35f;
            var c0 = _img != null ? _img.color : Color.white;

            // + Plazma multi-tick scheduling: 5 evenly-spaced ticks
            // across the (now 1.0s) Lifetime. Each tick re-runs ApplyDamage so rocks that just
            // spawned can also be hit. Total damage budget = 5 × 0.3 = 1.5× single-shot (// 의 1.5× 보존). 1초 lingering 으로 원본 plazmaBallTimer=5f sustained AOE 느낌 강화.
            int plazmaTicksFired = 0;
            int plazmaTotalTicks = 5;
            float plazmaTickInterval = life / plazmaTotalTicks;
            float nextPlazmaTickAt = plazmaTickInterval * 0.4f; // first tick lands a hair earlier than 1/5th

            float t = 0f;
            while (t < life)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / life);

                // Per-type animation flair ().
                if (!_usingParticleVisual)
                {
                    ApplyPerTypeAnim(k, startScale, endScale);
                // sync the back glow + front ring layers each frame.
                    UpdateLayerAnim(k);

                // Alpha fade is shared by all types — visible glow that softens toward end of life.
                    var c = c0; c.a = c0.a * (1f - k * 0.85f);
                    _img.color = c;
                }

                // Plazma damage ticks — fire as long as t crosses the next scheduled tick.
                if (_type == ProjectileType.PlazmaBall && _sourceRock != null
                    && plazmaTicksFired < plazmaTotalTicks && t >= nextPlazmaTickAt)
                {
                    plazmaTicksFired++;
                    // 0.5× → 0.3× (5 tick × 0.3 = 1.5× total, budget 유지).
                    ApplyDamage(info, _sourceRock, dmgMultiplier: 0.3f);
                    nextPlazmaTickAt = plazmaTickInterval * 0.4f + plazmaTickInterval * plazmaTicksFired;
                }

                yield return null;
            }
            // cascade hook fires at end of life. Source rock may be dead
            // (and pooled / destroyed) — TryCascade re-resolves a live anchor from active rocks.
            TryCascade();
            TryOriginalSpawnRockBonus();
            // Hand control back to the launcher pool. OnProjectileDespawned ticks down the
            // per-type alive counter; clearing _counted prevents OnDestroy from double-decrementing.
            if (_counted) { ProjectileLauncher.OnProjectileDespawned(_type); _counted = false; }
            _active = false;
            if (_particleRoot != null) _particleRoot.SetActive(false);
            ProjectileLauncher.Release(this);
        }

        /// <summary>wire up the Glow + Ring decoration layers per type.
        /// Glow is on for every type (tinted to the main color, larger sizeDelta, lower alpha).
        /// Ring is only used by Dynamite (white shockwave that scales up faster than the main sprite).</summary>
        void SetupLayers(ProjectileType type, ProjectileDef.Info info, Color mainColor)
        {
            // Back glow — always on if the sprite is imported.
            var glowSprite = ProjectileSpriteFactory.GetGlow();
            if (_glowChild != null && glowSprite != null)
            {
                _glowChild.sprite = glowSprite;
                var glowColor = mainColor;
                glowColor.a = 0.55f;
                _glowChild.color = glowColor;
                // Glow reads as a halo around the main sprite — bigger on every side.
                _glowChildRt.sizeDelta = info.Size * 1.7f;
                _glowChildRt.localScale = Vector3.one;
                _glowChild.gameObject.SetActive(true);
            }
            else if (_glowChild != null)
            {
                _glowChild.gameObject.SetActive(false);
            }

            // Front ring — Dynamite only (shockwave). Other types keep it hidden.
            bool wantsRing = type == ProjectileType.Dynamite;
            var ringSprite = ProjectileSpriteFactory.GetRing();
            if (_ringChild != null && wantsRing && ringSprite != null)
            {
                _ringChild.sprite = ringSprite;
                var ringColor = new Color(1f, 0.85f, 0.45f, 0.95f);
                _ringChild.color = ringColor;
                _ringChildRt.sizeDelta = info.Size; // starts at the main size, scales up in Animate
                _ringChildRt.localScale = Vector3.one * 0.6f;
                _ringChild.gameObject.SetActive(true);
            }
            else if (_ringChild != null)
            {
                _ringChild.gameObject.SetActive(false);
            }
        }

        /// <summary>drive child-layer scale/alpha each frame so they read as part of
        /// the same effect even though their tween shapes differ from the parent's.</summary>
        void UpdateLayerAnim(float k)
        {
            // Glow pulses gently — already inherits parent scale, so we only nudge its localScale
            // to give it a subtle breathing motion + alpha that lags the main sprite's fade.
            if (_glowChild != null && _glowChild.gameObject.activeSelf)
            {
                float glowPulse = 1f + 0.15f * Mathf.Sin(k * Mathf.PI * 4f);
                _glowChildRt.localScale = Vector3.one * glowPulse;
                var c = _glowChild.color;
                c.a = Mathf.Lerp(0.55f, 0f, k * 0.9f);
                _glowChild.color = c;
            }
            // Ring shockwave for Dynamite — expands faster than the main sprite (0.6 → 2.2) and
            // fades to zero by ~70% of life so the punch is sharp.
            if (_ringChild != null && _ringChild.gameObject.activeSelf)
            {
                float ringScale = Mathf.Lerp(0.6f, 2.2f, Mathf.SmoothStep(0f, 1f, k));
                _ringChildRt.localScale = Vector3.one * ringScale;
                var c = _ringChild.color;
                c.a = Mathf.Clamp01(1f - k / 0.7f) * 0.95f;
                _ringChild.color = c;
            }
        }

        /// <summary>animation tweaks per projectile type. <paramref name="k"/> is the
        /// 0..1 lifetime progress. Scale lerp is shared across types so the AOE punch reads;
        /// type-specific flair sits on top: Dynamite punches twice, Lightning spins, Plazma pulses.</summary>
        void ApplyPerTypeAnim(float k, Vector3 startScale, Vector3 endScale)
        {
            // Base scale ramp (all types share it).
            var scale = Vector3.Lerp(startScale, endScale, k);
            switch (_type)
            {
                case ProjectileType.Dynamite:
                {
                    // Two quick punches — small bump near 30% and another at 65% of life. Adds a
                    // staccato shockwave feel without needing a particle system.
                    float punch1 = Mathf.Exp(-Mathf.Pow((k - 0.30f) / 0.05f, 2f));
                    float punch2 = Mathf.Exp(-Mathf.Pow((k - 0.65f) / 0.06f, 2f));
                    float kick = 0.25f * (punch1 + punch2);
                    _rt.localScale = scale * (1f + kick);
                    break;
                }
                case ProjectileType.ElectricitySplash:
                {
                    // One full 360° rotation across the lifetime. Reads as crackling energy.
                    _rt.localScale = scale;
                    _rt.localRotation = Quaternion.Euler(0f, 0f, k * 360f);
                    break;
                }
                case ProjectileType.PlazmaBall:
                {
                    // Subtle pulsing — 3 cycles of ±8% scale over life so the orb feels alive
                    // while the multi-tick damage is applied.
                    float pulse = 1f + 0.08f * Mathf.Sin(k * Mathf.PI * 6f);
                    _rt.localScale = scale * pulse;
                    break;
                }
                case ProjectileType.BeamOfLight:
                {
                    // Beam_S stretches horizontally a bit more than Beam_PH to read as a wider beam.
                    if (_beamIsSVariant)
                    {
                        var s = scale; s.x *= 1.15f; s.y *= 0.85f;
                        _rt.localScale = s;
                    }
                    else
                    {
                        _rt.localScale = scale;
                    }
                    break;
                }
                default:
                    _rt.localScale = scale;
                    break;
            }
        }

        /// <summary>Original projectile spawn-rock branches. Dedicated original nodes are folded
        /// into the rebuilt projectile branches, so these only run once the related branch exists.</summary>
        void TryOriginalSpawnRockBonus()
        {
            var session = MiningSessionController.Instance;
            if (session == null || !session.IsInSession || session.ResultsShown) return;

            var sk = SkillTreeManager.Instance;
            if (sk == null) return;

            if (_type == ProjectileType.BeamOfLight)
            {
                bool hasBeamBranch = sk.GetLevel(UpgradeId.CascadeBeamToDyna) > 0
                    || sk.GetLevel(UpgradeId.CascadeBeamToBeam) > 0
                    || sk.GetLevel(UpgradeId.CascadeBeamToSplash) > 0
                    || sk.GetLevel(UpgradeId.CascadeBeamTime) > 0;
                if (hasBeamBranch && D100Helper.Roll(BeamSpawnRockChance))
                    session.SpawnBonusRockRandomRow();
            }
            else if (_type == ProjectileType.Dynamite)
            {
                bool hasDynamiteBranch = sk.GetLevel(UpgradeId.CascadeDynaToBeam) > 0
                    || sk.GetLevel(UpgradeId.CascadeDynaToDyna) > 0
                    || sk.GetLevel(UpgradeId.CascadeDynaToSplash) > 0;
                if (hasDynamiteBranch && D100Helper.Roll(DynamiteSpawnRockChance))
                    session.SpawnBonusRockRandomRow();
            }
        }

        /// <summary>Roll each cascade chance and ask the launcher to spawn the resulting projectile.</summary>
        void TryCascade()
        {
            if (_cascadeDepth >= MaxCascadeDepth) return;
            var sk = SkillTreeManager.Instance;
            if (sk == null) return;
            int nextDepth = _cascadeDepth + 1;
            // Re-resolve a usable anchor — the original source rock may have died during Animate.
            var anchor = (_sourceRock != null && !_sourceRock.IsDying) ? _sourceRock : FindLivingAnchor();
            switch (_type)
            {
                case ProjectileType.BeamOfLight:
                    TryCascadeSpawn(sk, UpgradeId.CascadeBeamToDyna,   ProjectileType.Dynamite,          anchor, nextDepth);
                    TryCascadeSpawn(sk, UpgradeId.CascadeBeamToBeam,   ProjectileType.BeamOfLight,       anchor, nextDepth);
                    TryCascadeSpawn(sk, UpgradeId.CascadeBeamToSplash, ProjectileType.ElectricitySplash, anchor, nextDepth);
                    TryCascadeEffect(sk, UpgradeId.CascadeBeamTime, ProjectileAddTimeChance,
                        () => MiningSessionController.Instance?.AddTimeLeft(1f));
                    break;
                case ProjectileType.Dynamite:
                    TryCascadeSpawn(sk, UpgradeId.CascadeDynaToBeam,   ProjectileType.BeamOfLight,       anchor, nextDepth);
                    TryCascadeSpawn(sk, UpgradeId.CascadeDynaToDyna,   ProjectileType.Dynamite,          anchor, nextDepth);
                    // Dyna -> Splash (번개) 추가.
                    TryCascadeSpawn(sk, UpgradeId.CascadeDynaToSplash, ProjectileType.ElectricitySplash, anchor, nextDepth);
                    break;
                case ProjectileType.PlazmaBall:
                    TryCascadeSpawn(sk, UpgradeId.CascadePlazmaToDyna,       ProjectileType.Dynamite,          anchor, nextDepth);
                    // Plazma -> Plazma / Splash 추가.
                    TryCascadeSpawn(sk, UpgradeId.CascadePlazmaToPlasmaBall, ProjectileType.PlazmaBall,        anchor, nextDepth);
                    TryCascadeSpawn(sk, UpgradeId.CascadePlazmaToSplash,     ProjectileType.ElectricitySplash, anchor, nextDepth);
                    // 원본 plazmaBallSpawnPickaxeChance: Plazma 소멸 시 random 3 rock auto-tick.
                    TryPlazmaPickaxe(sk);
                    break;
                case ProjectileType.ElectricitySplash:
                    // Lightning -> Dyna 추가 (원본 lightning splash 후 dynamite chain).
                    TryCascadeSpawn(sk, UpgradeId.CascadeLightningToDyna, ProjectileType.Dynamite, anchor, nextDepth);
                    break;
            }
        }

        /// <summary>원본 plazmaBallSpawnPickaxeChance 매칭. Plazma 소멸 시 lv*10% 확률로
        /// 무작위 3 rock 에 ApplyExternalHit (auto-tick). 원본 SelectRandomActiveRock(3) 와 동일 의미.</summary>
        static void TryPlazmaPickaxe(SkillTreeManager sk)
        {
            int lv = sk.GetLevel(UpgradeId.PlazmaSpawnPickaxe);
            if (lv <= 0) return;
            if (!D100Helper.Roll(lv * CascadeChancePerLevel)) return;
            var rocks = Object.FindObjectsByType<Rock>(FindObjectsSortMode.None);
            if (rocks == null || rocks.Length == 0) return;
            int hits = 0;
            for (int attempts = 0; attempts < 12 && hits < 3; attempts++)
            {
                var pick = rocks[UnityEngine.Random.Range(0, rocks.Length)];
                if (pick == null || pick.IsDying) continue;
                pick.ApplyExternalHit();
                hits++;
            }
            if (hits > 0) Rebuild.Stats.StatsManager.Instance?.RecordPickaxeSpawned();
        }

        static void TryCascadeSpawn(SkillTreeManager sk, UpgradeId id, ProjectileType target, Rock anchor, int nextDepth)
        {
            int lv = sk.GetLevel(id);
            if (lv <= 0 || anchor == null) return;
            if (!D100Helper.Roll(lv * CascadeChancePerLevel)) return;
            ProjectileLauncher.SpawnCascade(target, anchor, nextDepth);
        }

        static void TryCascadeEffect(SkillTreeManager sk, UpgradeId id, System.Action effect)
        {
            TryCascadeEffect(sk, id, CascadeChancePerLevel, effect);
        }

        static void TryCascadeEffect(SkillTreeManager sk, UpgradeId id, float chancePerLevel, System.Action effect)
        {
            int lv = sk.GetLevel(id);
            if (lv <= 0) return;
            if (!D100Helper.Roll(lv * chancePerLevel)) return;
            effect?.Invoke();
        }

        /// <summary>Pick any non-dying Rock — used when the original source rock died during
        /// our Animate so the cascade has something to anchor to. Returns null if the row is empty.</summary>
        static Rock FindLivingAnchor()
        {
            var rocks = Object.FindObjectsByType<Rock>(FindObjectsSortMode.None);
            if (rocks == null) return null;
            for (int i = 0; i < rocks.Length; i++)
            {
                if (rocks[i] != null && !rocks[i].IsDying) return rocks[i];
            }
            return null;
        }
    }
}
