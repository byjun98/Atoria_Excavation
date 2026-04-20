using System;
using System.Collections;
using Rebuild.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Rebuild.Mining
{
    /// <summary>Anvil carousel UI. Replaces the 14-grid card layout with a single
    /// spotlight slot + left/right arrows + 4-stat panel (Mine Time / Power / 2X / Area) + cost rows
    /// + action button. Subscribes to AnvilManager and GoldAndOreMechanics to keep state live.
    /// Delta lines vs the currently equipped pickaxe show ▴/▾ in 옥색 (better) or 단청 적 (worse).</summary>
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
        [SerializeField] Image spotlightRay;     // optional — pulsed alpha for "shine" feel

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
        // hold-to-craft.
        [Tooltip("Sits on the same GameObject as actionButton; relays pointer down/up.")]
        [SerializeField] PointerHoldForwarder actionHoldForwarder;
        [Tooltip("Radial-fill Image (FillMethod.Radial360) painted on top of the action button while crafting.")]
        [SerializeField] Image craftFillCircle;
        [Tooltip("'복원 중.../..../.....' dot-animation label shown while holding to craft.")]
        [SerializeField] TMP_Text craftingDotsLabel;

        [Header("Arrows")]
        [SerializeField] Button leftArrow;
        [SerializeField] Button rightArrow;

        // skin selector. spotlight 아래 1줄 (Prev "<" + Label + Next ">"). idx 13 (마지막
        // 곡괭이) 면 skin variant 없으므로 selector 전체 hide. 기존에 PNG 가 없어도 0/1/2 선택은 가능
        // (시각 차이 없음 — SpriteLibrary 가 base sprite 로 fallback).
        [Header("Skin selector")]
        [SerializeField] GameObject skinSelectorRoot;
        [SerializeField] Button skinPrevBtn;
        [SerializeField] Button skinNextBtn;
        [SerializeField] TMP_Text skinLabel;

        // equipped-pickaxe sidecar (top-right of carousel). Lets the player
        // see what's currently equipped while comparing other pickaxes in the spotlight.
        [Header("Equipped sidecar")]
        [SerializeField] GameObject equippedSidecarRoot;
        [SerializeField] Image equippedIcon;
        [SerializeField] TMP_Text equippedNameLabel;

        // celebration popup parent. Anchored RectTransform centered on the carousel
        // (or its panel) so PickaxeCraftPopupFx spawns where the player is already looking.
        [Header("Craft popup")]
        [SerializeField] RectTransform popupParent;

        int _focusIdx;
        Color _baseActionColor;
        Coroutine _flashCo;
        Coroutine _pulseCo;
        bool _completedThisHold;   // suppresses OnPointerUp cancel after a finished craft.
        bool _holdingForCraft;     // True between Down and Up while the focused pickaxe is mid-craft.

        // 단청 톤. DancheongPalette 와 동일 — runtime 의존성 줄이려 const 로 박음.
        static readonly Color s_equippedColor    = new Color32(0x36, 0x5E, 0x3B, 0xFF); // 단청 녹
        static readonly Color s_equipReadyColor  = new Color32(0x4A, 0x7A, 0x4E, 0xFF); // 단청 녹 brighter
        static readonly Color s_craftAffordColor = new Color32(0xC8, 0x44, 0x3A, 0xFF); // 단청 적
        static readonly Color s_craftDimColor    = new Color32(0x5A, 0x55, 0x50, 0xFF); // 회갈 disabled
        static readonly Color s_flashColor       = new Color(0.95f, 0.30f, 0.30f, 1f);
        static readonly Color s_betterColor      = new Color32(0x66, 0xCC, 0xAA, 0xFF); // 옥색
        static readonly Color s_worseColor       = new Color32(0xE0, 0x6A, 0x55, 0xFF); // 주홍 옅음
        static readonly Color s_neutralColor     = new Color32(0xF5, 0xE6, 0xD3, 0xFF); // 한지 흰

        public int FocusIdx => _focusIdx;

        void Awake()
        {
            if (actionImage != null) _baseActionColor = actionImage.color;
            if (actionButton != null)
            {
                // actionButton.onClick is no longer used; hold-to-craft is driven
                // by PointerHoldForwarder.OnDown/OnUp. Clear any prior listeners defensively.
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
            // skin selector buttons.
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

        /// <summary>RingSprite is created with HideFlags.HideAndDontSave so
        /// it never serialises into the scene, which means after every domain reload the bound
        /// reference is stale and the Image falls back to a white quad. Re-binding the sprite,
        /// fill type / method / origin every OnEnable guarantees the gauge always reads as a ring
        /// fill that radials cleanly from the top.</summary>
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

        /// <summary>v2 — the legacy craftingOverlay (full-screen 검정 panel
        /// with "복원 중..." text) was authored with Image.raycastTarget=true, which would
        /// intercept the pointer on the same frame it activates and fire actionButton's
        /// OnPointerExit → CancelCraft. Force the Image to NOT consume the pointer so the
        /// pointer keeps hitting actionButton beneath the overlay.</summary>
        void EnsureOverlayState()
        {
            if (craftingOverlay == null) return;
            var img = craftingOverlay.GetComponent<Image>();
            if (img != null) img.raycastTarget = false;
            // Also walk children — the "복원 중..." TMP shouldn't block pointer either.
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
                actionHoldForwarder.OnExit = HandleActionUp; // exit = same as release for cancel
            }

            EnsureFillCircleState();
            EnsureOverlayState();

            if (spotlightRay != null && _pulseCo == null)
                _pulseCo = StartCoroutine(SpotlightPulse());

            RefreshAll();
        }

        void OnDisable()
        {
            // If the panel hides mid-craft, abort cleanly so we don't leak gauge state.
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

        // ---- ------------ Public API ------------ ----
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
            // moving focus mid-craft aborts the gauge so the player doesn't pay for
            // a pickaxe they walked away from. CancelCraft fires OnCraftCancelled which clears the
            // gauge visual.
            if (AnvilManager.Instance != null && AnvilManager.Instance.IsCraftingAny)
                AnvilManager.Instance.CancelCraft();
            _holdingForCraft = false;
        }

        // ---- ------------ Event handlers ------------ ----
        void HandlePickaxeChanged(int _) => RefreshAll();
        void HandleSkinChanged(int idx, int _)
        {
            // 현재 focus 의 skin 만 바뀌어도 bigIcon refresh 필요 (다른 idx 면 selector 만 갱신).
            if (idx == _focusIdx) { ApplyStaticInfo(); RefreshSkinSelector(); }
        }

        void HandleCraftStateChanged(int idx, bool crafted)
        {
            if (crafted && idx == _focusIdx)
            {
                // the craft finished while the user was still holding. Mark the
                // hold as "completed" so the next OnPointerUp doesn't attempt a (now-irrelevant)
                // cancel. The gauge briefly stays full before the next RefreshAll clears it.
                _completedThisHold = true;
                _holdingForCraft = false;
                if (craftFillCircle != null) craftFillCircle.fillAmount = 0f;
                if (craftingDotsLabel != null) craftingDotsLabel.text = "";
            }
            // celebration popup on every successful craft (focus-agnostic, matching
            // original TheAnvil behavior). Fires once per craft completion. If the clip is wired
            // FinishedCrafting also rings out under the visual (best-effort; silent if missing).
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
            // 🚨 v2 — overlay IS re-activated, but with raycastTarget=false
            // (EnsureOverlayState) so the pointer keeps hitting actionButton beneath. The big
            // "복원 중..." text in the overlay matches the original UX where craftingFrame
            // pops up during the hold. Originally we disabled it; user reported the missing
            // visual.
            //
            // Also do NOT disable actionButton via Button.interactable=false — flipping to
            // disabledColor is a distracting flash and risks suppressing IPointerExit/Up
            // delivery on some EventSystem paths. PointerHoldForwarder receives the events
            // regardless via the IPointer*Handler interfaces.
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
                // original TheAnvil ticks dotCount every 0.35s (TheAnvil.cs:1830).
                // Convert t01 back to elapsed seconds: t01 × CraftDurationSec. Bucketing into
                // 0.35s slots gives the same cadence regardless of frame rate.
                float total = AnvilManager.CraftDurationSec;
                float elapsed = t01 * total;
                int dots = Mathf.FloorToInt(elapsed / 0.35f) % 4;
                craftingDotsLabel.text = "복원 중" + new string('.', dots);
            }
        }

        void HandleCraftCancelled(int idx)
        {
            if (idx != _focusIdx) return;
            // Silent reset — no RedFlash because the user actively released; this isn't an error.
            _holdingForCraft = false;
            if (craftingOverlay != null) craftingOverlay.SetActive(false);
            if (craftFillCircle != null) craftFillCircle.fillAmount = 0f;
            if (craftingDotsLabel != null) craftingDotsLabel.text = "";
            RefreshActionState();
        }

        // ---- ------------ Hold-to-craft action button ------------ ----
        void HandleActionDown()
        {
            if (AnvilManager.Instance == null) return;
            var anvil = AnvilManager.Instance;

            // Already crafted → single-press Equip, no hold needed.
            if (anvil.IsCrafted(_focusIdx))
            {
                anvil.Equip(_focusIdx);
                return;
            }

            // Not crafted → check affordability up front so a hold that can't ever succeed
            // surfaces the CantAfford SFX + red flash immediately (BeginCraft does this for us).
            _completedThisHold = false;
            _holdingForCraft = true;
            anvil.BeginCraft(_focusIdx);
        }

        void HandleActionUp()
        {
            // Suppress cancel when the gauge just completed — the up-event would otherwise abort
            // a craft that already succeeded (the cancel would no-op anyway since IsCraftingAny
            // is false, but we still clear the hold flag).
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

        // ---- ------------ Refresh ------------ ----
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

        /// <summary>focus 된 pickaxe 의 skin 0/1/2 표시 + idx 13 (마지막) 면 selector 자체 hide.</summary>
        void RefreshSkinSelector()
        {
            var anvil = AnvilManager.Instance;
            bool hasVariants = anvil != null && anvil.HasSkinVariants(_focusIdx);
            bool unlocked = anvil != null && anvil.IsCrafted(_focusIdx);
            // 변형 없거나 아직 미해금이면 selector 숨김 — 잠긴 곡괭이에 skin 선택 노출 X.
            bool visible = hasVariants && unlocked;
            if (skinSelectorRoot != null) skinSelectorRoot.SetActive(visible);
            if (!visible) return;
            if (skinLabel != null)
            {
                int s = anvil.GetSkinChosen(_focusIdx);
                skinLabel.text = s switch { 0 => "기본", 1 => "스킨 1", 2 => "스킨 2", _ => "기본" };
            }
        }

        /// <summary>prev/next 버튼 핸들러. AnvilManager.CycleSkin 호출 후 selector + bigIcon refresh.</summary>
        void CycleSkinAtFocus(int dir)
        {
            var anvil = AnvilManager.Instance;
            if (anvil == null) return;
            if (!anvil.HasSkinVariants(_focusIdx)) return;
            if (!anvil.IsCrafted(_focusIdx)) return; // 미해금 곡괭이 skin 변경 차단
            anvil.CycleSkin(_focusIdx, dir);
            // OnSkinChanged 이벤트로 자동 RefreshAll 호출되지만, immediate UI feedback 위해 명시 호출.
            ApplyStaticInfo();
            RefreshSkinSelector();
        }

        /// <summary>show the currently equipped pickaxe in a small thumbnail card
        /// regardless of which one is in the spotlight focus.</summary>
        void RefreshEquippedSidecar()
        {
            if (AnvilManager.Instance == null) return;
            int eqIdx = AnvilManager.Instance.CurrentIdx;
            var info = PickaxeDef.Get(eqIdx);

            if (equippedNameLabel != null) equippedNameLabel.text = info.Name;

            if (equippedIcon != null)
            {
                // equipped 의 skin variant 도 sidecar 에 반영.
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
            // lock the name + sprite of an un-crafted pickaxe so the player has to
            // earn the reveal. Mirrors original TheAnvil's color=Color.black silhouette pre-craft.
            bool unlocked = AnvilManager.Instance != null && AnvilManager.Instance.IsCrafted(_focusIdx);

            if (nameLabel != null) nameLabel.text = unlocked ? info.Name : "??????";

            if (bigIcon != null)
            {
                // equipped 한 skin variant 반영. GetSkinChosen 은 idx 13 도 안전 (0 반환).
                int skin = AnvilManager.Instance != null ? AnvilManager.Instance.GetSkinChosen(_focusIdx) : 0;
                var custom = SpriteLibrary.GetPickaxeSprite(_focusIdx, skin);
                bool hasCustom = custom != null && custom != RockSpriteFactory.GetRockSprite();
                bigIcon.sprite = custom;
                if (!unlocked)
                {
                    // Silhouette — dark tint matches the original black mask, alpha kept so the
                    // outline is still visible against the carousel background.
                    bigIcon.color = new Color(0.08f, 0.08f, 0.10f, 0.92f);
                }
                else if (hasCustom)
                {
                    bigIcon.color = Color.white;
                }
                else
                {
                    // placeholder gradient — gray→gold by idx.
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
            // masked stats for un-crafted pickaxes. The cost rows still render so
            // the player knows what's needed to unlock.
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
            // every pickaxe has positive minePower in the original (Pickaxe 1 = +2.5),
            // so the legacy "기본" branch is unreachable. Format with one decimal place for fractional values.
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

        /// <summary>placeholder delta label for un-crafted pickaxes. Keep the slot
        /// visible (no SetActive false) to avoid layout shift when the user crafts the pickaxe
        /// and the real delta replaces it.</summary>
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
            string arrow = better ? "▴" : (worse ? "▾" : "—");
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
                    // 통일된 FormatNumbers 포맷 (100k 이상 축약).
                    row.amountLabel.text = Rebuild.Util.FormatNumbers.Format(discounted);

                    // Highlight unaffordable rows.
                    var ore = cost.Currencies[i];
                    long have = GoldAndOreMechanics.Instance != null ? GoldAndOreMechanics.Instance.GetCount(ore) : 0;
                    row.amountLabel.color = have >= discounted ? s_neutralColor : s_worseColor;
                }
            }
        }

        // 자체 FormatCostAmount 제거, Rebuild.Util.FormatNumbers.Format 으로 통일.

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
                // keep interactable true during hold so PointerHoldForwarder
                // continues to receive OnPointerUp/Exit. BeginCraft self-rejects re-entry via
                // IsCraftingAny so spamming the button mid-hold is harmless.
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
                float k = Mathf.Sin(Time.unscaledTime * 1.6f) * 0.5f + 0.5f; // 0..1
                float alpha = Mathf.Lerp(0.30f, 0.55f, k);
                spotlightRay.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
                yield return null;
            }
        }
    }
}
