namespace LB1_AP_Mod;

public class LevelHandler
{
    private static unsafe byte* LevelBaseAddress => *(byte**)(Mod.BaseAddress + 0x6CA594);
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
        int offset = LevelMemoryOffsets[itemID];
        byte* storyMode = LevelBaseAddress + offset * OffsetMultiplier + StoryOffset;
        byte* freeplayMode = LevelBaseAddress + offset * OffsetMultiplier + FreeplayOffset;
        *storyMode = 1;
        *freeplayMode = 1;
    }

    // Helper Function to reset all levels to locked
    public static unsafe void ResetLevels()
    {
        foreach(int offset in LevelMemoryOffsets)
        {
            if (LevelBaseAddress == null)
            {
                Mod.PrintToLog("Level Base Address is null. Please report to a dev. Aborting.");
                return;
            }
            byte* storyMode = LevelBaseAddress + offset * OffsetMultiplier + StoryOffset;
            byte* freeplayMode = LevelBaseAddress + offset * OffsetMultiplier + FreeplayOffset;
            *storyMode = 0;
            *freeplayMode = 0;
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

}
