using System;

namespace Rebuild.Saving
{
    /// <summary>JSON-serialisable snapshot of every persistent field across
    /// the 15 managers. Loaded on Awake / saved on Quit / Pause / 30s auto-tick.
    ///
    /// Schema versioning lets future migrations () bump <see cref="schemaVersion"/>
    /// and translate older field layouts on load. Array sizes match the Shell enum counts —
    /// if a future enum extension grows them, the DTO arrays grow too and old saves load
    /// with the trailing slots default-initialised (level 0 / false / 0).</summary>
    [Serializable]
    public sealed class GameDataDTO
    {
        // Bump on enum-index-changing migrations. v1 = baseline, v2 = // (talent/stat/ach array sizes grown — Mathf.Min in each LoadData handles old saves safely).
        // v3 = (tutorial 6-frame array, talent state fields, pickaxe skins reservation,
        // stats grown 33→47, achievements grown 16→22, tooltip toggle).
        // v4 = .A (2026-05-16) MaterialsWorthMore MaxLevel 3 → 8 + ImprovedPickaxe 4 → 6 + 기타 원본 매칭.
        //      SkillTreeManager.LoadData 의 Mathf.Min 으로 기존 save 안전. 새 lv 슬롯은 0 (미구매).
        public int schemaVersion = 5;
        public long lastSavedUnix;

        // cumulative ore counts (8 ores, long-typed for idle accumulation).
        public long[] oreCounts = new long[8];

        // + XP / Level / 20 chosen talents.
        public int level = 1;
        public int currentXp = 0;
        public int talentPoints = 0;
        public int nextTalentPrice = 1;
        public bool[] talentsChosen = new bool[20];

        // Full skill-tree save state.
        // 38 () → 72 (ore + tick variety) → 77 (4 cascade + 1 PlazmaPickaxe).
        // SkillTreeManager.LoadData uses Mathf.Min so smaller legacy saves migrate safely
        // (trailing entries default to level 0 — fresh purchases needed).
        public int[] skillTreeLevels = new int[Rebuild.Mining.UpgradeDef.Count];

        // The Anvil: which pickaxe is equipped + which are crafted.
        // Array length matches PickaxeDef.Count (14). Old 5-slot saves migrate on next save —
        // AnvilManager.LoadData uses Mathf.Min so the trailing 9 default to false (un-crafted).
        public int currentPickaxeIdx = 0;
        public bool[] pickaxesCrafted = new bool[14];

        // The Mine idle upgrade levels.
        public int mineSpeedLevel = 0;
        public int mineMaterialLevel = 0;
        // TheMine 첫 잠금 해제 상태. 원본 GameData.isTheMineUnlocked 와 동일.
        // 기존 save 에 없음 → JsonUtility default false → 사용자가 다시 500 Gold 지불 필요.
        public bool isTheMineUnlocked = false;

        // Found artifacts (all 14). Old 6-slot saves migrate the
        // first six bools via ArtifactsManager.LoadData's Mathf.Min; the new 8 default to false
        // (un-found) and get rolled normally during play.
        public bool[] artifactsFound = new bool[14];

        // + Stats (47 counters, was 33), Achievements (22 unlock
        // flags, was 16), tutorial 6-frame pressedOk array (was single bool — schema v3 migrates
        // legacy single bool into pressedOk[0] only so the player still skips the boot tutorial).
        // Mathf.Min migration in StatsManager / AchievementsManager LoadData zeros trailing slots
        // for old saves; new fields slot in at append-only indices.
        public long[] stats = new long[47];
        public bool[] achievementsUnlocked = new bool[22];
        public bool[] achievementsNotified = new bool[22];
        public bool tutorialSeen = false; // legacy schema v2 — kept for migration only
        public bool[] tutorialPressedOk = new bool[6]; // 6 context-trigger frames

        // Ending sequence (BigRock destruction + Credits roll). Set true once the
        // player watches the credits all the way through. Persists across reboots so the
        // ending only plays once unless ResetAll is invoked.
        public bool isEndingCompleted = false;

        // audio mix. AudioListener.volume = master; SFX scales every PlayOneShot;
        // Music is applied by SettingsPanelUI to the BGM AudioSource on Canvas / root BGM.
        public float masterVolume = 1f;
        public float musicVolume = 1f;
        public float sfxVolume = 1f;

        // talent state extras (cardsLeft / talentLevel / etc). LevelMechanics
        // currently derives most of these from level + chosen array, but the originals are
        // independently tracked. Default values match original starting state.
        public int talentCardsLeft = 20;
        public int talentLevel = 1;
        public int talentExtraPointBook = 0;

        // pickaxe DLC skins reservation (always 0 for now; satisfies save-format
        // parity with the original game in case the DLC ships later).
        public int[] pickaxeSkinsChosen = new int[14];

        // settings extras.
        public bool isTooltipAnimOn = true;

        public GameDataDTO()
        {
            // Mr.Rusty (index 0) is always crafted by default — mirrors AnvilManager.Awake state.
            pickaxesCrafted[0] = true;
        }
    }
}
