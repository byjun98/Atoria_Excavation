using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Rebuild.Mining
{
    /// <summary>one pickaxe card inside the Anvil panel.
    /// Subscribes to AnvilManager and GoldAndOreMechanics to keep the status button live.</summary>
    public sealed class PickaxeNodeUI : MonoBehaviour
    {
        [SerializeField] int idx;
        [SerializeField] Image iconImage;
        [SerializeField] TMP_Text nameLabel;
        [SerializeField] TMP_Text damageLabel;
        [SerializeField] Transform costContainer;  // pre-built cost rows go here (1-2 children)
        [SerializeField] Button actionButton;
        [SerializeField] Image actionImage;
        [SerializeField] TMP_Text actionLabel;
        [SerializeField] GameObject craftingOverlay; // shows "Crafting..." text while busy

        Color _baseActionColor;
        Coroutine _flashCo;

        static readonly Color s_equippedColor = new Color(0.30f, 0.35f, 0.30f, 1f);
        static readonly Color s_equipReadyColor = new Color(0.30f, 0.65f, 0.85f, 1f);
        static readonly Color s_craftAffordColor = new Color(1f, 0.84f, 0f, 1f);
        static readonly Color s_craftDimColor = new Color(0.45f, 0.45f, 0.45f, 1f);
        static readonly Color s_flashColor = new Color(0.95f, 0.30f, 0.30f, 1f);

        public int Idx => idx;

        public void SetIdx(int newIdx)
        {
            idx = newIdx;
            ApplyStaticInfo();
            Refresh();
        }

        void Awake()
        {
            if (actionImage != null) _baseActionColor = actionImage.color;
            if (actionButton != null)
            {
                actionButton.onClick.RemoveAllListeners();
                actionButton.onClick.AddListener(HandleClick);
            }
            if (craftingOverlay != null) craftingOverlay.SetActive(false);
            ApplyStaticInfo();
        }

        void OnEnable()
        {
            if (AnvilManager.Instance != null)
            {
                AnvilManager.Instance.OnPickaxeChanged += HandlePickaxeChanged;
                AnvilManager.Instance.OnCraftStateChanged += HandleCraftStateChanged;
                AnvilManager.Instance.OnCraftingStarted += HandleCraftingStarted;
                AnvilManager.Instance.OnCraftFailed += HandleCraftFailed;
            }
            if (GoldAndOreMechanics.Instance != null)
                GoldAndOreMechanics.Instance.OnOreChanged += HandleOreChanged;
            Refresh();
        }

        void OnDisable()
        {
            if (AnvilManager.Instance != null)
            {
                AnvilManager.Instance.OnPickaxeChanged -= HandlePickaxeChanged;
                AnvilManager.Instance.OnCraftStateChanged -= HandleCraftStateChanged;
                AnvilManager.Instance.OnCraftingStarted -= HandleCraftingStarted;
                AnvilManager.Instance.OnCraftFailed -= HandleCraftFailed;
            }
            if (GoldAndOreMechanics.Instance != null)
                GoldAndOreMechanics.Instance.OnOreChanged -= HandleOreChanged;
        }

        void ApplyStaticInfo()
        {
            var info = PickaxeDef.Get(idx);
            if (nameLabel != null) nameLabel.text = info.Name;
            // Compact "+N dmg" label matches the 14-pickaxe grid card width.
            // fractional minePower (e.g. Pickaxe 1 = +2.5). Every entry positive, "Base" branch unreachable.
            if (damageLabel != null) damageLabel.text = $"+{info.DamageBonus:0.0} dmg";
            if (iconImage != null)
            {
                // user-supplied per-pickaxe PNG via Resources (Pickaxes/Pickaxe_XX).
                // Falls back to the placeholder: rock sprite with a gray→gold gradient tint.
                var custom = SpriteLibrary.GetPickaxeSprite(idx);
                bool hasCustom = custom != null && custom != RockSpriteFactory.GetRockSprite();
                iconImage.sprite = custom;
                if (hasCustom)
                {
                    iconImage.color = Color.white;
                }
                else
                {
                    float t = idx / Mathf.Max(1f, (float)(PickaxeDef.Count - 1));
                    iconImage.color = Color.Lerp(new Color(0.55f, 0.50f, 0.45f, 1f),
                                                 new Color(1.00f, 0.84f, 0.18f, 1f), t);
                }
                iconImage.preserveAspect = false;
                iconImage.raycastTarget = false;
            }
        }

        void HandlePickaxeChanged(int _) => Refresh();
        void HandleCraftStateChanged(int changed, bool _) { if (changed == idx) Refresh(); }
        void HandleOreChanged(OreType _, long __) => Refresh();

        void HandleCraftingStarted(int started)
        {
            if (started != idx) return;
            if (craftingOverlay != null) craftingOverlay.SetActive(true);
            if (actionButton != null) actionButton.interactable = false;
        }

        void HandleCraftFailed(int failed)
        {
            if (failed != idx) return;
            if (craftingOverlay != null) craftingOverlay.SetActive(false);
            if (gameObject.activeInHierarchy)
            {
                if (_flashCo != null) StopCoroutine(_flashCo);
                _flashCo = StartCoroutine(RedFlash());
            }
            Refresh();
        }

        public void Refresh()
        {
            if (AnvilManager.Instance == null) return;
            var anvil = AnvilManager.Instance;
            bool crafted = anvil.IsCrafted(idx);
            bool equipped = anvil.IsEquipped(idx);
            bool canAfford = anvil.CanAfford(idx);
            bool isBusy = anvil.IsCraftingAny;

            if (craftingOverlay != null && !isBusy) craftingOverlay.SetActive(false);

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
                actionButton.interactable = !equipped && !isBusy && (crafted || canAfford);
            }
        }

        void HandleClick()
        {
            if (AnvilManager.Instance == null) return;
            var anvil = AnvilManager.Instance;
            if (anvil.IsCrafted(idx)) anvil.Equip(idx);
            else StartCoroutine(anvil.TryCraft(idx));
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
    }
}
