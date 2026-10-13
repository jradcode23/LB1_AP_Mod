namespace LB1_AP_Mod;

public class LevelHandler
{
    private static unsafe byte* LevelBaseAddress => *(byte**)(Mod.BaseAddress + 0x6CA594);
    private const int StoryTrueSuperHeroOffset = 0;
    private const int FreeplaySuperHeroOffset = 1;
    private const int StoryOffset = 2;
    private const int FreeplayOffset = 3;
    private const int OffsetMultiplier = 4;

    private static readonly int[] LevelMemoryOffsets = [
        0x09, // H1-1
        0x0C, // H1-2
        0x0F, // H1-3
        0x12, // H1-4
        0x15, // H1-5
        0x1E, // H2-1
        0x21, // H2-2
        0x24, // H2-3
        0x27, // H2-4
        0x2A, // H2-5
        0x33, // H3-1
        0x36, // H3-2
        0x39, // H3-3
        0x3C, // H3-4
        0x3F, // H3-5
        0x45, // V1-1
        0x48, // V1-2
        0x4B, // V1-3
        0x4E, // V1-4
        0x51, // V1-5
        0x53, // V2-1
        0x57, // V2-2
        0x5A, // V2-3
        0x5D, // V2-4
        0x60, // V2-5
        0x63, // V3-1
        0x66, // V3-2
        0x69, // V3-3
        0x6C, // V3-4
        0x6F, // V3-5
    ];

    // Helper Function to unlock a level
    public static unsafe void UnlockLevel(int itemID)
    {
        if (itemID < 0 || itemID > 30)
        {
            Mod.PrintToLog($"Cannot Unlock Level, itemID is out of index: {itemID}");
            return;
        }
        if (LevelBaseAddress == null)
        {
            Mod.PrintToLog("Level Base Address is null. Please report to a dev. Aborting.");
            return;
        }
        int offset = LevelMemoryOffsets[itemID];
        byte* storyMode = LevelBaseAddress + (offset * OffsetMultiplier) + StoryOffset;
        byte* freeplayMode = LevelBaseAddress + (offset * OffsetMultiplier) + FreeplayOffset;
        *storyMode = 1;
        *freeplayMode = 1;
    }

    // Helper Function to reset all levels to locked
    public static unsafe void ResetLevels()
    {
        foreach (int offset in LevelMemoryOffsets)
        {
            if (LevelBaseAddress == null)
            {
                Mod.PrintToLog("Level Base Address is null. Please report to a dev. Aborting.");
                return;
            }
            byte* storyMode = LevelBaseAddress + (offset * OffsetMultiplier) + StoryOffset;
            byte* freeplayMode = LevelBaseAddress + (offset * OffsetMultiplier) + FreeplayOffset;
            *storyMode = 0;
            *freeplayMode = 0;
        }
    }

    // Helper Function to unlock a True Super Hero
    public static unsafe void UnlockTrueSuperHero(int itemID)
    {
        if (itemID < 0 || itemID > 30)
        {
            Mod.PrintToLog($"Cannot Unlock True Super Hero, itemID is out of index: {itemID}");
            return;
        }
        if (LevelBaseAddress == null)
        {
            Mod.PrintToLog("Level Base Address is null. Please report to a dev. Aborting.");
            return;
        }
        int offset = LevelMemoryOffsets[itemID];
        byte* storySuperHero = LevelBaseAddress + (offset * OffsetMultiplier) + StoryTrueSuperHeroOffset;
        byte* freeplaySuperHero = LevelBaseAddress + (offset * OffsetMultiplier) + FreeplaySuperHeroOffset;
        *storySuperHero = 1;
        *freeplaySuperHero = 1;
    }

    // Helper Function to reset all True Super Heros to locked
    public static unsafe void ResetTrueSuperHeroes()
    {
        foreach (int offset in LevelMemoryOffsets)
        {
            if (LevelBaseAddress == null)
            {
                Mod.PrintToLog("Level Base Address is null. Please report to a dev. Aborting.");
                return;
            }
            byte* storySuperHero = LevelBaseAddress + (offset * OffsetMultiplier) + StoryTrueSuperHeroOffset;
            byte* freeplaySuperHero = LevelBaseAddress + (offset * OffsetMultiplier) + FreeplaySuperHeroOffset;
            *storySuperHero = 0;
            *freeplaySuperHero = 0;
        }
    }

    // Base Address and helper functions to handle minikit counts
    private static unsafe byte* MinikitCountBase => *(byte**)(Mod.BaseAddress + 0x5C59CC);
    private const int MinikitOffset = 0x77DD;

    public static unsafe void UpdateMinikitCounts(bool isResetToItems)
    {
        int minikitMultiplier = isResetToItems ? Mod.LB1_AP!.SlotDataInstance!.MinikitGrouping : 1;
        if (minikitMultiplier == -1)
        {
            Mod.PrintToLog("Minikit Multiplier is -1, can't reset minikit counts");
            return;
        }

        byte* minikitCountBase = MinikitCountBase;
        if (minikitCountBase == null)
        {
            Mod.PrintToLog("Minikit Count Base Address is null. Please report to a dev. Aborting.");
            return;
        }

        int itemIDStart = isResetToItems
            ? minikitMultiplier switch
            {
                <= 1 => 0,
                <= 2 => 30,
                <= 5 => 60,
                <= 10 => 90,
                _ => 0
            }
            : 0;
        int currentLevelID = ConvertLevelIDToArchiID();
        byte* inLevelCount1 = (byte*)(Mod.BaseAddress + 0x6CA424);
        byte* inLevelCount2 = (byte*)(Mod.BaseAddress + 0x6CA42C);

        for (int levelID = 0; levelID < LevelMemoryOffsets.Length; levelID++)
        {
            int minikitCount = isResetToItems
                ? Mod.LB1_AP!.CountItemsReceivedWithId(itemIDStart + levelID + Game.MinikitOffset) * minikitMultiplier
                : Mod.LB1_AP!.CountLocationsCheckedInRange(
                    levelID * 10 + Game.MinikitOffset,
                    levelID * 10 + 9 + Game.MinikitOffset);

            if (minikitCount > 10)
            {
                minikitCount = 10;
            }

            int offset = LevelMemoryOffsets[levelID];
            byte* minikitAddress = minikitCountBase + (offset * OffsetMultiplier) + MinikitOffset;
            *minikitAddress = (byte)minikitCount;

            if (levelID == currentLevelID)
            {
                *inLevelCount1 = (byte)minikitCount;
                *inLevelCount2 = (byte)minikitCount;
            }
        }
    }

    public static unsafe void ResetMinikitCount()
    {
        foreach (int offset in LevelMemoryOffsets)
        {
            if (MinikitCountBase == null)
            {
                Mod.PrintToLog("Minikit Count Base Address is null. Please report to a dev. Aborting.");
                return;
            }
            byte* minikitCount = MinikitCountBase + (offset * OffsetMultiplier) + MinikitOffset;
            *minikitCount = 0;
        }
        if (!Game.IsInHub())
        {
            byte* inLevelCount1 = (byte*)(Mod.BaseAddress + 0x6CA424);
            byte* inLevelCount2 = (byte*)(Mod.BaseAddress + 0x6CA42C);
            *inLevelCount1 = 0;
            *inLevelCount2 = 0;
        }
    }

    private static readonly int[] StatusScreenMapId = [
        0x16, // H1-1
        0x22, // H1-2
        0x2E, // H1-3
        0x35, // H1-4
        0x4D, // H1-5
        0x5F, // H2-1
        0x79, // H2-2
        0x6B, // H2-3
        0x8C, // H2-4
        0x94, // H2-5
        0xA4, // H3-1
        0xAA, // H3-2
        0xB0, // H3-3
        0xBE, // H3-4
        0xCA, // H3-5
        0x1C, // V1-1
        0x29, // V1-2
        0x3D, // V1-3
        0x45, // V1-4
        0x55, // V1-5
        0x85, // V2-1
        0x65, // V2-2
        0x7F, // V2-3
        0x72, // V2-4
        0x9C, // V2-5
        0xB6, // V3-1
        0xD6, // V3-2
        0xDC, // V3-3
        0xC4, // V3-4
        0xD0, // V3-5 
    ];

    // Helper Function to return Level complete value if player enters that map
    public static int CheckStatusMap(int mapID)
    {
        return Array.IndexOf(StatusScreenMapId, mapID);
    }

    private static readonly int[] LevelIDs = [
        0x03, // H1-1
        0x04, // H1-2
        0x05, // H1-3
        0x06, // H1-4
        0x06, // H1-5
        0x0A, // H2-1
        0x0B, // H2-2
        0x0C, // H2-3
        0x0D, // H2-4
        0x0E, // H2-5
        0x11, // H3-1
        0x12, // H3-2
        0x13, // H3-3
        0x14, // H3-4
        0x15, // H3-5
        0x17, // V1-1
        0x18, // V1-2
        0x19, // V1-3
        0x1A, // V1-4
        0x1B, // V1-5
        0x1C, // V2-1
        0x1D, // V2-2
        0x1E, // V2-3
        0x1F, // V2-4
        0x20, // V2-5
        0x21, // V3-1
        0x22, // V3-2
        0x23, // V3-3
        0x24, // V3-4
        0x25, // V3-5
    ];

    public static unsafe int ConvertLevelIDToArchiID()
    {
        byte* levelID = (byte*)(Mod.BaseAddress + 0x5629C4);
        if (levelID == null)
        {
            Mod.PrintToLog("Cannot Convert Level ID. Level ID is null");
            return -1;
        }
        return Array.IndexOf(LevelIDs, *levelID);
    }

    private static unsafe byte* HostageArray => *(byte**)(Mod.BaseAddress + 0x6B09B0);
    private static unsafe byte* HostageCount => (*(byte**)(Mod.BaseAddress + 0x6B096C)) + 0x516;
    private const int TotalHostageCount = 25;
    private const int HostageByteCount = 4;

    // Helper Function to unlock a hostage
    public static unsafe void UnlockHostage(int itemID)
    {
        if (HostageArray == null || HostageCount == null)
        {
            Mod.PrintToLog("Hostage Array or Hostage Count is null. Please report to a dev. Aborting.");
            return;
        }
        if (itemID < 0 || itemID > TotalHostageCount)
        {
            Mod.PrintToLog($"Cannot unlock hostage, itemID is out of index: {itemID}");
            return;
        }

        *(HostageArray + itemID / 8) |= (byte)(1 << (itemID % 8));
        *HostageCount += 1;
    }

    // Helper Function to reset hostages
    public static unsafe void ResetHostages()
    {
        if (HostageArray == null || HostageCount == null)
        {
            Mod.PrintToLog("Hostage Array or Hostage Count is null. Please report to a dev. Aborting.");
            return;
        }
        for (int i = 0; i < HostageByteCount; i++)
        {
            *(HostageArray + i) = 0;
        }
        *HostageCount = 0;
    }
}
