using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Rebuild.Mining
{



    public sealed class SessionResultsUI : MonoBehaviour
    {
        [Header("Layout")]
        [SerializeField] GameObject panelRoot;
        [SerializeField] TMP_Text[] oreCountLabels = new TMP_Text[8];
        [SerializeField] TMP_Text[] barCountLabels = new TMP_Text[8];
        [SerializeField] Image[] oreIcons = new Image[8];
        [SerializeField] TMP_Text[] oreNameLabels = new TMP_Text[8];

        [Header("XP / Level row")]
        [SerializeField] GameObject xpRow;
        [SerializeField] TMP_Text xpGainedText;
        [SerializeField] GameObject levelUpRow;
        [SerializeField] TMP_Text levelUpText;

        [Header("Buttons")]
        [SerializeField] Button keepMiningButton;
        [SerializeField] Button returnToMenuButton;

        public Button KeepMiningButton => keepMiningButton;
        public Button ReturnToMenuButton => returnToMenuButton;

        void Awake()
        {




            ApplyOreVisuals();
        }

        void ApplyOreVisuals()
        {
            for (int i = 0; i < 8 && i < oreIcons.Length; i++)
            {
                if (oreIcons[i] != null)
                {
                    var ore = (Rebuild.Mining.OreType)i;


                    var hudSprite = SpriteLibrary.GetOreHudIcon(ore);
                    if (hudSprite != null)
                    {
                        oreIcons[i].sprite = hudSprite;
                        oreIcons[i].color = Color.white;
                        oreIcons[i].preserveAspect = true;
                    }
                    else
                    {
                        oreIcons[i].sprite = RockSpriteFactory.GetRockSprite();
                        oreIcons[i].color = RockSpriteFactory.GetOreColor(ore);
                    }
                }
                if (i < oreNameLabels.Length && oreNameLabels[i] != null)
                {
                    oreNameLabels[i].text = OreLocalization.OreNameKr((Rebuild.Mining.OreType)i);
                }
            }
        }

        public void Show(long[] sessionGained)
        {
            Show(sessionGained, barsCrafted: null, xpThisSession: 0, levelsGained: 0, talentPointsGained: 0);
        }

        public void Show(long[] sessionGained, int xpThisSession, int levelsGained, int talentPointsGained)
        {
            Show(sessionGained, barsCrafted: null, xpThisSession, levelsGained, talentPointsGained);
        }





        public void Show(long[] sessionGained, long[] barsCrafted, int xpThisSession, int levelsGained, int talentPointsGained)
        {
            if (panelRoot != null) panelRoot.SetActive(true);


            if (OreCounterUI.Instance != null) OreCounterUI.Instance.gameObject.SetActive(false);
            for (int i = 0; i < 8 && i < oreCountLabels.Length; i++)
            {
                if (oreCountLabels[i] == null) continue;
                long ore = (sessionGained != null && i < sessionGained.Length) ? sessionGained[i] : 0L;
                long bars = (barsCrafted != null && i < barsCrafted.Length) ? barsCrafted[i] : 0L;
                if (barCountLabels != null && i < barCountLabels.Length && barCountLabels[i] != null)
                {
                    oreCountLabels[i].text = Rebuild.Util.FormatNumbers.Format(ore);
                    barCountLabels[i].text = Rebuild.Util.FormatNumbers.Format(bars);
                    oreCountLabels[i].color = Rebuild.UI.MuseumUiTheme.Text;
                    barCountLabels[i].color = Rebuild.UI.MuseumUiTheme.Jade;
                }
                else if (ore <= 0 && bars <= 0)
                {
                    oreCountLabels[i].text = "0";
                    oreCountLabels[i].color = new Color(0.65f, 0.65f, 0.65f, 1f);
                }
                else if (barsCrafted == null)
                {

                    oreCountLabels[i].text = "+" + ore;
                    oreCountLabels[i].color = Color.white;
                }
                else
                {

                    oreCountLabels[i].text = $"+{ore}  →  {bars}";
                    oreCountLabels[i].color = Color.white;
                }
            }

            if (xpRow != null) xpRow.SetActive(xpThisSession > 0);
            if (xpGainedText != null) xpGainedText.text = $"+{xpThisSession} XP";

            bool didLevel = levelsGained > 0;
            if (levelUpRow != null) levelUpRow.SetActive(didLevel);
            if (levelUpText != null)
            {
                if (didLevel)
                {
                    string pts = talentPointsGained > 0 ? $"  (+{talentPointsGained} 특성 포인트)" : "";
                    levelUpText.text = $"레벨 업! +{levelsGained}{pts}";
                }
                else
                {
                    levelUpText.text = string.Empty;
                }
            }
        }

        public void Hide()
        {
            if (panelRoot != null) panelRoot.SetActive(false);

            if (OreCounterUI.Instance != null) OreCounterUI.Instance.gameObject.SetActive(true);
        }
    }
}
