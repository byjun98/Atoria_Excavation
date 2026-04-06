using System.Collections;
using Rebuild.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Rebuild.Mining
{
    /// <summary>
    /// Owns the Main Menu &lt;-&gt; Mining Session swap.
    /// canvas swap. rock spawn. timer + results panel + Keep-Mining loop.
    /// </summary>
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
        [SerializeField] MiningTimerUI timerUI;
        [SerializeField] SessionResultsUI resultsUI;
        [SerializeField] GameObject outOfTimeText;
        [SerializeField] float outOfTimeDisplaySec = 0.9f;

        [Header("Talent picker")]
        [SerializeField] TalentCardPickerUI talentPicker;

        public bool IsInSession { get; private set; }
        public bool TimerRunning { get; private set; }
        public bool ResultsShown { get; private set; }

        // expose the mining-scene canvas so OreDropPopupFx can parent its popups
        // there (on top of Canvas_Game so the rock's fade doesn't take popups with it, and on
        // the same canvas as OreCounterUI so the fly-to target sits in the same coord space).
        public Transform CanvasMiningSceneTransform =>
            canvasMiningScene != null ? canvasMiningScene.transform : null;

        // MiningCursorUI's ScanAndHit needs the TileGrid to enumerate live rocks
        // by row. The grid is auto-resolved from canvasGame in AutoResolve.
        public TileGrid TileGrid => tileGrid;

        // Timer state is a field so original bonus-time effects can extend it mid-session.
        float _timeLeft;
        Coroutine _timerCoroutine;
        Coroutine _zeusCoroutine;
        Coroutine _energiCoroutine;

        // MidasTouch talent — rolled once per StartSession. While true, Rock.ApplyOneTick
        // drops a +1 Gold every 25% per tick. ApplyKeepMining preserves it across the keep-mining loop.
        public bool IsMidasActiveThisSession { get; private set; }

        // EnergiDrinker talent — Time.unscaledTime endpoint of the current speed boost.
        // Rock.CurrentMineTime multiplies its result by 0.7 while Time.unscaledTime < EnergiBoostUntil.
        public float EnergiBoostUntil { get; private set; }
        public bool IsEnergiBoosted => Time.unscaledTime < EnergiBoostUntil;

        // auto-tick / bonus-rock coroutines.
        Coroutine _spawnXRockCoroutine;
        Coroutine _autoPickaxeCoroutine;
        // Artifacts.AncientDice base spawn loop. 원본 SpawnRocks.SpawnRockDice 매칭.
        Coroutine _diceSpawnCoroutine;
        // ShootCircleChance auto-coroutine (per-sec 30% small circle hit).
        Coroutine _shootCircleCoroutine;

        // running rock-mined counter for SpawnRockEveryXrock. Cleared every fresh session.
        // Public so Rock.FadeAndDestroy can increment + roll the threshold.
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
                // restore the system cursor while hovering EndSession so the player
                // sees the standard click affordance instead of the mining hand.
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
            if (GoldAndOreMechanics.Instance != null) GoldAndOreMechanics.Instance.ResetSession();
            if (LevelMechanics.Instance != null) LevelMechanics.Instance.ResetSession();
            // roll SpringSeason XP budget for this session. Original SetRockScreen
            // rolls Random.Range(2, 18) once per session and treats it as a multiplier constant
            // for the entire session, NOT an alive-flower count.
            if (FlowerMechanics.Instance != null) FlowerMechanics.Instance.RollSessionBudget();
            // reset the "one artifact per session" gate so this fresh session can
            // discover an artifact again.
            if (ArtifactsManager.Instance != null) ArtifactsManager.Instance.ResetSessionFlag();
            // reset the "one GoldenChest per session" gate.
            if (rockSpawner != null) rockSpawner.ResetGoldenChestFlag();
            // roll PotionDrinker / PotionChugger potions for this session.
            if (PotionBuffMechanics.Instance != null) PotionBuffMechanics.Instance.RollForSession();
            // start (or refresh) the ItsASign 5min buff cycle.
            if (SignBuffMechanics.Instance != null) SignBuffMechanics.Instance.RefreshForSession();
            // hover-channel can fire dozens of projectile rolls per session.
            // Clear leftover counters so a stale projectile from a previous session never
            // permanently blocks a slot (defensive — OnDestroy normally handles it).
            ProjectileLauncher.ResetCounters();
            // MidasTouch — rolled once at the start of every FRESH session (not Keep-Mining).
            var lm = LevelMechanics.Instance;
            IsMidasActiveThisSession = lm != null && lm.HasTalent(TalentId.MidasTouch)
                && D100Helper.Roll(0.20f);
            // count sessions where the golden run actually triggered.
            if (IsMidasActiveThisSession)
                Rebuild.Stats.StatsManager.Instance?.RecordMidasTouchSession();
            EnergiBoostUntil = 0f;
            // swap the OS cursor for the hand sprite. SetGolden must fire BEFORE
            // EnterMiningSession so the first cursor frame already shows the right variant.
            Rebuild.UI.SystemCursorController.Instance?.SetGolden(IsMidasActiveThisSession);
            Rebuild.UI.SystemCursorController.Instance?.EnterMiningSession();
            MiningCursorUI.Instance?.ResetMineCadence();
            // Zeus / EnergiDrinker — one-second poll coroutines run while in session.
            StartTalentPollCoroutines();
            // auto-tick / periodic bonus-rock coroutines (rebound every fresh session
            // so newly purchased SkillTree nodes take effect immediately).
            ResetRocksMinedCounter();
            StartBonusRockCoroutines();
            // 첫 mining session 진입 시 frame 0 (환영 + 게임 룰) popup.
            Rebuild.Tutorial.TutorialManager.Instance?.MaybeShow(0);
            if (tileGrid != null)
            {
                tileGrid.BuildIfNeeded();
                StartCoroutine(WaveThenSpawnThenTimer());
            }
            // Campfire visual + auto-tick (Camper talent) + Flower spawn binding (SpringSeason).
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
                rockSpawner.SpawnAll(
                    tileGrid.RowContainers,
                    tileGrid.TileStartInRow,
                    tileGrid.TileStepInRow,
                    tileGrid.TilesPerRow);
                MiningCursorUI.Instance?.ResetMineCadence();
            }
            // 원본 SetRockScreen.SetFlowers 모델: rock spawn 직후 꽃 일괄 spawn.
            // SpringSeason talent 활성 시에만 SpawnBatchAtStart 가 spawn.
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
            TimerRunning = true;
            _timeLeft = mineSessionTime;
            // talent: More Time adds 5s to every session.
            if (LevelMechanics.Instance != null && LevelMechanics.Instance.HasTalent(TalentId.MoreTime))
                _timeLeft += 5f;
            // SkillTree: MoreTime adds +1s per level.
            if (SkillTreeManager.Instance != null)
                _timeLeft += SkillTreeManager.Instance.GetLevel(UpgradeId.MoreTime);
            if (timerUI != null) { timerUI.SetVisible(true); timerUI.SetTime(_timeLeft); }
            float tickAccum = 0f;
            while (_timeLeft > 0f && IsInSession && !ResultsShown)
            {
                float dt = Time.deltaTime;
                _timeLeft -= dt;
                tickAccum += dt;
                // Original chanceToAdd1SecEverySec: 9% per elapsed second when purchased.
                while (tickAccum >= 1f)
                {
                    tickAccum -= 1f;
                    if (SkillTreeManager.Instance != null)
                    {
                        if (SkillTreeManager.Instance.GetLevel(UpgradeId.ChanceToAdd1SecondEverySecond) > 0
                            && D100Helper.Roll(0.09f))
                            _timeLeft += 1f;
                    }
                    // stat — accumulate one second of "time mining" each loop boundary.
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

        /// <summary>RockBonusTimeChance hook (and any future bonus-time effect):
        /// adds <paramref name="seconds"/> to the live session timer. No-ops outside an active session.</summary>
        public void AddTimeLeft(float seconds)
        {
            if (!TimerRunning) return;
            _timeLeft += seconds;
            if (timerUI != null) timerUI.SetTime(_timeLeft);
        }

        // ---- Zeus / EnergiDrinker poll coroutines ----
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

        /// <summary>Zeus talent — every 1s while in session, 5% chance to strike random rocks
        /// with Beam projectiles (uses ProjectileLauncher.SpawnCascade so caps + pool apply).
        /// 원본 LevelMechanics.cs:1461-1481 매칭. hits cap 3 → 15
        /// (zeusLightningAmount=15), per-hit yield Random.Range(0.05f, 0.1f) 도입.</summary>
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
                // hits cap 3 → 15. attempts limit = 45 (hits 의 3× headroom).
                // per-hit yield 0.05~0.1s (원본 매칭, burst 안 spreading).
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

        /// <summary>EnergiDrinker talent — every 1s while in session, 7% chance to grant a 3s
        /// mineTime ×0.7 boost. <see cref="EnergiBoostUntil"/> is read by Rock.CurrentMineTime.</summary>
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
                    // count distinct energi boost windows triggered.
                    Rebuild.Stats.StatsManager.Instance?.RecordEnergiDrink();
                }
            }
        }

        // ---- auto-tick / bonus-rock coroutines ----
        void StartBonusRockCoroutines()
        {
            StopBonusRockCoroutines();
            var sk = SkillTreeManager.Instance;
            if (sk != null && sk.GetLevel(UpgradeId.SpawnXRockEveryXSecond) > 0)
                _spawnXRockCoroutine = StartCoroutine(SpawnXRockLoop());
            // AncientDice artifact 활성 시 dice spawn loop 가동. 원본 SpawnRocks.SpawnRockDice 매칭.
            if (ArtifactsManager.Instance != null && ArtifactsManager.Instance.HasArtifact(ArtifactId.AncientDice))
                _diceSpawnCoroutine = StartCoroutine(SpawnRockDiceLoop());
            if (sk == null) return;
            if (sk.GetLevel(UpgradeId.SpawnPickaxeEverySecond) > 0)
                _autoPickaxeCoroutine = StartCoroutine(AutoPickaxeLoop());
            // ShootCircleChance 이제 per-sec auto coroutine (원본 30% per-sec).
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

        /// <summary>원본
        /// SpawnRocks.SpawnRockXSecond 매칭. SpawnXRockEveryXSecond SkillTree 노드 구매 후
        /// random row 에 rock 1개 spawn. period 는 lv1=1.3s, lv2=0.6s, lv3=0.2s.
        /// 원본 LocalizationScript.spawnEverySecondIncrease
        /// (0.7 / 0.7 / 0.4) 누적 모델 매칭.</summary>
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
                SpawnBonusRockRandomRow();
            }
        }

        /// <summary>원본 LocalizationScript.spawnEverySecondIncrease 누적 모델.
        /// lv1/lv2 = -0.7s, lv3 = -0.4s. 합산: 2.0 → 1.3 → 0.6 → 0.2 (floor).</summary>
        static float ComputeBaseSpawnPeriod(int lv)
        {
            float p = 2f;
            if (lv >= 1) p -= 0.7f;
            if (lv >= 2) p -= 0.7f;
            if (lv >= 3) p -= 0.4f;
            return Mathf.Max(0.2f, p);
        }

        /// <summary>Artifacts.AncientDice 보유 시 추가 spawn loop. 원본
        /// SpawnRocks.SpawnRockDice 매칭. base 0.7s (Artifacts.diceTime), Archaeologist 특성 -0.08s,
        /// Rune artifact -0.05s. 두 가지 모두 활성 시 누적 (min 0.2s). 원본의 buff = 느려지는 quirk 는
        /// 의도된 design 이 아닌 것으로 판단해 의도된 방향(감소) 으로 매칭.</summary>
        IEnumerator SpawnRockDiceLoop()
        {
            while (IsInSession)
            {
                float waitTime = ComputeDiceSpawnPeriod();
                yield return new WaitForSecondsRealtime(waitTime);
                if (ResultsShown || !IsInSession) continue;
                if (ArtifactsManager.Instance == null || !ArtifactsManager.Instance.HasArtifact(ArtifactId.AncientDice))
                    yield break; // artifact 가 빠지면 loop 종료 (안전망)
                SpawnBonusRockRandomRow();
            }
        }

        /// <summary>AncientDice base 0.7s. Archaeologist (-0.08) + Rune (-0.05) 누적.</summary>
        static float ComputeDiceSpawnPeriod()
        {
            float p = 0.7f;
            if (LevelMechanics.Instance != null && LevelMechanics.Instance.HasTalent(TalentId.Archaeologist))
                p -= 0.08f;
            if (ArtifactsManager.Instance != null && ArtifactsManager.Instance.HasArtifact(ArtifactId.Rune))
                p -= 0.05f;
            return Mathf.Max(0.2f, p);
        }

        /// <summary>SpawnPickaxeEverySecond — 원본 spawnPickaxeEverySecond base=1.2s,
        /// 누적 increment 0.2/0.3/0.3 (lv1=1.0, lv2=0.7, lv3=0.4). Routes through Rock.ApplyExternalHit
        /// so all stat hooks + projectile rolls + sound fires identically to a manual click.</summary>
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

        /// <summary>원본 spawnPickaxeEverySecond base=1.2s, decrement 누적 0.2/0.3/0.3.</summary>
        static float ComputeAutoPickaxePeriod(int lv)
        {
            float p = 1.2f;
            if (lv >= 1) p -= 0.2f;
            if (lv >= 2) p -= 0.3f;
            if (lv >= 3) p -= 0.3f;
            return Mathf.Max(0.2f, p);
        }

        /// <summary>ShootCircleChance auto-coroutine. 원본 circleShootChance=30f.
        /// Once per second it launches a small circle from the cursor; only rocks inside that
        /// circle receive a normal pickaxe hit.</summary>
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

        /// <summary>pick a random TileGrid row and spawn a bonus rock there.
        /// Used by both SpawnXRockEveryXSecond and Rock.FadeAndDestroy (SpawnRockEveryXrock /
        /// ChanceToSpawnRockWhenMined hooks).</summary>
        public void SpawnBonusRockRandomRow()
        {
            if (rockSpawner == null || tileGrid == null) return;
            var rows = tileGrid.RowContainers;
            if (rows == null || rows.Length == 0) return;
            var row = rows[UnityEngine.Random.Range(0, rows.Length)];
            if (row == null) return;
            rockSpawner.SpawnOneInRow(row);
        }

        /// <summary>Triggered by the EndSession HUD button — collapses the timer and shows results immediately.</summary>
        public void RequestEndSession()
        {
            if (!IsInSession || ResultsShown) return;
            if (_timerCoroutine != null) { StopCoroutine(_timerCoroutine); _timerCoroutine = null; }
            TimerRunning = false;
            if (timerUI != null) timerUI.SetTime(0f);
            StartCoroutine(ShowResultsRoutine());
        }

        IEnumerator ShowResultsRoutine()
        {
            ResultsShown = true;
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
                // snapshot the raw ore mined this session, then convert to bars before
                // showing results. The conversion credits _oreCounts (bar currency) and the table
                // returned drives the results panel's "ore → bars" rows.
                var oreThisSession = GoldAndOreMechanics.Instance.CopySessionGained();
                var barsCrafted = GoldAndOreMechanics.Instance.CraftSessionToBars();
                Rebuild.Audio.AudioManager.Instance?.Play("CraftingDone");
                resultsUI.Show(oreThisSession, barsCrafted, xp, lv, pts);
            }
            Rebuild.Audio.AudioManager.Instance?.Play("OutOfTime");
            // a session that reaches the results panel counts as completed. Bailing via
            // the menu before timer expiry skips this hook intentionally (matches original Shell).
            Rebuild.Stats.StatsManager.Instance?.RecordSessionCompleted();

            // if the player banked talent points this session, gate the results buttons
            // behind the talent picker until all points are spent (or no talents remain).
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
            if (rockSpawner != null) { rockSpawner.ClearAll(); rockSpawner.EnableInteraction(); }
            if (GoldAndOreMechanics.Instance != null) GoldAndOreMechanics.Instance.ResetSession();
            if (LevelMechanics.Instance != null) LevelMechanics.Instance.ResetSession();
            // roll SpringSeason XP budget for this session. Original SetRockScreen
            // rolls Random.Range(2, 18) once per session and treats it as a multiplier constant
            // for the entire session, NOT an alive-flower count.
            if (FlowerMechanics.Instance != null) FlowerMechanics.Instance.RollSessionBudget();
            // reset the "one artifact per session" gate so this fresh session can
            // discover an artifact again.
            if (ArtifactsManager.Instance != null) ArtifactsManager.Instance.ResetSessionFlag();
            // reset the "one GoldenChest per session" gate.
            if (rockSpawner != null) rockSpawner.ResetGoldenChestFlag();
            // roll PotionDrinker / PotionChugger potions for this session.
            if (PotionBuffMechanics.Instance != null) PotionBuffMechanics.Instance.RollForSession();
            // start (or refresh) the ItsASign 5min buff cycle.
            if (SignBuffMechanics.Instance != null) SignBuffMechanics.Instance.RefreshForSession();
            // re-roll MidasTouch on Keep-Mining (the original talent fires per session,
            // and "Keep Mining" mirrors the original behaviour of acting like a fresh session).
            // Zeus / EnergiDrinker coroutines also rebind in case the player just picked them.
            var lm = LevelMechanics.Instance;
            IsMidasActiveThisSession = lm != null && lm.HasTalent(TalentId.MidasTouch)
                && D100Helper.Roll(0.20f);
            if (IsMidasActiveThisSession)
                Rebuild.Stats.StatsManager.Instance?.RecordMidasTouchSession();
            StartTalentPollCoroutines();
            // same rebind for auto-tick / bonus-rock coroutines on Keep Mining.
            ResetRocksMinedCounter();
            StartBonusRockCoroutines();
            MiningCursorUI.Instance?.ResetMineCadence();
            // rebind so any newly-chosen Camper / SpringSeason talents take effect this round.
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
            // restore the OS cursor when the session ends.
            Rebuild.UI.SystemCursorController.Instance?.ExitMiningSession();
            // clear SpringSeason budget so idle XP outside of a session never
            // inherits the bonus.
            if (FlowerMechanics.Instance != null) FlowerMechanics.Instance.ResetSessionBudget();
            // clear Potion buffs so idle TheMine production doesn't keep them.
            if (PotionBuffMechanics.Instance != null) PotionBuffMechanics.Instance.ResetForSession();
            StopTalentPollCoroutines();
            StopBonusRockCoroutines();
            if (resultsUI != null) resultsUI.Hide();
            if (talentPicker != null) talentPicker.Hide();
            if (outOfTimeText != null) outOfTimeText.SetActive(false);
            if (_timerCoroutine != null) { StopCoroutine(_timerCoroutine); _timerCoroutine = null; }
            TimerRunning = false;
            if (rockSpawner != null) { rockSpawner.ClearAll(); rockSpawner.EnableInteraction(); }
            // stop campfire tick + wipe lingering flowers before hiding Canvas_Game.
            if (CampfireMechanics.Instance != null) CampfireMechanics.Instance.DeactivateForSession();
            if (FlowerMechanics.Instance != null) FlowerMechanics.Instance.ClearAll();
            if (canvasGame != null) canvasGame.SetActive(false);
            if (canvasMiningScene != null) canvasMiningScene.SetActive(false);
            if (mainMenu != null) mainMenu.ShowMainMenu();
            // first session end → tutorial frame 1 (oreAndCrafting).
            Rebuild.Tutorial.TutorialManager.Instance?.MaybeShow(1);
        }
    }
}
