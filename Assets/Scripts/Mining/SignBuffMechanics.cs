using System;
using System.Collections;
using UnityEngine;

namespace Rebuild.Mining
{
    public enum SignBuffKind { None, FasterMine, MoreMaterials, DoubleMaterialsChance, Mine3XFaster }









    public sealed class SignBuffMechanics : MonoBehaviour
    {
        public static SignBuffMechanics Instance { get; private set; }

        [SerializeField] float cycleSeconds = 300f;

        public SignBuffKind ActiveBuff { get; private set; }
        public float FasterMineFraction { get; private set; }
        public int MoreMaterialsAmount { get; private set; }
        public float DoubleMaterialsChance { get; private set; }
        public float Mine3XFasterChance { get; private set; }

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
