namespace LB1_AP_Mod;

public class Shop
{
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
            bool isInShop;
            lock (Mod.GameInstance!.StateLock)
            {
                isInShop = Mod.GameInstance!.isShopOpen;
            }
            if (isInShop)
            {
                LockPurchase(ID, ExtraOffset, 1);
            }
        }
        else if (ID <= 35)
        {
            UnlockRedBrick(byteOffset, bitOffset);
            ActivateSuitUpgrade(ID);
            bool isInShop;
            lock (Mod.GameInstance!.StateLock)
            {
                isInShop = Mod.GameInstance!.isShopOpen;
            }
            if (isInShop)
            {
                LockPurchase(ID, SuitUpgradeOffset, 0x15);
            }
        }
    }

    private static unsafe void UnlockRedBrick(int ByteOffset, int BitOffset)
    {
        *(ExtrasUnlockedArray + ByteOffset) |= (byte)(1 << BitOffset);
    }

    private static unsafe byte* SuitUpgradesActive => (byte*)(Mod.BaseAddress + 0x53802C);
    private const int SuitOffset = 0x1C;

    private unsafe static void ActivateSuitUpgrade(int ID)
    {
        if (SuitUpgradesActive == null)
        {
            Mod.PrintToLog("Can't Activate Suit Upgrade, Memory Address is null.");
            return;
        }
        int upgradeNumber = ID - 21; // Subtracting 21 cause that's how many extras there are
        *(SuitUpgradesActive + (upgradeNumber * SuitOffset)) |= 1 << 0;
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
            *(SuitUpgradesActive + (i * SuitOffset)) &= unchecked((byte)~(1 << 0));
        }
    }

    private const int ExtraOffset = 0xC74;
    private const int SuitUpgradeOffset = 0xC8C;
    private const int PurchaseLockOffset = 0x63;
    private const int PurchaseLockStride = 0x74;
    private static unsafe byte* ShopLockBase => *(byte**)(Mod.BaseAddress + 0x560894) + 0x53B0;

    /* 
    In LB1, there are a few things that can prevent you from purchasing something in the shop. 
    One of them is what we call the "Purchase Lock". 
    It makes it so you can't hit A on the character even if it shows as avaliable forpurchase.
    This Function resets all the shop items so the associated array in memory matches the Archi Location state.
    */
    public static unsafe void ResetShopPurchaseLocks()
    {
        byte* shopLockBase = ShopLockBase;
        if (shopLockBase == null)
        {
            Mod.PrintToLog("Shop Lock Base is Null. Can't reset");
            return;
        }

        ResetPurchaseLocks(shopLockBase, ExtraOffset, 1, 0x14);
        ResetPurchaseLocks(shopLockBase, SuitUpgradeOffset, 0x15, 0xF);
    }

    private static unsafe void ResetPurchaseLocks(
        byte* shopLockBase,
        int pointerOffset,
        int locationOffset,
        int itemCount)
    {
        byte* itemPointer = *(byte**)shopLockBase + pointerOffset;
        if (itemPointer == null)
        {
            Mod.PrintToLog("itemPointer (Reset) is Null. Can't reset");
            return;
        }

        for (int i = 0; i < itemCount; i++)
        {
            if (!Mod.LB1_AP!.IsLocationChecked(Game.RedBrickOffset + i + locationOffset))
            {
                byte* lockPointer = GetPurchaseLockPointer(itemPointer, i);
                if (lockPointer == null)
                {
                    Mod.PrintToLog($"Lock pointer at: {i} is null");
                    continue;
                }
                *lockPointer = 0;
            }
        }
    }

    private static unsafe byte* GetPurchaseLockPointer(byte* itemPointer, int shopOffset)
    {
        return *(byte**)itemPointer + PurchaseLockOffset + (shopOffset * PurchaseLockStride);
    }

    private static unsafe void LockPurchase(int id, int pointerOffset, int idOffset)
    {
        byte* shopLockBase = ShopLockBase;
        if (shopLockBase == null)
        {
            Mod.PrintToLog("Shop Lock Base is Null. Can't reset");
            return;
        }

        byte* itemPointer = *(byte**)shopLockBase + pointerOffset;
        if (itemPointer == null)
        {
            Mod.PrintToLog("itemPointer (Lock) is Null. Can't reset");
            return;
        }

        int shopOffset = id - idOffset;
        byte* lockPointer = GetPurchaseLockPointer(itemPointer, shopOffset);
        if (lockPointer == null)
        {
            Mod.PrintToLog($"Lock pointer at: {shopOffset} is null");
            return;
        }
        *lockPointer = 1;
    }
}