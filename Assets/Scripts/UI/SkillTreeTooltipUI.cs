using Rebuild.Mining;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Rebuild.UI
{



    public sealed class SkillTreeTooltipUI : MonoBehaviour
    {
        [SerializeField] GameObject panelRoot;
        [SerializeField] TMP_Text nameLabel;
        [SerializeField] TMP_Text descLabel;
        [SerializeField] TMP_Text levelPriceLabel;
        [SerializeField] Image currencyIcon;
        [SerializeField] RectTransform anchorRect;

        public static SkillTreeTooltipUI Instance { get; private set; }


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

                    levelPriceLabel.text = $"단계 {lv}/{max}    {currencyKr} {Rebuild.Util.FormatNumbers.Format(price)}";
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


            Camera cam = (_canvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : _canvas.worldCamera;
            Vector2 anchorScreen = RectTransformUtility.WorldToScreenPoint(cam, worldAnchor);



            var canvasSize = _canvasRect.rect.size;
            var panelSize = anchorRect.rect.size;
            float halfW = panelSize.x * 0.5f;
            float halfH = panelSize.y * 0.5f;

            Vector2 candidateRight = anchorScreen + new Vector2(kHorizontalOffset, 0f);
            Vector2 candidateLeft = anchorScreen + new Vector2(-kHorizontalOffset, 0f);


            Vector2 chosenLocal;
            if (TryFitInCanvas(candidateRight, cam, halfW, halfH, canvasSize, out chosenLocal)) { }
            else if (TryFitInCanvas(candidateLeft, cam, halfW, halfH, canvasSize, out chosenLocal)) { }
            else
            {

                RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRect, candidateRight, cam, out chosenLocal);
                chosenLocal.x = Mathf.Clamp(chosenLocal.x, -canvasSize.x * 0.5f + halfW, canvasSize.x * 0.5f - halfW);
                chosenLocal.y = Mathf.Clamp(chosenLocal.y, -canvasSize.y * 0.5f + halfH, canvasSize.y * 0.5f - halfH);
            }


            float minY = -canvasSize.y * 0.5f + halfH;
            float maxY = canvasSize.y * 0.5f - halfH;
            if (chosenLocal.y > maxY) chosenLocal.y = Mathf.Max(minY, chosenLocal.y - kVerticalOffset * 2f);
            else if (chosenLocal.y < minY) chosenLocal.y = Mathf.Min(maxY, chosenLocal.y + kVerticalOffset * 2f);


            anchorRect.position = _canvasRect.TransformPoint(chosenLocal);
        }



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
