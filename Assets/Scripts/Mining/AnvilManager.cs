using System;
using System.Collections;
using Rebuild.Saving;
using UnityEngine;

namespace Rebuild.Mining
{
    /// <summary>pickaxe roster + craft / equip.
    /// Singleton on MiningSessionRoot. persists <c>_currentIdx</c> + <c>_crafted</c>.
    /// </summary>
    public sealed class AnvilManager : MonoBehaviour, Rebuild.Saving.IDataPersistence
    {
        public static AnvilManager Instance { get; private set; }

        [SerializeField] int _currentIdx = 0;
        [SerializeField] bool[] _crafted = new bool[PickaxeDef.Count];
        // 원본 PickaxeMechanics 의 pickaxe1~13_skinsChosen 매칭. idx 0..12 만 skin
        // 변형 (3 variants: 0=base, 1=skin1, 2=skin2). idx 13 (마지막 곡괭이) 은 원본도 skin 없음.
        // 배열 13개 — idx 0..12 매핑. GameDataDTO.pickaxeSkinsChosen[14] 는 trailing entry (idx 13)
        // 항상 0 으로 유지.
        [SerializeField] int[] _skinChosenByPickaxe = new int[PickaxeDef.Count - 1];

        public const int MaxSkinIndex = 2;       // base / skin1 / skin2
        public const int SkinlessPickaxeIdx = 13; // idx 13 = skin 없음 (원본 매칭)

        // match original TheAnvil.craftingTime (2.4f). Hold-to-craft runs frame-by-
        // frame against unscaledDeltaTime so we can radial-fill the gauge and cancel mid-flight.
        // 🚨 const, NOT SerializeField — scene was serialised with the old 0.6f default and
        // changing the SerializeField default doesn't migrate existing scene values.
        // const guarantees the value the code uses regardless of scene.
        public const float CraftDurationSec = 2.4f;

        public int CurrentIdx => _currentIdx;
        // fractional damage (2.5~8.0). Rock / BigRock accumulate dmg as float and
        // RoundToInt at TakeDamage so the per-tick contribution is preserved.
        public float CurrentDamageBonus => PickaxeDef.Get(_currentIdx).DamageBonus;
        // Anvil 4 stats 도입. equipped pickaxe 의 정보 단축 접근.
        public float CurrentMineTime => PickaxeDef.Get(_currentIdx).MineTime;
        public float CurrentDoublePowerChance => PickaxeDef.Get(_currentIdx).DoublePowerChance;
        public float CurrentMiningAreaSize => PickaxeDef.Get(_currentIdx).MiningAreaSize;
        public bool IsCrafted(int idx) => _crafted[idx];
        public bool IsEquipped(int idx) => _currentIdx == idx;
        public bool IsCraftingAny { get; private set; }

        // exposed so UI can polling-fallback even before the per-frame event fires.
        public float CraftElapsed01 { get; private set; }
        public int CraftingIdx { get; private set; } = -1;
        public float CraftDelaySec => CraftDurationSec;

        public event Action<int> OnPickaxeChanged;     // newIdx
        public event Action<int, bool> OnCraftStateChanged; // idx, crafted
        public event Action<int> OnCraftingStarted;
        public event Action<int> OnCraftFailed;
        // per-frame craft progress (0..1) so the gauge can radial-fill.
        public event Action<int, float> OnCraftingProgress;
        // fired when CancelCraft() aborts an in-flight craft. UI can dismiss its
        // gauge silently (no red-flash, no SFX) since the user explicitly let go.
        public event Action<int> OnCraftCancelled;
        // skin 변경 시 UI 갱신 trigger. (idx, newSkin) 페이로드.
        public event Action<int, int> OnSkinChanged;

        Coroutine _craftCo;

        void Awake()
        {
            if (_crafted == null || _crafted.Length != PickaxeDef.Count) _crafted = new bool[PickaxeDef.Count];
            // MrRusty (idx 0) starts crafted — never unset.
            _crafted[0] = true;
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>EfficientBlacksmith talent: pickaxe craft cost x0.75 (ceil so the
        /// minimum spend never drops below 1). Mirrors original blacksmithDecrease constant.</summary>
        public static int DiscountedCost(int raw)
        {
            if (LevelMechanics.Instance != null && LevelMechanics.Instance.HasTalent(TalentId.EfficientBlacksmith))
                return Mathf.Max(1, Mathf.CeilToInt(raw * 0.75f));
            return raw;
        }

        public bool CanAfford(int idx)
        {
            var info = PickaxeDef.Get(idx);
            if (info.Cost.IsFree) return true;
            var ore = GoldAndOreMechanics.Instance;
            if (ore == null) return false;
            for (int i = 0; i < info.Cost.Currencies.Length; i++)
            {
                if (ore.GetCount(info.Cost.Currencies[i]) < DiscountedCost(info.Cost.Amounts[i])) return false;
            }
            return true;
        }

        /// <summary>hold-to-craft entry point. Starts the 2.4s gauge coroutine and
        /// keeps a handle so <see cref="CancelCraft"/> can stop it cleanly when the user releases
        /// the action button. Returns a Coroutine the caller can yield on, but normally fires the
        /// progress/state events directly.</summary>
        public Coroutine BeginCraft(int idx)
        {
            if (IsCraftingAny) return _craftCo;
            if (idx < 0 || idx >= PickaxeDef.Count) return null;
            if (_crafted[idx])
            {
                OnCraftFailed?.Invoke(idx);
                return null;
            }
            if (!CanAfford(idx))
            {
                Rebuild.Audio.AudioManager.Instance?.Play("CantAfford");
                OnCraftFailed?.Invoke(idx);
                return null;
            }
            _craftCo = StartCoroutine(CraftRoutine(idx));
            return _craftCo;
        }

        /// <summary>back-compat shim. Old code that called
        /// <c>StartCoroutine(anvil.TryCraft(idx))</c> still works (it just calls BeginCraft and
        /// yields one frame). New code should call <see cref="BeginCraft"/> directly so the
        /// returned handle is the canonical one.</summary>
        public IEnumerator TryCraft(int idx)
        {
            BeginCraft(idx);
            yield return null;
        }

        IEnumerator CraftRoutine(int idx)
        {
            IsCraftingAny = true;
            CraftingIdx = idx;
            CraftElapsed01 = 0f;
            OnCraftingStarted?.Invoke(idx);
            OnCraftingProgress?.Invoke(idx, 0f);

            // Frame-by-frame — unscaledDeltaTime so pause (timeScale=0) freezes the gauge,
            // matching original TheAnvil's Time.deltaTime behaviour in spirit (player can't
            // hold-craft past a pause). UI gauge polls CraftElapsed01 or hooks OnCraftingProgress.
            float elapsed = 0f;
            while (elapsed < CraftDurationSec)
            {
                elapsed += Time.unscaledDeltaTime;
                CraftElapsed01 = Mathf.Clamp01(elapsed / CraftDurationSec);
                OnCraftingProgress?.Invoke(idx, CraftElapsed01);
                yield return null;
            }
            CraftElapsed01 = 1f;
            OnCraftingProgress?.Invoke(idx, 1f);

            // Atomic spend — verify we still have the ore (race with another purchase).
            // EfficientBlacksmith talent: every cost line passes through DiscountedCost.
            var info = PickaxeDef.Get(idx);
            var ore = GoldAndOreMechanics.Instance;
            for (int i = 0; i < info.Cost.Currencies.Length; i++)
            {
                int amount = DiscountedCost(info.Cost.Amounts[i]);
                if (!ore.TrySpendOre(info.Cost.Currencies[i], amount))
                {
                    // Refund earlier deductions on failure (unlikely with single user).
                    for (int j = 0; j < i; j++)
                        ore.AddOre(info.Cost.Currencies[j], DiscountedCost(info.Cost.Amounts[j]));
                    IsCraftingAny = false;
                    CraftingIdx = -1;
                    CraftElapsed01 = 0f;
                    _craftCo = null;
                    OnCraftFailed?.Invoke(idx);
                    yield break;
                }
            }
            _crafted[idx] = true;
            IsCraftingAny = false;
            CraftingIdx = -1;
            CraftElapsed01 = 0f;
            _craftCo = null;
            Rebuild.Audio.AudioManager.Instance?.Play("CraftingDone");
            // drives "Pickaxes Crafted" stat + Apprentice Smith achievement.
            Rebuild.Stats.StatsManager.Instance?.RecordPickaxeCrafted();
            OnCraftStateChanged?.Invoke(idx, true);
        }

        /// <summary>abort an in-flight craft. Triggered when the user releases the
        /// action button before the gauge fills, when carousel navigation moves focus, or when
        /// the carousel panel deactivates. No-op when nothing is crafting.</summary>
        public void CancelCraft()
        {
            if (!IsCraftingAny) return;
            int cancelledIdx = CraftingIdx;
            if (_craftCo != null) StopCoroutine(_craftCo);
            _craftCo = null;
            IsCraftingAny = false;
            CraftingIdx = -1;
            CraftElapsed01 = 0f;
            OnCraftingProgress?.Invoke(cancelledIdx, 0f);
            OnCraftCancelled?.Invoke(cancelledIdx);
        }

        public void Equip(int idx)
        {
            if (idx < 0 || idx >= PickaxeDef.Count) return;
            if (!_crafted[idx]) return;
            if (_currentIdx == idx) return;
            _currentIdx = idx;
            Rebuild.Audio.AudioManager.Instance?.Play("Equip");
            OnPickaxeChanged?.Invoke(_currentIdx);
        }

        /// <summary>원본 PickaxeMechanics.SkinChosen 매칭. idx 0..12 만 의미 있음 —
        /// idx 13 (마지막 곡괭이) 은 skin 변형 없으므로 항상 0 반환. 반환값 0=base / 1=skin1 / 2=skin2.</summary>
        public int GetSkinChosen(int idx)
        {
            if (idx < 0 || idx >= _skinChosenByPickaxe.Length) return 0;
            return Mathf.Clamp(_skinChosenByPickaxe[idx], 0, MaxSkinIndex);
        }

        /// <summary>skin 변경. idx 13 은 무시 (skin 없음). value 는 0..MaxSkinIndex 범위.
        /// 사이클 helper: <see cref="CycleSkin"/>.</summary>
        public void SetSkinChosen(int idx, int value)
        {
            if (idx < 0 || idx >= _skinChosenByPickaxe.Length) return; // idx 13 자동 reject
            value = Mathf.Clamp(value, 0, MaxSkinIndex);
            if (_skinChosenByPickaxe[idx] == value) return;
            _skinChosenByPickaxe[idx] = value;
            Rebuild.Audio.AudioManager.Instance?.Play("Click_1");
            OnSkinChanged?.Invoke(idx, value);
            // equipped pickaxe 의 skin 바뀌었을 때 PickaxeChanged 도 fire (sprite refresh trigger).
            if (idx == _currentIdx) OnPickaxeChanged?.Invoke(_currentIdx);
        }

        /// <summary>prev/next 화살표용 cyclic helper. dir = +1 (next) / -1 (prev).
        /// 0..MaxSkinIndex 사이 wrap-around. idx 13 무시.</summary>
        public void CycleSkin(int idx, int dir)
        {
            if (idx < 0 || idx >= _skinChosenByPickaxe.Length) return;
            int count = MaxSkinIndex + 1;
            int next = ((_skinChosenByPickaxe[idx] + dir) % count + count) % count;
            SetSkinChosen(idx, next);
        }

        /// <summary>UI 가 skin selector 표시 여부 판단. idx 13 (skin 없음) 만 false.</summary>
        public bool HasSkinVariants(int idx) => idx >= 0 && idx != SkinlessPickaxeIdx && idx < PickaxeDef.Count;

        /// <summary>Debug hard reset — MrRusty becomes the only crafted pickaxe again.</summary>
        public void ResetAll()
        {
            for (int i = 0; i < _crafted.Length; i++) _crafted[i] = (i == 0);
            _currentIdx = 0;
            OnPickaxeChanged?.Invoke(_currentIdx);
            for (int i = 0; i < _crafted.Length; i++) OnCraftStateChanged?.Invoke(i, _crafted[i]);
        }

        // ---- IDataPersistence ----
        public void LoadData(GameDataDTO data)
        {
            if (data == null) return;
            if (_crafted == null || _crafted.Length != PickaxeDef.Count) _crafted = new bool[PickaxeDef.Count];
            if (data.pickaxesCrafted != null)
            {
                int n = Mathf.Min(_crafted.Length, data.pickaxesCrafted.Length);
                for (int i = 0; i < _crafted.Length; i++)
                    _crafted[i] = i < n && data.pickaxesCrafted[i];
            }
            _crafted[0] = true; // Mr.Rusty always crafted regardless of save state.
            int idx = Mathf.Clamp(data.currentPickaxeIdx, 0, PickaxeDef.Count - 1);
            if (!_crafted[idx]) idx = 0; // Defensive: never equip a non-crafted pickaxe.
            _currentIdx = idx;
            // skin 복원. DTO 의 pickaxeSkinsChosen[14] 중 idx 0..12 만 의미 있음.
            if (_skinChosenByPickaxe == null || _skinChosenByPickaxe.Length != PickaxeDef.Count - 1)
                _skinChosenByPickaxe = new int[PickaxeDef.Count - 1];
            if (data.pickaxeSkinsChosen != null)
            {
                int n = Mathf.Min(_skinChosenByPickaxe.Length, data.pickaxeSkinsChosen.Length);
                for (int i = 0; i < _skinChosenByPickaxe.Length; i++)
                    _skinChosenByPickaxe[i] = i < n ? Mathf.Clamp(data.pickaxeSkinsChosen[i], 0, MaxSkinIndex) : 0;
            }
            OnPickaxeChanged?.Invoke(_currentIdx);
            for (int i = 0; i < _crafted.Length; i++) OnCraftStateChanged?.Invoke(i, _crafted[i]);
            for (int i = 0; i < _skinChosenByPickaxe.Length; i++) OnSkinChanged?.Invoke(i, _skinChosenByPickaxe[i]);
        }

        public void SaveData(GameDataDTO data)
        {
            if (data == null) return;
            if (data.pickaxesCrafted == null || data.pickaxesCrafted.Length != _crafted.Length)
                data.pickaxesCrafted = new bool[_crafted.Length];
            for (int i = 0; i < _crafted.Length; i++) data.pickaxesCrafted[i] = _crafted[i];
            data.currentPickaxeIdx = _currentIdx;
            // skin 저장. DTO array (14) 의 trailing entry (idx 13) 는 항상 0 — 원본 매칭.
            if (data.pickaxeSkinsChosen == null || data.pickaxeSkinsChosen.Length != PickaxeDef.Count)
                data.pickaxeSkinsChosen = new int[PickaxeDef.Count];
            for (int i = 0; i < _skinChosenByPickaxe.Length && i < data.pickaxeSkinsChosen.Length - 1; i++)
                data.pickaxeSkinsChosen[i] = _skinChosenByPickaxe[i];
            data.pickaxeSkinsChosen[PickaxeDef.Count - 1] = 0; // idx 13 = skin 없음 보장
        }
    }
}
