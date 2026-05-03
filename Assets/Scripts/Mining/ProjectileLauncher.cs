using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Rebuild.Mining
{
    /// <summary>+ central click→spawn helper.
    /// <see cref="Rock.OnPointerClick"/> hands the source rock here; we roll each
    /// projectile's SkillTree chance and spawn one short-lived AOE per success.
    /// Multiple types can fire on the same click — each rolls independently.
    /// Hover-channel calls this every tick, so a long hover could flood the scene.
    /// <see cref="MaxSimultaneous"/> caps per-type
    /// activity using a counter that ProjectileFx decrements on destroy / pool release.
    /// <see cref="SpawnCascade"/> and a single shared pool keep cascade chains (Beam→Dyna,
    /// Dyna→Beam, etc.) don't churn the GC. Pool parent is created lazily under MiningSessionRoot
    /// so it persists across sessions.
    /// <para><b>type별 chance per level 분리.</b> 원본 매칭:
    /// • Beam = 10%/lv (per-sec 의 BeamS 유지 — Shell 은 per-hit 으로 통합)
    /// • Plazma = 4%/lv (원본 plazmaBallChance 4/5)
    /// • Dynamite = 0.1%/lv (원본 dynamiteStickChance 0.1/0.2 per-hit)
    /// • Lightning = 0.07%/lv (원본 lightningTriggerChancePH 0.07/0.08 per-hit)</para></summary>
    public static class ProjectileLauncher
    {
        // per-type chance per level. 원본 SkillTree.cs / LocalizationScript.cs
        // 의 trigger chance 모델 매칭. 인덱스는 ProjectileType enum 과 동일 순서.
        static readonly float[] ChancePerLevelByType = new float[ProjectileDef.Count]
        {
            0.10f,    // BeamOfLight — 원본 BeamS 10% per-sec, Shell per-hit 통합
            0.04f,    // PlazmaBall  — 원본 plazmaBallChance 4f
            0.001f,   // Dynamite    — 원본 dynamiteStickChance 0.1% per-hit
            0.0007f,  // ElectricitySplash (Lightning PH) — 원본 lightningTriggerChancePH 0.07%
        };

        // Per-type concurrent spawn caps raised for cascade headroom. With 4-tier
        // base chance × cascade multipliers a hover can easily hit ~15 alive per type.
        static readonly int[] MaxSimultaneous = new int[ProjectileDef.Count]
        {
            24, // BeamOfLight: cascade Beam→Beam + Dyna→Beam can re-fire
            20, // PlazmaBall
            20, // Dynamite: many cascade triggers land here
            24, // ElectricitySplash: Beam→Splash adds another vector
        };

        // Live counters: each ProjectileFx.Initialize bumps, OnDestroy / pool release decrements.
        static readonly int[] s_alive = new int[ProjectileDef.Count];

        // ---- Projectile pool ----
        const int PoolPrewarm = 30;
        const int PoolMax = 80;
        static readonly Stack<ProjectileFx> s_pool = new Stack<ProjectileFx>(PoolPrewarm);
        static Transform s_poolParent;

        public static int Alive(ProjectileType t) => s_alive[(int)t];
        public static void OnProjectileSpawned(ProjectileType t) { s_alive[(int)t]++; }
        public static void OnProjectileDespawned(ProjectileType t)
        {
            int i = (int)t;
            s_alive[i]--;
            if (s_alive[i] < 0) s_alive[i] = 0;
        }

        /// <summary>Reset counters — called by MiningSessionController.ApplyEnterSession / ExitSession
        /// so a stale projectile from a previous session can't permanently block a slot.</summary>
        public static void ResetCounters()
        {
            for (int i = 0; i < s_alive.Length; i++) s_alive[i] = 0;
        }

        public static int TrySpawnFromClick(Rock source)
        {
            if (source == null) return 0;
            var st = SkillTreeManager.Instance;
            if (st == null) return 0;

            // AllProjectileTrigger 원본 allProjcetileTriggerChance=15f (additive, single-level).
            // 매칭: lv>0 시 +15% additive multiplier (원본은 +current chance × 15%, 결과적으로 ×1.15 매칭).
            float globalMult = st.GetLevel(UpgradeId.AllProjectileTrigger) > 0 ? 1.15f : 1f;

            int spawned = 0;
            for (int i = 0; i < ProjectileDef.Count; i++)
            {
                var info = ProjectileDef.Get((ProjectileType)i);
                int lv = st.GetLevel(info.ChanceUpgradeId);
                if (lv <= 0) continue;
                // Cap guard — skip the roll entirely if we're already at the limit for this type.
                if (s_alive[i] >= MaxSimultaneous[i]) continue;
                // type 별 chance per level 분리. 원본 sub-percent matching.
                float chance = lv * ChancePerLevelByType[i] * globalMult;
                // D100 — +0.5pp bump. Loop counts as 4 dice (one per ProjectileType)
                // so this is the natural count site.
                if (D100Helper.Roll(chance))
                {
                    if (SpawnAtRock(info.Type, source, cascadeDepth: 0))
                    {
                        spawned++;
                        // per-type breakdown stat (also bumps the umbrella counter).
                        Rebuild.Stats.StatsManager.Instance?.RecordProjectileFired(info.Type, 1);
                    }
                }
            }
            return spawned;
        }

        /// <summary>Cascade entry point called by <see cref="ProjectileFx.TryCascade"/>.
        /// Honors per-type cap; the depth guard lives inside ProjectileFx itself.</summary>
        public static bool SpawnCascade(ProjectileType type, Rock anchor, int cascadeDepth)
        {
            if (anchor == null || anchor.IsDying) return false;
            if (cascadeDepth > ProjectileFx.MaxCascadeDepth) return false;
            if (s_alive[(int)type] >= MaxSimultaneous[(int)type]) return false;
            return SpawnAtRock(type, anchor, cascadeDepth);
        }

        static bool SpawnAtRock(ProjectileType type, Rock source, int cascadeDepth)
        {
            var srcRt = source.transform as RectTransform;
            if (srcRt == null) return false;
            var parent = srcRt.parent;
            if (parent == null) return false;

            var fx = GetFromPool();
            if (fx == null) return false;
            var go = fx.gameObject;
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = srcRt.anchoredPosition;
            rt.localScale = Vector3.one;

            // Sibling order: render projectile ABOVE the rock so the AOE flash is visible
            // (rocks render in spawn order; new projectile becomes the last sibling = topmost).
            go.transform.SetAsLastSibling();
            go.SetActive(true);

            fx.ResetForReuse();
            fx.Initialize(type, source, cascadeDepth);
            // per-type SFX. Beam handled by OverlappingSounds.PlayBeam in
            // ProjectileFx.Initialize already; Dynamite/Lightning/PlazmaBall fire here once per
            // spawn (cascade re-spawns also trigger the SFX — matches original ProjectileMechanics).
            switch (type)
            {
                case ProjectileType.Dynamite:
                    Rebuild.Audio.AudioManager.Instance?.Play("Dynamite"); break;
                case ProjectileType.ElectricitySplash:
                    Rebuild.Audio.AudioManager.Instance?.Play("Lightning"); break;
                case ProjectileType.PlazmaBall:
                    Rebuild.Audio.AudioManager.Instance?.Play("PlazmaBall"); break;
            }
            return true;
        }

        // ---- Pool internals ----
        static void EnsurePoolParent()
        {
            if (s_poolParent != null) return;
            var holder = new GameObject("ProjectilePool", typeof(RectTransform));
            // Live under MiningSessionRoot so the pool persists across session enter/exit.
            // Falls back to scene root if the session controller has not been created yet.
            var session = MiningSessionController.Instance;
            if (session != null) holder.transform.SetParent(session.transform, false);
            s_poolParent = holder.transform;
        }

        static ProjectileFx CreatePooledInstance()
        {
            EnsurePoolParent();
            var go = new GameObject("Projectile_Pooled",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(s_poolParent, false);
            go.SetActive(false);
            var fx = go.AddComponent<ProjectileFx>();
            return fx;
        }

        static ProjectileFx GetFromPool()
        {
            // Drop stale entries (scene reload can leave nulled references in the stack).
            while (s_pool.Count > 0)
            {
                var top = s_pool.Pop();
                if (top != null) return top;
            }
            return CreatePooledInstance();
        }

        public static void Release(ProjectileFx fx)
        {
            if (fx == null) return;
            EnsurePoolParent();
            var go = fx.gameObject;
            // Park the instance back under the pool parent and deactivate it. Reuse the same
            // ProjectileFx component on the next Get so its serialized state is consistent.
            go.SetActive(false);
            go.transform.SetParent(s_poolParent, false);
            if (s_pool.Count < PoolMax) s_pool.Push(fx);
            else Object.Destroy(go);
        }
    }
}
