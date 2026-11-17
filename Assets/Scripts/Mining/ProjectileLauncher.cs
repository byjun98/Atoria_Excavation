using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Rebuild.Mining
{















    public static class ProjectileLauncher
    {


        static readonly float[] ChancePerLevelByType = new float[ProjectileDef.Count]
        {
            0.10f,
            0.04f,
            0.001f,
            0.0007f,
        };



        static readonly int[] MaxSimultaneous = new int[ProjectileDef.Count]
        {
            24,
            20,
            20,
            24,
        };


        static readonly int[] s_alive = new int[ProjectileDef.Count];


        const int InitialPoolCapacity = 30;
        const int PoolMax = 80;
        static readonly Stack<ProjectileFx> s_pool = new Stack<ProjectileFx>(InitialPoolCapacity);
        static Transform s_poolParent;

        public static int Alive(ProjectileType t) => s_alive[(int)t];
        public static void OnProjectileSpawned(ProjectileType t) { s_alive[(int)t]++; }
        public static void OnProjectileDespawned(ProjectileType t)
        {
            int i = (int)t;
            s_alive[i]--;
            if (s_alive[i] < 0) s_alive[i] = 0;
        }



        public static void ResetCounters()
        {
            for (int i = 0; i < s_alive.Length; i++) s_alive[i] = 0;
        }

        public static int TrySpawnFromClick(Rock source)
        {
            if (source == null) return 0;
            var st = SkillTreeManager.Instance;
            if (st == null) return 0;



            float globalMult = st.GetLevel(UpgradeId.AllProjectileTrigger) > 0 ? 1.15f : 1f;

            int spawned = 0;
            for (int i = 0; i < ProjectileDef.Count; i++)
            {
                var info = ProjectileDef.Get((ProjectileType)i);
                int lv = st.GetLevel(info.ChanceUpgradeId);
                if (lv <= 0) continue;

                if (s_alive[i] >= MaxSimultaneous[i]) continue;

                float chance = lv * ChancePerLevelByType[i] * globalMult;


                if (D100Helper.Roll(chance))
                {
                    if (SpawnAtRock(info.Type, source, cascadeDepth: 0))
                    {
                        spawned++;

                        Rebuild.Stats.StatsManager.Instance?.RecordProjectileFired(info.Type, 1);
                    }
                }
            }
            return spawned;
        }



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



            go.transform.SetAsLastSibling();
            go.SetActive(true);

            fx.ResetForReuse();
            fx.Initialize(type, source, cascadeDepth);



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


        internal static Transform PoolParent
        {
            get
            {
                EnsurePoolParent();
                return s_poolParent;
            }
        }

        static void EnsurePoolParent()
        {
            if (s_poolParent != null) return;
            var holder = new GameObject("MiningVfxPool", typeof(RectTransform));


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


            go.SetActive(false);
            go.transform.SetParent(s_poolParent, false);
            if (s_pool.Count < PoolMax) s_pool.Push(fx);
            else Object.Destroy(go);
        }
    }
}
