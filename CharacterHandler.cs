using System.Numerics;

namespace LB1_AP_Mod;

public class CharacterHandler
{
	private static unsafe byte* characterBaseAddress => *(byte**)(Mod.BaseAddress + 0x6CB30);
    private static readonly byte unlockOffset = 0xE4;

	public static readonly Dictionary<int, int> characterMap = new()
	{
        {0, 0x00}, // Batman
        {1, 0x01}, // Robin
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
        {12, 0x2D}, // Sailor
        {13, 0x43}, // Police Marksman
        {14, 0x0F}, // Clayface
        {15, 0x16}, // Mr. Freeze
        {16, 0x15}, // Poison Ivy
        {17, 0x07}, // Two-Face
        {18, 0x0D}, // The Riddler
        {19, 0x19}, // Bane
        {20, 0x04}, // Catwoman
        {21, 0x05}, // Catwoman (Classic)
        {22, 0x0E}, // Killer Croc
        {23, 0x10}, // Man-Bat
        {24, 0x0C}, // The Penguin
        {25, 0x0B}, // Harley Quinn
        {26, 0x14}, // The Scarecrow
        {27, 0x1A}, // Killer Moth
        {28, 0x21}, // Mad Hatter
        {29, 0x09}, // The Joker
        {30, 0x0A}, // The Joker (Tropical)
        {31, 0x44}, // Poison Ivy Goon
        {32, 0x46}, // Zoo Sweeper
        {33, 0x3B}, // Freeze Girl
        {34, 0x3D}, // Yeti
        {35, 0x39}, // Riddler Goon
        {36, 0x3A}, // Riddler Henchman
        {37, 0x33}, // Penguin Goon
        {38, 0x34}, // Penguin Henchman
        {39, 0x29}, // Penguin Minion
        {40, 0x36}, // Joker Goon
        {41, 0x37}, // Joker Henchman
        {42, 0x38}, // Clown Goon
        {43, 0x08}, // Hush
        {44, 0x12}, // Rā's al Ghūl
        {45, 0x49}, // Hero 1
        {46, 0x4A}, // Hero 2

        // landcraft

        {46, 0x81}, // batmobile
        {47, 0x82}, // batcycle
        {48, 0x119}, // police car
        {49, 0x11A}, // police bike

        {50, 0x11F}, // police van
        {51, 0x121}, // bat-tank
        {52, 0x110}, // catwoman's motorcycle
        {53, 0xC6}, // two-face's armoured truck
    
        {54, 0xD2}, // mr. freeze's kart
        {55, 0xCD}, // harley quinn's hammer truck
        {56, 0x116}, // the joker's van
        {57, 0x11B}, // garbage truck

        // watercraft

        {58, 0x84}, // Batboat
        {59, 0xBD}, // Robin's Watercraft
        {60, 0x10D}, // Robin's Submarine
        {61, 0xD1}, // Police Watercraft
        {62, 0x9E}, // Police Boat

        {63, 0xCE}, // Penguin's Submarine
        {64, 0xBA}, // Killer Croc's Swamp Rider
        {65, 0xC0}, // Penguin Goon Submarine
        {66, 0x11D}, // Mr. Freeze's Iceberg
        {67, 0x11E}, // Mad Hatter's Steamboat

        // aircraft

        {68, 0x83}, // Batwing
        {69, 0xC4}, // Batcopter
        {70, 0x101}, // Harbour Helicopter
        {71, 0x112}, // Police Helicopter
        {72, 0x10F}, // Bruce Wayne's Private Jet

        {73, 0xC1}, // The Joker's Helicopter
        {74, 0xD0}, // The Scarecrow's Biplane
        {75, 0x118}, // Goon Helicopter
        {76, 0x11C}, // Riddler's jet
        {77, 0x122}, // Mad Hatter's Glider

        // special
        {78, 0x03},
    };

    // Returns the offset when searching the ID
    public static int GetCharacterByteOffset(int id)
    {
        return characterMap.TryGetValue(id, out var val) ? val : -1;
    }

    /*
    Helper function that does a reverse lookup of Character Purchases to the Dictionary above and returns the Archi ID.
    It takes the address that is being written to (gotten from our hook).
    */
    public static unsafe int GetPurchaseCharacterID(IntPtr address, int offset)
    {
        long byteIndex = address + offset + 0x74 - (IntPtr)(characterBaseAddress + unlockOffset);
        if (byteIndex < 0)
        {
            return -1;
        }

        int value = (int)byteIndex;
        var kvp = characterMap.FirstOrDefault(k => k.Value == value);
        return kvp.Equals(default(KeyValuePair<int, int>)) ? -1 : kvp.Key;
    }
}
