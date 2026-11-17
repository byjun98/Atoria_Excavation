using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Rebuild.Mining
{



    public sealed class UpgradeNodeUI : MonoBehaviour
    {
        [SerializeField] UpgradeId id;
        [SerializeField] Button buyButton;
        [SerializeField] Image buyImage;
        [SerializeField] TMP_Text nameLabel;
        [SerializeField] TMP_Text descLabel;
        [SerializeField] TMP_Text levelLabel;
        [SerializeField] TMP_Text priceLabel;
        [SerializeField] Image currencyIcon;

        Color _baseImageColor;
        Coroutine _flashCo;

        public UpgradeId Id => id;

        public void SetId(UpgradeId newId)
        {
            id = newId;
            ApplyStaticInfo();
            Refresh();
        }

        void Awake()
        {
            if (buyImage != null) _baseImageColor = buyImage.color;
            if (buyButton != null)
            {
                buyButton.onClick.RemoveAllListeners();
                buyButton.onClick.AddListener(HandleBuy);
            }
            ApplyStaticInfo();
        }

        void OnEnable()
        {
            if (SkillTreeManager.Instance != null)
            {
                SkillTreeManager.Instance.OnUpgradeChanged += HandleUpgradeChanged;
                SkillTreeManager.Instance.OnPurchaseFailed += HandlePurchaseFailed;
            }
            if (GoldAndOreMechanics.Instance != null)
                GoldAndOreMechanics.Instance.OnOreChanged += HandleOreChanged;
            Refresh();
        }

        void OnDisable()
        {
            if (SkillTreeManager.Instance != null)
            {
                SkillTreeManager.Instance.OnUpgradeChanged -= HandleUpgradeChanged;
                SkillTreeManager.Instance.OnPurchaseFailed -= HandlePurchaseFailed;
            }
            if (GoldAndOreMechanics.Instance != null)
                GoldAndOreMechanics.Instance.OnOreChanged -= HandleOreChanged;
        }

        void ApplyStaticInfo()
        {
            var info = UpgradeDef.Get(id);
            if (nameLabel != null) nameLabel.text = info.Name;
            if (descLabel != null) descLabel.text = info.Description;
            if (currencyIcon != null)
            {
                currencyIcon.sprite = RockSpriteFactory.GetRockSprite();
                currencyIcon.color = RockSpriteFactory.GetOreColor(info.Currency);
                currencyIcon.preserveAspect = false;
                currencyIcon.raycastTarget = false;
            }
        }

        void HandleUpgradeChanged(UpgradeId changed, int newLevel)
        {
            if (changed == id) Refresh();
        }

        void HandleOreChanged(OreType _, long __) => RefreshAffordability();

        void HandlePurchaseFailed(UpgradeId failed)
        {
            if (failed != id) return;
            if (gameObject.activeInHierarchy)
            {
                if (_flashCo != null) StopCoroutine(_flashCo);
                _flashCo = StartCoroutine(RedFlash());
            }
        }

        public void Refresh()
        {
            if (SkillTreeManager.Instance == null) return;
            var st = SkillTreeManager.Instance;
            int lv = st.GetLevel(id);
            int max = st.GetMaxLevel(id);
            if (levelLabel != null) levelLabel.text = $"{lv} / {max}";
            if (priceLabel != null)
            {
                if (st.IsMaxed(id)) priceLabel.text = "최대";

                else priceLabel.text = $"{OreLocalization.OreNameKr(st.GetCurrency(id))} {Rebuild.Util.FormatNumbers.Format(st.GetNextPrice(id))}";
            }
            RefreshAffordability();
        }

        void RefreshAffordability()
        {
            if (SkillTreeManager.Instance == null) return;
            var st = SkillTreeManager.Instance;
            bool maxed = st.IsMaxed(id);
            bool canAfford = !maxed && GoldAndOreMechanics.Instance != null
                && GoldAndOreMechanics.Instance.GetCount(st.GetCurrency(id)) >= st.GetNextPrice(id);
            if (buyButton != null) buyButton.interactable = canAfford;
            if (buyImage != null)
            {

                Color c = _baseImageColor;
                if (maxed) c = new Color(0.35f, 0.35f, 0.35f, _baseImageColor.a);
                else if (!canAfford) c = new Color(_baseImageColor.r * 0.6f, _baseImageColor.g * 0.6f, _baseImageColor.b * 0.6f, _baseImageColor.a);
                buyImage.color = c;
            }
        }

        void HandleBuy()
        {
            if (SkillTreeManager.Instance == null) return;
            SkillTreeManager.Instance.TryPurchase(id);
        }

        IEnumerator RedFlash()
        {
            if (buyImage == null) yield break;
            var flash = new Color(0.95f, 0.3f, 0.3f, _baseImageColor.a);
            float dur = 0.18f;
            float t = 0f;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                buyImage.color = Color.Lerp(flash, _baseImageColor, Mathf.Clamp01(t / dur));
                yield return null;
            }

            RefreshAffordability();
            _flashCo = null;
        }
    }
}
