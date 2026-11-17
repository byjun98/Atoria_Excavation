using System;
using System.Collections.Generic;
using Rebuild.Saving;
using UnityEngine;

namespace Rebuild.Mining
{





    public sealed class LevelMechanics : MonoBehaviour, Rebuild.Saving.IDataPersistence
    {
        public static LevelMechanics Instance { get; private set; }

        [Header("XP curve")]
        [SerializeField] int baseXpToNextLevel = 100;
        [SerializeField] int xpPerRockBase = 3;


        [SerializeField] int currentXp = 0;
        [SerializeField] int level = 1;
        [SerializeField] int talentPoints = 0;
        [SerializeField] int nextTalentPrice = 1;
        [SerializeField] bool[] _chosen = new bool[TalentDef.Count];


        int _xpThisSession;
        int _levelsGainedThisSession;
        int _pointsGainedThisSession;

        public int Level => level;
        public int CurrentXp => currentXp;


        public int XpToNextLevel
        {
            get
            {
                float xp = baseXpToNextLevel;
                for (int nextLevel = 2; nextLevel <= level; nextLevel++)
                    xp *= OriginalLevelMultiplier(nextLevel);
                return Mathf.Max(1, Mathf.RoundToInt(xp));
            }
        }
        public int TalentPointsAvailable => talentPoints;
        public int NextTalentPrice => Mathf.Max(1, nextTalentPrice);
        public bool CanChooseTalent => talentPoints >= NextTalentPrice;
        public int XpThisSession => _xpThisSession;
        public int LevelsGainedThisSession => _levelsGainedThisSession;
        public int PointsGainedThisSession => _pointsGainedThisSession;

        public event Action<int, int> OnXpChanged;
        public event Action<int, int> OnLevelUp;
        public event Action<TalentId> OnTalentChosen;

        void Awake()
        {
            if (_chosen == null || _chosen.Length != TalentDef.Count) _chosen = new bool[TalentDef.Count];
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public bool HasTalent(TalentId id) => _chosen[(int)id];



        static int TalentXlevelPeriodFor(int xlv)
        {
            switch (xlv)
            {
                case 1: return 15;
                case 2: return 10;
                case 3: return 5;
                default: return 0;
            }
        }

        static float OriginalLevelMultiplier(int reachedLevel)
        {
            float mul = 1.7f;
            if (reachedLevel > 2)
            {
                for (int i = 0; i < reachedLevel - 1; i++)
                {
                    mul -= 0.024f;
                    if (mul < 1.075f) mul = 1.075f;
                }
            }
            return mul;
        }



        static readonly float[] s_moreXpSteps = { 0.02f, 0.08f, 0.36f, 0.24f, 0.54f, 0.8f, 1.1f, 2.5f };
        public static float GetMoreXpBonus(int level)
        {
            if (level <= 0) return 0f;
            float total = 0f;
            int max = Mathf.Min(level, s_moreXpSteps.Length);
            for (int i = 0; i < max; i++) total += s_moreXpSteps[i];
            return total;
        }

        public int XpPerRock
        {
            get
            {
                int xp = xpPerRockBase;
                if (SkillTreeManager.Instance != null)
                    xp += Mathf.RoundToInt(GetMoreXpBonus(SkillTreeManager.Instance.GetLevel(UpgradeId.MoreXP)) * 100f);
                xp = Mathf.Max(1, xp);
                return HasTalent(TalentId.MoreXP) ? xp * 2 : xp;
            }
        }

        public void GiveXP(int amount)
        {
            if (amount <= 0) return;






            if (HasTalent(TalentId.SpringSeason) && FlowerMechanics.Instance != null)
            {
                int budget = FlowerMechanics.Instance.SessionFlowerBudget;
                if (budget > 0)
                {
                    float mul = 1f + 0.015f * budget;
                    if (PotionBuffMechanics.Instance != null && PotionBuffMechanics.Instance.IsPotionXpActive)
                        mul += PotionBuffMechanics.Instance.PotionXpIncrease;
                    amount = Mathf.RoundToInt(amount * mul);
                }
            }
            else if (PotionBuffMechanics.Instance != null && PotionBuffMechanics.Instance.IsPotionXpActive)
            {

                amount = Mathf.RoundToInt(amount * (1f + PotionBuffMechanics.Instance.PotionXpIncrease));
            }






            if (ArtifactsManager.Instance != null
                && ArtifactsManager.Instance.HasArtifact(ArtifactId.PurpleRing))
            {
                float archeoMult = HasTalent(TalentId.Archaeologist) ? 1.08f : 1f;
                float chance = 0.04f * ArtifactsManager.Instance.RuneMultiplier * archeoMult;
                if (D100Helper.Roll(chance))
                {
                    amount *= 2;

                    Rebuild.Stats.StatsManager.Instance?.RecordXpDoubled();
                }
            }
            currentXp += amount;
            _xpThisSession += amount;

            Rebuild.Stats.StatsManager.Instance?.RecordXpGained(amount);
            if (currentXp >= XpToNextLevel)
            {

                currentXp = 0;
                level++;
                talentPoints++;
                _levelsGainedThisSession++;
                _pointsGainedThisSession++;

                if (ArtifactsManager.Instance != null
                    && ArtifactsManager.Instance.HasArtifact(ArtifactId.Book)
                    && level % 5 == 0)
                {
                    talentPoints++;
                    _pointsGainedThisSession++;
                }




                if (SkillTreeManager.Instance != null)
                {
                    int xlv = SkillTreeManager.Instance.GetLevel(UpgradeId.TalentPointsPerXlevel);
                    int period = TalentXlevelPeriodFor(xlv);
                    if (period > 0 && level % period == 0)
                    {
                        talentPoints++;
                        _pointsGainedThisSession++;
                    }
                }

                Rebuild.Stats.StatsManager.Instance?.RecordLevelReached(level);

                Rebuild.Stats.StatsManager.Instance?.RecordLevelUp();

                Rebuild.Audio.AudioManager.Instance?.Play("LevelUp");
                OnLevelUp?.Invoke(level, talentPoints);
            }
            OnXpChanged?.Invoke(currentXp, XpToNextLevel);
        }

        public List<TalentId> GetUnchosenTalents()
        {
            var list = new List<TalentId>(TalentDef.Count);
            for (int i = 0; i < TalentDef.Count; i++)
                if (!_chosen[i]) list.Add((TalentId)i);
            return list;
        }

        public bool HasUnchosenTalents()
        {
            for (int i = 0; i < TalentDef.Count; i++) if (!_chosen[i]) return true;
            return false;
        }

        int CountChosenTalents()
        {
            int count = 0;
            for (int i = 0; i < _chosen.Length; i++)
                if (_chosen[i]) count++;
            return count;
        }

        public void ChooseTalent(TalentId id)
        {
            int idx = (int)id;
            if (_chosen[idx]) return;
            int price = NextTalentPrice;
            if (talentPoints < price) return;
            _chosen[idx] = true;
            talentPoints -= price;
            nextTalentPrice = price + 1;
            OnTalentChosen?.Invoke(id);
        }


        public void ResetSession()
        {
            _xpThisSession = 0;
            _levelsGainedThisSession = 0;
            _pointsGainedThisSession = 0;
            OnXpChanged?.Invoke(currentXp, XpToNextLevel);
        }


        public void ResetAll()
        {
            currentXp = 0;
            level = 1;
            talentPoints = 0;
            nextTalentPrice = 1;
            _xpThisSession = 0;
            _levelsGainedThisSession = 0;
            _pointsGainedThisSession = 0;
            for (int i = 0; i < _chosen.Length; i++) _chosen[i] = false;
            OnXpChanged?.Invoke(0, XpToNextLevel);
        }


        public void LoadData(GameDataDTO data)
        {
            if (data == null) return;
            level = Mathf.Max(1, data.level);
            currentXp = Mathf.Max(0, data.currentXp);
            talentPoints = Mathf.Max(0, data.talentPoints);
            nextTalentPrice = Mathf.Max(1, data.nextTalentPrice);
            if (_chosen == null || _chosen.Length != TalentDef.Count) _chosen = new bool[TalentDef.Count];
            if (data.talentsChosen != null)
            {
                int n = Mathf.Min(_chosen.Length, data.talentsChosen.Length);
                for (int i = 0; i < _chosen.Length; i++)
                    _chosen[i] = i < n && data.talentsChosen[i];
            }
            else
            {
                for (int i = 0; i < _chosen.Length; i++) _chosen[i] = false;
            }
            if (data.nextTalentPrice <= 0)
                nextTalentPrice = Mathf.Max(1, CountChosenTalents() + 1);

            OnXpChanged?.Invoke(currentXp, XpToNextLevel);
            for (int i = 0; i < _chosen.Length; i++)
                if (_chosen[i]) OnTalentChosen?.Invoke((TalentId)i);
        }

        public void SaveData(GameDataDTO data)
        {
            if (data == null) return;
            data.level = level;
            data.currentXp = currentXp;
            data.talentPoints = talentPoints;
            data.nextTalentPrice = NextTalentPrice;
            if (data.talentsChosen == null || data.talentsChosen.Length != _chosen.Length)
                data.talentsChosen = new bool[_chosen.Length];
            for (int i = 0; i < _chosen.Length; i++) data.talentsChosen[i] = _chosen[i];
        }
    }
}
