namespace LB1_AP_Mod;

public class Suits
{
    private enum SuitName
    {
        BatSuit,
        HeatSuit,
        GlideSuit,
        DemoSuit,
        SonicSuit,

        RobinSuit,
        DiveSuit,
        TechSuit,
        MagnetSuit,
        AttractSuit,

        BlockedSuit
    };

    private enum SuitCode : ushort
    {
        BatSuitCode = 0x7608,
        HeatSuitCode = 0x7658,
        GlideSuitCode = 0x76A8,
        DemoSuitCode = 0x76F8,
        SonicSuitCode = 0x7748,
        RobinSuitCode = 0x7798,
        DiveSuitCode = 0x77E8,
        TechSuitCode = 0x7838,
        MagnetSuitCode = 0x7888,
        AttractSuitCode = 0x78D8,
        BlockedSuitCode = 0xD628 // this likely points to something that coincidentally makes a suit that can't be worn.
    };

    public static int GetSuitID(uint id)
    {
        return (int)SuitCodeToSuitId((SuitCode)id);
    }

    private static SuitName SuitCodeToSuitId(SuitCode id)
    {
        return id switch
        {
            SuitCode.BatSuitCode => SuitName.BatSuit,
            SuitCode.HeatSuitCode => SuitName.HeatSuit,
            SuitCode.GlideSuitCode => SuitName.GlideSuit,
            SuitCode.DemoSuitCode => SuitName.DemoSuit,
            SuitCode.SonicSuitCode => SuitName.SonicSuit,
            SuitCode.RobinSuitCode => SuitName.RobinSuit,
            SuitCode.DiveSuitCode => SuitName.DiveSuit,
            SuitCode.TechSuitCode => SuitName.TechSuit,
            SuitCode.MagnetSuitCode => SuitName.MagnetSuit,
            SuitCode.AttractSuitCode => SuitName.AttractSuit,
            _ => SuitName.BlockedSuit,
        };
    }

    public static ushort GetSuitCode(uint id)
    {
        return (ushort)SuitNameToSuitCode(id);
    }

    private static SuitCode SuitNameToSuitCode(uint name)
    {
        return (SuitName)name switch
        {
            SuitName.BatSuit => SuitCode.BatSuitCode,
            SuitName.HeatSuit => SuitCode.HeatSuitCode,
            SuitName.GlideSuit => SuitCode.GlideSuitCode,
            SuitName.DemoSuit => SuitCode.DemoSuitCode,
            SuitName.SonicSuit => SuitCode.SonicSuitCode,
            SuitName.RobinSuit => SuitCode.RobinSuitCode,
            SuitName.DiveSuit => SuitCode.DiveSuitCode,
            SuitName.TechSuit => SuitCode.TechSuitCode,
            SuitName.MagnetSuit => SuitCode.MagnetSuitCode,
            SuitName.AttractSuit => SuitCode.AttractSuitCode,
            _ => SuitCode.BlockedSuitCode,
        };
    }

    private const byte DefaultSuitUnlockMask = 0x21; // Bit mask that has only Batsuit & Robin suit unlocked

    private static unsafe byte* SuitUnlocks1 => *(byte**)(Mod.BaseAddress + 0x6D0C10);
    private static unsafe byte* SuitUnlocks2 => (byte*)(Mod.BaseAddress + 0x6CA450);
    private static unsafe byte* SuitUnlocks3 => (byte*)(Mod.BaseAddress + 0x5CA294);

    public static unsafe void UnlockSuit(int id)
    {
        if (id > 10)
        {
            return;
        }
        int byteOffset = id / 8;
        int bitOffset = id % 8;
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
