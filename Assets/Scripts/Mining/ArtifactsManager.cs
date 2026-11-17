using System;
using System.Collections.Generic;
using Rebuild.Saving;
using UnityEngine;

namespace Rebuild.Mining
{




    public sealed class ArtifactsManager : MonoBehaviour, Rebuild.Saving.IDataPersistence
    {
        public static ArtifactsManager Instance { get; private set; }
        public const int GuaranteeMissLimit = 250;

        [Header("State (persistable)")]
        [SerializeField] bool[] _found = new bool[ArtifactDef.Count];





        bool _oneArtifactThisSession;






        bool _oneArtifactPreRolledThisSession;
        int _missStreak;

        public int MissesUntilGuarantee => GuaranteeMissLimit - _missStreak;

        public ArtifactId? GetGuaranteedArtifact()
        {
            ArtifactId? rarest = null;
            float lowestChance = float.PositiveInfinity;
            for (int i = 0; i < _found.Length; i++)
            {
                if (_found[i]) continue;
                var id = (ArtifactId)i;
                float chance = ArtifactDef.Get(id).SpawnChance;
                if (chance >= lowestChance) continue;
                lowestChance = chance;
                rarest = id;
            }
            return rarest;
        }

        public event Action<ArtifactId> OnArtifactFound;

        void Awake()
        {
            if (_found == null || _found.Length != ArtifactDef.Count) _found = new bool[ArtifactDef.Count];
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public bool HasArtifact(ArtifactId id) => _found[(int)id];


















        public float RuneBonus => HasArtifact(ArtifactId.Rune) ? 0.05f : 0f;


        public float RuneMultiplier => 1f + RuneBonus;





        int _goldStackCounter;



        public bool TryGoldStackProc()
        {
            if (!HasArtifact(ArtifactId.GoldStack)) return false;
            int threshold = HasArtifact(ArtifactId.Rune) ? 24 : 25;
            _goldStackCounter++;
            if (_goldStackCounter >= threshold)
            {
                _goldStackCounter = 0;
                return true;
            }
            return false;
        }

        public bool AllFound()
        {
            for (int i = 0; i < _found.Length; i++) if (!_found[i]) return false;
            return true;
        }






        public void ResetSessionFlag()
        {
            _oneArtifactThisSession = false;
            _oneArtifactPreRolledThisSession = false;
        }









        public ArtifactId? TryRoll()
        {

            if (_oneArtifactThisSession) return null;
            if (AllFound()) return null;


            float mult = 1f;
            var lm = LevelMechanics.Instance;
            if (lm != null)
            {
                if (lm.HasTalent(TalentId.XMarksTheSpot)) mult *= 1.4f;
                if (lm.HasTalent(TalentId.Explorer))      mult *= 1.5f;
                if (lm.HasTalent(TalentId.ArtifactBuff))  mult *= 1.08f;
            }


            for (int i = 0; i < _found.Length; i++)
            {
                if (_found[i]) continue;
                var info = ArtifactDef.Get((ArtifactId)i);
                float chance = info.SpawnChance * mult;
                if (chance <= 0f) continue;


                if (UnityEngine.Random.value < D100Helper.Bonus(chance))
                {
                    var pick = (ArtifactId)i;
                    MarkFound(pick);
                    return pick;
                }
            }
            if (++_missStreak < GuaranteeMissLimit) return null;
            var guaranteed = GetGuaranteedArtifact();
            if (guaranteed.HasValue) MarkFound(guaranteed.Value);
            return guaranteed;
        }

        public void MarkFound(ArtifactId id)
        {
            int idx = (int)id;
            if (_found[idx]) return;
            _found[idx] = true;
            _missStreak = 0;


            _oneArtifactThisSession = true;
            Rebuild.Audio.AudioManager.Instance?.Play("FoundArtifact");

            Rebuild.Stats.StatsManager.Instance?.RecordArtifactFound();
            OnArtifactFound?.Invoke(id);
        }









        public ArtifactId? TryRollOnRockSpawn()
        {

            if (_oneArtifactPreRolledThisSession) return null;

            if (AllFound()) return null;

            if (_oneArtifactThisSession) return null;


            float mult = 1f;
            var lm = LevelMechanics.Instance;
            if (lm != null)
            {
                if (lm.HasTalent(TalentId.XMarksTheSpot)) mult *= 1.4f;
                if (lm.HasTalent(TalentId.Explorer))      mult *= 1.5f;
                if (lm.HasTalent(TalentId.ArtifactBuff))  mult *= 1.08f;
            }


            for (int i = 0; i < _found.Length; i++)
            {
                if (_found[i]) continue;
                var info = ArtifactDef.Get((ArtifactId)i);
                float chance = info.SpawnChance * mult;
                if (chance <= 0f) continue;
                if (UnityEngine.Random.value < D100Helper.Bonus(chance))
                {
                    var pick = (ArtifactId)i;

                    _oneArtifactPreRolledThisSession = true;
                    return pick;
                }
            }
            return null;
        }





        public ArtifactId? ConfirmPreRolledArtifact(ArtifactId id)
        {
            if (_found[(int)id]) return null;
            if (_oneArtifactThisSession) return null;
            MarkFound(id);
            return id;
        }


        public void ResetAll()
        {
            for (int i = 0; i < _found.Length; i++) _found[i] = false;
            _missStreak = 0;
            ResetSessionFlag();
        }


        public void LoadData(GameDataDTO data)
        {
            if (data == null) return;
            if (_found == null || _found.Length != ArtifactDef.Count) _found = new bool[ArtifactDef.Count];
            if (data.artifactsFound != null)
            {
                int n = Mathf.Min(_found.Length, data.artifactsFound.Length);
                for (int i = 0; i < _found.Length; i++)
                    _found[i] = i < n && data.artifactsFound[i];
            }
            else
            {
                for (int i = 0; i < _found.Length; i++) _found[i] = false;
            }
            _missStreak = AllFound() ? 0 : Mathf.Clamp(data.artifactMissStreak, 0, GuaranteeMissLimit - 1);
            ResetSessionFlag();


            for (int i = 0; i < _found.Length; i++)
                if (_found[i]) OnArtifactFound?.Invoke((ArtifactId)i);
        }

        public void SaveData(GameDataDTO data)
        {
            if (data == null) return;
            if (data.artifactsFound == null || data.artifactsFound.Length != _found.Length)
                data.artifactsFound = new bool[_found.Length];
            for (int i = 0; i < _found.Length; i++) data.artifactsFound[i] = _found[i];
            data.artifactMissStreak = _missStreak;
        }
    }
}
