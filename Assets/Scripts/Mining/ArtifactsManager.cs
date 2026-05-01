using System;
using System.Collections.Generic;
using Rebuild.Saving;
using UnityEngine;

namespace Rebuild.Mining
{
    /// <summary>artifact discovery + permanent flag store.
    /// Rock.FadeAndDestroy rolls <see cref="TryRoll"/> on every destroy; if it returns a non-null id
    /// the popup is shown and the flag is set. Other managers query <see cref="HasArtifact"/>.
    /// persists <c>_found</c>.</summary>
    public sealed class ArtifactsManager : MonoBehaviour, Rebuild.Saving.IDataPersistence
    {
        public static ArtifactsManager Instance { get; private set; }

        [Header("State (persistable)")]
        [SerializeField] bool[] _found = new bool[ArtifactDef.Count];

        // original Artifacts.oneArtifactSpawned: at most 1 artifact discovered per
        // session. In-memory only — reset every ApplyEnterSession. Without this gate, a lucky
        // session could discover 2-3 artifacts in quick succession and break the "rare event"
        // pacing.
        bool _oneArtifactThisSession;

        // pre-spawn model 추가. 원본 Artifacts.ArtifactDropChances 는 rock spawn 시점
        // 14 artifact roll → 성공 시 해당 rock 의 child 로 AllArtifacts group reparent + rock 작아지면
        // sprite 노출. Shell 의 RockSpawner.SpawnOne 가 매 rock spawn 마다 TryRollOnRockSpawn 호출 →
        // 성공 시 rock.AssignArtifact(id) → Rock 이 child overlay sprite 부착 + break 시 ForceArtifact
        // 로 popup. 이번 세션에 이미 한 번 박힌 경우 더 박지 않음 (원본 oneArtifactSpawned 매칭).
        bool _oneArtifactPreRolledThisSession;

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

        // Rune universal multiplier 복원 (원본 Artifacts.runeImproveAll = 0.05f).
        // 원본의 Rune 은 12+ site 에서 (1 + runeImproveAll) 로 곱해지는 universal multiplier 였음.
        // 의 click-damage stack simplification 을 푼다.
        //
        // 적용 site (원본 매칭):
        //   - Horn        : hornRockDecrease × (1 + RuneBonus)     → RockSpawner.SpawnOne
        //   - AncientDevice: ancientDeviceTimeReduction × (1+...)  → MineManager.CurrentMiningTime
        //   - PurpleRing  : purpleRingChance × (1 + RuneBonus)     → LevelMechanics.GiveXP
        //   - Cheese      : cheeseChance × (1 + RuneBonus)         → Rock.FadeAndDestroy
        //   - Axe         : double/insta 분기 × (1 + RuneBonus)    → Rock.ApplyExternalHit
        //   - WolfClaw    : clawChance × (1 + RuneBonus)           → Rock.ApplyExternalHit
        //   - Meteorite   : fullXRockChance × (1 + 0.03)           → RockSpawner.PickTier (Rune 도 +0.0015 secondary)
        //   - dice spawn  : -0.05s                                 → MiningSessionController.ComputeDiceSpawnPeriod (이미)
        //   - GoldStack   : counter threshold 25→24 (-1)           → Rock.FadeAndDestroy GoldStack counter
        //   - Damage (Rune 자체) +5% damage 도 복원 (Bone/WolfClaw 와 stack)  — 의 click stack 도 유지

        /// <summary>.D — Rune artifact 보유 시 0.05f, 미보유 시 0f. 원본 <c>runeImproveAll</c>.</summary>
        public float RuneBonus => HasArtifact(ArtifactId.Rune) ? 0.05f : 0f;

        /// <summary>.D — (1 + RuneBonus). 원본의 multiplier chain 식 <c>num × (1 + runeImproveAll)</c> 용.</summary>
        public float RuneMultiplier => 1f + RuneBonus;

        // GoldStack counter 모델. 원본 GoldAndOreMechanics.cs:234-260 매칭.
        // gold ore drop 시 _goldStackCounter++, 그것이 threshold 도달 시 reset + 2배 보장.
        // threshold = 25 base, Rune 보유 시 24 (Archaeologist 는 Shell 에 source 없어 base 만 처리).
        // 매 gold rock 이 2배 되던 Shell 기존 모델 (always-double) → 4% 효과율 (1/25) 로 정확화.
        int _goldStackCounter;

        /// <summary>.D — Gold ore drop 시 호출. counter 가 threshold 도달이면 true + reset.
        /// 원본 GoldAndOreMechanics.GiveMaterialOre 의 goldSackIncrement 로직 매칭.</summary>
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

        /// <summary>clear the "one artifact per session" gate. Called by
        /// <see cref="MiningSessionController.ApplyEnterSession"/> so each fresh session can
        /// discover at most one new artifact.
        /// <para>Pre-roll gate (<see cref="_oneArtifactPreRolledThisSession"/>) 도 함께 초기화.
        /// 원본은 oneArtifactSpawned 한 flag 가 spawn/break 양쪽 가드.</para></summary>
        public void ResetSessionFlag()
        {
            _oneArtifactThisSession = false;
            _oneArtifactPreRolledThisSession = false;
        }

        /// <summary>Independent per-artifact roll (mirrors original
        /// Artifacts.ArtifactDropChances). Each un-found artifact rolls its own SpawnChance.
        /// XMarksTheSpot / Explorer / ArtifactBuff talents multiply every roll. Once any roll
        /// succeeds in a session, no further artifact can be found until the next session
        /// (ResetSessionFlag flips the gate at ApplyEnterSession).
        /// <para>Roll order = ArtifactId enum order (Horn 0 → Skull 13). Multiple successes in
        /// the same destroy are impossible because <c>_oneArtifactThisSession</c> flips after
        /// the first one.</para></summary>
        public ArtifactId? TryRoll()
        {
            // gate — at most one artifact per session.
            if (_oneArtifactThisSession) return null;
            if (AllFound()) return null;

            // Talent multipliers — applied to every per-artifact roll (mirrors original).
            float mult = 1f;
            var lm = LevelMechanics.Instance;
            if (lm != null)
            {
                if (lm.HasTalent(TalentId.XMarksTheSpot)) mult *= 1.4f;
                if (lm.HasTalent(TalentId.Explorer))      mult *= 1.5f;
                if (lm.HasTalent(TalentId.ArtifactBuff))  mult *= 1.08f;
            }

            // Independent per-artifact rolls. First success wins.
            for (int i = 0; i < _found.Length; i++)
            {
                if (_found[i]) continue;
                var info = ArtifactDef.Get((ArtifactId)i);
                float chance = info.SpawnChance * mult;
                if (chance <= 0f) continue;
                // Bonus helper 는 pass-through (D100 talent 효과는
                // GoldAndOreMechanics.CraftSessionToBars 의 session lottery 로 이전). 호출 무해.
                if (UnityEngine.Random.value < D100Helper.Bonus(chance))
                {
                    var pick = (ArtifactId)i;
                    MarkFound(pick);
                    return pick;
                }
            }
            return null;
        }

        public void MarkFound(ArtifactId id)
        {
            int idx = (int)id;
            if (_found[idx]) return;
            _found[idx] = true;
            // flip the per-session gate so further TryRoll calls in this session
            // return null immediately. Reset at next session's ApplyEnterSession.
            _oneArtifactThisSession = true;
            Rebuild.Audio.AudioManager.Instance?.Play("FoundArtifact");
            // feeds Curator (find all 6) and is the source of truth for "Artifacts Found" stat.
            Rebuild.Stats.StatsManager.Instance?.RecordArtifactFound();
            OnArtifactFound?.Invoke(id);
        }

        /// <summary>.E — pre-spawn roll. <see cref="RockSpawner.SpawnOne"/> 가 매 rock spawn 마다
        /// 호출. 원본 Artifacts.ArtifactDropChances 의 spawn-time roll 매칭. 14 artifact 독립 roll →
        /// 첫 성공 시 해당 rock 에 ArtifactId 할당 + pre-roll gate flip. 세션당 최대 1개만 pre-roll
        /// (원본 oneArtifactSpawned 매칭).
        /// <para><b>주의</b>: 이 메서드는 <see cref="MarkFound"/> 를 호출하지 않음 — rock 깨질 때
        /// <see cref="ConfirmPreRolledArtifact"/> 가 호출되어야 발견 처리. 즉 pre-roll 된 rock 이
        /// 화면 클리어 (timer out) 로 사라지면 발견이 무산됨 (원본도 동일 — AllArtifacts 가 rock
        /// 의 child 일 때 rock 이 비폭발 사라지면 artifact 도 동반 disable).</para></summary>
        public ArtifactId? TryRollOnRockSpawn()
        {
            // Pre-roll gate: 이번 세션 이미 완료 시 추가 roll 차단.
            if (_oneArtifactPreRolledThisSession) return null;
            // 이미 발견된 artifact 만 남았으면 의미 없음.
            if (AllFound()) return null;
            // 이미 break-time TryRoll 로 발견된 경우 (예: 다른 rock 깰 때) 도 차단.
            if (_oneArtifactThisSession) return null;

            // Talent multipliers — TryRoll 과 동일.
            float mult = 1f;
            var lm = LevelMechanics.Instance;
            if (lm != null)
            {
                if (lm.HasTalent(TalentId.XMarksTheSpot)) mult *= 1.4f;
                if (lm.HasTalent(TalentId.Explorer))      mult *= 1.5f;
                if (lm.HasTalent(TalentId.ArtifactBuff))  mult *= 1.08f;
            }

            // Independent per-artifact rolls. First success wins.
            for (int i = 0; i < _found.Length; i++)
            {
                if (_found[i]) continue;
                var info = ArtifactDef.Get((ArtifactId)i);
                float chance = info.SpawnChance * mult;
                if (chance <= 0f) continue;
                if (UnityEngine.Random.value < D100Helper.Bonus(chance))
                {
                    var pick = (ArtifactId)i;
                    // pre-roll gate flip — 이번 세션에 추가 rock 에 박히지 않음.
                    _oneArtifactPreRolledThisSession = true;
                    return pick;
                }
            }
            return null;
        }

        /// <summary>.E — pre-roll 된 artifact 의 발견 처리. <see cref="Rock.FadeAndDestroy"/>
        /// 가 rock 이 깨질 때 <see cref="Rock.AssignedArtifact"/> 가 있으면 호출.
        /// <para>이미 발견된 경우 (other rock break-time TryRoll 이 먼저 발견) 면 no-op 반환. 발견되지
        /// 않은 경우 MarkFound 동작 (Audio + Stat + Event + _oneArtifactThisSession flag).</para></summary>
        public ArtifactId? ConfirmPreRolledArtifact(ArtifactId id)
        {
            if (_found[(int)id]) return null; // 이미 발견.
            if (_oneArtifactThisSession) return null; // 다른 경로로 이미 발견.
            MarkFound(id);
            return id;
        }

        /// <summary>Debug hard reset.</summary>
        public void ResetAll()
        {
            for (int i = 0; i < _found.Length; i++) _found[i] = false;
        }

        // ---- IDataPersistence ----
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
            // Re-fire OnArtifactFound for every already-found artifact so UI grids refresh
            // (ArtifactNodeUI subscribes on enable, so it's a no-op when this runs pre-OnEnable).
            for (int i = 0; i < _found.Length; i++)
                if (_found[i]) OnArtifactFound?.Invoke((ArtifactId)i);
        }

        public void SaveData(GameDataDTO data)
        {
            if (data == null) return;
            if (data.artifactsFound == null || data.artifactsFound.Length != _found.Length)
                data.artifactsFound = new bool[_found.Length];
            for (int i = 0; i < _found.Length; i++) data.artifactsFound[i] = _found[i];
        }
    }
}
