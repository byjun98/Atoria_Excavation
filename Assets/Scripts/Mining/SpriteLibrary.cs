using UnityEngine;

namespace Rebuild.Mining
{





















    public static class SpriteLibrary
    {


        static readonly Sprite[] s_oreSprites = new Sprite[8];
        static readonly bool[] s_oreLoaded = new bool[8];
        static Sprite s_rockBase;
        static bool s_rockBaseLoaded;
        static readonly Sprite[] s_artifactSprites = new Sprite[14];
        static readonly bool[] s_artifactLoaded = new bool[14];



        static readonly Sprite[] s_artifactRockSprites = new Sprite[14];
        static readonly bool[] s_artifactRockLoaded = new bool[14];
        static readonly Sprite[] s_pickaxeSprites = new Sprite[14];
        static readonly bool[] s_pickaxeLoaded = new bool[14];


        static readonly Sprite[] s_pickaxeSkinSprites = new Sprite[26];
        static readonly bool[] s_pickaxeSkinLoaded = new bool[26];

        static readonly Sprite[] s_oreHudIcons = new Sprite[8];
        static readonly bool[] s_oreHudIconsLoaded = new bool[8];
        static readonly Sprite[] s_oreBarIcons = new Sprite[8];
        static readonly bool[] s_oreBarIconsLoaded = new bool[8];
        static readonly Sprite[] s_talentIcons = new Sprite[TalentDef.Count];
        static readonly bool[] s_talentIconsLoaded = new bool[TalentDef.Count];
        static readonly Sprite[] s_skillTreeIcons = new Sprite[UpgradeDef.Count];
        static readonly bool[] s_skillTreeIconsLoaded = new bool[UpgradeDef.Count];
        static readonly Sprite[] s_achievementIcons = new Sprite[Rebuild.Stats.AchievementDef.Count];
        static readonly bool[] s_achievementIconsLoaded = new bool[Rebuild.Stats.AchievementDef.Count];
        static readonly Sprite[] s_bigRockStages = new Sprite[3];
        static readonly bool[] s_bigRockStagesLoaded = new bool[3];
        static readonly Sprite[] s_diamondSprites = new Sprite[3];
        static readonly bool[] s_diamondSpritesLoaded = new bool[3];
        static Sprite s_campfireSprite;
        static bool s_campfireLoaded;
        static Sprite s_flowerSprite;
        static bool s_flowerLoaded;
        static Sprite s_tileSprite;
        static bool s_tileLoaded;
        static Sprite s_cardFrameSprite;
        static bool s_cardFrameLoaded;
        static Sprite s_sparkleSprite;
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


            public readonly bool ShouldTint;
            public RockVisual(Sprite s, Color c, bool tint) { Sprite = s; TintColor = c; ShouldTint = tint; }
        }



        public static RockVisual GetRockVisual(OreType ore) => GetRockVisual(ore, RockTier.Normal);


        static readonly Sprite[] s_chunkSprites = new Sprite[8];
        static readonly bool[] s_chunkLoaded = new bool[8];
        static readonly Sprite[] s_fullSprites = new Sprite[8];
        static readonly bool[] s_fullLoaded = new bool[8];


        static readonly Sprite[] s_brokenSprites = new Sprite[8];
        static readonly bool[] s_brokenLoaded = new bool[8];

        static Sprite s_brokenRockShared;
        static bool s_brokenRockSharedLoaded;



        public static RockVisual GetRockVisual(OreType ore, RockTier tier)
        {
            int i = (int)ore;
            var oreColor = RockSpriteFactory.GetOreColor(ore);


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

            return new RockVisual(RockSpriteFactory.GetRockSprite(), oreColor, true);
        }








        public static RockVisual GetBrokenRockVisual(OreType ore)
        {
            int i = (int)ore;
            var oreColor = RockSpriteFactory.GetOreColor(ore);


            if (!s_brokenLoaded[i])
            {
                s_brokenSprites[i] = Resources.Load<Sprite>($"Sprites/Ore/Broken/{ore}");
                s_brokenLoaded[i] = true;
            }
            if (s_brokenSprites[i] != null)
                return new RockVisual(s_brokenSprites[i], Color.white, false);


            if (!s_brokenRockSharedLoaded)
            {
                s_brokenRockShared = Resources.Load<Sprite>("Sprites/Rock/RockBroken");
                s_brokenRockSharedLoaded = true;
            }
            if (s_brokenRockShared != null)
                return new RockVisual(s_brokenRockShared, oreColor, true);


            if (!s_rockBaseLoaded)
            {
                s_rockBase = Resources.Load<Sprite>("Sprites/Rock");
                s_rockBaseLoaded = true;
            }
            var dimmed = new Color(oreColor.r * 0.65f, oreColor.g * 0.65f, oreColor.b * 0.65f, oreColor.a);
            if (s_rockBase != null)
                return new RockVisual(s_rockBase, dimmed, true);


            return new RockVisual(RockSpriteFactory.GetRockSprite(), dimmed, true);
        }



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






        public static Sprite GetPickaxeSprite(int idx) => GetPickaxeSprite(idx, 0);

        public static Sprite GetPickaxeSprite(int idx, int skin)
        {
            if (idx < 0 || idx >= s_pickaxeSprites.Length) return RockSpriteFactory.GetRockSprite();

            if (skin > 0 && idx < 13)
            {
                int slot = idx + (skin - 1) * 13;
                if (slot >= 0 && slot < s_pickaxeSkinSprites.Length)
                {
                    if (!s_pickaxeSkinLoaded[slot])
                    {
                        s_pickaxeSkinSprites[slot] = Resources.Load<Sprite>($"Sprites/Pickaxes/Pickaxe_{idx:D2}_Skin{skin}");
                        s_pickaxeSkinLoaded[slot] = true;
                    }
                    if (s_pickaxeSkinSprites[slot] != null) return s_pickaxeSkinSprites[slot];

                }
            }
            if (!s_pickaxeLoaded[idx])
            {
                s_pickaxeSprites[idx] = Resources.Load<Sprite>($"Sprites/Pickaxes/Pickaxe_{idx:D2}");
                s_pickaxeLoaded[idx] = true;
            }
            return s_pickaxeSprites[idx] != null ? s_pickaxeSprites[idx] : RockSpriteFactory.GetRockSprite();
        }



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





            var ore = TryGetMatchingOre(id);
            if (ore.HasValue)
            {
                var visual = GetRockVisual(ore.Value);
                if (visual.Sprite != null && !visual.ShouldTint)
                    return visual.Sprite;
            }
            return null;
        }



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

                case UpgradeId.GoldBoost:    return OreType.Gold;
                case UpgradeId.CopperBoost:  return OreType.Copper;
                case UpgradeId.IronBoost:    return OreType.Iron;
                default: return null;
            }
        }



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


        static Sprite s_mineEntrance;  static bool s_mineEntranceLoaded;
        static Sprite s_mineCart;      static bool s_mineCartLoaded;
        static Sprite s_displayStand;  static bool s_displayStandLoaded;


        static Sprite s_chestRock;  static bool s_chestRockLoaded;
        static Sprite s_goldenChestRock;  static bool s_goldenChestRockLoaded;


        public static Sprite GetChestRockSprite()
        {
            if (!s_chestRockLoaded)
            {
                s_chestRock = Resources.Load<Sprite>("Sprites/Rock/Chest");
                s_chestRockLoaded = true;
            }
            return s_chestRock;
        }


        public static Sprite GetGoldenChestRockSprite()
        {
            if (!s_goldenChestRockLoaded)
            {
                s_goldenChestRock = Resources.Load<Sprite>("Sprites/Rock/GoldenChest");
                s_goldenChestRockLoaded = true;
            }
            return s_goldenChestRock;
        }



        static readonly System.Collections.Generic.Dictionary<string, Sprite> s_typographyCache
            = new System.Collections.Generic.Dictionary<string, Sprite>();
        static readonly System.Collections.Generic.HashSet<string> s_typographyMissCache
            = new System.Collections.Generic.HashSet<string>();




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


        public static Sprite GetMineEntranceSprite()
        {
            if (!s_mineEntranceLoaded)
            {
                s_mineEntrance = Resources.Load<Sprite>("Sprites/World/MineEntrance");
                s_mineEntranceLoaded = true;
            }
            return s_mineEntrance;
        }


        public static Sprite GetMineCartSprite()
        {
            if (!s_mineCartLoaded)
            {
                s_mineCart = Resources.Load<Sprite>("Sprites/World/MineCart");
                s_mineCartLoaded = true;
            }
            return s_mineCart;
        }


        public static Sprite GetDisplayStandSprite()
        {
            if (!s_displayStandLoaded)
            {
                s_displayStand = Resources.Load<Sprite>("Sprites/UI/DisplayStand");
                s_displayStandLoaded = true;
            }
            return s_displayStand;
        }



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
