using System;
using System.Collections;
using Rebuild.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Rebuild.Mining
{




    public sealed class AnvilCarouselUI : MonoBehaviour
    {
        [Serializable]
        public class CostRow
        {
            public GameObject root;
            public Image icon;
            public TMP_Text amountLabel;
        }

        [Header("Spotlight")]
        [SerializeField] Image bigIcon;
        [SerializeField] TMP_Text nameLabel;
        [SerializeField] GameObject equippedBadge;
        [SerializeField] TMP_Text counterLabel;
        [SerializeField] Image spotlightRay;

        [Header("Stats (4) — value lines")]
        [SerializeField] TMP_Text mineTimeValue;
        [SerializeField] TMP_Text minePowerValue;
        [SerializeField] TMP_Text doubleChanceValue;
        [SerializeField] TMP_Text areaSizeValue;

        [Header("Stats (4) — delta vs equipped")]
        [SerializeField] TMP_Text mineTimeDelta;
        [SerializeField] TMP_Text minePowerDelta;
        [SerializeField] TMP_Text doubleChanceDelta;
        [SerializeField] TMP_Text areaSizeDelta;

        [Header("Cost")]
        [SerializeField] CostRow[] costRows = new CostRow[3];
        [SerializeField] GameObject freeLabel;

        [Header("Action")]
        [SerializeField] Button actionButton;
        [SerializeField] Image actionImage;
        [SerializeField] TMP_Text actionLabel;
        [SerializeField] GameObject craftingOverlay;

        [Tooltip("Sits on the same GameObject as actionButton; relays pointer down/up.")]
        [SerializeField] PointerHoldForwarder actionHoldForwarder;
        [Tooltip("Radial-fill Image (FillMethod.Radial360) painted on top of the action button while crafting.")]
        [SerializeField] Image craftFillCircle;
        [Tooltip("'복원 중.../..../.....' dot-animation label shown while holding to craft.")]
        [SerializeField] TMP_Text craftingDotsLabel;

        [Header("Arrows")]
        [SerializeField] Button leftArrow;
        [SerializeField] Button rightArrow;




        [Header("Skin selector")]
        [SerializeField] GameObject skinSelectorRoot;
        [SerializeField] Button skinPrevBtn;
        [SerializeField] Button skinNextBtn;
        [SerializeField] TMP_Text skinLabel;



        [Header("Equipped sidecar")]
        [SerializeField] GameObject equippedSidecarRoot;
        [SerializeField] Image equippedIcon;
        [SerializeField] TMP_Text equippedNameLabel;



        [Header("Craft popup")]
        [SerializeField] RectTransform popupParent;

        int _focusIdx;
        Color _baseActionColor;
        Coroutine _flashCo;
        Coroutine _pulseCo;
        bool _completedThisHold;
        bool _holdingForCraft;


        static readonly Color s_equippedColor    = new Color32(0x36, 0x5E, 0x3B, 0xFF);
        static readonly Color s_equipReadyColor  = new Color32(0x4A, 0x7A, 0x4E, 0xFF);
        static readonly Color s_craftAffordColor = new Color32(0xC8, 0x44, 0x3A, 0xFF);
        static readonly Color s_craftDimColor    = new Color32(0x5A, 0x55, 0x50, 0xFF);
        static readonly Color s_flashColor       = new Color(0.95f, 0.30f, 0.30f, 1f);
        static readonly Color s_betterColor      = new Color32(0x66, 0xCC, 0xAA, 0xFF);
        static readonly Color s_worseColor       = new Color32(0xE0, 0x6A, 0x55, 0xFF);
        static readonly Color s_neutralColor     = new Color32(0xF5, 0xE6, 0xD3, 0xFF);

        public int FocusIdx => _focusIdx;

        void Awake()
        {
            if (actionImage != null) _baseActionColor = actionImage.color;
            if (actionButton != null)
            {


                actionButton.onClick.RemoveAllListeners();
            }
            if (leftArrow != null)
            {
                leftArrow.onClick.RemoveAllListeners();
                leftArrow.onClick.AddListener(GoPrev);
            }
            if (rightArrow != null)
            {
                rightArrow.onClick.RemoveAllListeners();
                rightArrow.onClick.AddListener(GoNext);
            }

            if (skinPrevBtn != null)
            {
                skinPrevBtn.onClick.RemoveAllListeners();
                skinPrevBtn.onClick.AddListener(() => CycleSkinAtFocus(-1));
            }
            if (skinNextBtn != null)
            {
                skinNextBtn.onClick.RemoveAllListeners();
                skinNextBtn.onClick.AddListener(() => CycleSkinAtFocus(+1));
            }
            if (craftingOverlay != null) craftingOverlay.SetActive(false);
            if (equippedBadge != null) equippedBadge.SetActive(false);
            EnsureFillCircleState();
            if (craftingDotsLabel != null) craftingDotsLabel.text = "";
        }






        void EnsureFillCircleState()
        {
            if (craftFillCircle == null) return;
            craftFillCircle.sprite = RingSpriteFactory.GetRing();
            craftFillCircle.type = Image.Type.Filled;
            craftFillCircle.fillMethod = Image.FillMethod.Radial360;
            craftFillCircle.fillOrigin = (int)Image.Origin360.Top;
            craftFillCircle.fillClockwise = true;
            craftFillCircle.fillAmount = 0f;
            craftFillCircle.raycastTarget = false;
            craftFillCircle.preserveAspect = true;
        }






        void EnsureOverlayState()
        {
            if (craftingOverlay == null) return;
            var img = craftingOverlay.GetComponent<Image>();
            if (img != null) img.raycastTarget = false;

            var graphics = craftingOverlay.GetComponentsInChildren<Graphic>(includeInactive: true);
            for (int i = 0; i < graphics.Length; i++)
                if (graphics[i] != null) graphics[i].raycastTarget = false;
        }

        void OnEnable()
        {
            if (AnvilManager.Instance != null)
            {
                _focusIdx = Mathf.Clamp(AnvilManager.Instance.CurrentIdx, 0, PickaxeDef.Count - 1);
                AnvilManager.Instance.OnPickaxeChanged += HandlePickaxeChanged;
                AnvilManager.Instance.OnCraftStateChanged += HandleCraftStateChanged;
                AnvilManager.Instance.OnCraftingStarted += HandleCraftingStarted;
                AnvilManager.Instance.OnCraftFailed += HandleCraftFailed;
                AnvilManager.Instance.OnCraftingProgress += HandleCraftingProgress;
                AnvilManager.Instance.OnCraftCancelled += HandleCraftCancelled;
                AnvilManager.Instance.OnSkinChanged += HandleSkinChanged;
            }
            if (GoldAndOreMechanics.Instance != null)
                GoldAndOreMechanics.Instance.OnOreChanged += HandleOreChanged;

            if (actionHoldForwarder != null)
            {
                actionHoldForwarder.OnDown = HandleActionDown;
                actionHoldForwarder.OnUp = HandleActionUp;
                actionHoldForwarder.OnExit = HandleActionUp;
            }

            EnsureFillCircleState();
            EnsureOverlayState();

            if (spotlightRay != null && _pulseCo == null)
                _pulseCo = StartCoroutine(SpotlightPulse());

            RefreshAll();
        }

        void OnDisable()
        {

            if (AnvilManager.Instance != null && AnvilManager.Instance.IsCraftingAny)
                AnvilManager.Instance.CancelCraft();

            if (AnvilManager.Instance != null)
            {
                AnvilManager.Instance.OnPickaxeChanged -= HandlePickaxeChanged;
                AnvilManager.Instance.OnCraftStateChanged -= HandleCraftStateChanged;
                AnvilManager.Instance.OnCraftingStarted -= HandleCraftingStarted;
                AnvilManager.Instance.OnCraftFailed -= HandleCraftFailed;
                AnvilManager.Instance.OnCraftingProgress -= HandleCraftingProgress;
                AnvilManager.Instance.OnCraftCancelled -= HandleCraftCancelled;
                AnvilManager.Instance.OnSkinChanged -= HandleSkinChanged;
            }
            if (GoldAndOreMechanics.Instance != null)
                GoldAndOreMechanics.Instance.OnOreChanged -= HandleOreChanged;

            if (actionHoldForwarder != null)
            {
                actionHoldForwarder.OnDown = null;
                actionHoldForwarder.OnUp = null;
                actionHoldForwarder.OnExit = null;
            }

            if (_pulseCo != null) { StopCoroutine(_pulseCo); _pulseCo = null; }

            _holdingForCraft = false;
            _completedThisHold = false;
            if (craftFillCircle != null) craftFillCircle.fillAmount = 0f;
            if (craftingDotsLabel != null) craftingDotsLabel.text = "";
        }


        public void GoNext()
        {
            CancelHoldIfNeeded();
            _focusIdx = (_focusIdx + 1) % PickaxeDef.Count;
            RefreshAll();
        }

        public void GoPrev()
        {
            CancelHoldIfNeeded();
            _focusIdx = (_focusIdx - 1 + PickaxeDef.Count) % PickaxeDef.Count;
            RefreshAll();
        }

        public void SetFocus(int idx)
        {
            CancelHoldIfNeeded();
            _focusIdx = Mathf.Clamp(idx, 0, PickaxeDef.Count - 1);
            RefreshAll();
        }

        void CancelHoldIfNeeded()
        {



            if (AnvilManager.Instance != null && AnvilManager.Instance.IsCraftingAny)
                AnvilManager.Instance.CancelCraft();
            _holdingForCraft = false;
        }


        void HandlePickaxeChanged(int _) => RefreshAll();
        void HandleSkinChanged(int idx, int _)
        {

            if (idx == _focusIdx) { ApplyStaticInfo(); RefreshSkinSelector(); }
        }

        void HandleCraftStateChanged(int idx, bool crafted)
        {
            if (crafted && idx == _focusIdx)
            {



                _completedThisHold = true;
                _holdingForCraft = false;
                if (craftFillCircle != null) craftFillCircle.fillAmount = 0f;
                if (craftingDotsLabel != null) craftingDotsLabel.text = "";
            }



            if (crafted && popupParent != null && gameObject.activeInHierarchy)
            {
                var sprite = SpriteLibrary.GetPickaxeSprite(idx);
                bool hasCustom = sprite != null && sprite != RockSpriteFactory.GetRockSprite();
                Color tint;
                if (hasCustom)
                {
                    tint = Color.white;
                }
                else
                {
                    float t = idx / Mathf.Max(1f, (float)(PickaxeDef.Count - 1));
                    tint = Color.Lerp(new Color(0.55f, 0.50f, 0.45f, 1f),
                                       new Color(1.00f, 0.84f, 0.18f, 1f), t);
                }
                PickaxeCraftPopupFx.Spawn(popupParent, sprite, tint);
                Rebuild.Audio.AudioManager.Instance?.Play("FinishedCrafting");
            }
            RefreshAll();
        }

        void HandleOreChanged(OreType _, long __) => RefreshActionState();

        void HandleCraftingStarted(int started)
        {
            if (started != _focusIdx) return;










            if (craftingOverlay != null) craftingOverlay.SetActive(true);
            if (craftFillCircle != null) craftFillCircle.fillAmount = 0f;
            if (craftingDotsLabel != null) craftingDotsLabel.text = "복원 중";
        }

        void HandleCraftFailed(int failed)
        {
            if (failed != _focusIdx) return;
            _holdingForCraft = false;
            if (craftingOverlay != null) craftingOverlay.SetActive(false);
            if (craftFillCircle != null) craftFillCircle.fillAmount = 0f;
            if (craftingDotsLabel != null) craftingDotsLabel.text = "";
            if (gameObject.activeInHierarchy)
            {
                if (_flashCo != null) StopCoroutine(_flashCo);
                _flashCo = StartCoroutine(RedFlash());
            }
            RefreshAll();
        }

        void HandleCraftingProgress(int idx, float t01)
        {
            if (idx != _focusIdx) return;
            if (craftFillCircle != null) craftFillCircle.fillAmount = t01;
            if (craftingDotsLabel != null)
            {



                float total = AnvilManager.CraftDurationSec;
                float elapsed = t01 * total;
                int dots = Mathf.FloorToInt(elapsed / 0.35f) % 4;
                craftingDotsLabel.text = "복원 중" + new string('.', dots);
            }
        }

        void HandleCraftCancelled(int idx)
        {
            if (idx != _focusIdx) return;

            _holdingForCraft = false;
            if (craftingOverlay != null) craftingOverlay.SetActive(false);
            if (craftFillCircle != null) craftFillCircle.fillAmount = 0f;
            if (craftingDotsLabel != null) craftingDotsLabel.text = "";
            RefreshActionState();
        }


        void HandleActionDown()
        {
            if (AnvilManager.Instance == null) return;
            var anvil = AnvilManager.Instance;


            if (anvil.IsCrafted(_focusIdx))
            {
                anvil.Equip(_focusIdx);
                return;
            }



            _completedThisHold = false;
            _holdingForCraft = true;
            anvil.BeginCraft(_focusIdx);
        }

        void HandleActionUp()
        {



            if (_completedThisHold)
            {
                _completedThisHold = false;
                _holdingForCraft = false;
                return;
            }
            if (!_holdingForCraft) return;
            _holdingForCraft = false;
            if (AnvilManager.Instance != null && AnvilManager.Instance.IsCraftingAny)
                AnvilManager.Instance.CancelCraft();
        }


        void RefreshAll()
        {
            ApplyStaticInfo();
            RefreshStats();
            RefreshCost();
            RefreshActionState();
            RefreshCounter();
            RefreshEquippedSidecar();
            RefreshSkinSelector();
        }


        void RefreshSkinSelector()
        {
            var anvil = AnvilManager.Instance;
            bool hasVariants = anvil != null && anvil.HasSkinVariants(_focusIdx);
            bool unlocked = anvil != null && anvil.IsCrafted(_focusIdx);

            bool visible = hasVariants && unlocked;
            if (skinSelectorRoot != null) skinSelectorRoot.SetActive(visible);
            if (!visible) return;
            if (skinLabel != null)
            {
                int s = anvil.GetSkinChosen(_focusIdx);
                skinLabel.text = s switch { 0 => "기본", 1 => "스킨 1", 2 => "스킨 2", _ => "기본" };
            }
        }


        void CycleSkinAtFocus(int dir)
        {
            var anvil = AnvilManager.Instance;
            if (anvil == null) return;
            if (!anvil.HasSkinVariants(_focusIdx)) return;
            if (!anvil.IsCrafted(_focusIdx)) return;
            anvil.CycleSkin(_focusIdx, dir);

            ApplyStaticInfo();
            RefreshSkinSelector();
        }



        void RefreshEquippedSidecar()
        {
            if (AnvilManager.Instance == null) return;
            int eqIdx = AnvilManager.Instance.CurrentIdx;
            var info = PickaxeDef.Get(eqIdx);

            if (equippedNameLabel != null) equippedNameLabel.text = info.Name;

            if (equippedIcon != null)
            {

                int eqSkin = AnvilManager.Instance.GetSkinChosen(eqIdx);
                var custom = SpriteLibrary.GetPickaxeSprite(eqIdx, eqSkin);
                bool hasCustom = custom != null && custom != RockSpriteFactory.GetRockSprite();
                equippedIcon.sprite = custom;
                if (hasCustom)
                {
                    equippedIcon.color = Color.white;
                }
                else
                {
                    float t = eqIdx / Mathf.Max(1f, (float)(PickaxeDef.Count - 1));
                    equippedIcon.color = Color.Lerp(new Color(0.55f, 0.50f, 0.45f, 1f),
                                                     new Color(1.00f, 0.84f, 0.18f, 1f), t);
                }
                equippedIcon.preserveAspect = true;
                equippedIcon.raycastTarget = false;
            }
        }

        void ApplyStaticInfo()
        {
            var info = PickaxeDef.Get(_focusIdx);


            bool unlocked = AnvilManager.Instance != null && AnvilManager.Instance.IsCrafted(_focusIdx);

            if (nameLabel != null) nameLabel.text = unlocked ? info.Name : "??????";

            if (bigIcon != null)
            {

                int skin = AnvilManager.Instance != null ? AnvilManager.Instance.GetSkinChosen(_focusIdx) : 0;
                var custom = SpriteLibrary.GetPickaxeSprite(_focusIdx, skin);
                bool hasCustom = custom != null && custom != RockSpriteFactory.GetRockSprite();
                bigIcon.sprite = custom;
                if (!unlocked)
                {


                    bigIcon.color = new Color(0.08f, 0.08f, 0.10f, 0.92f);
                }
                else if (hasCustom)
                {
                    bigIcon.color = Color.white;
                }
                else
                {

                    float t = _focusIdx / Mathf.Max(1f, (float)(PickaxeDef.Count - 1));
                    bigIcon.color = Color.Lerp(new Color(0.55f, 0.50f, 0.45f, 1f),
                                                new Color(1.00f, 0.84f, 0.18f, 1f), t);
                }
                bigIcon.preserveAspect = true;
                bigIcon.raycastTarget = false;
            }
        }

        void RefreshStats()
        {
            var info = PickaxeDef.Get(_focusIdx);


            bool unlocked = AnvilManager.Instance != null && AnvilManager.Instance.IsCrafted(_focusIdx);
            if (!unlocked)
            {
                if (mineTimeValue != null) mineTimeValue.text = "??.??s";
                if (minePowerValue != null) minePowerValue.text = "+?.?";
                if (doubleChanceValue != null) doubleChanceValue.text = "?.?%";
                if (areaSizeValue != null) areaSizeValue.text = "?.??";
                SetMaskedDelta(mineTimeDelta);
                SetMaskedDelta(minePowerDelta);
                SetMaskedDelta(doubleChanceDelta);
                SetMaskedDelta(areaSizeDelta);
                return;
            }

            if (mineTimeValue != null) mineTimeValue.text = $"{info.MineTime:0.00}s";


            if (minePowerValue != null) minePowerValue.text = $"+{info.DamageBonus:0.0}";
            if (doubleChanceValue != null) doubleChanceValue.text = $"{info.DoublePowerChance * 100f:0.0}%";
            if (areaSizeValue != null) areaSizeValue.text = $"{info.MiningAreaSize:0.00}";

            if (AnvilManager.Instance == null) return;
            int equippedIdx = AnvilManager.Instance.CurrentIdx;
            var equipped = PickaxeDef.Get(equippedIdx);
            bool same = (equippedIdx == _focusIdx);
            SetDeltaLabel(mineTimeDelta, info.MineTime, equipped.MineTime, same, lowerIsBetter: true, format: "0.00", suffix: "s");
            SetDeltaLabel(minePowerDelta, info.DamageBonus, equipped.DamageBonus, same, lowerIsBetter: false, format: "0.0");
            SetDeltaLabel(doubleChanceDelta, info.DoublePowerChance * 100f, equipped.DoublePowerChance * 100f, same, lowerIsBetter: false, format: "0.0", suffix: "%");
            SetDeltaLabel(areaSizeDelta, info.MiningAreaSize, equipped.MiningAreaSize, same, lowerIsBetter: false, format: "0.00");
        }




        static void SetMaskedDelta(TMP_Text label)
        {
            if (label == null) return;
            label.text = "?? ??";
            label.color = s_neutralColor;
        }

        static void SetDeltaLabel(TMP_Text label, float focused, float equipped, bool same,
                                  bool lowerIsBetter, string format, string suffix = "")
        {
            if (label == null) return;
            if (same)
            {
                label.text = "현재 장착";
                label.color = s_neutralColor;
                return;
            }
            float delta = focused - equipped;
            const float kEps = 0.0001f;
            bool better = lowerIsBetter ? (delta < -kEps) : (delta > kEps);
            bool worse = lowerIsBetter ? (delta > kEps) : (delta < -kEps);
            string arrow = better ? "▴" : (worse ? "▾" : "");
            string sign = delta > kEps ? "+" : (delta < -kEps ? "-" : "");
            float absDelta = Mathf.Abs(delta);
            label.text = $"{arrow} {sign}{absDelta.ToString(format)}{suffix}";
            label.color = better ? s_betterColor : (worse ? s_worseColor : s_neutralColor);
        }

        void RefreshCost()
        {
            var info = PickaxeDef.Get(_focusIdx);
            var cost = info.Cost;
            if (cost.IsFree)
            {
                if (freeLabel != null) freeLabel.SetActive(true);
                if (costRows != null)
                {
                    foreach (var r in costRows)
                        if (r != null && r.root != null) r.root.SetActive(false);
                }
                return;
            }
            if (freeLabel != null) freeLabel.SetActive(false);
            if (costRows == null) return;

            int n = cost.Currencies.Length;
            for (int i = 0; i < costRows.Length; i++)
            {
                var row = costRows[i];
                if (row == null || row.root == null) continue;
                if (i >= n) { row.root.SetActive(false); continue; }
                row.root.SetActive(true);

                if (row.icon != null)
                {
                    var ore = cost.Currencies[i];
                    var oreBar = SpriteLibrary.GetOreBarIcon(ore);
                    if (oreBar == null) oreBar = SpriteLibrary.GetOreHudIcon(ore);
                    if (oreBar != null)
                    {
                        row.icon.sprite = oreBar;
                        row.icon.color = Color.white;
                        row.icon.preserveAspect = true;
                    }
                    else
                    {
                        row.icon.sprite = RockSpriteFactory.GetRockSprite();
                        row.icon.color = RockSpriteFactory.GetOreColor(ore);
                        row.icon.preserveAspect = false;
                    }
                    row.icon.raycastTarget = false;
                }
                if (row.amountLabel != null)
                {
                    int discounted = AnvilManager.DiscountedCost(cost.Amounts[i]);

                    row.amountLabel.text = Rebuild.Util.FormatNumbers.Format(discounted);


                    var ore = cost.Currencies[i];
                    long have = GoldAndOreMechanics.Instance != null ? GoldAndOreMechanics.Instance.GetCount(ore) : 0;
                    row.amountLabel.color = have >= discounted ? s_neutralColor : s_worseColor;
                }
            }
        }



        void RefreshActionState()
        {
            if (AnvilManager.Instance == null) return;
            var anvil = AnvilManager.Instance;
            bool crafted = anvil.IsCrafted(_focusIdx);
            bool equipped = anvil.IsEquipped(_focusIdx);
            bool canAfford = anvil.CanAfford(_focusIdx);
            bool isBusy = anvil.IsCraftingAny;

            if (craftingOverlay != null && !isBusy) craftingOverlay.SetActive(false);
            if (equippedBadge != null) equippedBadge.SetActive(equipped);

            if (actionLabel != null)
            {
                if (equipped) actionLabel.text = "장착됨";
                else if (crafted) actionLabel.text = "장착";
                else actionLabel.text = "복원";
            }
            if (actionImage != null)
            {
                Color c;
                if (equipped) c = s_equippedColor;
                else if (crafted) c = s_equipReadyColor;
                else c = canAfford ? s_craftAffordColor : s_craftDimColor;
                actionImage.color = c;
                _baseActionColor = c;
            }
            if (actionButton != null)
            {



                actionButton.interactable = !equipped && (crafted || canAfford);
            }
        }

        void RefreshCounter()
        {
            if (counterLabel != null) counterLabel.text = $"{_focusIdx + 1} / {PickaxeDef.Count}";
        }

        IEnumerator RedFlash()
        {
            if (actionImage == null) yield break;
            float dur = 0.20f;
            float t = 0f;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                actionImage.color = Color.Lerp(s_flashColor, _baseActionColor, Mathf.Clamp01(t / dur));
                yield return null;
            }
            actionImage.color = _baseActionColor;
            _flashCo = null;
        }

        IEnumerator SpotlightPulse()
        {
            if (spotlightRay == null) yield break;
            Color baseColor = spotlightRay.color;
            while (true)
            {
                float k = Mathf.Sin(Time.unscaledTime * 1.6f) * 0.5f + 0.5f;
                float alpha = Mathf.Lerp(0.30f, 0.55f, k);
                spotlightRay.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
                yield return null;
            }
        }
    }
}
