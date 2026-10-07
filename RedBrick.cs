namespace LB1_AP_Mod;

public class RedBrick
{
    // TODO: To implement purchase lock behavior
    private static unsafe byte* ExtrasUnlockedArray => *(byte**)(Mod.BaseAddress + 0x6BE2C0);
    public static unsafe void UnlockExtra(int ID)
    {
        if (ExtrasUnlockedArray == null)
        {
            Mod.PrintToLog("Cannot Unlock Extra. Memory Address is null");
            return;
        }

        int byteOffset = ID / 8;
        int bitOffset = ID % 8;

        if (ID < 21)
        {
            UnlockRedBrick(byteOffset, bitOffset);
        }
        else if (ID <= 35)
        {
            UnlockRedBrick(byteOffset, bitOffset);
            ActivateSuitUpgrade(ID);
        }
    }

    private static unsafe void UnlockRedBrick(int ByteOffset, int BitOffset)
    {
        *(ExtrasUnlockedArray + ByteOffset) |= (byte)(1 << BitOffset);
    }

    private static unsafe byte* SuitUpgradesActive = (byte*)(Mod.BaseAddress + 0x53802C);
    private const int Offset = 0x1C;

    private unsafe static void ActivateSuitUpgrade(int ID)
    {
        if (SuitUpgradesActive == null)
        {
            Mod.PrintToLog("Can't Activate Suit Upgrade, Memory Address is null.");
            return;
        }
        int upgradeNumber = ID - 21; // Subtracting 21 cause that's how many extras there are
        *(SuitUpgradesActive + (upgradeNumber * Offset)) |= 1 << 0;
    }

    private static readonly byte[] mask = [0x01, 0x00, 0x00, 0x00];

    public unsafe static void ResetRedBricks()
    {
        if (ExtrasUnlockedArray == null || SuitUpgradesActive == null)
        {
            Mod.PrintToLog("Can't Reset Extras. One of the memory addresses is null");
            return;
        }

        for (int i = 0; i < mask.Length; i++)
        {
            *(ExtrasUnlockedArray + i) = mask[i];
        }

        for (int i = 0; i < 16; i++)
        {
            *(SuitUpgradesActive + (i * Offset)) &= unchecked((byte)~(1 << 0));
        }
    }
}