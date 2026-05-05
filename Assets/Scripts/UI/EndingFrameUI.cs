using System.Collections;
using Rebuild.Mining;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Rebuild.UI
{
    /// <summary>full-screen endgame ceremony.
    /// Lives on Canvas_Ending (sortingOrder 308 — above MainMenu, below SaveToast / Tutorial).
    /// Flow: TheEndingManager.OnEligible → triggerButton visible on MainMenu → click triggers
    /// EnterEnding() → BigRock appears → click-to-destroy → 3 sequential diamond pops → credits
    /// scroll → ToMenu button. Skipping the credits is allowed (Skip button).</summary>
    public sealed class EndingFrameUI : MonoBehaviour
    {
        public static EndingFrameUI Instance { get; private set; }

        [Header("Trigger (on MainMenu)")]
        [SerializeField] Button triggerButton;
        [SerializeField] GameObject triggerButtonRoot;

        [Header("Ending frame")]
        [SerializeField] GameObject frameRoot;
        [SerializeField] BigRock bigRock;
        [SerializeField] GameObject bigRockHolder;
        [SerializeField] TMP_Text instructionText;
        [SerializeField] GameObject[] diamondRoots = new GameObject[3];
        [SerializeField] RectTransform creditsScroller;
        [SerializeField] TMP_Text creditsText;
        [SerializeField] Button skipButton;
        [SerializeField] Button toMenuButton;

        [Header("Crafting stage")]
        [SerializeField] GameObject craftingFrame;
        [SerializeField] Image craftingCircle;       // Radial360 fill
        [SerializeField] TMP_Text craftingText;       // "복원 중" + dot cycle
        [SerializeField] Button craftButton;          // shown after diamond pop, click → 3s craft
        [SerializeField] float craftingDuration = 3f;

        // credits pre-roll labels. Each appears 1.311s after the previous one with
        // a CardPop SFX so the player has a beat to read each before the scroll starts.
        [Header("Credits pre-roll")]
        [SerializeField] GameObject youCraftedTextRoot;
        [SerializeField] GameObject diamondPickaxeIconRoot;
        [SerializeField] GameObject thankYouForPlayingRoot;

        [Header("Tuning")]
        // original 1.1s interval between diamond pops (was 0.55s shorthand).
        [SerializeField] float diamondInterval = 1.1f;
        [SerializeField] float diamondPunchDuration = 0.45f;
        // user decision 2026-05-14: 12s scroll (casual-friendly, not the 37s original).
        [SerializeField] float creditsScrollDuration = 12f;
        [SerializeField] float creditsScrollStartY = -640f;
        [SerializeField] float creditsScrollEndY = 640f;

        Coroutine _sequenceCo;
        bool _craftPressed;
        // set by OnCraftPressed; FloatDiamond coroutines watch this and bail out.
        bool _isCraftingStarted;
        int _diamondsLanded;

        void Awake()
        {
            Instance = this;
            if (triggerButton != null)
            {
                triggerButton.onClick.RemoveAllListeners();
                triggerButton.onClick.AddListener(OnTriggerClicked);
            }
            if (skipButton != null)
            {
                skipButton.onClick.RemoveAllListeners();
                skipButton.onClick.AddListener(OnSkip);
            }
            if (toMenuButton != null)
            {
                toMenuButton.onClick.RemoveAllListeners();
                toMenuButton.onClick.AddListener(OnReturnToMenu);
            }
            if (craftButton != null)
            {
                craftButton.onClick.RemoveAllListeners();
                craftButton.onClick.AddListener(OnCraftPressed);
                craftButton.gameObject.SetActive(false);
            }
            if (craftingFrame != null) craftingFrame.SetActive(false);
            if (frameRoot != null) frameRoot.SetActive(false);
            HideAllDiamonds();
            SetCreditsVisible(false);
            if (toMenuButton != null) toMenuButton.gameObject.SetActive(false);
            UpdateTriggerVisibility();
        }

        void OnEnable()
        {
            if (TheEndingManager.Instance != null)
            {
                TheEndingManager.Instance.OnEligible += UpdateTriggerVisibility;
                TheEndingManager.Instance.OnEnded += UpdateTriggerVisibility;
            }
            if (bigRock != null) bigRock.OnDestroyed += OnBigRockDestroyed;
            UpdateTriggerVisibility();
        }

        void OnDisable()
        {
            if (TheEndingManager.Instance != null)
            {
                TheEndingManager.Instance.OnEligible -= UpdateTriggerVisibility;
                TheEndingManager.Instance.OnEnded -= UpdateTriggerVisibility;
            }
            if (bigRock != null) bigRock.OnDestroyed -= OnBigRockDestroyed;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void UpdateTriggerVisibility()
        {
            if (triggerButtonRoot == null) return;
            bool show = TheEndingManager.Instance != null
                        && TheEndingManager.Instance.IsEligible
                        && !TheEndingManager.Instance.IsCompleted;
            triggerButtonRoot.SetActive(show);
        }

        void OnTriggerClicked()
        {
            Rebuild.Audio.AudioManager.Instance?.Play("Click_1");
            EnterEnding();
        }

        public void EnterEnding()
        {
            // Suspend any active mining session — the ending owns the screen now.
            var session = MiningSessionController.Instance;
            if (session != null && session.IsInSession)
            {
                session.OnReturnToMenu(); // collapses back to MainMenu, then we cover it.
            }
            if (frameRoot != null) frameRoot.SetActive(true);
            HideAllDiamonds();
            SetCreditsVisible(false);
            if (toMenuButton != null) toMenuButton.gameObject.SetActive(false);
            if (craftButton != null) craftButton.gameObject.SetActive(false);
            if (craftingFrame != null) craftingFrame.SetActive(false);
            _craftPressed = false;
            _isCraftingStarted = false;
            _diamondsLanded = 0;
            if (instructionText != null) instructionText.text = "천 년 잠든 황룡사 발굴지에\n잠든 신라의 보물을 깨워주세요!";
            if (bigRockHolder != null) bigRockHolder.SetActive(true);
            // 가 maxHp 3500 로 올릴 예정. 현재 Inspector default 그대로 사용.
            if (bigRock != null) bigRock.Initialize(bigRock.MaxHp);
        }

        void OnCraftPressed()
        {
            _craftPressed = true;
            _isCraftingStarted = true; // signals FloatDiamond coroutines to bail.
            Rebuild.Audio.AudioManager.Instance?.Play("Merge");
            if (craftButton != null) craftButton.gameObject.SetActive(false);
        }

        void OnBigRockDestroyed()
        {
            Rebuild.Audio.AudioManager.Instance?.Play("FoundArtifact");
            if (_sequenceCo != null) StopCoroutine(_sequenceCo);
            _sequenceCo = StartCoroutine(DiamondsThenCredits());
        }

        IEnumerator DiamondsThenCredits()
        {
            if (instructionText != null) instructionText.text = "";
            // Hide the (now-faded) BigRock holder.
            yield return new WaitForSecondsRealtime(0.4f);
            if (bigRockHolder != null) bigRockHolder.SetActive(false);

            // Sequential diamond pop-in.
            for (int i = 0; i < diamondRoots.Length; i++)
            {
                if (diamondRoots[i] == null) continue;
                yield return PopDiamond(diamondRoots[i]);
                yield return new WaitForSecondsRealtime(diamondInterval - diamondPunchDuration);
            }

            // Crafting stage: show CraftButton, wait for press, then radial gauge.
            yield return new WaitForSecondsRealtime(0.6f);
            if (craftButton != null)
            {
                craftButton.gameObject.SetActive(true);
                Rebuild.Audio.AudioManager.Instance?.Play("CardPop");
                while (!_craftPressed) yield return null;
            }
            yield return CraftingStage();

            // Hide diamonds + crafting frame, then roll credits.
            for (int i = 0; i < diamondRoots.Length; i++)
                if (diamondRoots[i] != null) diamondRoots[i].SetActive(false);
            if (craftingFrame != null) craftingFrame.SetActive(false);

            // credits pre-roll + scroll. Mirrors original TheEnding.RollCredits.
            yield return RollCredits();

            // Done — mark completed + show Return to Menu.
            if (TheEndingManager.Instance != null) TheEndingManager.Instance.MarkEnded();
            if (toMenuButton != null) toMenuButton.gameObject.SetActive(true);
            _sequenceCo = null;
        }

        /// <summary>pre-roll labels (YouCrafted / DiamondPickaxe / ThankYou) appear
        /// 1.311s apart with CardPop accents, then 12s scroll. CreditsMusic fires at the start;
        /// the AudioManager handles fade-in if has wired the clip (silent fallback).</summary>
        IEnumerator RollCredits()
        {
            // Reset pre-roll labels.
            if (youCraftedTextRoot != null) youCraftedTextRoot.SetActive(false);
            if (diamondPickaxeIconRoot != null) diamondPickaxeIconRoot.SetActive(false);
            if (thankYouForPlayingRoot != null) thankYouForPlayingRoot.SetActive(false);

            // 1s beat, then start music.
            yield return new WaitForSecondsRealtime(1f);
            Rebuild.Audio.AudioManager.Instance?.Stop("CreditsMusic");
            Rebuild.Audio.AudioManager.Instance?.Play("CreditsMusic");

            // 0.67s + CardPop + youCrafted.
            yield return new WaitForSecondsRealtime(0.67f);
            Rebuild.Audio.AudioManager.Instance?.Play("CardPop");
            if (youCraftedTextRoot != null) youCraftedTextRoot.SetActive(true);

            // 1.311s + CardPop + diamondPickaxe.
            yield return new WaitForSecondsRealtime(1.311f);
            Rebuild.Audio.AudioManager.Instance?.Play("CardPop");
            if (diamondPickaxeIconRoot != null) diamondPickaxeIconRoot.SetActive(true);

            // 1.311s + CardPop + thankYou.
            yield return new WaitForSecondsRealtime(1.311f);
            Rebuild.Audio.AudioManager.Instance?.Play("CardPop");
            if (thankYouForPlayingRoot != null) thankYouForPlayingRoot.SetActive(true);

            // 2s read time before the credit roll starts.
            yield return new WaitForSecondsRealtime(2f);

            // Scroll credits bottom→top (user decision: 12s instead of original 37s).
            SetCreditsVisible(true);
            if (creditsScroller != null)
            {
                creditsScroller.anchoredPosition = new Vector2(creditsScroller.anchoredPosition.x, creditsScrollStartY);
                float t = 0f;
                while (t < creditsScrollDuration)
                {
                    t += Time.unscaledDeltaTime;
                    float k = Mathf.Clamp01(t / creditsScrollDuration);
                    float y = Mathf.Lerp(creditsScrollStartY, creditsScrollEndY, k);
                    creditsScroller.anchoredPosition = new Vector2(creditsScroller.anchoredPosition.x, y);
                    yield return null;
                }
            }
        }

        /// <summary>original CraftingCoroutine. 3s radial fill + dot animation +
        /// diamonds slide to center. Completes with CraftingDone + FinishedCrafting SFX.</summary>
        IEnumerator CraftingStage()
        {
            if (craftingFrame != null) craftingFrame.SetActive(true);
            if (craftingCircle != null) craftingCircle.fillAmount = 0f;
            if (craftingText != null) craftingText.text = "복원 중";

            // Capture diamond start positions so they can slide to center.
            var startPos = new Vector2[diamondRoots.Length];
            for (int i = 0; i < diamondRoots.Length; i++)
                if (diamondRoots[i] != null)
                    startPos[i] = ((RectTransform)diamondRoots[i].transform).anchoredPosition;

            float elapsed = 0f;
            while (elapsed < craftingDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t01 = Mathf.Clamp01(elapsed / craftingDuration);
                // Radial fill 0→1.
                if (craftingCircle != null) craftingCircle.fillAmount = t01;
                // Dot cycle every 0.35s (matches original TheAnvil dotTimer cadence).
                if (craftingText != null)
                {
                    int dots = Mathf.FloorToInt(elapsed / 0.35f) % 4;
                    craftingText.text = "복원 중" + new string('.', dots);
                }
                // Diamonds slide to center (SmoothStep ease).
                float s = Mathf.SmoothStep(0f, 1f, t01);
                for (int i = 0; i < diamondRoots.Length; i++)
                {
                    if (diamondRoots[i] == null) continue;
                    var drt = (RectTransform)diamondRoots[i].transform;
                    drt.anchoredPosition = Vector2.LerpUnclamped(startPos[i], Vector2.zero, s);
                }
                yield return null;
            }
            if (craftingCircle != null) craftingCircle.fillAmount = 1f;

            Rebuild.Audio.AudioManager.Instance?.Play("CraftingDone");
            Rebuild.Audio.AudioManager.Instance?.Play("FinishedCrafting");
            // Brief beat so the player sees the full gauge before diamonds vanish.
            yield return new WaitForSecondsRealtime(0.4f);
        }

        IEnumerator PopDiamond(GameObject diamond)
        {
            diamond.SetActive(true);
            var rt = (RectTransform)diamond.transform;
            rt.localScale = Vector3.zero;
            // EndingWoosh on diamond entry. 에서 clip 매핑되기 전엔 silent.
            Rebuild.Audio.AudioManager.Instance?.Play("EndingWoosh");
            float t = 0f;
            while (t < diamondPunchDuration)
            {
                t += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(t / diamondPunchDuration);
                float e = 1f - Mathf.Pow(1f - p, 3f);
                rt.localScale = Vector3.one * Mathf.Lerp(0f, 1.15f, e);
                yield return null;
            }
            t = 0f; float settle = 0.12f;
            while (t < settle)
            {
                t += Time.unscaledDeltaTime;
                rt.localScale = Vector3.Lerp(Vector3.one * 1.15f, Vector3.one, Mathf.Clamp01(t / settle));
                yield return null;
            }
            rt.localScale = Vector3.one;

            // landing chime + start the infinite sin/cos float that runs until
            // crafting begins. Index-based DiamodSound1/2/3 lets each landing carry a unique tone.
            _diamondsLanded++;
            Rebuild.Audio.AudioManager.Instance?.Play($"DiamodSound{Mathf.Clamp(_diamondsLanded, 1, 3)}");
            StartCoroutine(FloatDiamond(diamond));
        }

        /// <summary>infinite gentle sway while the player waits for the CraftButton.
        /// Mirrors original FloatGameObject: scale breathes between 1.0×0.95 and 1.0×1.05, position
        /// sways ±13 px circular, rotation ±5°. Bails out (and resets) when CraftButton fires.</summary>
        IEnumerator FloatDiamond(GameObject diamond)
        {
            if (diamond == null) yield break;
            var rt = (RectTransform)diamond.transform;
            Vector2 basePos = rt.anchoredPosition;
            Vector3 baseScale = rt.localScale;
            Quaternion baseRot = rt.localRotation;
            // Random timing offsets so the three diamonds don't move in lock-step.
            float scaleOff = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
            float posOff = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
            float rotOff = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
            while (!_isCraftingStarted)
            {
                float time = Time.unscaledTime;
                float sBreath = 1f + (Mathf.Sin(time + scaleOff) * 0.05f);
                rt.localScale = baseScale * sBreath;
                rt.anchoredPosition = basePos + new Vector2(Mathf.Sin(time + posOff) * 13f,
                                                              Mathf.Cos(time + posOff) * 13f);
                // Carry the base rotation (45° for diamond shape) and add the sway.
                rt.localRotation = baseRot * Quaternion.Euler(0f, 0f, Mathf.Sin(time + rotOff) * 5f);
                yield return null;
            }
            // Crafting started — snap back to rest pose so MoveDiamondsToCenter starts clean.
            rt.anchoredPosition = basePos;
            rt.localScale = baseScale;
            rt.localRotation = baseRot;
        }

        void OnSkip()
        {
            Rebuild.Audio.AudioManager.Instance?.Play("UI_Click1");
            if (_sequenceCo != null) StopCoroutine(_sequenceCo);
            _sequenceCo = null;
            if (TheEndingManager.Instance != null) TheEndingManager.Instance.MarkEnded();
            if (bigRockHolder != null) bigRockHolder.SetActive(false);
            // Reveal all diamonds + the final ToMenu button so the player still sees the payoff.
            for (int i = 0; i < diamondRoots.Length; i++)
            {
                if (diamondRoots[i] != null)
                {
                    diamondRoots[i].SetActive(true);
                    ((RectTransform)diamondRoots[i].transform).localScale = Vector3.one;
                }
            }
            SetCreditsVisible(true);
            if (creditsScroller != null)
                creditsScroller.anchoredPosition = new Vector2(creditsScroller.anchoredPosition.x, 0f);
            if (toMenuButton != null) toMenuButton.gameObject.SetActive(true);
        }

        void OnReturnToMenu()
        {
            Rebuild.Audio.AudioManager.Instance?.Play("Click_1");
            if (_sequenceCo != null) StopCoroutine(_sequenceCo);
            _sequenceCo = null;
            if (frameRoot != null) frameRoot.SetActive(false);
            UpdateTriggerVisibility();
        }

        void HideAllDiamonds()
        {
            for (int i = 0; i < diamondRoots.Length; i++)
                if (diamondRoots[i] != null) diamondRoots[i].SetActive(false);
        }

        void SetCreditsVisible(bool visible)
        {
            if (creditsScroller != null) creditsScroller.gameObject.SetActive(visible);
        }
    }
}
