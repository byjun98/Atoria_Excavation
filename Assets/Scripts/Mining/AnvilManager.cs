using System;
using System.Collections;
using Rebuild.Saving;
using UnityEngine;

namespace Rebuild.Mining
{



    public sealed class AnvilManager : MonoBehaviour, Rebuild.Saving.IDataPersistence
    {
        public static AnvilManager Instance { get; private set; }

        [SerializeField] int _currentIdx = 0;
        [SerializeField] bool[] _crafted = new bool[PickaxeDef.Count];




        [SerializeField] int[] _skinChosenByPickaxe = new int[PickaxeDef.Count - 1];

        public const int MaxSkinIndex = 2;
        public const int SkinlessPickaxeIdx = 13;






        public const float CraftDurationSec = 2.4f;

        public int CurrentIdx => _currentIdx;


        public float CurrentDamageBonus => PickaxeDef.Get(_currentIdx).DamageBonus;

        public float CurrentMineTime => PickaxeDef.Get(_currentIdx).MineTime;
        public float CurrentDoublePowerChance => PickaxeDef.Get(_currentIdx).DoublePowerChance;
        public float CurrentMiningAreaSize => PickaxeDef.Get(_currentIdx).MiningAreaSize;
        public bool IsCrafted(int idx) => _crafted[idx];
        public bool IsEquipped(int idx) => _currentIdx == idx;
        public bool IsCraftingAny { get; private set; }


        public float CraftElapsed01 { get; private set; }
        public int CraftingIdx { get; private set; } = -1;
        public float CraftDelaySec => CraftDurationSec;

        public event Action<int> OnPickaxeChanged;
        public event Action<int, bool> OnCraftStateChanged;
        public event Action<int> OnCraftingStarted;
        public event Action<int> OnCraftFailed;

        public event Action<int, float> OnCraftingProgress;


        public event Action<int> OnCraftCancelled;

        public event Action<int, int> OnSkinChanged;

        Coroutine _craftCo;

        void Awake()
        {
            if (_crafted == null || _crafted.Length != PickaxeDef.Count) _crafted = new bool[PickaxeDef.Count];

            _crafted[0] = true;
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }



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



            var info = PickaxeDef.Get(idx);
            var ore = GoldAndOreMechanics.Instance;
            for (int i = 0; i < info.Cost.Currencies.Length; i++)
            {
                int amount = DiscountedCost(info.Cost.Amounts[i]);
                if (!ore.TrySpendOre(info.Cost.Currencies[i], amount))
                {

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

            Rebuild.Stats.StatsManager.Instance?.RecordPickaxeCrafted();
            OnCraftStateChanged?.Invoke(idx, true);
        }




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



        public int GetSkinChosen(int idx)
        {
            if (idx < 0 || idx >= _skinChosenByPickaxe.Length) return 0;
            return Mathf.Clamp(_skinChosenByPickaxe[idx], 0, MaxSkinIndex);
        }



        public void SetSkinChosen(int idx, int value)
        {
            if (idx < 0 || idx >= _skinChosenByPickaxe.Length) return;
            value = Mathf.Clamp(value, 0, MaxSkinIndex);
            if (_skinChosenByPickaxe[idx] == value) return;
            _skinChosenByPickaxe[idx] = value;
            Rebuild.Audio.AudioManager.Instance?.Play("Click_1");
            OnSkinChanged?.Invoke(idx, value);

            if (idx == _currentIdx) OnPickaxeChanged?.Invoke(_currentIdx);
        }



        public void CycleSkin(int idx, int dir)
        {
            if (idx < 0 || idx >= _skinChosenByPickaxe.Length) return;
            int count = MaxSkinIndex + 1;
            int next = ((_skinChosenByPickaxe[idx] + dir) % count + count) % count;
            SetSkinChosen(idx, next);
        }


        public bool HasSkinVariants(int idx) => idx >= 0 && idx != SkinlessPickaxeIdx && idx < PickaxeDef.Count;


        public void ResetAll()
        {
            for (int i = 0; i < _crafted.Length; i++) _crafted[i] = (i == 0);
            _currentIdx = 0;
            OnPickaxeChanged?.Invoke(_currentIdx);
            for (int i = 0; i < _crafted.Length; i++) OnCraftStateChanged?.Invoke(i, _crafted[i]);
        }


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
            _crafted[0] = true;
            int idx = Mathf.Clamp(data.currentPickaxeIdx, 0, PickaxeDef.Count - 1);
            if (!_crafted[idx]) idx = 0;
            _currentIdx = idx;

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

            if (data.pickaxeSkinsChosen == null || data.pickaxeSkinsChosen.Length != PickaxeDef.Count)
                data.pickaxeSkinsChosen = new int[PickaxeDef.Count];
            for (int i = 0; i < _skinChosenByPickaxe.Length && i < data.pickaxeSkinsChosen.Length - 1; i++)
                data.pickaxeSkinsChosen[i] = _skinChosenByPickaxe[i];
            data.pickaxeSkinsChosen[PickaxeDef.Count - 1] = 0;
        }
    }
}
