using System.Collections;
using Rebuild.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Rebuild.Mining
{
    /// <summary>icon-only clickable node for the connected skill tree.
    /// Re-skin of <see cref="UpgradeNodeUI"/> with no inline name/desc/price labels — those
    /// surface in <see cref="Rebuild.UI.SkillTreeTooltipUI"/> on hover instead.
    /// State is mirrored on the frame tint (단청 palette): maxed=금, affordable=녹 hover,
    /// owned but not affordable=녹 idle, locked=회갈 disabled.</summary>
    public sealed class TreeNodeUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] UpgradeId id;
        [SerializeField] Image iconImage;
        [SerializeField] Image frameImage;
        [SerializeField] TMP_Text levelLabel;
        [SerializeField] Button buyButton;
        [SerializeField] CanvasGroup visibilityGroup;

        Color _baseFrameColor;
        Coroutine _flashCo;
        // click punch animation (scale 0.85 + rotate -14° quick punch then settle).
        Coroutine _punchCo;
        Vector3 _baseLocalScale = Vector3.one;
        Quaternion _baseLocalRotation = Quaternion.identity;
        bool _baseTransformCached;

        // track parent id locally so the OnUpgradeChanged handler can
        // refresh state when the PARENT levels up (unlocking this node) without paying for a
        // dictionary lookup per event. Cached at Init time; SkillTreeLayout is the source of truth.
        UpgradeId _parentId;
        bool _hasParent;

        // Locked 상태 (prereq unmet) tint. Darker than Disabled so the
        // visual reads as "can't even attempt" vs Disabled's "owned but can't afford right now".
        static readonly Color s_lockedFrameColor = new Color(0.16f, 0.13f, 0.11f, 1f);

        public UpgradeId Id => id;
        public RectTransform Rect => (RectTransform)transform;

        /// <summary>Setup-time entry point — partner Editor Setup script assigns the id then calls this.</summary>
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
                // prefer the user-authored per-node icon when present
                // (Sprites/SkillTree/{UpgradeId}.png). Falls back to the rock silhouette tinted
                // by the upgrade's purchase currency so each node still reads as an ore tier.
                var userIcon = SpriteLibrary.GetSkillTreeIcon(id);
                if (userIcon != null)
                {
                    iconImage.sprite = userIcon;
                    iconImage.color = Color.white;   // user art carries its own color
                    iconImage.preserveAspect = true;
                    iconImage.raycastTarget = false;
                    return;
                }
                if (iconImage.sprite == null)
                    iconImage.sprite = RockSpriteFactory.GetRockSprite();
                var tint = RockSpriteFactory.GetOreColor(info.Currency);
                // Faint tint so the icon reads as an accent, not a screaming color block.
                tint.a = 0.85f;
                iconImage.color = tint;
                iconImage.preserveAspect = true;
                iconImage.raycastTarget = false;
            }
        }

        void HandleUpgradeChanged(UpgradeId changed, int newLevel)
        {
            // Self changed → refresh level label + state.
            // Parent changed (first level purchased) → we just got unlocked: refresh.
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

            // prereq gate. Locked nodes appear darker and reject clicks.
            bool prereqMet = st == null || st.IsPrereqMet(id);
            bool maxed = st != null && st.IsMaxed(id);
            bool canAfford = false;
            if (prereqMet && !maxed && st != null && GoldAndOreMechanics.Instance != null)
            {
                canAfford = GoldAndOreMechanics.Instance.GetCount(st.GetCurrency(id))
                            >= st.GetNextPrice(id);
            }

            // progressive hide. 부모 미해금 노드는 자체 SetActive(false) 로 완전 숨김
            // (원본 게임 사진 5: 5 root + 자식 노드 hidden 상태). 부모 해금 → 자식 자동 visible.
            // root 노드 (parent=null) 는 항상 visible.
            bool shouldShow = !_hasParent
                              || (st != null && st.GetLevel(_parentId) > 0);
            ApplyVisibility(shouldShow);
            if (!shouldShow) return; // 숨김 상태에선 시각 갱신 불필요.

            Color frame;
            if (!prereqMet) frame = s_lockedFrameColor;       // 회갈 보다 어두운 잠금 상태
            else if (maxed) frame = DancheongPalette.TitleGold;
            else if (canAfford) frame = DancheongPalette.CardHover;
            else if (lv > 0) frame = DancheongPalette.CardIdle;
            else frame = DancheongPalette.Disabled;

            _baseFrameColor = frame;
            if (frameImage != null) frameImage.color = frame;
            if (buyButton != null) buyButton.interactable = prereqMet && canAfford;

            // Dim the icon when locked so the whole tile reads as out-of-reach.
            if (iconImage != null)
            {
                var c = iconImage.color;
                c.a = prereqMet ? 0.85f : 0.28f;
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
            // punch animation on every click (success or failure). Even if the
            // purchase fails, the player sees feedback that their input was registered.
            CacheBaseTransform();
            if (_punchCo != null) StopCoroutine(_punchCo);
            if (gameObject.activeInHierarchy)
                _punchCo = StartCoroutine(PunchAnim());
            // SkillTreeManager fires OnPurchaseFailed internally when funds are short.
            SkillTreeManager.Instance?.TryPurchase(id);
        }

        void CacheBaseTransform()
        {
            if (_baseTransformCached) return;
            _baseLocalScale = transform.localScale;
            _baseLocalRotation = transform.localRotation;
            _baseTransformCached = true;
        }

        IEnumerator PunchAnim()
        {
            const float punchDur = 0.05f;
            const float settleDur = 0.15f;
            var punchScale = _baseLocalScale * 0.85f;
            var punchRot = _baseLocalRotation * Quaternion.Euler(0f, 0f, -14f);
            float t = 0f;
            // Quick punch down.
            while (t < punchDur)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / punchDur);
                transform.localScale = Vector3.Lerp(_baseLocalScale, punchScale, k);
                transform.localRotation = Quaternion.Slerp(_baseLocalRotation, punchRot, k);
                yield return null;
            }
            // Settle back with ease-out cubic.
            t = 0f;
            while (t < settleDur)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / settleDur);
                float eased = 1f - Mathf.Pow(1f - k, 3f);
                transform.localScale = Vector3.Lerp(punchScale, _baseLocalScale, eased);
                transform.localRotation = Quaternion.Slerp(punchRot, _baseLocalRotation, eased);
                yield return null;
            }
            transform.localScale = _baseLocalScale;
            transform.localRotation = _baseLocalRotation;
            _punchCo = null;
        }

        IEnumerator RedFlash()
        {
            if (frameImage == null) yield break;
            var flash = new Color(0.94f, 0.30f, 0.30f, _baseFrameColor.a);
            const float dur = 0.20f;
            float t = 0f;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                frameImage.color = Color.Lerp(flash, _baseFrameColor, Mathf.Clamp01(t / dur));
                yield return null;
            }
            // Final settle — RefreshState will pick the correct steady-state tint.
            RefreshState();
            _flashCo = null;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            var rt = (RectTransform)transform;
            SkillTreeTooltipUI.Instance?.Show(id, rt.position);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            SkillTreeTooltipUI.Instance?.Hide();
        }
    }
}
