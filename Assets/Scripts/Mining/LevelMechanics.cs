using System;
using System.Collections.Generic;
using Rebuild.Saving;
using UnityEngine;

namespace Rebuild.Mining
{
    /// <summary>XP / Level / Talent system.
    /// Singleton on MiningSessionRoot. Per-rock XP accumulates session-wide; reaching
    /// the threshold awards a talentPoint. Talent picker UI consumes points one at a time.
    /// Chosen talents are queried by other managers via <see cref="HasTalent"/>.
    /// persists <c>level</c>, <c>currentXp</c>, <c>talentPoints</c>, <c>_chosen</c>.</summary>
    public sealed class LevelMechanics : MonoBehaviour, Rebuild.Saving.IDataPersistence
    {
        public static LevelMechanics Instance { get; private set; }

        [Header("XP curve")]
        [SerializeField] int baseXpToNextLevel = 100;
        [SerializeField] int xpPerRockBase = 3;

        // Persistent state (will save).
        [SerializeField] int currentXp = 0;
        [SerializeField] int level = 1;
        [SerializeField] int talentPoints = 0;
        [SerializeField] int nextTalentPrice = 1;
        [SerializeField] bool[] _chosen = new bool[TalentDef.Count];

        // Per-session telemetry shown on the results screen.
        int _xpThisSession;
        int _levelsGainedThisSession;
        int _pointsGainedThisSession;

        public int Level => level;
        public int CurrentXp => currentXp;
        // Original stores XP as floats but displays XP * 100. Keep integer centi-XP here:
        // base xpFromRock=0.03 -> 3, xpNeedForNextLvl=1.0 -> 100.
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

        public event Action<int, int> OnXpChanged;          // (currentXp, xpToNextLevel)
        public event Action<int, int> OnLevelUp;             // (newLevel, totalTalentPoints)
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

        /// <summary>TalentPointsPerXlevel SkillTree node. Each purchase tier
        /// shrinks the bonus-talent interval (lv1=15, lv2=10, lv3=5). Level 0 = no bonus.</summary>
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

        /// <summary>Original XP increase value is added to xpFromRock directly:
        /// base 0.03, max 5.67. Returned value is still in original float-XP units.</summary>
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
            // SpringSeason XP multiplier. Original LevelMechanics.GiveXP (line 566-616)
            // multiplies the awarded XP by `1 + 0.015 × flowersOnScreen` (flowersOnScreen is a
            // per-session constant rolled in Random.Range(2, 18), NOT a live alive-count). Applied
            // BEFORE the PurpleRing double so the artifact still doubles the post-SpringSeason xp.
            // PotionXp adds to the SpringSeason multiplier (mirrors original
            // num3 += potionXp_increase). Stacks additively, not multiplicatively.
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
                // SpringSeason 없을 때도 PotionXp 단독으로 곱셈 적용.
                amount = Mathf.RoundToInt(amount * (1f + PotionBuffMechanics.Instance.PotionXpIncrease));
            }
            // PurpleRing artifact: 4% chance to double the awarded XP for this call.
            // 원본 LevelMechanics.cs:606-613 의 multiplier chain 복원:
            //   `if (Random < purpleRingChance × (1 + archeologistIncrease + runeImproveAll)) xp *= 2`
            // Rune 보유 시 4.2%, 미보유 시 4%.
            // Archaeologist multi-site multiplier 복원. PurpleRing chance
            // 가 Archaeologist 보유 시 추가 ×1.08 (원본 archeologistIncreaseDISPLAY=0.08).
            if (ArtifactsManager.Instance != null
                && ArtifactsManager.Instance.HasArtifact(ArtifactId.PurpleRing))
            {
                float archeoMult = HasTalent(TalentId.Archaeologist) ? 1.08f : 1f;
                float chance = 0.04f * ArtifactsManager.Instance.RuneMultiplier * archeoMult;
                if (D100Helper.Roll(chance))
                {
                    amount *= 2;
                    // stat — count PurpleRing procs separately from generic XpGained.
                    Rebuild.Stats.StatsManager.Instance?.RecordXpDoubled();
                }
            }
            currentXp += amount;
            _xpThisSession += amount;
            // stat — lifetime XP accumulated (after the PurpleRing double).
            Rebuild.Stats.StatsManager.Instance?.RecordXpGained(amount);
            if (currentXp >= XpToNextLevel)
            {
                // Original drops overflow instead of chaining multiple level-ups from one rock.
                currentXp = 0;
                level++;
                talentPoints++;
                _levelsGainedThisSession++;
                _pointsGainedThisSession++;
                // Book artifact: every 5 levels grants one bonus talent point on top.
                if (ArtifactsManager.Instance != null
                    && ArtifactsManager.Instance.HasArtifact(ArtifactId.Book)
                    && level % 5 == 0)
                {
                    talentPoints++;
                    _pointsGainedThisSession++;
                }
                // TalentPointsPerXlevel SkillTree node. 3 levels purchased = LV X마다 +1
                // talent (lv1=15, lv2=10, lv3=5). Original game's TalentPointsPerXlevel pattern.
                // Stacks with the Book artifact (Book +5lv bonus fires independently — both can hit
                // on the same level-up).
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
                // feed the highest-ever level into the stat panel + achievement watcher.
                Rebuild.Stats.StatsManager.Instance?.RecordLevelReached(level);
                // cumulative level-ups (distinct from the peak MaxLevelReached).
                Rebuild.Stats.StatsManager.Instance?.RecordLevelUp();
                // LevelUp SFX (silent fallback when clip not wired).
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

        /// <summary>Called by MiningSessionController on every new StartSession.</summary>
        public void ResetSession()
        {
            _xpThisSession = 0;
            _levelsGainedThisSession = 0;
            _pointsGainedThisSession = 0;
            OnXpChanged?.Invoke(currentXp, XpToNextLevel);
        }

        /// <summary>Debug hard reset.</summary>
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

        // ---- IDataPersistence ----
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
            // Notify subscribers so any UI driven by these fields refreshes immediately.
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
