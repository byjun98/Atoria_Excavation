using Rebuild.Mining;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Rebuild.UI
{
    /// <summary>single floating popup that <see cref="TreeNodeUI"/> nodes drive on
    /// pointer hover. Shows name/description/level-price plus a currency icon. Singleton so
    /// nodes don't each carry their own panel and can position relative to themselves.</summary>
    public sealed class SkillTreeTooltipUI : MonoBehaviour
    {
        [SerializeField] GameObject panelRoot;
        [SerializeField] TMP_Text nameLabel;
        [SerializeField] TMP_Text descLabel;
        [SerializeField] TMP_Text levelPriceLabel;
        [SerializeField] Image currencyIcon;
        [SerializeField] RectTransform anchorRect;

        public static SkillTreeTooltipUI Instance { get; private set; }

        // Cached on first Show so we don't GetComponentInParent every hover.
        Canvas _canvas;
        RectTransform _canvasRect;

        void Awake()
        {
            Instance = this;
            Hide();
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>Pop the panel near <paramref name="worldAnchor"/> (the node's world position)
        /// offset to the right of the node so the icon stays readable. Fully-qualified parameter
        /// type because Assembly-CSharp has a legacy global <c>UpgradeId</c> enum.</summary>
        public void Show(Rebuild.Mining.UpgradeId id, Vector3 worldAnchor)
        {
            var info = Rebuild.Mining.UpgradeDef.Get(id);
            var st = SkillTreeManager.Instance;
            int lv = st != null ? st.GetLevel(id) : 0;
            int max = st != null ? st.GetMaxLevel(id) : info.MaxLevel;
            bool maxed = st != null && st.IsMaxed(id);

            if (nameLabel != null)
            {
                nameLabel.text = info.Name;
                nameLabel.color = DancheongPalette.TitleGold;
            }
            if (descLabel != null)
            {
                descLabel.text = info.Description;
                descLabel.color = DancheongPalette.BodyHanji;
            }
            if (levelPriceLabel != null)
            {
                if (maxed)
                {
                    levelPriceLabel.text = "최대 단계";
                }
                else
                {
                    int price = st != null ? st.GetNextPrice(id) : 0;
                    string currencyKr = OreLocalization.OreNameKr(info.Currency);
                    // Painite 1M+ 가격 등은 K/M/B/T 축약.
                    levelPriceLabel.text = $"{lv}/{max}  ·  {currencyKr} {Rebuild.Util.FormatNumbers.Format(price)}";
                }
                levelPriceLabel.color = DancheongPalette.BodyHanji;
            }
            if (currencyIcon != null)
            {
                var spr = SpriteLibrary.GetOreBarIcon(info.Currency);
                if (spr == null) spr = SpriteLibrary.GetOreHudIcon(info.Currency);
                if (spr != null)
                {
                    currencyIcon.sprite = spr;
                    currencyIcon.color = Color.white;
                }
                else
                {
                    currencyIcon.sprite = RockSpriteFactory.GetRockSprite();
                    currencyIcon.color = RockSpriteFactory.GetOreColor(info.Currency);
                }
                currencyIcon.preserveAspect = true;
                currencyIcon.raycastTarget = false;
            }

            PositionPanel(worldAnchor);
            if (panelRoot != null) panelRoot.SetActive(true);
        }

        public void Hide()
        {
            if (panelRoot != null) panelRoot.SetActive(false);
        }

        // node 와 tooltip 사이 gap (px). 양쪽 모두 적용.
        const float kHorizontalOffset = 180f;
        const float kVerticalOffset = 60f;

        void PositionPanel(Vector3 worldAnchor)
        {
            if (anchorRect == null) return;
            if (_canvas == null)
            {
                _canvas = GetComponentInParent<Canvas>();
                if (_canvas != null) _canvasRect = _canvas.transform as RectTransform;
            }
            if (_canvas == null || _canvasRect == null) return;

            // World -> screen -> canvas-local. ScreenSpaceCamera mode needs the canvas's camera.
            Camera cam = (_canvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : _canvas.worldCamera;
            Vector2 anchorScreen = RectTransformUtility.WorldToScreenPoint(cam, worldAnchor);

            // 원본 CheckToolTipOffScreen 모델: 오른쪽으로 먼저 시도 → overflow 시 왼쪽 flip.
            // 위/아래 동일. 합쳐서 4 corner placement 시도. clamp 보다 깔끔 (edge node 와 안 겹침).
            var canvasSize = _canvasRect.rect.size;
            var panelSize = anchorRect.rect.size;
            float halfW = panelSize.x * 0.5f;
            float halfH = panelSize.y * 0.5f;

            Vector2 candidateRight = anchorScreen + new Vector2(kHorizontalOffset, 0f);
            Vector2 candidateLeft = anchorScreen + new Vector2(-kHorizontalOffset, 0f);

            // 후보별로 canvas-local 변환 후 panel 의 4 모서리가 canvas 안에 들어가는지 확인.
            Vector2 chosenLocal;
            if (TryFitInCanvas(candidateRight, cam, halfW, halfH, canvasSize, out chosenLocal)) { }
            else if (TryFitInCanvas(candidateLeft, cam, halfW, halfH, canvasSize, out chosenLocal)) { }
            else
            {
                // 양쪽 모두 fit 안 되면 right 후보 + clamp 으로 fallback.
                RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRect, candidateRight, cam, out chosenLocal);
                chosenLocal.x = Mathf.Clamp(chosenLocal.x, -canvasSize.x * 0.5f + halfW, canvasSize.x * 0.5f - halfW);
                chosenLocal.y = Mathf.Clamp(chosenLocal.y, -canvasSize.y * 0.5f + halfH, canvasSize.y * 0.5f - halfH);
            }

            // 추가: panel 의 위/아래 가장자리가 canvas 밖이면 vertical flip (위 → 아래로 push, 아래 → 위로).
            float minY = -canvasSize.y * 0.5f + halfH;
            float maxY = canvasSize.y * 0.5f - halfH;
            if (chosenLocal.y > maxY) chosenLocal.y = Mathf.Max(minY, chosenLocal.y - kVerticalOffset * 2f);
            else if (chosenLocal.y < minY) chosenLocal.y = Mathf.Min(maxY, chosenLocal.y + kVerticalOffset * 2f);

            anchorRect.anchoredPosition = chosenLocal;
        }

        /// <summary>screen 좌표 후보 → canvas-local 변환 후 panel 의 4 모서리가
        /// canvas 안에 들어가는지 확인. fit 되면 true + local, 안 되면 false.</summary>
        bool TryFitInCanvas(Vector2 screenPos, Camera cam, float halfW, float halfH, Vector2 canvasSize, out Vector2 local)
        {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRect, screenPos, cam, out local))
                return false;
            float minX = -canvasSize.x * 0.5f + halfW;
            float maxX = canvasSize.x * 0.5f - halfW;
            return local.x >= minX && local.x <= maxX;
        }
    }
}
