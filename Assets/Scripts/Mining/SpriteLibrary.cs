using UnityEngine;

namespace Rebuild.Mining
{
    /// <summary>central lookup for user-supplied gameplay sprites.
    /// <para>
    /// The user is authoring Rock / Ore / Artifact / Pickaxe art and dropping the PNGs into
    /// <c>Assets/Resources/Sprites/...</c>. Whenever a sprite is present at the expected
    /// path, the rest of the game (RockSpawner, ArtifactNodeUI, PickaxeNodeUI, etc.) picks it up
    /// automatically; when it is absent we fall back to <see cref="RockSpriteFactory"/>'s runtime
    /// generators so Play mode never blanks out.
    /// </para>
    /// <para>
    /// Expected paths (under <c>Assets/Resources/</c>):
    /// <list type="bullet">
    /// <item><b>Sprites/Rock.png</b> — base rock silhouette, tinted by ore color when no per-ore variant exists.</item>
    /// <item><b>Sprites/Ore/{Gold|Copper|Iron|Cobalt|Uranium|Ismium|Iridium|Painite}.png</b> — optional per-ore variants; presence disables the auto-tint.</item>
    /// <item><b>Sprites/Artifacts/{ArtifactDef.GetSpriteResourceName(id)}.png</b> — 14 unique artifact icons. Legacy enum filenames are still used as fallback.</item>
    /// <item><b>Sprites/Pickaxes/Pickaxe_{00..13}.png</b> — 14 pickaxe icons, file index matches <see cref="PickaxeDef"/> idx.</item>
    /// <item><b>Sprites/OreBars/{Gold|Copper|Iron|Cobalt|Uranium|Ismium|Iridium|Painite}.png</b> — persistent bar currency icons.</item>
    /// <item><b>Sprites/Talents/{TalentId}.png</b> — 25 talent icons (enum name).</item>
    /// <item><b>Sprites/SkillTree/{UpgradeId}.png</b> — 72 SkillTree node icons (enum name — e.g. <c>MoreTime.png</c>, <c>CobaltSpawn.png</c>).</item>
    /// </list>
    /// </para>
    /// Use <c>Tools &gt; Rebuild &gt; Verify Sprite Library</c> to print which slots are filled vs missing.</summary>
    public static class SpriteLibrary
    {
        // Cache one entry per slot so Resources.Load only runs the first time it's asked.
        // bool tracks "we already tried to load this one" so a null sprite doesn't get retried every spawn.
        static readonly Sprite[] s_oreSprites = new Sprite[8];
        static readonly bool[] s_oreLoaded = new bool[8];
        static Sprite s_rockBase;
        static bool s_rockBaseLoaded;
        static readonly Sprite[] s_artifactSprites = new Sprite[14];
        static readonly bool[] s_artifactLoaded = new bool[14];
        // 옵션 A reservation. Sprites/Rock/Artifact/{ArtifactId}.png 14 slot — 사용자
        // 후속 PNG 작업 시 자동 swap. PNG 부재 시 null 반환 → Rock.EnsureArtifactOverlay 가 옵션 B
        // (기존 s_artifactSprites) 로 fallback. 이 cache 는 옵션 A 와 옵션 B 의 시각 차이를 분리.
        static readonly Sprite[] s_artifactRockSprites = new Sprite[14];
        static readonly bool[] s_artifactRockLoaded = new bool[14];
        static readonly Sprite[] s_pickaxeSprites = new Sprite[14];
        static readonly bool[] s_pickaxeLoaded = new bool[14];
        // pickaxe skin variants. 13 pickaxe × 2 skin = 26 slot (idx 13 skin 없음).
        // Layout: [pickaxeIdx, skinVariant] flatten — skin 1 first (0..12), then skin 2 (13..25).
        static readonly Sprite[] s_pickaxeSkinSprites = new Sprite[26];
        static readonly bool[] s_pickaxeSkinLoaded = new bool[26];
        // expansion — additional categories the user is authoring.
        static readonly Sprite[] s_oreHudIcons = new Sprite[8];        // small icons for the top-left counter
        static readonly bool[] s_oreHudIconsLoaded = new bool[8];
        static readonly Sprite[] s_oreBarIcons = new Sprite[8];        // persistent bar currency icons
        static readonly bool[] s_oreBarIconsLoaded = new bool[8];
        static readonly Sprite[] s_talentIcons = new Sprite[TalentDef.Count];       // 25 talents ()
        static readonly bool[] s_talentIconsLoaded = new bool[TalentDef.Count];
        static readonly Sprite[] s_skillTreeIcons = new Sprite[UpgradeDef.Count];   // 72 SkillTree nodes ()
        static readonly bool[] s_skillTreeIconsLoaded = new bool[UpgradeDef.Count];
        static readonly Sprite[] s_achievementIcons = new Sprite[Rebuild.Stats.AchievementDef.Count];
        static readonly bool[] s_achievementIconsLoaded = new bool[Rebuild.Stats.AchievementDef.Count];
        static readonly Sprite[] s_bigRockStages = new Sprite[3];      // BigRock 3 destruction stages ()
        static readonly bool[] s_bigRockStagesLoaded = new bool[3];
        static readonly Sprite[] s_diamondSprites = new Sprite[3];     // cyan / violet / pink ending diamonds
        static readonly bool[] s_diamondSpritesLoaded = new bool[3];
        static Sprite s_campfireSprite;                                 // Camper visual
        static bool s_campfireLoaded;
        static Sprite s_flowerSprite;                                   // SpringSeason visual
        static bool s_flowerLoaded;
        static Sprite s_tileSprite;                                     // mining session floor tile (isometric)
        static bool s_tileLoaded;
        static Sprite s_cardFrameSprite;                                // Talent / SkillTree node card background
        static bool s_cardFrameLoaded;
        static Sprite s_sparkleSprite;                                  // level-up / unlock flair
        static bool s_sparkleLoaded;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetRuntimeCaches()
        {
            ClearArtifactSpriteCache();
        }

        public static void ClearArtifactSpriteCache()
        {
            System.Array.Clear(s_artifactSprites, 0, s_artifactSprites.Length);
            System.Array.Clear(s_artifactLoaded, 0, s_artifactLoaded.Length);
            System.Array.Clear(s_artifactRockSprites, 0, s_artifactRockSprites.Length);
            System.Array.Clear(s_artifactRockLoaded, 0, s_artifactRockLoaded.Length);
        }

        public readonly struct RockVisual
        {
            public readonly Sprite Sprite;
            public readonly Color TintColor;
            /// <summary>True when no per-ore PNG was found — caller should keep the ore tint on the Image
            /// so the base rock silhouette still reads as that ore color.</summary>
            public readonly bool ShouldTint;
            public RockVisual(Sprite s, Color c, bool tint) { Sprite = s; TintColor = c; ShouldTint = tint; }
        }

        /// <summary>Resolve a rock visual for the given ore. Tries the per-ore PNG first; falls back
        /// to the base Rock.png + ore tint; finally back to the runtime ellipse + tint.</summary>
        public static RockVisual GetRockVisual(OreType ore) => GetRockVisual(ore, RockTier.Normal);

        // Chunk/Full tier sprite cache (8 ore × 2 tier).
        static readonly Sprite[] s_chunkSprites = new Sprite[8];
        static readonly bool[] s_chunkLoaded = new bool[8];
        static readonly Sprite[] s_fullSprites = new Sprite[8];
        static readonly bool[] s_fullLoaded = new bool[8];
        // Broken tier sprite cache (8 ore × 1 변환). 50% HP 시점 swap.
        // 원본 RockMechanics.DamageRock 의 `currentBrokenRock.SetActive(true)` 매칭.
        static readonly Sprite[] s_brokenSprites = new Sprite[8];
        static readonly bool[] s_brokenLoaded = new bool[8];
        // 공유 broken rock fallback (per-ore PNG 없을 때).
        static Sprite s_brokenRockShared;
        static bool s_brokenRockSharedLoaded;

        /// <summary>tier 별 rock 외형. Chunk/Full PNG 없으면 Normal sprite 로 fallback,
        /// 그것도 없으면 base Rock.png + tint, 최종 runtime ellipse + tint.</summary>
        public static RockVisual GetRockVisual(OreType ore, RockTier tier)
        {
            int i = (int)ore;
            var oreColor = RockSpriteFactory.GetOreColor(ore);

            // Chunk/Full tier 시도.
            if (tier == RockTier.Chunk)
            {
                if (!s_chunkLoaded[i])
                {
                    s_chunkSprites[i] = Resources.Load<Sprite>($"Sprites/Ore/Chunk/{ore}");
                    s_chunkLoaded[i] = true;
                }
                if (s_chunkSprites[i] != null)
                    return new RockVisual(s_chunkSprites[i], Color.white, false);
            }
            else if (tier == RockTier.Full)
            {
                if (!s_fullLoaded[i])
                {
                    s_fullSprites[i] = Resources.Load<Sprite>($"Sprites/Ore/Full/{ore}");
                    s_fullLoaded[i] = true;
                }
                if (s_fullSprites[i] != null)
                    return new RockVisual(s_fullSprites[i], Color.white, false);
            }

            // Normal tier (또는 Chunk/Full PNG 부재 시 fallback).
            if (!s_oreLoaded[i])
            {
                s_oreSprites[i] = Resources.Load<Sprite>($"Sprites/Ore/{ore}");
                s_oreLoaded[i] = true;
            }
            if (s_oreSprites[i] != null)
                return new RockVisual(s_oreSprites[i], Color.white, false);
            if (!s_rockBaseLoaded)
            {
                s_rockBase = Resources.Load<Sprite>("Sprites/Rock");
                s_rockBaseLoaded = true;
            }
            if (s_rockBase != null)
                return new RockVisual(s_rockBase, oreColor, true);
            // Final fallback — runtime ellipse with ore tint (the baseline).
            return new RockVisual(RockSpriteFactory.GetRockSprite(), oreColor, true);
        }

        /// <summary>broken-stage visual. 원본 RockMechanics.DamageRock 의 50% HP 시점
        /// <c>currentBrokenRock</c> SetActive 대응. fallback 체인:
        /// (1) per-ore <c>Sprites/Ore/Broken/{Ore}.png</c> →
        /// (2) 공유 <c>Sprites/Rock/RockBroken.png</c> + ore tint →
        /// (3) base <c>Sprites/Rock.png</c> 어둡게 (×0.65) + ore tint →
        /// (4) runtime ellipse + ore tint.
        /// 어느 단계든 sprite 가 있으면 즉시 반환 (다음 단계 시도 안 함).</summary>
        public static RockVisual GetBrokenRockVisual(OreType ore)
        {
            int i = (int)ore;
            var oreColor = RockSpriteFactory.GetOreColor(ore);

            // (1) per-ore broken PNG.
            if (!s_brokenLoaded[i])
            {
                s_brokenSprites[i] = Resources.Load<Sprite>($"Sprites/Ore/Broken/{ore}");
                s_brokenLoaded[i] = true;
            }
            if (s_brokenSprites[i] != null)
                return new RockVisual(s_brokenSprites[i], Color.white, false);

            // (2) 공유 broken rock.
            if (!s_brokenRockSharedLoaded)
            {
                s_brokenRockShared = Resources.Load<Sprite>("Sprites/Rock/RockBroken");
                s_brokenRockSharedLoaded = true;
            }
            if (s_brokenRockShared != null)
                return new RockVisual(s_brokenRockShared, oreColor, true);

            // (3) base rock + dimmer ore tint (시각 hint 만).
            if (!s_rockBaseLoaded)
            {
                s_rockBase = Resources.Load<Sprite>("Sprites/Rock");
                s_rockBaseLoaded = true;
            }
            var dimmed = new Color(oreColor.r * 0.65f, oreColor.g * 0.65f, oreColor.b * 0.65f, oreColor.a);
            if (s_rockBase != null)
                return new RockVisual(s_rockBase, dimmed, true);

            // (4) runtime ellipse + dimmed tint.
            return new RockVisual(RockSpriteFactory.GetRockSprite(), dimmed, true);
        }

        /// <summary>Resolve an artifact icon. Falls back to the rock sprite + artifact tint when
        /// no PNG exists for the id (matches the placeholder visuals).</summary>
        public static Sprite GetArtifactSprite(ArtifactId id)
        {
            int i = (int)id;
            if (i < 0 || i >= s_artifactSprites.Length) return RockSpriteFactory.GetRockSprite();
            if (!s_artifactLoaded[i])
            {
                var resourceName = ArtifactDef.GetSpriteResourceName(id);
                s_artifactSprites[i] = Resources.Load<Sprite>($"Sprites/Artifacts/{resourceName}");
                if (s_artifactSprites[i] == null)
                    s_artifactSprites[i] = Resources.Load<Sprite>($"Sprites/Artifacts/{id}");
                s_artifactLoaded[i] = true;
            }
            return s_artifactSprites[i] != null ? s_artifactSprites[i] : RockSpriteFactory.GetRockSprite();
        }

        /// <summary>.E — option A path. rock-shaped artifact PNG (Sprites/Rock/Artifact/{id}.png).
        /// PNG 가 없으면 null 반환 — Rock.EnsureArtifactOverlay 가 option B (작은 icon overlay) 로 fallback.
        /// 사용자가 후속 PNG 작업하면 자동 swap. 현재는 null 만 반환 (PNG 미배치).</summary>
        public static Sprite GetArtifactRockSprite(ArtifactId id)
        {
            int i = (int)id;
            if (i < 0 || i >= s_artifactRockSprites.Length) return null;
            if (!s_artifactRockLoaded[i])
            {
                var resourceName = ArtifactDef.GetSpriteResourceName(id);
                s_artifactRockSprites[i] = Resources.Load<Sprite>($"Sprites/Rock/Artifact/{resourceName}");
                if (s_artifactRockSprites[i] == null)
                    s_artifactRockSprites[i] = Resources.Load<Sprite>($"Sprites/Rock/Artifact/{id}");
                s_artifactRockLoaded[i] = true;
            }
            return s_artifactRockSprites[i];
        }

        /// <summary>Resolve a pickaxe icon. Falls back to the rock sprite when no PNG exists (the
        /// placeholder uses gray→gold gradient tint via PickaxeNodeUI).
        /// <para>overload: skin=0 returns base sprite (이 메서드 동일), skin=1/2 시도 후
        /// 누락이면 base sprite 로 fallback. idx 13 (마지막 곡괭이) 은 skin 변형 없음 — skin
        /// 인자 무시.</para></summary>
        public static Sprite GetPickaxeSprite(int idx) => GetPickaxeSprite(idx, 0);

        public static Sprite GetPickaxeSprite(int idx, int skin)
        {
            if (idx < 0 || idx >= s_pickaxeSprites.Length) return RockSpriteFactory.GetRockSprite();
            // skin 1 or 2 시도 (idx 13 은 skin 변형 없음, skip).
            if (skin > 0 && idx < 13)
            {
                int slot = idx + (skin - 1) * 13; // skin 1 = 0..12, skin 2 = 13..25
                if (slot >= 0 && slot < s_pickaxeSkinSprites.Length)
                {
                    if (!s_pickaxeSkinLoaded[slot])
                    {
                        s_pickaxeSkinSprites[slot] = Resources.Load<Sprite>($"Sprites/Pickaxes/Pickaxe_{idx:D2}_Skin{skin}");
                        s_pickaxeSkinLoaded[slot] = true;
                    }
                    if (s_pickaxeSkinSprites[slot] != null) return s_pickaxeSkinSprites[slot];
                    // skin PNG 누락 → base sprite 로 fallback (시각 차이 없음).
                }
            }
            if (!s_pickaxeLoaded[idx])
            {
                s_pickaxeSprites[idx] = Resources.Load<Sprite>($"Sprites/Pickaxes/Pickaxe_{idx:D2}");
                s_pickaxeLoaded[idx] = true;
            }
            return s_pickaxeSprites[idx] != null ? s_pickaxeSprites[idx] : RockSpriteFactory.GetRockSprite();
        }

        /// <summary>HUD-side ore icon (top-left counter / results panel). Distinct from
        /// <see cref="GetRockVisual"/> which returns the rock-with-ore visual used at mining time.</summary>
        public static Sprite GetOreHudIcon(OreType ore)
        {
            int i = (int)ore;
            if (i < 0 || i >= s_oreHudIcons.Length) return null;
            if (!s_oreHudIconsLoaded[i])
            {
                s_oreHudIcons[i] = Resources.Load<Sprite>($"Sprites/OreHud/{ore}");
                s_oreHudIconsLoaded[i] = true;
            }
            return s_oreHudIcons[i];
        }

        /// <summary>Persistent bar icon used by ResourcePanel, SkillTree currency, Anvil costs,
        /// and TheMine bar popups. Raw mining-session ore HUD stays on <see cref="GetOreHudIcon"/>.</summary>
        public static Sprite GetOreBarIcon(OreType ore)
        {
            int i = (int)ore;
            if (i < 0 || i >= s_oreBarIcons.Length) return null;
            if (!s_oreBarIconsLoaded[i])
            {
                s_oreBarIcons[i] = Resources.Load<Sprite>($"Sprites/OreBars/{ore}");
                s_oreBarIconsLoaded[i] = true;
            }
            return s_oreBarIcons[i];
        }

        public static Sprite GetTalentIcon(TalentId id)
        {
            int i = (int)id;
            if (i < 0 || i >= s_talentIcons.Length) return null;
            if (!s_talentIconsLoaded[i])
            {
                s_talentIcons[i] = Resources.Load<Sprite>($"Sprites/Talents/{id}");
                s_talentIconsLoaded[i] = true;
            }
            return s_talentIcons[i];
        }

        /// <summary>per-node SkillTree icon. File name matches the
        /// <see cref="UpgradeId"/> enum name (e.g. <c>Sprites/SkillTree/MoreTime.png</c>,
        /// <c>Sprites/SkillTree/CobaltSpawn.png</c>). Returns null when no PNG exists — caller
        /// (<see cref="TreeNodeUI.ApplyStaticInfo"/>) falls back to the ore-tinted rock silhouette.</summary>
        public static Sprite GetSkillTreeIcon(UpgradeId id)
        {
            int i = (int)id;
            if (i < 0 || i >= s_skillTreeIcons.Length) return null;
            if (!s_skillTreeIconsLoaded[i])
            {
                s_skillTreeIcons[i] = Resources.Load<Sprite>($"Sprites/SkillTree/{id}");
                s_skillTreeIconsLoaded[i] = true;
            }
            if (s_skillTreeIcons[i] != null) return s_skillTreeIcons[i];

            // v2 — ore-progression nodes (Spawn/Chunk/Full of Copper/Iron/Cobalt/
            // Uranium/Ismium/Iridium/Painite) automatically reuse the existing Sprites/Ore/{ore}.png
            // when no dedicated SkillTree icon exists. The user has already authored the 8 ore
            // sprites — no need to redraw them for 21 progression nodes.
            var ore = TryGetMatchingOre(id);
            if (ore.HasValue)
            {
                var visual = GetRockVisual(ore.Value);
                if (visual.Sprite != null && !visual.ShouldTint)
                    return visual.Sprite;
            }
            return null;
        }

        /// <summary>v2 — map an ore-progression SkillTree node to its matching ore.
        /// Returns null for non-progression nodes (e.g. MoreTime, BeamChance).</summary>
        static OreType? TryGetMatchingOre(UpgradeId id)
        {
            switch (id)
            {
                case UpgradeId.CopperSpawn:  case UpgradeId.CopperChunk:  case UpgradeId.CopperFull:  return OreType.Copper;
                case UpgradeId.IronSpawn:    case UpgradeId.IronChunk:    case UpgradeId.IronFull:    return OreType.Iron;
                case UpgradeId.CobaltSpawn:  case UpgradeId.CobaltChunk:  case UpgradeId.CobaltFull:  return OreType.Cobalt;
                case UpgradeId.UraniumSpawn: case UpgradeId.UraniumChunk: case UpgradeId.UraniumFull: return OreType.Uranium;
                case UpgradeId.IsmiumSpawn:  case UpgradeId.IsmiumChunk:  case UpgradeId.IsmiumFull:  return OreType.Ismium;
                case UpgradeId.IridiumSpawn: case UpgradeId.IridiumChunk: case UpgradeId.IridiumFull: return OreType.Iridium;
                case UpgradeId.PainiteSpawn: case UpgradeId.PainiteChunk: case UpgradeId.PainiteFull: return OreType.Painite;
                // Boost nodes (GoldBoost / CopperBoost / IronBoost) also map to ore.
                case UpgradeId.GoldBoost:    return OreType.Gold;
                case UpgradeId.CopperBoost:  return OreType.Copper;
                case UpgradeId.IronBoost:    return OreType.Iron;
                default: return null;
            }
        }

        /// <summary>BigRock 3 destruction stages — endgame. Stage 0 = full HP, 1 = ~1/3
        /// damaged, 2 = ~2/3 damaged. EndingFrameUI swaps the visual as HP drops.</summary>
        public static Sprite GetBigRockStage(int stage)
        {
            if (stage < 0 || stage >= s_bigRockStages.Length) return null;
            if (!s_bigRockStagesLoaded[stage])
            {
                s_bigRockStages[stage] = Resources.Load<Sprite>($"Sprites/Ending/BigRock_{stage + 1}");
                s_bigRockStagesLoaded[stage] = true;
            }
            return s_bigRockStages[stage];
        }

        /// <summary>Ending reward diamonds — idx 0=cyan, 1=violet, 2=pink ().</summary>
        public static Sprite GetDiamondSprite(int idx)
        {
            if (idx < 0 || idx >= s_diamondSprites.Length) return null;
            if (!s_diamondSpritesLoaded[idx])
            {
                string name = idx switch { 0 => "Diamond_Cyan", 1 => "Diamond_Violet", 2 => "Diamond_Pink", _ => null };
                if (name != null) s_diamondSprites[idx] = Resources.Load<Sprite>($"Sprites/Ending/{name}");
                s_diamondSpritesLoaded[idx] = true;
            }
            return s_diamondSprites[idx];
        }

        public static Sprite GetCampfireSprite()
        {
            if (!s_campfireLoaded)
            {
                s_campfireSprite = Resources.Load<Sprite>("Sprites/World/Campfire");
                s_campfireLoaded = true;
            }
            return s_campfireSprite;
        }

        public static Sprite GetFlowerSprite()
        {
            if (!s_flowerLoaded)
            {
                s_flowerSprite = Resources.Load<Sprite>("Sprites/World/Flower");
                s_flowerLoaded = true;
            }
            return s_flowerSprite;
        }

        public static Sprite GetTileSprite()
        {
            if (!s_tileLoaded)
            {
                s_tileSprite = Resources.Load<Sprite>("Sprites/World/Tile");
                s_tileLoaded = true;
            }
            return s_tileSprite;
        }

        public static Sprite GetCardFrameSprite()
        {
            if (!s_cardFrameLoaded)
            {
                s_cardFrameSprite = Resources.Load<Sprite>("Sprites/UI/CardFrame");
                s_cardFrameLoaded = true;
            }
            return s_cardFrameSprite;
        }

        public static Sprite GetSparkleSprite()
        {
            if (!s_sparkleLoaded)
            {
                s_sparkleSprite = Resources.Load<Sprite>("Sprites/UI/Sparkle");
                s_sparkleLoaded = true;
            }
            return s_sparkleSprite;
        }

        public static Sprite GetAchievementSprite(Rebuild.Stats.AchievementId id)
        {
            int i = (int)id;
            var fallback = GetSparkleSprite();
            if (fallback == null) fallback = RockSpriteFactory.GetRockSprite();
            if (i < 0 || i >= s_achievementIcons.Length) return fallback;
            if (!s_achievementIconsLoaded[i])
            {
                s_achievementIcons[i] = Resources.Load<Sprite>($"Sprites/Achievements/{id}");
                s_achievementIconsLoaded[i] = true;
            }
            return s_achievementIcons[i] != null ? s_achievementIcons[i] : fallback;
        }

        // TheMine + Artifacts 탭 시각 centerpiece (3 sprite).
        static Sprite s_mineEntrance;  static bool s_mineEntranceLoaded;
        static Sprite s_mineCart;      static bool s_mineCartLoaded;
        static Sprite s_displayStand;  static bool s_displayStandLoaded;

        // Chest rock variant (사용자 PNG 2장).
        static Sprite s_chestRock;  static bool s_chestRockLoaded;
        static Sprite s_goldenChestRock;  static bool s_goldenChestRockLoaded;

        /// <summary>chests_chosen talent rock variant. PNG 없으면 null 반환 (caller fallback).</summary>
        public static Sprite GetChestRockSprite()
        {
            if (!s_chestRockLoaded)
            {
                s_chestRock = Resources.Load<Sprite>("Sprites/Rock/Chest");
                s_chestRockLoaded = true;
            }
            return s_chestRock;
        }

        /// <summary>goldenChests_chosen talent rock variant. PNG 없으면 null 반환.</summary>
        public static Sprite GetGoldenChestRockSprite()
        {
            if (!s_goldenChestRockLoaded)
            {
                s_goldenChestRock = Resources.Load<Sprite>("Sprites/Rock/GoldenChest");
                s_goldenChestRockLoaded = true;
            }
            return s_goldenChestRock;
        }

        // Typography sprite (15 PNG). 고정 문구 (타이틀 / 버튼 / 헤더) PNG 교체.
        // 동적 텍스트 (숫자/카드 설명) 는 폰트 유지. PNG 없으면 caller 가 TMP_Text fallback.
        static readonly System.Collections.Generic.Dictionary<string, Sprite> s_typographyCache
            = new System.Collections.Generic.Dictionary<string, Sprite>();
        static readonly System.Collections.Generic.HashSet<string> s_typographyMissCache
            = new System.Collections.Generic.HashSet<string>();

        /// <summary>Typography PNG (Sprites/UI/Typography/{name}.png).
        /// caller 가 Image 컴포넌트 의 sprite 로 박을 때 사용. null 반환 시 TMP_Text fallback 권장.
        /// 예: Title_Atoria, Btn_Start, Tab_Skills, Header_Welcome.</summary>
        public static Sprite GetTypography(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            if (s_typographyCache.TryGetValue(name, out var sp)) return sp;
            if (s_typographyMissCache.Contains(name)) return null;
            sp = Resources.Load<Sprite>($"Sprites/UI/Typography/{name}");
            if (sp != null) s_typographyCache[name] = sp;
            else s_typographyMissCache.Add(name);
            return sp;
        }

        /// <summary>TheMine 탭 중앙 광산 입구 sprite (신라 분묘 입구).</summary>
        public static Sprite GetMineEntranceSprite()
        {
            if (!s_mineEntranceLoaded)
            {
                s_mineEntrance = Resources.Load<Sprite>("Sprites/World/MineEntrance");
                s_mineEntranceLoaded = true;
            }
            return s_mineEntrance;
        }

        /// <summary>TheMine 탭 입구 앞 광물 카트 sprite.</summary>
        public static Sprite GetMineCartSprite()
        {
            if (!s_mineCartLoaded)
            {
                s_mineCart = Resources.Load<Sprite>("Sprites/World/MineCart");
                s_mineCartLoaded = true;
            }
            return s_mineCart;
        }

        /// <summary>Artifacts 탭 박물관 진열대 sprite.</summary>
        public static Sprite GetDisplayStandSprite()
        {
            if (!s_displayStandLoaded)
            {
                s_displayStand = Resources.Load<Sprite>("Sprites/UI/DisplayStand");
                s_displayStandLoaded = true;
            }
            return s_displayStand;
        }

        /// <summary>Editor utility — clears the cache so a re-import without a domain reload still
        /// sees the new sprites. Not normally needed at runtime.</summary>
        public static void InvalidateCache()
        {
            for (int i = 0; i < s_oreSprites.Length; i++) { s_oreSprites[i] = null; s_oreLoaded[i] = false; }
            for (int i = 0; i < s_chunkSprites.Length; i++) { s_chunkSprites[i] = null; s_chunkLoaded[i] = false; }
            for (int i = 0; i < s_fullSprites.Length; i++) { s_fullSprites[i] = null; s_fullLoaded[i] = false; }
            for (int i = 0; i < s_artifactSprites.Length; i++) { s_artifactSprites[i] = null; s_artifactLoaded[i] = false; }
            for (int i = 0; i < s_artifactRockSprites.Length; i++) { s_artifactRockSprites[i] = null; s_artifactRockLoaded[i] = false; }
            for (int i = 0; i < s_pickaxeSprites.Length; i++) { s_pickaxeSprites[i] = null; s_pickaxeLoaded[i] = false; }
            for (int i = 0; i < s_oreHudIcons.Length; i++) { s_oreHudIcons[i] = null; s_oreHudIconsLoaded[i] = false; }
            for (int i = 0; i < s_oreBarIcons.Length; i++) { s_oreBarIcons[i] = null; s_oreBarIconsLoaded[i] = false; }
            for (int i = 0; i < s_talentIcons.Length; i++) { s_talentIcons[i] = null; s_talentIconsLoaded[i] = false; }
            for (int i = 0; i < s_skillTreeIcons.Length; i++) { s_skillTreeIcons[i] = null; s_skillTreeIconsLoaded[i] = false; }
            for (int i = 0; i < s_achievementIcons.Length; i++) { s_achievementIcons[i] = null; s_achievementIconsLoaded[i] = false; }
            for (int i = 0; i < s_bigRockStages.Length; i++) { s_bigRockStages[i] = null; s_bigRockStagesLoaded[i] = false; }
            for (int i = 0; i < s_diamondSprites.Length; i++) { s_diamondSprites[i] = null; s_diamondSpritesLoaded[i] = false; }
            s_rockBase = null; s_rockBaseLoaded = false;
            s_campfireSprite = null; s_campfireLoaded = false;
            s_flowerSprite = null; s_flowerLoaded = false;
            s_tileSprite = null; s_tileLoaded = false;
            s_cardFrameSprite = null; s_cardFrameLoaded = false;
            s_sparkleSprite = null; s_sparkleLoaded = false;
            s_mineEntrance = null; s_mineEntranceLoaded = false;
            s_mineCart = null; s_mineCartLoaded = false;
            s_displayStand = null; s_displayStandLoaded = false;
            s_typographyCache.Clear();
            s_typographyMissCache.Clear();
            s_chestRock = null; s_chestRockLoaded = false;
            s_goldenChestRock = null; s_goldenChestRockLoaded = false;
        }
    }
}
