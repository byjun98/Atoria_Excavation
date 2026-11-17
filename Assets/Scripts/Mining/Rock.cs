using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Unity.Profiling;

namespace Rebuild.Mining
{








    [RequireComponent(typeof(Image))]
    public sealed class Rock : MonoBehaviour
    {
        static readonly ProfilerMarker s_damageMarker = new ProfilerMarker("Atoria.Rock.Damage");
        [SerializeField] int maxHp = 10;
        [SerializeField] float punchDuration = 0.12f;
        [SerializeField] float punchScale = 0.82f;
        [SerializeField] float flashDuration = 0.08f;
        [SerializeField] float fadeDuration = 0.25f;

        Image _img;
        Color _baseColor;
        Vector3 _baseScale;
        float _hp;
        bool _isDying;
        Coroutine _feedbackRoutine;
        RockCrackVisual _cracks;
        float _startingHp;
        float _halfHpThreshold;

        public OreType Ore { get; set; }
        public RockTier Tier { get; set; }
        public int Hp => Mathf.CeilToInt(_hp);
        public bool IsDying => _isDying;
        internal int GridCell { get; set; } = -1;
        internal bool HasReservedRock { get; private set; }
        RectTransform _reservedRockBadge;

        internal void ShowReservedRockBadge()
        {
            HasReservedRock = true;
            var go = new GameObject("ReservedRockBadge", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            _reservedRockBadge = (RectTransform)go.transform;
            _reservedRockBadge.SetParent(transform, false);
            var anchor = ((RectTransform)transform).pivot;
            _reservedRockBadge.anchorMin = _reservedRockBadge.anchorMax = anchor;
            _reservedRockBadge.anchoredPosition = new Vector2(53f, 30f);
            _reservedRockBadge.sizeDelta = new Vector2(58f, 36f);
            var background = go.GetComponent<Image>();
            background.color = new Color32(40, 31, 24, 242);
            background.raycastTarget = false;

            var label = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TMPro.TextMeshProUGUI))
                .GetComponent<TMPro.TextMeshProUGUI>();
            var labelRect = label.rectTransform;
            labelRect.SetParent(_reservedRockBadge, false);
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;
            label.text = "×2";
            label.fontSize = 29f;
            label.fontStyle = TMPro.FontStyles.Bold;
            label.color = Rebuild.UI.MuseumUiTheme.Gold;
            label.alignment = TMPro.TextAlignmentOptions.Center;
            label.raycastTarget = false;
        }



        public enum ChestKind { None = 0, Chest = 1, Golden = 2 }
        public ChestKind Chest { get; set; }





        public ArtifactId? AssignedArtifact { get; private set; }


        GameObject _artifactOverlayGo;
        Image _artifactOverlayImg;
        bool _artifactOverlayRevealed;

        void Awake()
        {
            if (!TryGetComponent<RockQueryObserver>(out _)) gameObject.AddComponent<RockQueryObserver>();
            _img = GetComponent<Image>();
            _baseColor = _img.color;
            _baseScale = transform.localScale;
            _hp = maxHp;
            _startingHp = maxHp;
        }

        void OnDestroy()
        {
            RockQuery.Invalidate();
            RockSpawner.Instance?.RemoveFromRow(this, transform.parent as RectTransform);
        }

        void OnEnable() => RockQuery.Invalidate();
        void OnDisable() => RockQuery.Invalidate();




        static readonly float[] s_materialsWorthSteps = { 1f, 2f, 3f, 9f, 12f, 4f, 6f, 18f };
        public static float GetMaterialsWorthMoreBonus(int level)
        {
            if (level <= 0) return 0f;
            float total = 0f;
            int max = Mathf.Min(level, s_materialsWorthSteps.Length);
            for (int i = 0; i < max; i++) total += s_materialsWorthSteps[i];
            return total;
        }





        static readonly float[] s_improvedPickaxeSteps = { 0.031f, 0.034f, 0.04f, 0.045f, 0.054f, 0.063f };
        public static float GetImprovedPickaxeBonus(int level)
        {
            if (level <= 0) return 0f;
            float total = 0f;
            int max = Mathf.Min(level, s_improvedPickaxeSteps.Length);
            for (int i = 0; i < max; i++) total += s_improvedPickaxeSteps[i];
            return total;
        }

        public void Initialize(OreType ore, int hp) => Initialize(ore, hp, RockTier.Normal, ChestKind.None);

        public void Initialize(OreType ore, int hp, RockTier tier) => Initialize(ore, hp, tier, ChestKind.None);

        public void Initialize(OreType ore, float hp, RockTier tier, ChestKind chest)
        {
            Ore = ore;
            Tier = tier;
            Chest = chest;
            _hp = hp;
            _startingHp = hp;
            _halfHpThreshold = Mathf.Max(1f, hp * 0.5f);
            if (_cracks != null)
            {
                _cracks.SetProgress(0);
                _cracks.SetOpacity(1f);
            }

            AssignedArtifact = null;
            _artifactOverlayRevealed = false;



            if (_img != null) _img.raycastTarget = false;



            if (LevelMechanics.Instance != null && LevelMechanics.Instance.HasTalent(TalentId.ShapeShifter))
            {
                _baseScale *= 1.06f;
                transform.localScale = _baseScale;
            }
        }








        public void AssignArtifact(ArtifactId id)
        {
            if (Chest != ChestKind.None) return;
            AssignedArtifact = id;
            EnsureArtifactOverlay(id);
        }

        void EnsureArtifactOverlay(ArtifactId id)
        {
            if (_artifactOverlayGo != null) return;


            var spriteA = SpriteLibrary.GetArtifactRockSprite(id);
            var spriteB = SpriteLibrary.GetArtifactSprite(id);
            var fallbackBase = RockSpriteFactory.GetRockSprite();
            var info = ArtifactDef.Get(id);
            Sprite chosen;
            bool tint;
            float scale;
            if (spriteA != null)
            {

                chosen = spriteA; tint = false; scale = 1.0f;
            }
            else if (spriteB != null && spriteB != fallbackBase)
            {

                chosen = spriteB; tint = false; scale = 0.45f;
            }
            else
            {

                chosen = fallbackBase; tint = true; scale = 0.45f;
            }

            _artifactOverlayGo = new GameObject($"ArtifactOverlay_{id}",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            _artifactOverlayGo.transform.SetParent(transform, false);
            var rt = (RectTransform)_artifactOverlayGo.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            var rockRt = (RectTransform)transform;
            rt.sizeDelta = rockRt.sizeDelta * scale;
            rt.localScale = Vector3.one;

            _artifactOverlayImg = _artifactOverlayGo.GetComponent<Image>();
            _artifactOverlayImg.sprite = chosen;
            _artifactOverlayImg.color = tint ? info.Tint : Color.white;
            _artifactOverlayImg.preserveAspect = true;
            _artifactOverlayImg.raycastTarget = false;

            _artifactOverlayGo.SetActive(false);
        }

        void RevealArtifactOverlayIfReady()
        {
            if (_artifactOverlayRevealed) return;
            if (_artifactOverlayGo == null) return;
            _artifactOverlayRevealed = true;
            _artifactOverlayGo.SetActive(true);
        }



        bool _hammerDoubleNextDeath;





        public void ApplyExternalHit()
        {
            if (_isDying) return;



            Rebuild.Audio.OverlappingSounds.Instance?.PlayPickaxe();
            Rebuild.Stats.StatsManager.Instance?.RecordClick();

            Rebuild.Stats.StatsManager.Instance?.RecordPickaxeHit();
            var sk = SkillTreeManager.Instance;
            var lm = LevelMechanics.Instance;











            bool hasAxe = ArtifactsManager.Instance != null
                && ArtifactsManager.Instance.HasArtifact(ArtifactId.Axe);
            float axeRuneMult = ArtifactsManager.Instance != null
                ? ArtifactsManager.Instance.RuneMultiplier : 1f;
            float axeArcheoMult = (lm != null && lm.HasTalent(TalentId.Archaeologist)) ? 1.08f : 1f;
            float instaArtifact = hasAxe ? 0.01f * axeRuneMult * axeArcheoMult : 0f;
            float axeDoubleChance = hasAxe ? 0.02f * axeRuneMult * axeArcheoMult : 0f;

            float instaSkill = sk != null ? sk.GetLevel(UpgradeId.InstaMineChance) * 0.01f : 0f;
            float instaHammer = (lm != null && lm.HasTalent(TalentId.HammerTime)) ? 0.004f : 0f;
            float instaTotal = instaArtifact + instaSkill + instaHammer;
            if (instaTotal > 0f && D100Helper.Roll(instaTotal))
            {
                Rebuild.Stats.StatsManager.Instance?.RecordInstaMine();

                if (instaHammer > 0f && UnityEngine.Random.value < instaHammer / instaTotal)
                {
                    _hammerDoubleNextDeath = true;
                    Rebuild.Stats.StatsManager.Instance?.RecordHammerTrigger();
                }
                TakeDamage(int.MaxValue / 2);
                MiningCursorUI.Instance?.PlaySwingAt(transform.parent, ((RectTransform)transform).anchoredPosition);
                ProjectileLauncher.TrySpawnFromClick(this);
                return;
            }


            bool axeDouble = false;
            if (axeDoubleChance > 0f && D100Helper.Roll(axeDoubleChance))
            {
                axeDouble = true;
                Rebuild.Stats.StatsManager.Instance?.RecordDoubleDamage();
            }





            float dmg = 0f;
            if (AnvilManager.Instance != null)
                dmg += AnvilManager.Instance.CurrentDamageBonus;

            float strengthMult = 1f;
            if (sk != null)
            {


                int impLv = sk.GetLevel(UpgradeId.ImprovedPickaxe);
                if (impLv > 0) strengthMult += GetImprovedPickaxeBonus(impLv);
            }
            if (PotionBuffMechanics.Instance != null)
                strengthMult += PotionBuffMechanics.Instance.PotionPickaxeStats;




            if (strengthMult != 1f) dmg *= strengthMult;








            if (ArtifactsManager.Instance != null)
            {
                float artifactMult = 1f;
                if (ArtifactsManager.Instance.HasArtifact(ArtifactId.Bone)) artifactMult += 0.04f;
                if (ArtifactsManager.Instance.HasArtifact(ArtifactId.WolfClaw))
                {

                    float wolfArcheoMult = (lm != null && lm.HasTalent(TalentId.Archaeologist)) ? 1.08f : 1f;
                    artifactMult += 0.06f * ArtifactsManager.Instance.RuneMultiplier * wolfArcheoMult;
                }
                if (ArtifactsManager.Instance.HasArtifact(ArtifactId.Rune)) artifactMult += 0.05f;
                dmg *= artifactMult;
            }

            if (axeDouble)
                dmg *= 2f;


            if (sk != null)
            {
                float doubleDmgChance = sk.GetLevel(UpgradeId.DoubleDamageChance) * 0.01f;
                if (doubleDmgChance > 0f && D100Helper.Roll(doubleDmgChance))
                {
                    dmg *= 2f;
                    Rebuild.Stats.StatsManager.Instance?.RecordDoubleDamage();
                }
            }


            if (AnvilManager.Instance != null)
            {
                float anvilDouble = AnvilManager.Instance.CurrentDoublePowerChance;
                if (anvilDouble > 0f && D100Helper.Roll(anvilDouble))
                {
                    dmg *= 2f;
                    Rebuild.Stats.StatsManager.Instance?.RecordDoubleDamage();
                }
            }
            TakeDamage(dmg);



            MiningCursorUI.Instance?.PlaySwingAt(transform.parent, ((RectTransform)transform).anchoredPosition);


            if (lm != null && lm.HasTalent(TalentId.MidasTouch)
                && MiningSessionController.Instance != null && MiningSessionController.Instance.IsMidasActiveThisSession
                && D100Helper.Roll(0.25f)
                && GoldAndOreMechanics.Instance != null)
            {
                GoldAndOreMechanics.Instance.AddOre(OreType.Gold, 1);
            }






            ProjectileLauncher.TrySpawnFromClick(this);


        }

        public void TakeDamage(float amount)
        {
            using var damageSample = s_damageMarker.Auto();
            if (_isDying) return;
            if (amount <= 0f) return;
            _hp -= amount;
            if (_hp <= 0)
            {
                _isDying = true;
                StartCoroutine(FadeAndDestroy());
            }
            else
            {
                if (_hp <= _halfHpThreshold && Chest == ChestKind.None)
                    RevealArtifactOverlayIfReady();
                if (_feedbackRoutine != null) StopCoroutine(_feedbackRoutine);
                _feedbackRoutine = StartCoroutine(HitFeedback());
            }
        }






        public void InstaKill()
        {
            if (_isDying) return;
            _hp = 0;
            _isDying = true;
            StartCoroutine(FadeAndDestroy());
        }

        IEnumerator HitFeedback()
        {

            float t = 0f;
            var punched = _baseScale * punchScale;
            float crackTarget = Chest == ChestKind.None
                ? Mathf.InverseLerp(.12f, .90f, 1f - _hp / Mathf.Max(1f, _startingHp)) : 0f;
            if (crackTarget > 0 && _cracks == null) _cracks = RockCrackVisual.Create((RectTransform)transform);
            float crackStart = _cracks != null ? _cracks.Progress : 0f;

            while (t < punchDuration * 0.4f)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / (punchDuration * 0.4f));
                transform.localScale = Vector3.Lerp(_baseScale, punched, k);
                _img.color = Color.Lerp(_baseColor, Color.white, Mathf.Min(1f, k * 1.5f));
                if (_cracks != null) _cracks.SetProgress(Mathf.Lerp(crackStart, crackTarget, k * .4f));
                yield return null;
            }

            t = 0f;
            float settleDur = punchDuration * 0.6f;
            while (t < settleDur)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / settleDur);
                transform.localScale = Vector3.Lerp(punched, _baseScale, k);
                float flashFade = Mathf.Clamp01((t - flashDuration) / Mathf.Max(0.0001f, settleDur - flashDuration));
                _img.color = Color.Lerp(Color.white, _baseColor, flashFade);
                if (_cracks != null) _cracks.SetProgress(Mathf.Lerp(crackStart, crackTarget, .4f + k * .6f));
                yield return null;
            }
            transform.localScale = _baseScale;
            _img.color = _baseColor;
            if (_cracks != null) _cracks.SetProgress(crackTarget);
            _feedbackRoutine = null;
        }

        IEnumerator FadeAndDestroy()
        {
            if (_feedbackRoutine != null) StopCoroutine(_feedbackRoutine);
            _img.raycastTarget = false;
            if (_reservedRockBadge != null) _reservedRockBadge.gameObject.SetActive(false);

            Rebuild.Audio.OverlappingSounds.Instance?.PlayRockBreak();



            var rockAnchored = ((RectTransform)transform).anchoredPosition;
            if (transform.parent != null)
                RockBreakParticleFx.Spawn(transform.parent, rockAnchored, 1f);



            ScreenShake.Instance?.Shake();












            int oreCount = RockTierDef.OreDropMultiplier(Tier);
            int xpAmount = LevelMechanics.Instance != null ? LevelMechanics.Instance.XpPerRock : 0;
            float worthMultiplier = 1f;
            if (SkillTreeManager.Instance != null)
            {


                worthMultiplier += GetMaterialsWorthMoreBonus(
                    SkillTreeManager.Instance.GetLevel(UpgradeId.MaterialsWorthMore));

                oreCount += SkillTreeManager.Instance.GetLevel(UpgradeId.MoreMaterialsFromRock);



                float doubleChance = SkillTreeManager.Instance.GetLevel(UpgradeId.DoubleXpGoldChance) * 0.10f;
                if (PotionBuffMechanics.Instance != null
                    && PotionBuffMechanics.Instance.PotionDoubleChanceIncrease > 0f)
                {
                    doubleChance += PotionBuffMechanics.Instance.PotionDoubleChanceIncrease * 0.01f;
                }
                if (doubleChance > 0f && D100Helper.Roll(doubleChance))
                {
                    oreCount *= 2;
                    xpAmount *= 2;


                    Rebuild.Stats.StatsManager.Instance?.RecordDoubleOre();
                }
                float fullOreChance = SkillTreeManager.Instance.GetLevel(UpgradeId.FullOreChance) * 0.05f;
                if (fullOreChance > 0f && D100Helper.Roll(fullOreChance))
                {
                    oreCount *= 3;

                    Rebuild.Stats.StatsManager.Instance?.RecordFullOreMined(Ore);
                }
            }


            if (PotionBuffMechanics.Instance != null
                && PotionBuffMechanics.Instance.PotionMaterialWorthMore > 0)
            {
                worthMultiplier += PotionBuffMechanics.Instance.PotionMaterialWorthMore;
            }

            int oreDrop = Mathf.Max(1, Mathf.RoundToInt(oreCount * worthMultiplier));





            int totalOreDrop = oreDrop;
            if (ArtifactsManager.Instance != null && Ore == OreType.Gold
                && ArtifactsManager.Instance.TryGoldStackProc())
            {
                totalOreDrop += oreDrop;
                Rebuild.Stats.StatsManager.Instance?.RecordDoubleOre();
            }




            if (_hammerDoubleNextDeath)
            {
                totalOreDrop *= 2;
                _hammerDoubleNextDeath = false;
            }

            if (GoldAndOreMechanics.Instance != null)
            {
                GoldAndOreMechanics.Instance.AddOre(Ore, totalOreDrop);


                if (MiningSessionController.Instance != null && OreCounterUI.Instance != null)
                {
                    var parent = MiningSessionController.Instance.CanvasMiningSceneTransform;
                    var target = OreCounterUI.Instance.GetEntryRT(Ore);
                    if (parent != null && target != null)
                        OreDropPopupFx.Spawn(parent, transform.position, Ore, totalOreDrop, target);
                }

                if (LevelMechanics.Instance != null && LevelMechanics.Instance.HasTalent(TalentId.GoldenTouch)
                    && Ore != OreType.Gold)
                {
                    GoldAndOreMechanics.Instance.AddOre(OreType.Gold, 1);
                }





                if (ArtifactsManager.Instance != null
                    && ArtifactsManager.Instance.HasArtifact(ArtifactId.Cheese))
                {
                    float cheeseArcheoMult = (LevelMechanics.Instance != null
                        && LevelMechanics.Instance.HasTalent(TalentId.Archaeologist)) ? 1.08f : 1f;
                    if (D100Helper.Roll(0.002f * ArtifactsManager.Instance.RuneMultiplier * cheeseArcheoMult))
                        GoldAndOreMechanics.Instance.AddOre(OreType.Gold, 1);
                }





                if (Chest == ChestKind.Chest)
                {
                    GoldAndOreMechanics.Instance.AddOre(Ore, 25);
                    Rebuild.Stats.StatsManager.Instance?.RecordChestOpened();
                }
                else if (Chest == ChestKind.Golden)
                {
                    GoldAndOreMechanics.Instance.AddOre(OreType.Gold, 50);
                    Rebuild.Stats.StatsManager.Instance?.RecordChestOpened();
                    Rebuild.Stats.StatsManager.Instance?.RecordGoldenChest();
                }
            }

            Rebuild.Stats.StatsManager.Instance?.RecordRockDestroyed(Ore, totalOreDrop);

            if (LevelMechanics.Instance != null && xpAmount > 0)
                LevelMechanics.Instance.GiveXP(xpAmount);











            if (ArtifactsManager.Instance != null)
            {
                ArtifactId? rolled;
                if (AssignedArtifact.HasValue)
                {
                    rolled = ArtifactsManager.Instance.ConfirmPreRolledArtifact(AssignedArtifact.Value);
                }
                else
                {
                    rolled = ArtifactsManager.Instance.TryRoll();
                }
                if (rolled.HasValue)
                {
                    if (ArtifactFoundPopupUI.Instance != null)
                        ArtifactFoundPopupUI.Instance.Show(rolled.Value);
                    if (transform.parent != null)
                    {




                        var artifactTarget = rockAnchored + new Vector2(0f, 260f);
                        ArtifactRockOverlayUI.Spawn(transform.parent, rockAnchored, artifactTarget, rolled.Value);
                    }
                }
            }

            if (FlowerMechanics.Instance != null)
                FlowerMechanics.Instance.TryRollOnRockDestroy(this);
            bool respawnInRow = false;


            if (SkillTreeManager.Instance != null)
            {
                var sk = SkillTreeManager.Instance;
                MiningSessionController.Instance?.RollSkillBonusTime(UpgradeId.RockBonusTimeChance);
                float respawnChance = sk.GetLevel(UpgradeId.RespawnRockChance) * 0.05f;
                respawnInRow = respawnChance > 0f && D100Helper.Roll(respawnChance);


                if (sk.GetLevel(UpgradeId.ChanceAdd1SecondEveryRockMined) > 0
                    && MiningSessionController.Instance != null
                    && D100Helper.Roll(0.00075f))
                {
                    MiningSessionController.Instance.AddTimeLeft(1f);
                }

                int xrockLv = sk.GetLevel(UpgradeId.SpawnRockEveryXrock);
                if (xrockLv > 0 && MiningSessionController.Instance != null)
                {
                    MiningSessionController.Instance.RecordRockMinedForSpawnCounter();
                    int period = Mathf.Max(3, 6 - Mathf.Clamp(xrockLv, 0, 3));
                    if (MiningSessionController.Instance.RocksMinedThisSession % period == 0)
                        MiningSessionController.Instance.SpawnBonusRockRandomRow();
                }
            }

            float t = 0f;
            var startScale = transform.localScale;
            var endScale = startScale * 1.15f;
            while (t < fadeDuration)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / fadeDuration);
                transform.localScale = Vector3.Lerp(startScale, endScale, k);
                var c = _baseColor; c.a = 1f - k;
                _img.color = c;
                if (_cracks != null) _cracks.SetOpacity(c.a);
                yield return null;
            }
            _img.enabled = false;
            RockSpawner.Instance?.CompleteBreak(this, respawnInRow);
            Destroy(gameObject);
        }
    }
}
