namespace LB1_AP_Mod;

public class Suits
{
    private enum SuitName
    {
        Bat_Suit,
        Heat_Suit,
        Glide_Suit,
        Demo_Suit,
        Sonar_Suit,

        Robin_Suit,
        Dive_Suit,
        Tech_Suit,
        Magnet_Suit,
        Attracto_Suit,

        Blocked_Suit
    };

    private enum SuitID : ushort
    {
        Bat_SuitID = 0x7608,
        Heat_SuitID = 0x7658,
        Glide_SuitID = 0x76A8,
        Demo_SuitID = 0x76F8,
        Sonar_SuitID = 0x7748,
        Robin_SuitID = 0x7798,
        Dive_SuitID = 0x77E8,
        Tech_SuitID = 0x7838,
        Magnet_SuitID = 0x7888,
        Attracto_SuitID = 0x78D8,
        Blocked_SuitID = 0xD628 // this likely points to something that coincidentally makes a suit that can't be worn.
    };

    private SuitName SuitIDtoSuitName(uint id)
    {
        return SuitIDtoSuitName((SuitID)id);
    }

    private SuitName SuitIDtoSuitName(SuitID id)
    {
        switch (id)
        {
            case SuitID.Bat_SuitID: return SuitName.Bat_Suit;
            case SuitID.Heat_SuitID: return SuitName.Heat_Suit;
            case SuitID.Glide_SuitID: return SuitName.Glide_Suit;
            case SuitID.Demo_SuitID: return SuitName.Demo_Suit;
            case SuitID.Sonar_SuitID: return SuitName.Sonar_Suit;

            case SuitID.Robin_SuitID: return SuitName.Robin_Suit;
            case SuitID.Dive_SuitID: return SuitName.Dive_Suit;
            case SuitID.Tech_SuitID: return SuitName.Tech_Suit;
            case SuitID.Magnet_SuitID: return SuitName.Magnet_Suit;
            case SuitID.Attracto_SuitID: return SuitName.Attracto_Suit;

            default: return SuitName.Blocked_Suit;
        }
    }

    private SuitID SuitNametoSuitID(uint name)
    {
        return SuitNametoSuitID((SuitName)name);
    }

    private SuitID SuitNametoSuitID(SuitName name)
    {
        switch (name)
        {
            case SuitName.Bat_Suit: return SuitID.Bat_SuitID;
            case SuitName.Heat_Suit: return SuitID.Heat_SuitID;
            case SuitName.Glide_Suit: return SuitID.Glide_SuitID;
            case SuitName.Demo_Suit: return SuitID.Demo_SuitID;
            case SuitName.Sonar_Suit: return SuitID.Sonar_SuitID;

            case SuitName.Robin_Suit: return SuitID.Robin_SuitID;
            case SuitName.Dive_Suit: return SuitID.Dive_SuitID;
            case SuitName.Tech_Suit: return SuitID.Tech_SuitID;
            case SuitName.Magnet_Suit: return SuitID.Magnet_SuitID;
            case SuitName.Attracto_Suit: return SuitID.Attracto_SuitID;

            default: return SuitID.Blocked_SuitID;
        }
    }

    private const byte DefaultSuitUnlockMask = 0x21; // Bit mask that has only Batsuit & Robin suit unlocked

    private static unsafe byte* SuitUnlocks1 => *(byte**)(Mod.BaseAddress + 0x6D0C10);
    private static unsafe byte* SuitUnlocks2 => (byte*)(Mod.BaseAddress + 0x6CA450);
    private static unsafe byte* SuitUnlocks3 => (byte*)(Mod.BaseAddress + 0x5CA294);

    public static unsafe void UnlockSuit(int ID)
    {
        int byteOffset = ID / 8;
        int bitOffset = ID % 8;
        *(SuitUnlocks1 + byteOffset) |= (byte)(1 << bitOffset);
        *(SuitUnlocks2 + byteOffset) |= (byte)(1 << bitOffset);
        *(SuitUnlocks3 + byteOffset) |= (byte)(1 << bitOffset);
    }

    private static unsafe void ResetSuitUnlocks(byte* suitUnlocks)
    {
        suitUnlocks[0] = DefaultSuitUnlockMask;
        suitUnlocks[1] = 0;
    }

    public static unsafe void ResetSuits()
    {
        ResetSuitUnlocks(SuitUnlocks1);
        ResetSuitUnlocks(SuitUnlocks2);
        ResetSuitUnlocks(SuitUnlocks3);
    }
}
