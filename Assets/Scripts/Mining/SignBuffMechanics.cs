using System;
using System.Collections;
using UnityEngine;

namespace Rebuild.Mining
{
    public enum SignBuffKind { None, FasterMine, MoreMaterials, DoubleMaterialsChance, Mine3XFaster }

    /// <summary>original ItsASign talent: a sign next to TheMine cycles through 4
    /// random buffs every 300s (5 minutes). MineManager reads the active buff to tweak its tick
    /// interval and bar yield. Re-rolls avoid picking the previous buff so back-to-back buffs
    /// always shift the gameplay.
    /// <para>Singleton lives on MiningSessionRoot — activated when the player owns the ItsASign
    /// talent; cycle persists across sessions (idle TheMine production benefits even outside
    /// mining sessions).</para>
    /// </summary>
    public sealed class SignBuffMechanics : MonoBehaviour
    {
        public static SignBuffMechanics Instance { get; private set; }

        [SerializeField] float cycleSeconds = 300f; // 5 minutes (original ChangeSignBuff wait)

        public SignBuffKind ActiveBuff { get; private set; }
        public float FasterMineFraction { get; private set; }    // 0.04 ~ 0.07 (mineTime ×= 1-this)
        public int MoreMaterialsAmount { get; private set; }     // 4 ~ 8 (flat +N bars per tick)
        public float DoubleMaterialsChance { get; private set; } // 4 ~ 7 (% probability per tick)
        public float Mine3XFasterChance { get; private set; }    // 4 ~ 7 (% probability per tick)

        public event Action OnBuffChanged;

        Coroutine _cycleRoutine;
        int _lastPick;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>Called by <see cref="MiningSessionController"/> at session start so the
        /// cycle restarts cleanly (and ItsASign-less sessions don't wake the timer).</summary>
        public void RefreshForSession()
        {
            bool talentOn = LevelMechanics.Instance != null
                && LevelMechanics.Instance.HasTalent(TalentId.ItsASign);
            if (!talentOn)
            {
                Deactivate();
                return;
            }
            if (_cycleRoutine == null) _cycleRoutine = StartCoroutine(CycleLoop());
        }

        public void Deactivate()
        {
            if (_cycleRoutine != null) { StopCoroutine(_cycleRoutine); _cycleRoutine = null; }
            ResetBuff();
            OnBuffChanged?.Invoke();
        }

        void ResetBuff()
        {
            ActiveBuff = SignBuffKind.None;
            FasterMineFraction = 0f;
            MoreMaterialsAmount = 0;
            DoubleMaterialsChance = 0f;
            Mine3XFasterChance = 0f;
        }

        IEnumerator CycleLoop()
        {
            // Roll once immediately so the talent feels instant on first acquisition.
            RollNextBuff();
            while (true)
            {
                yield return new WaitForSeconds(cycleSeconds);
                RollNextBuff();
            }
        }

        void RollNextBuff()
        {
            ResetBuff();
            int pick;
            // Avoid back-to-back same buff (matches original `while (signBuff == num)`).
            int safety = 8;
            do
            {
                pick = UnityEngine.Random.Range(1, 5);
                if (--safety <= 0) break;
            } while (pick == _lastPick);
            _lastPick = pick;
            switch (pick)
            {
                case 1:
                    ActiveBuff = SignBuffKind.FasterMine;
                    FasterMineFraction = UnityEngine.Random.Range(0.04f, 0.07f);
                    break;
                case 2:
                    ActiveBuff = SignBuffKind.MoreMaterials;
                    MoreMaterialsAmount = UnityEngine.Random.Range(4, 9);
                    break;
                case 3:
                    ActiveBuff = SignBuffKind.DoubleMaterialsChance;
                    DoubleMaterialsChance = UnityEngine.Random.Range(4f, 8f);
                    break;
                case 4:
                    ActiveBuff = SignBuffKind.Mine3XFaster;
                    Mine3XFasterChance = UnityEngine.Random.Range(4f, 8f);
                    break;
            }
            OnBuffChanged?.Invoke();
        }
    }
}
