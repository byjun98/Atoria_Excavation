using System.Collections;
using Rebuild.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Rebuild.Mining
{




    public sealed class MiningSessionController : MonoBehaviour
    {
        public static MiningSessionController Instance { get; private set; }

        [Header("Auto-resolved if empty")]
        [SerializeField] GameObject canvasGame;
        [SerializeField] GameObject canvasMiningScene;
        [SerializeField] TileGrid tileGrid;
        [SerializeField] RockSpawner rockSpawner;
        [SerializeField] CircleTransitionController circleTransition;
        [SerializeField] MainMenuController mainMenu;
        [SerializeField] Button endSessionBtn;

        [Header("Timer + Results")]
        [SerializeField] float mineSessionTime = 15f;
        [SerializeField] float sessionStartDelaySec = 1f;
        [SerializeField] MiningTimerUI timerUI;
        [SerializeField] SessionResultsUI resultsUI;
        [SerializeField] GameObject outOfTimeText;
        [SerializeField] float outOfTimeDisplaySec = 0.9f;

        [Header("Talent picker")]
        [SerializeField] TalentCardPickerUI talentPicker;

        public bool IsInSession { get; private set; }
        public bool TimerRunning { get; private set; }
        public bool ResultsShown { get; private set; }




        public Transform CanvasMiningSceneTransform =>
            canvasMiningScene != null ? canvasMiningScene.transform : null;



        public TileGrid TileGrid => tileGrid;


        float _timeLeft;
        Coroutine _timerCoroutine;
        Coroutine _zeusCoroutine;
        Coroutine _energiCoroutine;
        int _rockBonusSeconds;
        int _tickBonusSeconds;
        string _outOfTimeTextDefault;
        bool _outOfTimeTextDefaultCaptured;



        public bool IsMidasActiveThisSession { get; private set; }



        public float EnergiBoostUntil { get; private set; }
        public bool IsEnergiBoosted => Time.unscaledTime < EnergiBoostUntil;


        Coroutine _spawnXRockCoroutine;
        Coroutine _autoPickaxeCoroutine;

        Coroutine _diceSpawnCoroutine;

        Coroutine _shootCircleCoroutine;



        public int RocksMinedThisSession { get; private set; }
        public void RecordRockMinedForSpawnCounter() { RocksMinedThisSession++; }
        public void ResetRocksMinedCounter() { RocksMinedThisSession = 0; }

        void Awake()
        {
            Instance = this;
            AutoResolve();
            HookButtons();
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Start()
        {
            if (canvasGame != null) canvasGame.SetActive(false);
            if (canvasMiningScene != null) canvasMiningScene.SetActive(false);
        }

        void AutoResolve()
        {
            if (canvasGame == null)
            {
                var go = GameObject.Find("Canvas_Game");
                if (go != null) canvasGame = go;
            }
            if (canvasMiningScene == null)
            {
                var go = GameObject.Find("Canvas_UI_MiningScene");
                if (go != null) canvasMiningScene = go;
            }
            if (tileGrid == null && canvasGame != null)
                tileGrid = canvasGame.GetComponent<TileGrid>();
            if (rockSpawner == null && canvasGame != null)
                rockSpawner = canvasGame.GetComponent<RockSpawner>();
            if (circleTransition == null)
            {
                var go = GameObject.Find("Canvas_CircleTransition_andSettings");
                if (go != null) circleTransition = go.GetComponent<CircleTransitionController>();
            }
            if (mainMenu == null)
            {
                var go = GameObject.Find("Canvas_U_MainMenuAndUpgrades");
                if (go != null) mainMenu = go.GetComponent<MainMenuController>();
            }
            if (endSessionBtn == null && canvasMiningScene != null)
            {
                var t = canvasMiningScene.transform.Find("EndSession_btn");
                if (t != null) endSessionBtn = t.GetComponent<Button>();
            }
        }

        void HookButtons()
        {
            if (endSessionBtn != null)
            {
                endSessionBtn.onClick.RemoveListener(RequestEndSession);
                endSessionBtn.onClick.AddListener(RequestEndSession);


                if (endSessionBtn.GetComponent<Rebuild.UI.HoverCursorReset>() == null)
                    endSessionBtn.gameObject.AddComponent<Rebuild.UI.HoverCursorReset>();
            }
            if (resultsUI != null)
            {
                if (resultsUI.KeepMiningButton != null)
                {
                    resultsUI.KeepMiningButton.onClick.RemoveListener(OnKeepMining);
                    resultsUI.KeepMiningButton.onClick.AddListener(OnKeepMining);
                }
                if (resultsUI.ReturnToMenuButton != null)
                {
                    resultsUI.ReturnToMenuButton.onClick.RemoveListener(OnReturnToMenu);
                    resultsUI.ReturnToMenuButton.onClick.AddListener(OnReturnToMenu);
                }
            }
        }

        public void StartSession()
        {
            if (IsInSession) return;
            if (circleTransition == null) { ApplyEnterSession(); return; }
            circleTransition.PlayTransition(ApplyEnterSession);
        }

        void ApplyEnterSession()
        {
            IsInSession = true;
            ResultsShown = false;
            if (mainMenu != null) mainMenu.HideMainMenu();
            if (canvasGame != null) canvasGame.SetActive(true);
            if (canvasMiningScene != null) canvasMiningScene.SetActive(true);
            if (resultsUI != null) resultsUI.Hide();
            if (outOfTimeText != null) outOfTimeText.SetActive(false);
            if (endSessionBtn != null) endSessionBtn.interactable = true;
            TimerRunning = false;
            HideStartPrompt();
            if (GoldAndOreMechanics.Instance != null) GoldAndOreMechanics.Instance.ResetSession();
            if (LevelMechanics.Instance != null) LevelMechanics.Instance.ResetSession();



            if (FlowerMechanics.Instance != null) FlowerMechanics.Instance.RollSessionBudget();


            if (ArtifactsManager.Instance != null) ArtifactsManager.Instance.ResetSessionFlag();

            if (rockSpawner != null) rockSpawner.ResetGoldenChestFlag();

            if (PotionBuffMechanics.Instance != null) PotionBuffMechanics.Instance.RollForSession();

            if (SignBuffMechanics.Instance != null) SignBuffMechanics.Instance.RefreshForSession();



            ProjectileLauncher.ResetCounters();

            var lm = LevelMechanics.Instance;
            IsMidasActiveThisSession = lm != null && lm.HasTalent(TalentId.MidasTouch)
                && D100Helper.Roll(0.20f);

            if (IsMidasActiveThisSession)
                Rebuild.Stats.StatsManager.Instance?.RecordMidasTouchSession();
            EnergiBoostUntil = 0f;


            Rebuild.UI.SystemCursorController.Instance?.SetGolden(IsMidasActiveThisSession);
            Rebuild.UI.SystemCursorController.Instance?.EnterMiningSession();
            MiningCursorUI.Instance?.ResetMineCadence();
            StopTalentPollCoroutines();
            StopBonusRockCoroutines();
            ResetRocksMinedCounter();

            Rebuild.Tutorial.TutorialManager.Instance?.MaybeShow(0);
            if (tileGrid != null)
            {
                tileGrid.BuildIfNeeded();
                StartCoroutine(WaveThenSpawnThenTimer());
            }

            if (canvasGame != null)
            {
                if (FlowerMechanics.Instance != null)
                {
                    FlowerMechanics.Instance.ClearAll();
                    FlowerMechanics.Instance.BindCanvasGame(canvasGame.transform);
                }
                if (CampfireMechanics.Instance != null)
                    CampfireMechanics.Instance.ActivateForSession(canvasGame.transform);
            }
        }

        IEnumerator WaveThenSpawnThenTimer()
        {
            yield return tileGrid.PlayWaveAnim();
            if (rockSpawner != null && tileGrid != null)
            {
                rockSpawner.SpawnAll(tileGrid);
                MiningCursorUI.Instance?.ResetMineCadence();
            }


            FlowerMechanics.Instance?.SpawnBatchAtStart();
            StartTimer();
        }

        void StartTimer()
        {
            if (_timerCoroutine != null) StopCoroutine(_timerCoroutine);
            _timerCoroutine = StartCoroutine(RunTimer());
        }

        IEnumerator RunTimer()
        {
            TimerRunning = false;
            _rockBonusSeconds = 0;
            _tickBonusSeconds = 0;
            _timeLeft = mineSessionTime;

            if (LevelMechanics.Instance != null && LevelMechanics.Instance.HasTalent(TalentId.MoreTime))
                _timeLeft += 5f;

            if (SkillTreeManager.Instance != null)
                _timeLeft += SkillTreeManager.Instance.GetLevel(UpgradeId.MoreTime);
            if (timerUI != null) { timerUI.SetVisible(true); timerUI.SetTime(_timeLeft); }
            MiningCursorUI.Instance?.ResetMineCadence();
            float delay = Mathf.Max(0f, sessionStartDelaySec);
            if (delay > 0f)
            {
                ShowStartPrompt();
                float delayLeft = delay;
                while (delayLeft > 0f && IsInSession && !ResultsShown)
                {
                    delayLeft -= Time.unscaledDeltaTime;
                    yield return null;
                }
                HideStartPrompt();
                if (!IsInSession || ResultsShown)
                {
                    _timerCoroutine = null;
                    yield break;
                }
                MiningCursorUI.Instance?.ResetMineCadence();
            }
            TimerRunning = true;

            StartTalentPollCoroutines();
            StartBonusRockCoroutines();
            float tickAccum = 0f;
            while (_timeLeft > 0f && IsInSession && !ResultsShown)
            {
                float dt = Time.deltaTime;
                _timeLeft -= dt;
                tickAccum += dt;

                while (tickAccum >= 1f)
                {
                    tickAccum -= 1f;
                    if (SkillTreeManager.Instance != null)
                    {
                        if (SkillTreeManager.Instance.GetLevel(UpgradeId.ChanceToAdd1SecondEverySecond) > 0
                            && D100Helper.Roll(0.09f))
                            _timeLeft += 1f;
                    }
                    RollSkillBonusTime(UpgradeId.TickBonusTimeChance);

                    Rebuild.Stats.StatsManager.Instance?.RecordTimeMined(1);
                }
                if (timerUI != null) timerUI.SetTime(_timeLeft);
                yield return null;
            }
            TimerRunning = false;
            _timerCoroutine = null;
            if (!ResultsShown && IsInSession)
            {
                if (timerUI != null) timerUI.SetTime(0f);
                StartCoroutine(ShowResultsRoutine());
            }
        }



        public void AddTimeLeft(float seconds)
        {
            if (!TimerRunning) return;
            _timeLeft += seconds;
            if (timerUI != null) timerUI.SetTime(_timeLeft);
        }

        internal void RollSkillBonusTime(UpgradeId upgrade)
        {
            var skills = SkillTreeManager.Instance;
            if (!TimerRunning || !IsInSession || ResultsShown || Time.timeScale == 0f || skills == null) return;

            int level = Mathf.Clamp(skills.GetLevel(upgrade), 0, skills.GetMaxLevel(upgrade));
            bool fromRock = upgrade == UpgradeId.RockBonusTimeChance;
            int limit = fromRock ? level * 2 : level;
            int awarded = fromRock ? _rockBonusSeconds : _tickBonusSeconds;
            if (awarded >= limit) return;
            if (!D100Helper.Roll(level * (fromRock ? 0.04f : 0.03f))) return;

            if (fromRock) _rockBonusSeconds++;
            else _tickBonusSeconds++;
            AddTimeLeft(1f);
        }


        void StartTalentPollCoroutines()
        {
            StopTalentPollCoroutines();
            var lm = LevelMechanics.Instance;
            if (lm == null) return;
            if (lm.HasTalent(TalentId.Zeus)) _zeusCoroutine = StartCoroutine(ZeusLoop());
            if (lm.HasTalent(TalentId.EnergiDrinker)) _energiCoroutine = StartCoroutine(EnergiDrinkerLoop());
        }

        void StopTalentPollCoroutines()
        {
            if (_zeusCoroutine != null) { StopCoroutine(_zeusCoroutine); _zeusCoroutine = null; }
            if (_energiCoroutine != null) { StopCoroutine(_energiCoroutine); _energiCoroutine = null; }
            EnergiBoostUntil = 0f;
        }





        IEnumerator ZeusLoop()
        {
            var wait = new WaitForSecondsRealtime(1f);
            while (IsInSession)
            {
                yield return wait;
                if (ResultsShown || !IsInSession) continue;
                if (!D100Helper.Roll(0.05f)) continue;
                var rocks = Object.FindObjectsByType<Rock>(FindObjectsSortMode.None);
                if (rocks == null || rocks.Length == 0) continue;
                int hits = 0;


                for (int attempts = 0; attempts < 45 && hits < 15; attempts++)
                {
                    if (ResultsShown || !IsInSession) break;
                    var pick = rocks[UnityEngine.Random.Range(0, rocks.Length)];
                    if (pick == null || pick.IsDying) continue;
                    if (ProjectileLauncher.SpawnCascade(ProjectileType.BeamOfLight, pick, cascadeDepth: 1))
                    {
                        hits++;
                        yield return new WaitForSecondsRealtime(UnityEngine.Random.Range(0.05f, 0.1f));
                    }
                }
                if (hits > 0) Rebuild.Stats.StatsManager.Instance?.RecordZeusStrike(hits);
            }
        }



        IEnumerator EnergiDrinkerLoop()
        {
            var wait = new WaitForSecondsRealtime(1f);
            while (IsInSession)
            {
                yield return wait;
                if (ResultsShown || !IsInSession) continue;
                if (D100Helper.Roll(0.07f))
                {
                    EnergiBoostUntil = Time.unscaledTime + 3f;

                    Rebuild.Stats.StatsManager.Instance?.RecordEnergiDrink();
                }
            }
        }


        void StartBonusRockCoroutines()
        {
            StopBonusRockCoroutines();
            var sk = SkillTreeManager.Instance;
            if (sk != null && sk.GetLevel(UpgradeId.SpawnXRockEveryXSecond) > 0)
                _spawnXRockCoroutine = StartCoroutine(SpawnXRockLoop());

            if (ArtifactsManager.Instance != null && ArtifactsManager.Instance.HasArtifact(ArtifactId.AncientDice))
                _diceSpawnCoroutine = StartCoroutine(SpawnRockDiceLoop());
            if (sk == null) return;
            if (sk.GetLevel(UpgradeId.SpawnPickaxeEverySecond) > 0)
                _autoPickaxeCoroutine = StartCoroutine(AutoPickaxeLoop());

            if (sk.GetLevel(UpgradeId.ShootCircleChance) > 0)
                _shootCircleCoroutine = StartCoroutine(ShootCircleLoop());
        }

        void StopBonusRockCoroutines()
        {
            if (_spawnXRockCoroutine != null) { StopCoroutine(_spawnXRockCoroutine); _spawnXRockCoroutine = null; }
            if (_autoPickaxeCoroutine != null) { StopCoroutine(_autoPickaxeCoroutine); _autoPickaxeCoroutine = null; }
            if (_diceSpawnCoroutine != null) { StopCoroutine(_diceSpawnCoroutine); _diceSpawnCoroutine = null; }
            if (_shootCircleCoroutine != null) { StopCoroutine(_shootCircleCoroutine); _shootCircleCoroutine = null; }
        }


        IEnumerator SpawnXRockLoop()
        {
            while (IsInSession)
            {
                int lv = SkillTreeManager.Instance != null
                    ? SkillTreeManager.Instance.GetLevel(UpgradeId.SpawnXRockEveryXSecond) : 0;
                if (lv <= 0) yield break;
                float period = ComputeBaseSpawnPeriod(lv);
                yield return new WaitForSecondsRealtime(period);
                if (ResultsShown || !IsInSession) continue;
                int count = ComputePeriodicSpawnCount(lv);
                for (int i = 0; i < count; i++) SpawnBonusRockRandomRow();
            }
        }

        static int ComputePeriodicSpawnCount(int lv) => lv <= 0 ? 0 : lv >= 3 ? 3 : 1;

        static float ComputeBaseSpawnPeriod(int lv) => lv switch
        {
            >= 3 => 0.2f,
            2 => 0.45f,
            1 => 1f,
            _ => 2f
        };





        IEnumerator SpawnRockDiceLoop()
        {
            while (IsInSession)
            {
                float waitTime = ComputeDiceSpawnPeriod();
                yield return new WaitForSecondsRealtime(waitTime);
                if (ResultsShown || !IsInSession) continue;
                if (ArtifactsManager.Instance == null || !ArtifactsManager.Instance.HasArtifact(ArtifactId.AncientDice))
                    yield break;
                SpawnBonusRockRandomRow();
            }
        }


        static float ComputeDiceSpawnPeriod()
        {
            float p = 0.7f;
            if (LevelMechanics.Instance != null && LevelMechanics.Instance.HasTalent(TalentId.Archaeologist))
                p -= 0.08f;
            if (ArtifactsManager.Instance != null && ArtifactsManager.Instance.HasArtifact(ArtifactId.Rune))
                p -= 0.05f;
            return Mathf.Max(0.2f, p);
        }




        IEnumerator AutoPickaxeLoop()
        {
            while (IsInSession)
            {
                int lv = SkillTreeManager.Instance != null
                    ? SkillTreeManager.Instance.GetLevel(UpgradeId.SpawnPickaxeEverySecond) : 0;
                if (lv <= 0) yield break;
                float period = ComputeAutoPickaxePeriod(lv);
                yield return new WaitForSecondsRealtime(period);
                if (ResultsShown || !IsInSession) continue;
                var rocks = Object.FindObjectsByType<Rock>(FindObjectsSortMode.None);
                if (rocks == null || rocks.Length == 0) continue;
                Rock pick = null;
                for (int attempt = 0; attempt < 10; attempt++)
                {
                    var candidate = rocks[UnityEngine.Random.Range(0, rocks.Length)];
                    if (candidate != null && !candidate.IsDying) { pick = candidate; break; }
                }
                pick?.ApplyExternalHit();
                Rebuild.Stats.StatsManager.Instance?.RecordPickaxeSpawned();
            }
        }


        static float ComputeAutoPickaxePeriod(int lv)
        {
            float p = 1.2f;
            if (lv >= 1) p -= 0.2f;
            if (lv >= 2) p -= 0.3f;
            if (lv >= 3) p -= 0.3f;
            return Mathf.Max(0.2f, p);
        }




        IEnumerator ShootCircleLoop()
        {
            var wait = new WaitForSecondsRealtime(1f);
            while (IsInSession)
            {
                yield return wait;
                if (ResultsShown || !IsInSession) continue;
                if (SkillTreeManager.Instance == null) continue;
                if (SkillTreeManager.Instance.GetLevel(UpgradeId.ShootCircleChance) <= 0) continue;
                if (!D100Helper.Roll(0.30f)) continue;
                MiningCursorUI.Instance?.ShootCirclePulse();
            }
        }


        public void SpawnBonusRockRandomRow()
        {
            if (TimerRunning && !ResultsShown) rockSpawner?.SpawnOneInEmptyCell();
        }


        public void RequestEndSession()
        {
            if (!IsInSession || ResultsShown) return;
            if (_timerCoroutine != null) { StopCoroutine(_timerCoroutine); _timerCoroutine = null; }
            TimerRunning = false;
            HideStartPrompt();
            if (timerUI != null) timerUI.SetTime(0f);
            StartCoroutine(ShowResultsRoutine());
        }

        IEnumerator ShowResultsRoutine()
        {
            ResultsShown = true;
            StopTalentPollCoroutines();
            StopBonusRockCoroutines();
            if (endSessionBtn != null) endSessionBtn.interactable = false;
            if (rockSpawner != null) rockSpawner.DisableInteraction();
            if (outOfTimeText != null)
            {
                outOfTimeText.SetActive(true);
                var tmp = outOfTimeText.GetComponent<TMP_Text>();
                if (tmp != null)
                {
                    float t = 0f;
                    var c = tmp.color; c.a = 0f; tmp.color = c;
                    while (t < outOfTimeDisplaySec * 0.5f)
                    {
                        t += Time.unscaledDeltaTime;
                        c.a = Mathf.Clamp01(t / (outOfTimeDisplaySec * 0.5f)); tmp.color = c;
                        yield return null;
                    }
                    yield return new WaitForSecondsRealtime(outOfTimeDisplaySec * 0.3f);
                    t = 0f;
                    while (t < outOfTimeDisplaySec * 0.4f)
                    {
                        t += Time.unscaledDeltaTime;
                        c.a = 1f - Mathf.Clamp01(t / (outOfTimeDisplaySec * 0.4f)); tmp.color = c;
                        yield return null;
                    }
                    c.a = 0f; tmp.color = c;
                }
                else
                {
                    yield return new WaitForSecondsRealtime(outOfTimeDisplaySec);
                }
                outOfTimeText.SetActive(false);
            }
            if (resultsUI != null && GoldAndOreMechanics.Instance != null)
            {
                int xp = LevelMechanics.Instance != null ? LevelMechanics.Instance.XpThisSession : 0;
                int lv = LevelMechanics.Instance != null ? LevelMechanics.Instance.LevelsGainedThisSession : 0;
                int pts = LevelMechanics.Instance != null ? LevelMechanics.Instance.PointsGainedThisSession : 0;



                var oreThisSession = GoldAndOreMechanics.Instance.CopySessionGained();
                var barsCrafted = GoldAndOreMechanics.Instance.CraftSessionToBars();
                Rebuild.Audio.AudioManager.Instance?.Play("CraftingDone");
                resultsUI.Show(oreThisSession, barsCrafted, xp, lv, pts);
            }
            Rebuild.Audio.AudioManager.Instance?.Play("OutOfTime");


            Rebuild.Stats.StatsManager.Instance?.RecordSessionCompleted();



            MaybeShowTalentPicker();
        }

        void MaybeShowTalentPicker()
        {
            var lm = LevelMechanics.Instance;
            if (lm == null || talentPicker == null)
            {
                SetResultButtonsInteractable(true);
                return;
            }
            if (lm.CanChooseTalent && lm.HasUnchosenTalents())
            {
                SetResultButtonsInteractable(false);
                talentPicker.OnCardChosen -= HandleTalentChosen;
                talentPicker.OnCardChosen += HandleTalentChosen;
                talentPicker.Show(lm.GetUnchosenTalents());
            }
            else
            {
                SetResultButtonsInteractable(true);
            }
        }

        void HandleTalentChosen()
        {
            Rebuild.Audio.AudioManager.Instance?.Play("CardPop");
            var lm = LevelMechanics.Instance;
            if (lm != null && lm.CanChooseTalent && lm.HasUnchosenTalents())
            {
                talentPicker.Show(lm.GetUnchosenTalents());
            }
            else
            {
                if (talentPicker != null)
                {
                    talentPicker.OnCardChosen -= HandleTalentChosen;
                    talentPicker.Hide();
                }
                SetResultButtonsInteractable(true);
            }
        }

        void SetResultButtonsInteractable(bool value)
        {
            if (resultsUI == null) return;
            if (resultsUI.KeepMiningButton != null) resultsUI.KeepMiningButton.interactable = value;
            if (resultsUI.ReturnToMenuButton != null) resultsUI.ReturnToMenuButton.interactable = value;
        }

        public void OnKeepMining()
        {
            if (!IsInSession || !ResultsShown) return;
            if (circleTransition == null) { ApplyKeepMining(); return; }
            circleTransition.PlayTransition(ApplyKeepMining);
        }

        void ApplyKeepMining()
        {
            ResultsShown = false;
            if (resultsUI != null) resultsUI.Hide();
            if (talentPicker != null) talentPicker.Hide();
            if (outOfTimeText != null) outOfTimeText.SetActive(false);
            if (endSessionBtn != null) endSessionBtn.interactable = true;
            TimerRunning = false;
            HideStartPrompt();
            if (rockSpawner != null) { rockSpawner.ClearAll(); rockSpawner.EnableInteraction(); }
            if (GoldAndOreMechanics.Instance != null) GoldAndOreMechanics.Instance.ResetSession();
            if (LevelMechanics.Instance != null) LevelMechanics.Instance.ResetSession();



            if (FlowerMechanics.Instance != null) FlowerMechanics.Instance.RollSessionBudget();


            if (ArtifactsManager.Instance != null) ArtifactsManager.Instance.ResetSessionFlag();

            if (rockSpawner != null) rockSpawner.ResetGoldenChestFlag();

            if (PotionBuffMechanics.Instance != null) PotionBuffMechanics.Instance.RollForSession();

            if (SignBuffMechanics.Instance != null) SignBuffMechanics.Instance.RefreshForSession();



            var lm = LevelMechanics.Instance;
            IsMidasActiveThisSession = lm != null && lm.HasTalent(TalentId.MidasTouch)
                && D100Helper.Roll(0.20f);
            if (IsMidasActiveThisSession)
                Rebuild.Stats.StatsManager.Instance?.RecordMidasTouchSession();
            StopTalentPollCoroutines();
            StopBonusRockCoroutines();
            ResetRocksMinedCounter();
            MiningCursorUI.Instance?.ResetMineCadence();

            if (canvasGame != null)
            {
                if (FlowerMechanics.Instance != null)
                {
                    FlowerMechanics.Instance.ClearAll();
                    FlowerMechanics.Instance.BindCanvasGame(canvasGame.transform);
                }
                if (CampfireMechanics.Instance != null)
                    CampfireMechanics.Instance.ActivateForSession(canvasGame.transform);
            }
            if (tileGrid != null) StartCoroutine(WaveThenSpawnThenTimer());
        }

        public void OnReturnToMenu()
        {
            if (!IsInSession) return;
            if (circleTransition == null) { ApplyExitSession(); return; }
            circleTransition.PlayTransition(ApplyExitSession);
        }

        public void EndSession() => OnReturnToMenu();

        void ApplyExitSession()
        {
            IsInSession = false;
            ResultsShown = false;
            IsMidasActiveThisSession = false;

            Rebuild.UI.SystemCursorController.Instance?.ExitMiningSession();


            if (FlowerMechanics.Instance != null) FlowerMechanics.Instance.ResetSessionBudget();

            if (PotionBuffMechanics.Instance != null) PotionBuffMechanics.Instance.ResetForSession();
            StopTalentPollCoroutines();
            StopBonusRockCoroutines();
            if (resultsUI != null) resultsUI.Hide();
            if (talentPicker != null) talentPicker.Hide();
            if (outOfTimeText != null) outOfTimeText.SetActive(false);
            if (_timerCoroutine != null) { StopCoroutine(_timerCoroutine); _timerCoroutine = null; }
            TimerRunning = false;
            HideStartPrompt();
            if (rockSpawner != null) { rockSpawner.ClearAll(); rockSpawner.EnableInteraction(); }

            if (CampfireMechanics.Instance != null) CampfireMechanics.Instance.DeactivateForSession();
            if (FlowerMechanics.Instance != null) FlowerMechanics.Instance.ClearAll();
            if (canvasGame != null) canvasGame.SetActive(false);
            if (canvasMiningScene != null) canvasMiningScene.SetActive(false);
            if (mainMenu != null) mainMenu.ShowMainMenu();

            Rebuild.Tutorial.TutorialManager.Instance?.MaybeShow(1);
        }

        void ShowStartPrompt()
        {
            if (outOfTimeText == null) return;
            var tmp = outOfTimeText.GetComponent<TMP_Text>();
            if (tmp == null) return;
            CaptureOutOfTimeTextDefault(tmp);
            tmp.text = "발굴 시작!";
            var color = tmp.color;
            color.a = 1f;
            tmp.color = color;
            outOfTimeText.SetActive(true);
        }

        void HideStartPrompt()
        {
            if (outOfTimeText == null) return;
            var tmp = outOfTimeText.GetComponent<TMP_Text>();
            if (tmp != null)
            {
                CaptureOutOfTimeTextDefault(tmp);
                tmp.text = _outOfTimeTextDefault;
            }
            outOfTimeText.SetActive(false);
        }

        void CaptureOutOfTimeTextDefault(TMP_Text tmp)
        {
            if (_outOfTimeTextDefaultCaptured || tmp == null) return;
            _outOfTimeTextDefault = tmp.text;
            _outOfTimeTextDefaultCaptured = true;
        }
    }
}
