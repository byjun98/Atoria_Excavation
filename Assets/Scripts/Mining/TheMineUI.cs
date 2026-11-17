using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Rebuild.Mining
{


    public sealed class TheMineUI : MonoBehaviour
    {
        [Header("Progress")]
        [SerializeField] Slider progressSlider;
        [SerializeField] Image lastOreIcon;
        [SerializeField] TMP_Text lastOreLabel;
        [SerializeField] TMP_Text statsLabel;

        [Header("Speed upgrade")]
        [SerializeField] Button speedButton;
        [SerializeField] Image speedButtonImage;
        [SerializeField] TMP_Text speedLevelLabel;
        [SerializeField] TMP_Text speedPriceLabel;

        [Header("Material upgrade")]
        [SerializeField] Button materialButton;
        [SerializeField] Image materialButtonImage;
        [SerializeField] TMP_Text materialLevelLabel;
        [SerializeField] TMP_Text materialPriceLabel;

        [Header("Popup tween")]
        [Tooltip("MineEntranceVisual — fly-from origin for tick popups. Null = skip popup spawn (e.g. user PNG missing).")]
        [SerializeField] RectTransform popupSourceRect;
        [Tooltip("Left-side ResourcePanel — popup fly-to destination resolved per ore type.")]
        [SerializeField] ResourcePanelUI popupTargetPanel;

        Color _baseSpeedColor;
        Color _baseMaterialColor;
        Coroutine _iconFlashCo;
        Coroutine _speedFlashCo;
        Coroutine _materialFlashCo;

        void Awake()
        {
            if (speedButtonImage != null) _baseSpeedColor = speedButtonImage.color;
            if (materialButtonImage != null) _baseMaterialColor = materialButtonImage.color;
            if (speedButton != null)
            {
                speedButton.onClick.RemoveAllListeners();


                speedButton.onClick.AddListener(HandleSpeedClick);
            }
            if (materialButton != null)
            {
                materialButton.onClick.RemoveAllListeners();
                materialButton.onClick.AddListener(() => MineManager.Instance?.TryUpgradeMaterial());
            }
        }

        void HandleSpeedClick()
        {
            var mm = MineManager.Instance;
            if (mm == null) return;
            if (!mm.IsUnlocked) mm.TryPurchaseUnlock();
            else                mm.TryUpgradeSpeed();
        }

        void OnEnable()
        {
            if (MineManager.Instance != null)
            {
                MineManager.Instance.OnProgressChanged += HandleProgress;
                MineManager.Instance.OnTick += HandleTick;
                MineManager.Instance.OnUpgradeChanged += Refresh;
                MineManager.Instance.OnUpgradeFailed += HandleUpgradeFailed;
                MineManager.Instance.OnUnlockChanged += Refresh;
            }
            if (GoldAndOreMechanics.Instance != null)
                GoldAndOreMechanics.Instance.OnOreChanged += HandleOreChanged;
            Refresh();
        }

        void OnDisable()
        {
            if (MineManager.Instance != null)
            {
                MineManager.Instance.OnProgressChanged -= HandleProgress;
                MineManager.Instance.OnTick -= HandleTick;
                MineManager.Instance.OnUpgradeChanged -= Refresh;
                MineManager.Instance.OnUpgradeFailed -= HandleUpgradeFailed;
                MineManager.Instance.OnUnlockChanged -= Refresh;
            }
            if (GoldAndOreMechanics.Instance != null)
                GoldAndOreMechanics.Instance.OnOreChanged -= HandleOreChanged;
        }

        void HandleProgress(float p)
        {
            if (progressSlider != null) progressSlider.value = p;
        }

        void HandleTick(OreType ore, int amount)
        {
            if (lastOreIcon != null)
            {
                var barSprite = SpriteLibrary.GetOreBarIcon(ore);
                if (barSprite == null) barSprite = SpriteLibrary.GetOreHudIcon(ore);
                if (barSprite != null)
                {
                    lastOreIcon.sprite = barSprite;
                    lastOreIcon.color = Color.white;
                    lastOreIcon.preserveAspect = true;
                }
                else
                {
                    lastOreIcon.sprite = RockSpriteFactory.GetRockSprite();
                    lastOreIcon.color = RockSpriteFactory.GetOreColor(ore);
                    lastOreIcon.preserveAspect = false;
                }
                lastOreIcon.raycastTarget = false;
                if (gameObject.activeInHierarchy)
                {
                    if (_iconFlashCo != null) StopCoroutine(_iconFlashCo);
                    _iconFlashCo = StartCoroutine(IconFlash());
                }
            }
            if (lastOreLabel != null) lastOreLabel.text = $"최근 획득: {OreLocalization.OreNameKr(ore)} 주괴 {amount}개";




            if (gameObject.activeInHierarchy
                && popupSourceRect != null && popupTargetPanel != null && amount > 0)
            {
                for (int i = 0; i < amount; i++)
                    SpawnOnePopup(ore, i);
            }
        }

        void SpawnOnePopup(OreType ore, int index)
        {



            var hostCanvas = popupSourceRect.GetComponentInParent<Canvas>();
            if (hostCanvas == null) return;
            var rootRT = (RectTransform)hostCanvas.rootCanvas.transform;
            Camera cam = hostCanvas.rootCanvas.worldCamera;



            Vector2 startLocal;
            Vector2 entranceScreen = RectTransformUtility.WorldToScreenPoint(cam, popupSourceRect.position);
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rootRT, entranceScreen, cam, out startLocal))
                startLocal = Vector2.zero;
            startLocal += new Vector2(UnityEngine.Random.Range(-60f, 60f), UnityEngine.Random.Range(-30f, 30f));


            Vector2 targetLocal;
            var rowRT = popupTargetPanel.GetRowIconRT(ore);
            if (rowRT == null)
            {

                return;
            }
            Vector2 targetScreen = RectTransformUtility.WorldToScreenPoint(cam, rowRT.position);
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rootRT, targetScreen, cam, out targetLocal))
                targetLocal = Vector2.zero;

            var sprite = SpriteLibrary.GetOreBarIcon(ore);
            if (sprite == null) sprite = SpriteLibrary.GetOreHudIcon(ore);
            Color tint = Color.white;
            if (sprite == null)
            {
                sprite = RockSpriteFactory.GetRockSprite();
                tint = RockSpriteFactory.GetOreColor(ore);
            }


            float delay = index * 0.05f;
            var capturedOre = ore;
            MineOrePopupFx.Spawn(rootRT, startLocal, targetLocal, sprite, tint,
                size: 60f, spawnDelay: delay,
                onArrived: () =>
                {


                    Rebuild.Audio.AudioManager.Instance?.Play("MineCollect");
                    if (popupTargetPanel != null) popupTargetPanel.PulseRow(capturedOre);
                });
        }

        void HandleOreChanged(OreType _, long __) => RefreshAffordability();

        void HandleUpgradeFailed()
        {

            if (gameObject.activeInHierarchy)
            {
                if (speedButtonImage != null && _speedFlashCo == null)
                    _speedFlashCo = StartCoroutine(RedFlash(speedButtonImage, _baseSpeedColor, () => _speedFlashCo = null));
                if (materialButtonImage != null && _materialFlashCo == null)
                    _materialFlashCo = StartCoroutine(RedFlash(materialButtonImage, _baseMaterialColor, () => _materialFlashCo = null));
            }
        }

        public void Refresh()
        {
            var mm = MineManager.Instance;
            if (mm == null) return;


            bool unlocked = mm.IsUnlocked;
            ConfigureMuseumStatusLayout(unlocked);
            if (progressSlider != null) progressSlider.gameObject.SetActive(unlocked);
            if (lastOreIcon != null) lastOreIcon.gameObject.SetActive(unlocked);
            if (lastOreLabel != null) lastOreLabel.gameObject.SetActive(unlocked);
            if (materialButton != null) materialButton.gameObject.SetActive(unlocked);

            if (!unlocked)
            {

                var unlockCurrencyKr = OreLocalization.OreNameKr(mm.UnlockCurrency);
                if (statsLabel != null)
                    statsLabel.text = $"자동 발굴터가 잠겨 있습니다.\n{unlockCurrencyKr} {MineManager.UnlockPrice}개로 열 수 있습니다.";
                if (speedLevelLabel != null) speedLevelLabel.text = "잠금 해제";
                if (speedPriceLabel != null) speedPriceLabel.text = $"{unlockCurrencyKr} {MineManager.UnlockPrice}";
                if (materialLevelLabel != null) materialLevelLabel.text = string.Empty;
                if (materialPriceLabel != null) materialPriceLabel.text = string.Empty;
                RefreshAffordability();
                return;
            }

            if (statsLabel != null)
                statsLabel.text = $"{mm.BaseMiningTime:0.0}초마다 주괴를 {mm.CurrentBarsMined}개 얻습니다.";

            if (speedLevelLabel != null)
                speedLevelLabel.text = $"속도  {mm.SpeedLevel}/{mm.MaxSpeedLevel}";
            if (speedPriceLabel != null)

                speedPriceLabel.text = mm.IsSpeedMaxed ? "최대" : $"{OreLocalization.OreNameKr(mm.SpeedCurrency)} {Rebuild.Util.FormatNumbers.Format(mm.GetSpeedNextPrice())}";

            if (materialLevelLabel != null)
                materialLevelLabel.text = $"주괴  {mm.MaterialLevel}/{mm.MaxMaterialLevel}";
            if (materialPriceLabel != null)
                materialPriceLabel.text = mm.IsMaterialMaxed ? "최대" : $"{OreLocalization.OreNameKr(mm.MaterialCurrency)} {Rebuild.Util.FormatNumbers.Format(mm.GetMaterialNextPrice())}";

            RefreshAffordability();
        }

        void ConfigureMuseumStatusLayout(bool unlocked)
        {

            if (statsLabel == null || statsLabel.transform.parent == null ||
                statsLabel.transform.parent.Find("ChanceHud/MuseumChanceTitle") == null) return;
            if (lastOreIcon != null && lastOreIcon.transform.parent != null)
                lastOreIcon.transform.parent.gameObject.SetActive(unlocked);
            var rect = statsLabel.rectTransform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(0f, unlocked ? -418f : -378f);
            rect.sizeDelta = new Vector2(1080f, unlocked ? 36f : 76f);
        }

        void RefreshAffordability()
        {
            var mm = MineManager.Instance;
            var ore = GoldAndOreMechanics.Instance;
            if (mm == null) return;


            if (!mm.IsUnlocked)
            {
                bool unlockAfford = ore != null && ore.GetCount(mm.UnlockCurrency) >= MineManager.UnlockPrice;
                if (speedButton != null) speedButton.interactable = unlockAfford;
                if (speedButtonImage != null)
                    speedButtonImage.color = unlockAfford ? _baseSpeedColor : Dim(_baseSpeedColor);
                return;
            }

            bool sMax = mm.IsSpeedMaxed;
            bool sAfford = !sMax && ore != null && ore.GetCount(mm.SpeedCurrency) >= mm.GetSpeedNextPrice();
            if (speedButton != null) speedButton.interactable = sAfford;
            if (speedButtonImage != null)
                speedButtonImage.color = sMax ? new Color(0.35f, 0.35f, 0.35f, _baseSpeedColor.a)
                                              : (sAfford ? _baseSpeedColor : Dim(_baseSpeedColor));

            bool mMax = mm.IsMaterialMaxed;
            bool mAfford = !mMax && ore != null && ore.GetCount(mm.MaterialCurrency) >= mm.GetMaterialNextPrice();
            if (materialButton != null) materialButton.interactable = mAfford;
            if (materialButtonImage != null)
                materialButtonImage.color = mMax ? new Color(0.35f, 0.35f, 0.35f, _baseMaterialColor.a)
                                                : (mAfford ? _baseMaterialColor : Dim(_baseMaterialColor));
        }

        static Color Dim(Color c) => new Color(c.r * 0.6f, c.g * 0.6f, c.b * 0.6f, c.a);

        IEnumerator IconFlash()
        {
            if (lastOreIcon == null) yield break;
            var t = lastOreIcon.transform;
            var baseScale = Vector3.one;
            var bigScale = baseScale * 1.35f;
            float dur = 0.18f, k = 0f;
            while (k < dur)
            {
                k += Time.unscaledDeltaTime;
                t.localScale = Vector3.Lerp(baseScale, bigScale, Mathf.Clamp01(k / dur));
                yield return null;
            }
            k = 0f;
            while (k < dur)
            {
                k += Time.unscaledDeltaTime;
                t.localScale = Vector3.Lerp(bigScale, baseScale, Mathf.Clamp01(k / dur));
                yield return null;
            }
            t.localScale = baseScale;
            _iconFlashCo = null;
        }

        IEnumerator RedFlash(Image img, Color baseColor, System.Action clearRef)
        {
            var flash = new Color(0.95f, 0.30f, 0.30f, baseColor.a);
            float dur = 0.20f, k = 0f;
            while (k < dur)
            {
                k += Time.unscaledDeltaTime;
                img.color = Color.Lerp(flash, baseColor, Mathf.Clamp01(k / dur));
                yield return null;
            }
            img.color = baseColor;
            clearRef?.Invoke();
        }
    }
}
