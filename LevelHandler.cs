namespace LB1_AP_Mod;

public class LevelHandler
{
    public static unsafe byte* LevelBaseAddress => *(byte**)(Mod.BaseAddress + 0x06C66A);

    // Bitmask for how the level unlock byte is structed.
    [Flags]
    public enum BitMask
    {
        None = 0,
        StoryUnlocked = 1 << 1,
        FreeplayUnlocked = 1 << 2,
    }

    // Enum representing each level and their offset from the base pointer
    public enum LevelData: ushort
    {
        // Misc
        ShopRoom = 0x05,
        TrophyRoomHero = 0x06,
        TrophyRoomEvil = 0x07,
        ExperimentRoomEvil = 0x08,
        MissionRoomHero = 0x09,
        MissionRoomEvil = 0x0A,

        // H1
        H1_1 = 0x12,
        H1_2 = 0x1E,
        H1_3 = 0x2B,
        H1_4 = 0x30,
        H1_5 = 0x48,

        // H2
        H2_1 = 0x5B,
        H2_2 = 0x74,
        H2_3 = 0x66, // diff order
        H2_4 = 0x87,
        H2_5 = 0x8E,

        // H3
        H3_1 = 0xA0,
        H3_2 = 0xA6,
        H3_3 = 0xAC,
        H3_4 = 0xB8,
        H3_5 = 0xC6,

        // V1
        V1_1 = 0x18,
        V1_2 = 0x24,
        V1_3 = 0x37,
        V1_4 = 0x3F,
        V1_5 = 0x50,

        // V2
        V2_1 = 0x61,
        V2_2 = 0x81, // diff order
        V2_3 = 0x7B,
        V2_4 = 0x6D, // diff order
        V2_5 = 0x96,

        // V3
        V3_1 = 0xB2,
        V3_2 = 0xD2,
        V3_3 = 0xD8,
        V3_4 = 0xC0,
        V3_5 = 0xCC,
    }

    // Used to fetch the level number, since in level activity is stored in the same addresses, we use this to make sure the right check is sent
    public static LevelData[] LevelUnlockOrder =
    [
        LevelData.H1_1,
        LevelData.H1_2,
        LevelData.H1_3,
        LevelData.H1_4,
        LevelData.H1_5,

        LevelData.H2_1,
        LevelData.H2_2,
        LevelData.H2_3,
        LevelData.H2_4,
        LevelData.H2_5,

        LevelData.H3_1,
        LevelData.H3_2,
        LevelData.H3_3,
        LevelData.H3_4,
        LevelData.H3_5,

        LevelData.V1_1,
        LevelData.V1_2,
        LevelData.V1_3,
        LevelData.V1_4,
        LevelData.V1_5,

        LevelData.V2_1,
        LevelData.V2_2,
        LevelData.V2_3,
        LevelData.V2_4,
        LevelData.V2_5,

        LevelData.V3_1,
        LevelData.V3_2,
        LevelData.V3_2,
        LevelData.V3_4,
        LevelData.V3_5,
    ];

    // Helper function to look up the applicable level based on the Archi ID
    public static LevelData ConvertIDToLeveData(int id)
    {
        LevelData level = LevelUnlockOrder[id];
        return level;
    }

    // Helper function to unlock a level
    public static unsafe void UnlockLevel(LevelData level)
    {
        byte* ptr = LevelBaseAddress + (ushort)level;
        if (ptr == null || LevelBaseAddress == null)
        {
            Mod.PrintToLog($"Can't Unlock Level, null pointer at 0x{(nuint)ptr:X}");
        }
        *ptr |= (byte)(BitMask.StoryUnlocked | BitMask.FreeplayUnlocked);
    }

    // Helper function to lock a level
    public static unsafe void LockLevel(LevelData level)
    {
        byte* ptr = LevelBaseAddress + (ushort)level;
        if (ptr == null || LevelBaseAddress == null)
        {
            Mod.PrintToLog($"Can't Lock Level, null pointer at 0x{(nuint)ptr:X}");
        }
        *ptr &= unchecked((byte)~(byte)(BitMask.StoryUnlocked | BitMask.FreeplayUnlocked));
    }

    // Helper function to unlock a Super Hero
    public static unsafe void UnlockSuperHero(LevelData level)
    {
        // Adjusting the address since SH address is before the rest of the in level activity
        byte* story = LevelBaseAddress + (ushort)level - 6;
        byte* freeplay = story + 1;
        if (story == null || freeplay == null || LevelBaseAddress == null)
        {
            Mod.PrintToLog($"Can't Unlock SH, null pointer at 0x{(nuint)story:X}");
        }
        *story = 1;
        *freeplay = 1;
    }

    // Helper function to reset all In Level Unlocks
    public static unsafe void ResetLevels()
    {
        for (int i = 0; i < LevelUnlockOrder.Length; i++)
        {
            byte* level = LevelBaseAddress + (ushort)LevelUnlockOrder[i];
            byte* story = level - 6;
            byte* freeplay = story + 1;
            *level = 0;
            *story = 0;
            *freeplay = 0;
        }
    }
}
