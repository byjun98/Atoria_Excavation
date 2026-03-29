using System;
using UnityEngine;
using UnityEngine.UI;

namespace Rebuild.UI
{
    /// <summary>
    /// main-menu shell. Owns 5-tab toggle, KeepOnMining/Settings/Stats/Credits/Back buttons,
    /// and orchestrates StartScreen <-> MainMenu transition via CircleTransitionController.
    /// </summary>
    [RequireComponent(typeof(Canvas))]
    public sealed class MainMenuController : MonoBehaviour
    {
        public static MainMenuController Instance { get; private set; }

        public enum Tab { Upgrades = 0, Talents = 1, Anvil = 2, Mine = 3, Artifacts = 4 }

        [Header("Cross-canvas refs (auto-resolved if empty)")]
        [SerializeField] GameObject startScreenCanvasGo;
        [SerializeField] CircleTransitionController circleTransition;

        [Header("MainMenu panel")]
        [SerializeField] GameObject mainMenuPanel;

        [Header("Tab buttons (auto-resolved by name)")]
        [SerializeField] Button skillTreeBtn;
        [SerializeField] Button talentsBtn;
        [SerializeField] Button anvilBtn;
        [SerializeField] Button mineBtn;
        [SerializeField] Button artifactsBtn;

        [Header("Tab panels")]
        [SerializeField] GameObject skillTreePanel;
        [SerializeField] GameObject talentsPanel;
        [SerializeField] GameObject anvilPanel;
        [SerializeField] GameObject minePanel;
        [SerializeField] GameObject artifactsPanel;

        [Header("Other buttons")]
        [SerializeField] Button keepOnMiningBtn;
        [SerializeField] Button backToStartBtn;
        [SerializeField] Button settingsBtn;
        [SerializeField] Button statsBtn;
        [SerializeField] Button infoBtn;

        [Header("Tab visuals")]
        [SerializeField] Color selectedColor = new Color(1f, 0.84f, 0f, 1f); // #FFD600
        [SerializeField] Color idleColor = new Color(0.65f, 0.65f, 0.65f, 1f);

        Button[] _tabButtons;
        GameObject[] _tabPanels;
        Tab _currentTab = Tab.Upgrades;

        void Awake()
        {
            Instance = this;

            var canvas = GetComponent<Canvas>();
            if (canvas != null && canvas.worldCamera == null) canvas.worldCamera = Camera.main;

            AutoResolve();
            CacheTabArrays();
            ConfigureInputLayering();
            HookButtons();
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Start()
        {
            if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
            ApplyTabVisuals(_currentTab);
        }

        void AutoResolve()
        {
            if (mainMenuPanel == null)
            {
                var t = transform.Find("MainMenu");
                if (t != null) mainMenuPanel = t.gameObject;
            }
            if (startScreenCanvasGo == null)
            {
                var go = GameObject.Find("Canvas_StartScreen");
                if (go != null) startScreenCanvasGo = go;
            }
            if (circleTransition == null)
            {
                var go = GameObject.Find("Canvas_CircleTransition_andSettings");
                if (go != null) circleTransition = go.GetComponent<CircleTransitionController>();
            }

            skillTreeBtn = skillTreeBtn != null ? skillTreeBtn : FindBtn("MainMenu/TopButtons/SkillTree_btn");
            talentsBtn   = talentsBtn   != null ? talentsBtn   : FindBtn("MainMenu/TopButtons/Talents_btn");
            anvilBtn     = anvilBtn     != null ? anvilBtn     : FindBtn("MainMenu/TopButtons/TheAnvil_btn");
            mineBtn      = mineBtn      != null ? mineBtn      : FindBtn("MainMenu/TopButtons/TheMine_btn");
            artifactsBtn = artifactsBtn != null ? artifactsBtn : FindBtn("MainMenu/TopButtons/Artifacts_btn");

            skillTreePanel = skillTreePanel != null ? skillTreePanel : FindGo("MainMenu/TabPanels/SkillTreePanel");
            talentsPanel   = talentsPanel   != null ? talentsPanel   : FindGo("MainMenu/TabPanels/TalentsPanel");
            anvilPanel     = anvilPanel     != null ? anvilPanel     : FindGo("MainMenu/TabPanels/AnvilPanel");
            minePanel      = minePanel      != null ? minePanel      : FindGo("MainMenu/TabPanels/MinePanel");
            artifactsPanel = artifactsPanel != null ? artifactsPanel : FindGo("MainMenu/TabPanels/ArtifactsPanel");

            keepOnMiningBtn = keepOnMiningBtn != null ? keepOnMiningBtn : FindBtn("MainMenu/KeepOnMining_MainMenu");
            backToStartBtn  = backToStartBtn  != null ? backToStartBtn  : FindBtn("MainMenu/BackToStart_btn");
            settingsBtn     = settingsBtn     != null ? settingsBtn     : FindBtn("MainMenu/settingsBtn");
            statsBtn        = statsBtn        != null ? statsBtn        : FindBtn("MainMenu/statsBtn");
            infoBtn         = infoBtn         != null ? infoBtn         : FindBtn("MainMenu/infoBtn");
        }

        Button FindBtn(string relPath)
        {
            var t = transform.Find(relPath);
            return t != null ? t.GetComponent<Button>() : null;
        }

        GameObject FindGo(string relPath)
        {
            var t = transform.Find(relPath);
            return t != null ? t.gameObject : null;
        }

        void CacheTabArrays()
        {
            _tabButtons = new[] { skillTreeBtn, talentsBtn, anvilBtn, mineBtn, artifactsBtn };
            _tabPanels = new[] { skillTreePanel, talentsPanel, anvilPanel, minePanel, artifactsPanel };
        }

        void HookButtons()
        {
            if (skillTreeBtn != null) skillTreeBtn.onClick.AddListener(() => SelectTab(Tab.Upgrades));
            if (talentsBtn   != null) talentsBtn.onClick.AddListener(() => SelectTab(Tab.Talents));
            if (anvilBtn     != null) anvilBtn.onClick.AddListener(() => SelectTab(Tab.Anvil));
            if (mineBtn      != null) mineBtn.onClick.AddListener(() => SelectTab(Tab.Mine));
            if (artifactsBtn != null) artifactsBtn.onClick.AddListener(() => SelectTab(Tab.Artifacts));

            if (keepOnMiningBtn != null) keepOnMiningBtn.onClick.AddListener(OnKeepOnMining);
            if (backToStartBtn  != null) backToStartBtn.onClick.AddListener(OnBackToStart);
            if (settingsBtn     != null) settingsBtn.onClick.AddListener(OnSettings);
            if (statsBtn        != null) statsBtn.onClick.AddListener(OnStats);
            if (infoBtn         != null) infoBtn.onClick.AddListener(OnInfo);
        }

        /// <summary>Called by StartScreenController when Play is pressed.</summary>
        public void PlayGame()
        {
            if (circleTransition == null)
            {
                ApplyStartToMainMenu();
                return;
            }
            circleTransition.PlayTransition(ApplyStartToMainMenu);
        }

        void ApplyStartToMainMenu()
        {
            if (startScreenCanvasGo != null) startScreenCanvasGo.SetActive(false);
            if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
            ConfigureInputLayering();
            SelectTab(Tab.Upgrades);
        }

        public void BackToStartScreen()
        {
            if (circleTransition == null)
            {
                ApplyMainMenuToStart();
                return;
            }
            circleTransition.PlayTransition(ApplyMainMenuToStart);
        }

        void ApplyMainMenuToStart()
        {
            if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
            if (startScreenCanvasGo != null) startScreenCanvasGo.SetActive(true);
        }

        public void SelectTab(Tab tab)
        {
            _currentTab = tab;
            ApplyTabVisuals(tab);
            ConfigureInputLayering();
            // context-triggered tutorial frames. Each tab checks an unlock
            // condition before requesting its frame; MaybeShow silently skips when already seen.
            MaybeShowTabTutorial(tab);
        }

        void ConfigureInputLayering()
        {
            // Full-screen tab panels are visual backdrops; their child buttons/nodes still receive
            // input, but the backdrop Image itself must not eat clicks meant for the top controls.
            DisablePanelBackgroundRaycast(skillTreePanel);
            DisablePanelBackgroundRaycast(talentsPanel);
            DisablePanelBackgroundRaycast(anvilPanel);
            DisablePanelBackgroundRaycast(minePanel);
            DisablePanelBackgroundRaycast(artifactsPanel);

            var panelsRoot = skillTreePanel != null ? skillTreePanel.transform.parent : null;
            if (panelsRoot != null) panelsRoot.SetAsFirstSibling();

            if (keepOnMiningBtn != null) keepOnMiningBtn.transform.SetAsLastSibling();
            var topButtons = skillTreeBtn != null ? skillTreeBtn.transform.parent : null;
            if (topButtons != null) topButtons.SetAsLastSibling();
            if (backToStartBtn != null) backToStartBtn.transform.SetAsLastSibling();
            if (infoBtn != null) infoBtn.transform.SetAsLastSibling();
            if (statsBtn != null) statsBtn.transform.SetAsLastSibling();
            if (settingsBtn != null) settingsBtn.transform.SetAsLastSibling();
        }

        static void DisablePanelBackgroundRaycast(GameObject panel)
        {
            if (panel == null) return;
            var image = panel.GetComponent<Image>();
            if (image != null) image.raycastTarget = false;
        }

        void MaybeShowTabTutorial(Tab tab)
        {
            var tm = Rebuild.Tutorial.TutorialManager.Instance;
            if (tm == null) return;
            switch (tab)
            {
                case Tab.Talents:
                    if (Rebuild.Mining.LevelMechanics.Instance != null
                        && Rebuild.Mining.LevelMechanics.Instance.Level >= 1)
                        tm.MaybeShow(2);
                    break;
                case Tab.Anvil:
                    if (Rebuild.Mining.SkillTreeManager.Instance != null
                        && Rebuild.Mining.SkillTreeManager.Instance.GetLevel(Rebuild.Mining.UpgradeId.ImprovedPickaxe) >= 1)
                        tm.MaybeShow(3);
                    break;
                case Tab.Mine:
                    // 가 SpawnCopper 같은 SkillTree gate 를 도입한다 — 그 전에는 항상 표시.
                    tm.MaybeShow(4);
                    break;
                case Tab.Artifacts:
                    int found = 0;
                    if (Rebuild.Mining.ArtifactsManager.Instance != null)
                    {
                        for (int i = 0; i < Rebuild.Mining.ArtifactDef.Count; i++)
                            if (Rebuild.Mining.ArtifactsManager.Instance.HasArtifact((Rebuild.Mining.ArtifactId)i)) { found++; break; }
                    }
                    if (found > 0) tm.MaybeShow(5);
                    break;
            }
        }

        void ApplyTabVisuals(Tab tab)
        {
            int idx = (int)tab;
            if (_tabPanels != null)
            {
                for (int i = 0; i < _tabPanels.Length; i++)
                {
                    var panel = _tabPanels[i];
                    if (panel == null) continue;
                    panel.SetActive(i == idx);
                    // fade-in 새 탭 (0.2s). CanvasGroup 미존재 시 자동 add.
                    if (i == idx)
                    {
                        var cg = panel.GetComponent<CanvasGroup>();
                        if (cg == null) cg = panel.AddComponent<CanvasGroup>();
                        if (isActiveAndEnabled) StartCoroutine(FadeInPanel(cg));
                        else cg.alpha = 1f;
                    }
                }
            }
            if (_tabButtons != null)
            {
                for (int i = 0; i < _tabButtons.Length; i++)
                {
                    var btn = _tabButtons[i];
                    if (btn == null) continue;
                    var img = btn.GetComponent<Image>();
                    if (img != null)
                        img.color = UsesLabeledMenuButtonSprite(img) ? Color.white : ((i == idx) ? selectedColor : idleColor);
                }
            }
        }

        static bool UsesLabeledMenuButtonSprite(Image image)
        {
            return image != null
                && image.sprite != null
                && image.sprite.name.StartsWith("button_", StringComparison.Ordinal);
        }

        static System.Collections.IEnumerator FadeInPanel(CanvasGroup cg)
        {
            cg.alpha = 0f;
            const float dur = 0.2f;
            float t = 0f;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                cg.alpha = Mathf.Clamp01(t / dur);
                yield return null;
            }
            cg.alpha = 1f;
        }

        void OnKeepOnMining()
        {
            // hand off to MiningSessionController, which owns the StartScreen <-> Mining swap.
            var session = Rebuild.Mining.MiningSessionController.Instance;
            if (session != null) { session.StartSession(); return; }
            Debug.LogWarning("[Rebuild][] KeepOnMining clicked but MiningSessionController.Instance is null. Run Tools > Rebuild > Setup Scene.");
        }

        /// <summary>Called by MiningSessionController.StartSession midpoint.</summary>
        public void HideMainMenu()
        {
            if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        }

        /// <summary>Called by MiningSessionController.EndSession midpoint.</summary>
        public void ShowMainMenu()
        {
            if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
            ConfigureInputLayering();
            SelectTab(_currentTab);
        }

        void OnSettings()
        {
            // open the modal settings overlay (Canvas_SettingsPanel).
            if (SettingsPanelUI.Instance != null) SettingsPanelUI.Instance.Show();
            else Debug.Log("[Rebuild][] settingsBtn clicked but SettingsPanelUI.Instance is null. Run Tools > Rebuild > Setup Scene.");
        }
        void OnStats()
        {
            // open the modal stats overlay (Canvas_StatsPanel). If the panel was
            // never set up the button stays a no-op so the menu still works.
            if (StatsPanelUI.Instance != null) StatsPanelUI.Instance.Show();
            else Debug.Log("[Rebuild][] statsBtn clicked but StatsPanelUI.Instance is null. Run Tools > Rebuild > Setup Scene.");
        }
        void OnInfo()     => Debug.Log("[Rebuild][] infoBtn clicked - info hover.");

        void OnBackToStart() => BackToStartScreen();
    }
}
