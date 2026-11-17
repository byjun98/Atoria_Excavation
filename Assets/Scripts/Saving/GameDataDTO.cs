using System;

namespace Rebuild.Saving
{







    [Serializable]
    public sealed class GameDataDTO
    {





        public int schemaVersion = 5;
        public long lastSavedUnix;


        public long[] oreCounts = new long[8];
        public int[] goldRingTenths;


        public int level = 1;
        public int currentXp = 0;
        public int talentPoints = 0;
        public int nextTalentPrice = 1;
        public bool[] talentsChosen = new bool[20];





        public int[] skillTreeLevels = new int[Rebuild.Mining.UpgradeDef.Count];




        public int currentPickaxeIdx = 0;
        public bool[] pickaxesCrafted = new bool[14];


        public int mineSpeedLevel = 0;
        public int mineMaterialLevel = 0;


        public bool isTheMineUnlocked = false;




        public bool[] artifactsFound = new bool[14];
        public int artifactMissStreak;






        public long[] stats = new long[47];
        public bool[] achievementsUnlocked = new bool[22];
        public bool[] achievementsNotified = new bool[22];
        public bool tutorialSeen = false;
        public bool[] tutorialPressedOk = new bool[6];




        public bool isEndingCompleted = false;



        public float masterVolume = 1f;
        public float musicVolume = 1f;
        public float sfxVolume = 1f;




        public int talentCardsLeft = 20;
        public int talentLevel = 1;
        public int talentExtraPointBook = 0;



        public int[] pickaxeSkinsChosen = new int[14];


        public bool isTooltipAnimOn = true;

        public GameDataDTO()
        {

            pickaxesCrafted[0] = true;
        }
    }
}
