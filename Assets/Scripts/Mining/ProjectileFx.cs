using System.Collections;
using System.Collections.Generic;
using System.Buffers;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.UI;

namespace Rebuild.Mining
{

































    [RequireComponent(typeof(Image), typeof(RectTransform))]
    public sealed class ProjectileFx : MonoBehaviour
    {
        public const int MaxCascadeDepth = 2;
        const float CascadeChancePerLevel = 0.10f;
        const float ProjectileAddTimeChance = 0.008f;
        const float BeamSpawnRockChance = 0.24f;
        const float DynamiteSpawnRockChance = 0.21f;
        static readonly ProfilerMarker s_queryMarker = new ProfilerMarker("Atoria.Projectile.Query");
        static readonly ProfilerMarker s_sortMarker = new ProfilerMarker("Atoria.Projectile.Sort");
        static readonly ProfilerMarker s_damageMarker = new ProfilerMarker("Atoria.Projectile.Damage");
        static readonly Dictionary<Rock, float> s_hitDistances = new Dictionary<Rock, float>(128);
        static readonly IComparer<Rock> s_hitComparer = Comparer<Rock>.Create(CompareHitDistance);

        Image _img;
        RectTransform _rt;
        ProjectileType _type;
        bool _counted;
        bool _active;
        int _cascadeDepth;
        Rock _sourceRock;
        PlasmaChainVisual _plasmaVisual;
        ProjectileBurstVisual _burstVisual;


        bool _beamIsSVariant;




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


            if (_counted) ProjectileLauncher.OnProjectileDespawned(_type);
            if (_plasmaVisual != null) Destroy(_plasmaVisual.gameObject);
            if (_burstVisual != null) Destroy(_burstVisual.gameObject);
        }

        void OnDisable()
        {
            if (_plasmaVisual != null) _plasmaVisual.Clear();
            if (_burstVisual != null) _burstVisual.gameObject.SetActive(false);
        }




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



        public void ResetForReuse()
        {
            _counted = false;
            _active = false;
            _cascadeDepth = 0;
            _sourceRock = null;
            _beamIsSVariant = false;
            if (_plasmaVisual != null) _plasmaVisual.Clear();
            if (_burstVisual != null) _burstVisual.gameObject.SetActive(false);
            if (_img != null) _img.enabled = true;

            transform.localRotation = Quaternion.identity;

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

            _beamIsSVariant = (type == ProjectileType.BeamOfLight) && UnityEngine.Random.value < 0.5f;
            ProjectileLauncher.OnProjectileSpawned(type);


            var info = ApplyStatMods(ProjectileDef.Get(type));

            _img.sprite = ProjectileSpriteFactory.Get(type);


            var mainColor = (type == ProjectileType.BeamOfLight && _beamIsSVariant)
                ? new Color(0.30f, 0.95f, 1.00f, 0.95f)
                : info.Tint;
            _img.color = mainColor;
            _img.raycastTarget = false;
            _img.preserveAspect = false;
            _rt.sizeDelta = info.Size;
            _rt.localScale = Vector3.one * 0.4f;
            _rt.localRotation = Quaternion.identity;

            EnsureChildLayers();
            SetupLayers(type, info, mainColor);

            var board = _rt.parent.parent as RectTransform;
            float sizeScale = info.Size.x / ProjectileDef.Get(type).Size.x;
            bool isLightning = type == ProjectileType.ElectricitySplash;
            bool usingChainVisual = (type == ProjectileType.PlazmaBall || isLightning)
                && PlasmaChainVisual.TryBegin(board, _rt.position, sizeScale, isLightning, ref _plasmaVisual);

            float angle = type == ProjectileType.ElectricitySplash
                ? UnityEngine.Random.Range(0f, 360f) * (28f / 360f) - 14f : 0f;
            bool usingBurstVisual = !usingChainVisual && ProjectileBurstVisual.TryBegin(board, _rt.position, type,
                _beamIsSVariant, sizeScale, angle, ref _burstVisual);
            if (usingChainVisual || usingBurstVisual)
            {
                _img.enabled = false;
                _img.raycastTarget = false;
                _rt.localScale = Vector3.one;
                if (_glowChild != null) _glowChild.gameObject.SetActive(false);
                if (_ringChild != null) _ringChild.gameObject.SetActive(false);
            }



            Rebuild.Audio.OverlappingSounds.Instance?.PlayBeam();



            if (type != ProjectileType.PlazmaBall)
                ApplyDamage(info, source, dmgMultiplier: 1f);

            StartCoroutine(Animate(info, usingChainVisual, usingBurstVisual));
        }




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


        static float GameplayLifetime(ProjectileDef.Info info)
        {
            switch (info.Type)
            {
                case ProjectileType.BeamOfLight: return Mathf.Max(info.Lifetime, 0.75f);
                case ProjectileType.Dynamite: return Mathf.Max(info.Lifetime, 0.90f);
                case ProjectileType.ElectricitySplash: return Mathf.Max(info.Lifetime, 0.50f);
                default: return info.Lifetime;
            }
        }

        static bool CanAffectMiningRound()
        {
            var session = MiningSessionController.Instance;
            return session != null && session.IsInSession && session.TimerRunning && !session.ResultsShown;
        }

        static int CompareHitDistance(Rock a, Rock b)
        {
            if (a == null && b == null) return 0;
            if (a == null) return 1;
            if (b == null) return -1;
            return s_hitDistances[a].CompareTo(s_hitDistances[b]);
        }

        internal static void SortHitCandidates(Rock[] rocks, Vector3 origin)
        {
            SortHitCandidateRange(rocks, rocks.Length, origin);
        }

        static void SortHitCandidateRange(Rock[] rocks, int count, Vector3 origin)
        {

            for (int i = 0; i < count; i++)
            {
                Rock rock = rocks[i];
                if (rock != null) s_hitDistances[rock] = (rock.transform.position - origin).sqrMagnitude;
            }
            System.Array.Sort(rocks, 0, count, s_hitComparer);
            s_hitDistances.Clear();
        }

        void ApplyDamage(ProjectileDef.Info info, Rock source, float dmgMultiplier)
        {
            using var damageSample = s_damageMarker.Auto();
            if (source == null || !CanAffectMiningRound()) return;
            Rock[] snapshot;
            using (s_queryMarker.Auto())
                snapshot = RockQuery.Snapshot();
            if (snapshot.Length == 0) return;

            int count = snapshot.Length;
            var rocks = ArrayPool<Rock>.Shared.Rent(count);
            System.Array.Copy(snapshot, rocks, count);
            try
            {
                using (s_sortMarker.Auto())
                    SortHitCandidateRange(rocks, count, source.transform.position);



                int dmg = Mathf.Max(1, Mathf.RoundToInt(info.Damage * dmgMultiplier));
                if (_type == ProjectileType.BeamOfLight && _beamIsSVariant) dmg *= 2;


                var sk = SkillTreeManager.Instance;
                if (sk != null)
                {
                    float doubleChance = sk.GetLevel(UpgradeId.AllProjectileDouble) * 0.05f;
                    if (doubleChance > 0f && D100Helper.Roll(doubleChance)) dmg *= 2;
                }

                var chain = _plasmaVisual;
                if (chain != null && !chain.isActiveAndEnabled) chain = null;
                if (chain != null) chain.BeginHit();
                int hit = 0;
                for (int i = 0; i < count && hit < info.MaxTargets; i++)
                {
                    var r = rocks[i];
                    if (r == null || r.IsDying) continue;
                    if (chain != null) chain.RecordHit(r.transform.position);
                    r.TakeDamage(dmg);
                    hit++;
                }
            }
            finally
            {
                System.Array.Clear(rocks, 0, count);
                ArrayPool<Rock>.Shared.Return(rocks);
            }
        }

        IEnumerator Animate(ProjectileDef.Info info, bool usingChainVisual, bool usingBurstVisual)
        {
            float life = Mathf.Max(0.05f, GameplayLifetime(info));
            var startScale = Vector3.one * 0.4f;
            var endScale = Vector3.one * 1.35f;
            var c0 = _img != null ? _img.color : Color.white;




            int plazmaTicksFired = 0;
            int plazmaTotalTicks = 5;
            float plazmaTickInterval = life / plazmaTotalTicks;
            float nextPlazmaTickAt = plazmaTickInterval * 0.4f;

            float t = 0f;
            while (t < life && CanAffectMiningRound())
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / life);


                if (!usingBurstVisual && !usingChainVisual)
                {
                    ApplyPerTypeAnim(k, startScale, endScale);

                    UpdateLayerAnim(k);


                    var c = c0; c.a = c0.a * (1f - k * 0.85f);
                    _img.color = c;
                }


                if (_type == ProjectileType.PlazmaBall && _sourceRock != null
                    && plazmaTicksFired < plazmaTotalTicks && t >= nextPlazmaTickAt)
                {
                    plazmaTicksFired++;

                    ApplyDamage(info, _sourceRock, dmgMultiplier: 0.3f);
                    nextPlazmaTickAt = plazmaTickInterval * 0.4f + plazmaTickInterval * plazmaTicksFired;
                }
                if (usingChainVisual && _plasmaVisual != null)
                    _plasmaVisual.Advance(t, life);
                if (usingBurstVisual && _burstVisual != null)
                    _burstVisual.Advance(t);

                yield return null;
            }


            if (CanAffectMiningRound())
            {
                TryCascade();
                TryOriginalSpawnRockBonus();
            }


            if (_counted) { ProjectileLauncher.OnProjectileDespawned(_type); _counted = false; }
            _active = false;
            ProjectileLauncher.Release(this);
        }




        void SetupLayers(ProjectileType type, ProjectileDef.Info info, Color mainColor)
        {

            var glowSprite = ProjectileSpriteFactory.GetGlow();
            if (_glowChild != null && glowSprite != null)
            {
                _glowChild.sprite = glowSprite;
                var glowColor = mainColor;
                glowColor.a = 0.55f;
                _glowChild.color = glowColor;

                _glowChildRt.sizeDelta = info.Size * 1.7f;
                _glowChildRt.localScale = Vector3.one;
                _glowChild.gameObject.SetActive(true);
            }
            else if (_glowChild != null)
            {
                _glowChild.gameObject.SetActive(false);
            }


            bool wantsRing = type == ProjectileType.Dynamite;
            var ringSprite = ProjectileSpriteFactory.GetRing();
            if (_ringChild != null && wantsRing && ringSprite != null)
            {
                _ringChild.sprite = ringSprite;
                var ringColor = new Color(1f, 0.85f, 0.45f, 0.95f);
                _ringChild.color = ringColor;
                _ringChildRt.sizeDelta = info.Size;
                _ringChildRt.localScale = Vector3.one * 0.6f;
                _ringChild.gameObject.SetActive(true);
            }
            else if (_ringChild != null)
            {
                _ringChild.gameObject.SetActive(false);
            }
        }



        void UpdateLayerAnim(float k)
        {


            if (_glowChild != null && _glowChild.gameObject.activeSelf)
            {
                float glowPulse = 1f + 0.15f * Mathf.Sin(k * Mathf.PI * 4f);
                _glowChildRt.localScale = Vector3.one * glowPulse;
                var c = _glowChild.color;
                c.a = Mathf.Lerp(0.55f, 0f, k * 0.9f);
                _glowChild.color = c;
            }


            if (_ringChild != null && _ringChild.gameObject.activeSelf)
            {
                float ringScale = Mathf.Lerp(0.6f, 2.2f, Mathf.SmoothStep(0f, 1f, k));
                _ringChildRt.localScale = Vector3.one * ringScale;
                var c = _ringChild.color;
                c.a = Mathf.Clamp01(1f - k / 0.7f) * 0.95f;
                _ringChild.color = c;
            }
        }




        void ApplyPerTypeAnim(float k, Vector3 startScale, Vector3 endScale)
        {

            var scale = Vector3.Lerp(startScale, endScale, k);
            switch (_type)
            {
                case ProjectileType.Dynamite:
                {


                    float punch1 = Mathf.Exp(-Mathf.Pow((k - 0.30f) / 0.05f, 2f));
                    float punch2 = Mathf.Exp(-Mathf.Pow((k - 0.65f) / 0.06f, 2f));
                    float kick = 0.25f * (punch1 + punch2);
                    _rt.localScale = scale * (1f + kick);
                    break;
                }
                case ProjectileType.ElectricitySplash:
                {

                    _rt.localScale = scale;
                    _rt.localRotation = Quaternion.Euler(0f, 0f, k * 360f);
                    break;
                }
                case ProjectileType.PlazmaBall:
                {


                    float pulse = 1f + 0.08f * Mathf.Sin(k * Mathf.PI * 6f);
                    _rt.localScale = scale * pulse;
                    break;
                }
                case ProjectileType.BeamOfLight:
                {

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


        void TryCascade()
        {
            if (_cascadeDepth >= MaxCascadeDepth) return;
            var sk = SkillTreeManager.Instance;
            if (sk == null) return;
            int nextDepth = _cascadeDepth + 1;

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

                    TryCascadeSpawn(sk, UpgradeId.CascadeDynaToSplash, ProjectileType.ElectricitySplash, anchor, nextDepth);
                    break;
                case ProjectileType.PlazmaBall:
                    TryCascadeSpawn(sk, UpgradeId.CascadePlazmaToDyna,       ProjectileType.Dynamite,          anchor, nextDepth);

                    TryCascadeSpawn(sk, UpgradeId.CascadePlazmaToPlasmaBall, ProjectileType.PlazmaBall,        anchor, nextDepth);
                    TryCascadeSpawn(sk, UpgradeId.CascadePlazmaToSplash,     ProjectileType.ElectricitySplash, anchor, nextDepth);

                    TryPlazmaPickaxe(sk);
                    break;
                case ProjectileType.ElectricitySplash:

                    TryCascadeSpawn(sk, UpgradeId.CascadeLightningToDyna, ProjectileType.Dynamite, anchor, nextDepth);
                    break;
            }
        }



        static void TryPlazmaPickaxe(SkillTreeManager sk)
        {
            int lv = sk.GetLevel(UpgradeId.PlazmaSpawnPickaxe);
            if (lv <= 0) return;
            if (!D100Helper.Roll(lv * CascadeChancePerLevel)) return;
            var rocks = RockQuery.Snapshot();
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



        static Rock FindLivingAnchor()
        {
            var rocks = RockQuery.Snapshot();
            if (rocks == null) return null;
            for (int i = 0; i < rocks.Length; i++)
            {
                if (rocks[i] != null && !rocks[i].IsDying) return rocks[i];
            }
            return null;
        }
    }
}
