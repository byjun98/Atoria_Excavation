using UnityEngine;

namespace Rebuild.Mining
{










    public sealed class PotionBuffMechanics : MonoBehaviour
    {
        public static PotionBuffMechanics Instance { get; private set; }

        public bool IsPotionXpActive { get; private set; }
        public float PotionXpIncrease { get; private set; }
        public float PotionDoubleChanceIncrease { get; private set; }
        public int PotionMaterialWorthMore { get; private set; }
        public float PotionPickaxeStats { get; private set; }

        public bool IsActiveThisSession =>
            IsPotionXpActive || PotionDoubleChanceIncrease > 0f
            || PotionMaterialWorthMore > 0 || PotionPickaxeStats > 0f;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }




        public void RollForSession()
        {
            ResetForSession();
            if (LevelMechanics.Instance == null) return;
            bool drinker = LevelMechanics.Instance.HasTalent(TalentId.PotionDrinker);
            bool chugger = LevelMechanics.Instance.HasTalent(TalentId.PotionChugger);
            if (!drinker && !chugger) return;
            if (chugger)
            {
                ActivateXp();
                ActivateDoubleChance();
                ActivateMaterialWorth();
                ActivatePickaxeStats();
                if (drinker)
                {
                    switch (UnityEngine.Random.Range(0, 4))
                    {
                        case 0: PotionXpIncrease *= 1.25f; break;
                        case 1: PotionDoubleChanceIncrease *= 1.25f; break;
                        case 2: PotionMaterialWorthMore++; break;
                        case 3: PotionPickaxeStats *= 1.25f; break;
                    }
                }
            }
            else
            {

                int pick = UnityEngine.Random.Range(1, 5);
                if (pick == 1) ActivateMaterialWorth();
                if (pick == 2) ActivateXp();
                if (pick == 3) ActivatePickaxeStats();
                if (pick == 4) ActivateDoubleChance();
            }


            Rebuild.Stats.StatsManager.Instance?.RecordPotion();
        }

        public void ResetForSession()
        {
            IsPotionXpActive = false;
            PotionXpIncrease = 0f;
            PotionDoubleChanceIncrease = 0f;
            PotionMaterialWorthMore = 0;
            PotionPickaxeStats = 0f;
        }

        void ActivateXp()
        {
            IsPotionXpActive = true;
            PotionXpIncrease = UnityEngine.Random.Range(0.13f, 0.21f);
        }

        void ActivateDoubleChance()
        {

            PotionDoubleChanceIncrease = 5f;
        }

        void ActivateMaterialWorth()
        {

            PotionMaterialWorthMore = UnityEngine.Random.Range(2, 6);
        }

        void ActivatePickaxeStats()
        {


            PotionPickaxeStats = 0.05f;
        }
    }
}
