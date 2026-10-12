namespace LB1_AP_Mod;

public class CharacterHandler
{
    private static readonly Dictionary<int, int> CharacterOffsets = new()
    {
        // {0, 0x00}, // Batman
        // {1, 0x01}, // Robin
        {2, 0x18}, // Bruce Wayne
        {3, 0x13}, // Alfred
        {4, 0x02}, // Batgirl
        {5, 0x11}, // Nightwing
        {6, 0x1B}, // Commissioner Gordon
        {7, 0x40}, // Police Officer
        {8, 0xCB}, // Fishmonger
        {9, 0x32}, // Military Policeman
        {10, 0x30}, // Security Guard
        {11, 0x42}, // Swat
        {12, 0x2B}, // Scientist
        {13, 0x2D}, // Sailor
        {14, 0x43}, // Police Marksman
        {15, 0x0F}, // Clayface
        {16, 0x16}, // Mr. Freeze
        {17, 0x15}, // Poison Ivy
        {18, 0x07}, // Two-Face
        {19, 0x0D}, // The Riddler
        {20, 0x19}, // Bane
        {21, 0x04}, // Catwoman
        {22, 0x05}, // Catwoman (Classic)
        {23, 0x0E}, // Killer Croc
        {24, 0x10}, // Man-Bat
        {25, 0x0C}, // The Penguin
        {26, 0x0B}, // Harley Quinn
        {27, 0x14}, // The Scarecrow
        {28, 0x1A}, // Killer Moth
        {29, 0x21}, // Mad Hatter
        {30, 0x09}, // The Joker
        {31, 0x0A}, // The Joker (Tropical)
        {32, 0x44}, // Poison Ivy Goon
        {33, 0x46}, // Zoo Sweeper
        {34, 0x3B}, // Freeze Girl
        {35, 0x3D}, // Yeti
        {36, 0x39}, // Riddler Goon
        {37, 0x3A}, // Riddler Henchman
        {38, 0x33}, // Penguin Goon
        {39, 0x34}, // Penguin Henchman
        {40, 0x29}, // Penguin Minion
        {41, 0x36}, // Joker Goon
        {42, 0x37}, // Joker Henchman
        {43, 0x38}, // Clown Goon
        {44, 0x08}, // Hush
        {45, 0x12}, // Rā's al Ghūl
        // {46, 0x49}, // Hero 1
        // {47, 0x4A}, // Hero 2

        // landcraft

        {48, 0x81}, // batmobile
        {49, 0x82}, // batcycle
        {50, 0x119}, // police car
        {51, 0x11A}, // police bike
        {52, 0x11F}, // police van
        {53, 0x121}, // bat-tank
        {54, 0x110}, // catwoman's motorcycle
        {55, 0xC6}, // two-face's armoured truck
        {56, 0xD2}, // mr. freeze's kart
        {57, 0xCD}, // harley quinn's hammer truck
        {58, 0x116}, // the joker's van
        {59, 0x11B}, // garbage truck

        // watercraft

        {60, 0x84}, // Batboat
        {61, 0xBD}, // Robin's Watercraft
        {62, 0x10D}, // Robin's Submarine
        {63, 0xD1}, // Police Watercraft
        {64, 0x9E}, // Police Boat
        {65, 0xCE}, // Penguin's Submarine
        {66, 0xBA}, // Killer Croc's Swamp Rider
        {67, 0xC0}, // Penguin Goon Submarine
        {68, 0x11D}, // Mr. Freeze's Iceberg
        {69, 0x11E}, // Mad Hatter's Steamboat

        // aircraft

        {70, 0x83}, // Batwing
        {71, 0xC4}, // Batcopter
        {72, 0x101}, // Harbour Helicopter
        {73, 0x112}, // Police Helicopter
        {74, 0x10F}, // Bruce Wayne's Private Jet

        {75, 0xC1}, // The Joker's Helicopter
        {76, 0xD0}, // The Scarecrow's Biplane
        {77, 0x118}, // Goon Helicopter
        {78, 0x11C}, // Riddler's jet
        {79, 0x122}, // Mad Hatter's Glider
    };

    private static unsafe byte* CharacterBaseAddress => *(byte**)(Mod.BaseAddress + 0x6CB830);

    // Returns the offset when searching the ID
    private static int GetCharacterByteOffset(int id)
    {
        return CharacterOffsets.TryGetValue(id, out var val) ? val : -1;
    }

    // Returns the ID when searching the offset
    public static int GetCharacterArchiID(int offset)
    {
        var kvp = CharacterOffsets.FirstOrDefault(k => k.Value == offset);
        return kvp.Equals(default(KeyValuePair<int, int>)) ? -1 : kvp.Key;
    }

    // Helper function to unlock a playable character based on archi ID, takes archi ID as a parameter
    public static unsafe void UnlockCharacter(int id)
    {
        int byteOffset = GetCharacterByteOffset(id);

        byte* ptr = CharacterBaseAddress + byteOffset;
        if (ptr == null || CharacterBaseAddress == null)
        {
            Mod.PrintToLog($"Can't Unlock Character, null pointer at 0x{(uint)ptr:X}");
            Mod.PrintToLog($"Character Base address is: 0x{(uint)CharacterBaseAddress:X}");
            Mod.PrintToLog($"byteOffset is: {byteOffset}");
            return;
        }
        *ptr = 1;
    }

    // Resets all playable characters to locked
    public static unsafe void ResetUnlockedCharacters()
    {
        foreach (var kvp in CharacterOffsets)
        {
            int byteOffset = kvp.Value;

            byte* ptr = CharacterBaseAddress + byteOffset;
            if (ptr == null || CharacterBaseAddress == null)
            {
                Mod.PrintToLog($"Can't Unlock Character, null pointer at 0x{(uint)ptr:X}");
                Mod.PrintToLog($"Character Base address is: 0x{(uint)CharacterBaseAddress:X}");
                Mod.PrintToLog($"byteOffset is: {byteOffset}");

                return;
            }
            *ptr = 0;
        }
    }

    // This Dictionary contains a list of the level ID & the Archi IDs of the 
    // vanilla character unlocks following archi logic
    private static readonly Dictionary<int, List<int>> VanillaUnlocks = new()
    {
        [2] = [48, 49],
        [6] = [60, 61],
        [12] = [70, 71],
        [15] = [15, 19],
        [16] = [16, 19],
        [17] = [17, 19],
        [18] = [18, 19],
        [19] = [18, 19],
        [20] = [20, 25],
        [21] = [21, 25],
        [22] = [25, 65, 66],
        [23] = [23, 25],
        [24] = [21, 25],
        [25] = [26, 30],
        [26] = [30, 75, 76],
        [27] = [27, 30],
        [28] = [28, 30],
        [29] = [26, 30],
    };

    public static void SendVanillaCharacters(int levelBeaten)
    {
        if (VanillaUnlocks.TryGetValue(levelBeaten, out var unlocks))
        {
            foreach (int id in unlocks)
            {
                Game.CheckAndReportLocation(id);
            }
        }
    }
    // This Dictionary contains a list of the level ID & the Archi IDs of the 
    // special character unlocks (i.e. bruce wayne/Nightwing) so we can take into
    // account the Simplified Unlock Settings
    private static readonly Dictionary<int, List<int>> SpecialUnlocks = new()
    {
        [4] = [2, 3, 4, 5, 7, 9, 10],
        [9] = [2, 3, 4, 5, 7, 9, 10],
        [14] = [2, 3, 4, 5, 7, 9, 10],
    };

    private const int BatTankOffset = 0x121;
    private const int BatTankID = 53;
    private const int HushOffset = 0x8;
    private const int HushID = 44;
    private const int RasOffset = 0x12;
    private const int RasID = 45;

    private static unsafe void SetSpecialCharacterState(byte* characterBase, int byteOffset, int locationID)
    {
        if (byteOffset < 0)
        {
            Mod.PrintToLog($"Invalid character byte offset: {byteOffset}");
            return;
        }

        byte* characterPointer = characterBase + byteOffset;
        *characterPointer = Mod.LB1_AP!.IsLocationChecked(locationID) ? (byte)3 : (byte)2;
    }

    public static unsafe void HandleSpecialCharacters()
    {
        byte* characterBase = CharacterBaseAddress;
        if (characterBase == null)
        {
            Mod.PrintToLog("Can't update special characters, character base address is null.");
            return;
        }

        bool isAllHeroComplete = true;
        foreach (var (levelID, characterIDs) in SpecialUnlocks)
        {
            if (!Mod.LB1_AP!.IsLocationChecked(levelID + Game.LevelOffset))
            {
                isAllHeroComplete = false;
                continue;
            }

            foreach (int characterID in characterIDs)
            {
                SetSpecialCharacterState(characterBase, GetCharacterByteOffset(characterID), characterID);
            }
        }

        if (isAllHeroComplete)
        {
            SetSpecialCharacterState(characterBase, BatTankOffset, BatTankID);
        }

        if (Mod.LB1_AP!.SlotDataInstance!.ShuffleHushAndRas == 1)
        {
            int hostageCount = Mod.LB1_AP!.CountItemsReceivedInRange(400, 424); // Count All hostage items
            if (hostageCount >= Mod.LB1_AP!.SlotDataInstance!.HushUnlockCondition)
            {
                SetSpecialCharacterState(characterBase, HushOffset, HushID);
            }

            int minikitCount = MinikitHandler.GetMinikitsReceivedCount();
            if (minikitCount >= Mod.LB1_AP!.SlotDataInstance!.RasUnlockCondition)
            {
                SetSpecialCharacterState(characterBase, RasOffset, RasID);
            }
        }
    }
}
