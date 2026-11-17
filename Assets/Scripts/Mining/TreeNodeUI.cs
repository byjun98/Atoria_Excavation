using System.Collections;
using Rebuild.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Rebuild.Mining
{




    public sealed class TreeNodeUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] UpgradeId id;
        [SerializeField] Image iconImage;
        [SerializeField] Image frameImage;
        [SerializeField] Image faceImage;
        [SerializeField] TMP_Text levelLabel;
        [SerializeField] Button buyButton;
        [SerializeField] CanvasGroup visibilityGroup;

        Color _baseFrameColor;
        Coroutine _flashCo;
        SkillTreeDragController _dragController;
        SkillPurchaseFeedback _purchaseFeedback;
        static TreeNodeUI s_hoveredNode;
        public static event System.Action<UpgradeId?> HoveredNodeChanged;




        UpgradeId _parentId;
        bool _hasParent;

        public UpgradeId Id => id;
        public RectTransform Rect => (RectTransform)transform;
        public RectTransform VisualRect => frameImage != null ? frameImage.rectTransform : Rect;

        internal static bool IsDiscovered(UpgradeId upgrade, SkillTreeManager manager)
        {
            return manager == null || manager.GetLevel(upgrade) > 0 || manager.IsPrereqMet(upgrade);
        }

        public void ConfigureAppearance(Image face, Image border)
        {
            faceImage = face;
            frameImage = border;
            if (buyButton != null) buyButton.transition = Selectable.Transition.None;
            RefreshState();
        }


        public void Init(UpgradeId newId)
        {
            id = newId;
            CacheParent();
            EnsureVisibilityGroup();
            ApplyStaticInfo();
            RefreshState();
        }

        void Awake()
        {
            EnsureVisibilityGroup();
            if (buyButton != null)
            {
                buyButton.onClick.RemoveAllListeners();
                buyButton.onClick.AddListener(HandleClick);
            }
            CacheParent();
            ApplyStaticInfo();
        }

        void CacheParent()
        {
            var p = SkillTreeLayout.GetParent(id);
            _hasParent = p.HasValue;
            if (_hasParent) _parentId = p.Value;
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
            RefreshState();
        }

        void OnDisable()
        {
            ClearHover();
            if (_flashCo != null) StopCoroutine(_flashCo);
            _flashCo = null;
            if (frameImage != null) frameImage.color = _baseFrameColor;
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
            if (iconImage != null)
            {
                var info = UpgradeDef.Get(id);



                var userIcon = SpriteLibrary.GetSkillTreeIcon(id);
                if (userIcon != null)
                {
                    iconImage.sprite = userIcon;
                    iconImage.color = Color.white;
                    iconImage.preserveAspect = true;
                    iconImage.raycastTarget = false;
                    return;
                }
                if (iconImage.sprite == null)
                    iconImage.sprite = RockSpriteFactory.GetRockSprite();
                var tint = RockSpriteFactory.GetOreColor(info.Currency);

                tint.a = 0.85f;
                iconImage.color = tint;
                iconImage.preserveAspect = true;
                iconImage.raycastTarget = false;
            }
        }

        void HandleUpgradeChanged(UpgradeId changed, int newLevel)
        {


            if (changed == id || (_hasParent && changed == _parentId))
                RefreshState();
        }

        void HandleOreChanged(OreType _, long __) => RefreshState();

        void HandlePurchaseFailed(UpgradeId failed)
        {
            if (failed != id) return;
            if (!gameObject.activeInHierarchy) return;
            if (_flashCo != null) StopCoroutine(_flashCo);
            _flashCo = StartCoroutine(RedFlash());
        }

        public void RefreshState()
        {
            var st = SkillTreeManager.Instance;
            int lv = st != null ? st.GetLevel(id) : 0;
            int max = st != null ? st.GetMaxLevel(id) : UpgradeDef.Get(id).MaxLevel;

            if (levelLabel != null)
                levelLabel.text = lv > 0 ? $"{lv}/{max}" : string.Empty;


            bool prereqMet = st == null || st.IsPrereqMet(id);
            bool maxed = st != null && st.IsMaxed(id);
            bool canAfford = false;
            if (prereqMet && !maxed && st != null && GoldAndOreMechanics.Instance != null)
            {
                canAfford = GoldAndOreMechanics.Instance.GetCount(st.GetCurrency(id))
                            >= st.GetNextPrice(id);
            }

            bool discovered = IsDiscovered(id, st);
            ApplyVisibility(discovered);
            if (!discovered)
            {
                if (s_hoveredNode == this) SkillTreeTooltipUI.Instance?.Hide();
                ClearHover();
                return;
            }

            Color frame;
            if (maxed) frame = MuseumUiTheme.Gold;
            else if (lv == 0 && _hasParent)
                frame = canAfford ? Color.Lerp(MuseumUiTheme.Locked, MuseumUiTheme.Jade, .45f) : MuseumUiTheme.Locked;
            else if (canAfford) frame = MuseumUiTheme.Jade;
            else if (lv > 0) frame = Color.Lerp(MuseumUiTheme.Locked, MuseumUiTheme.Jade, 0.6f);
            else frame = MuseumUiTheme.Muted;

            _baseFrameColor = frame;
            if (frameImage != null) frameImage.color = frame;
            if (faceImage != null) faceImage.color = lv == 0 && _hasParent ? MuseumUiTheme.Panel : MuseumUiTheme.Surface;
            if (levelLabel != null) levelLabel.color = maxed ? MuseumUiTheme.Gold : MuseumUiTheme.Muted;
            if (buyButton != null) buyButton.interactable = prereqMet && canAfford;


            if (iconImage != null)
            {
                var c = iconImage.color;
                c.a = lv == 0 && _hasParent ? .48f : 1f;
                iconImage.color = c;
            }
        }

        void EnsureVisibilityGroup()
        {
            if (visibilityGroup != null) return;
            visibilityGroup = GetComponent<CanvasGroup>();
            if (visibilityGroup == null) visibilityGroup = gameObject.AddComponent<CanvasGroup>();
        }

        void ApplyVisibility(bool visible)
        {
            EnsureVisibilityGroup();
            visibilityGroup.alpha = visible ? 1f : 0f;
            visibilityGroup.interactable = visible;
            visibilityGroup.blocksRaycasts = visible;
            if (!visible && buyButton != null) buyButton.interactable = false;
        }

        void HandleClick()
        {
            if (_dragController == null) _dragController = GetComponentInParent<SkillTreeDragController>();
            if (_dragController != null && _dragController.ShouldSuppressClick) return;
            var manager = SkillTreeManager.Instance;
            if (manager == null || !manager.TryPurchase(id)) return;
            if (_purchaseFeedback == null) _purchaseFeedback = GetComponent<SkillPurchaseFeedback>();
            if (_purchaseFeedback != null) _purchaseFeedback.PlayBought(manager.IsMaxed(id));
        }

        IEnumerator RedFlash()
        {
            if (frameImage == null) yield break;
            var flash = MuseumUiTheme.Danger;
            flash.a = _baseFrameColor.a;
            const float dur = 0.20f;
            float t = 0f;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                frameImage.color = Color.Lerp(flash, _baseFrameColor, Mathf.Clamp01(t / dur));
                yield return null;
            }

            RefreshState();
            _flashCo = null;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_dragController == null) _dragController = GetComponentInParent<SkillTreeDragController>();
            if (_dragController != null && _dragController.ShouldSuppressClick) return;
            s_hoveredNode = this;
            HoveredNodeChanged?.Invoke(id);
            var rt = (RectTransform)transform;
            SkillTreeTooltipUI.Instance?.Show(id, rt.position);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            ClearHover();
            SkillTreeTooltipUI.Instance?.Hide();
        }

        void ClearHover()
        {
            if (s_hoveredNode != this) return;
            s_hoveredNode = null;
            HoveredNodeChanged?.Invoke(null);
        }
    }
}
