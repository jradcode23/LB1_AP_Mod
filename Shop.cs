using Reloaded.Memory;
using Reloaded.Memory.Interfaces;
using Archipelago.MultiClient.Net.Models;

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

        if (ID > 35)
        {
            return;
        }

        UnlockRedBrick(byteOffset, bitOffset);
        bool isSuitUpgrade = ID >= 21;
        if (isSuitUpgrade)
        {
            ActivateSuitUpgrade(ID);
        }

        bool isInShop;
        lock (Mod.GameInstance!.StateLock)
        {
            isInShop = Mod.GameInstance!.isShopOpen;
        }

        if (isInShop)
        {
            LockPurchase(
                ID,
                isSuitUpgrade ? SuitUpgradeOffset : ExtraOffset,
                isSuitUpgrade ? 0x15 : 1);
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
    public static readonly int[] shopCharacters =
    [
        2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 22, 24, 29, 31, 32, 33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43, 44, 45, 50, 51, 52, 53, 54, 55, 56, 57, 58, 59, 62, 63, 64, 67, 68, 69, 72, 73, 74, 77, 78, 79
    ];
    private const int ExtraOffset = 0xC74;
    private const int CharacterOffset = 0xC7C;
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
    private static int CheckShopCharacter(int ID)
    {
        return Array.IndexOf(shopCharacters, ID);
    }

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

        byte* itemPointer = *(byte**)shopLockBase + CharacterOffset;

        for (int i = 2; i < 80; i++)
        {
            int shopID = CheckShopCharacter(i);
            if (shopID != -1)
            {
                bool isLocationChecked = Mod.LB1_AP!.IsLocationChecked(i);

                byte* lockPointer = GetPurchaseLockPointer(itemPointer, shopID);
                *lockPointer = isLocationChecked ? (byte)1 : (byte)0;
            }
        }
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

    // This function NOPs the code in the shop that shows character text. We reverse this when outside of the shop cause the code runs in other locations too
    public static void NOPShopTextCode()
    {
        Memory.Instance.SafeWrite(Mod.BaseAddress + 0x2D75A8, [0x90, 0x90]);
        Memory.Instance.SafeWrite(Mod.BaseAddress + 0x2D75B3, [0x90, 0x90, 0x90]);
        Memory.Instance.SafeWrite(Mod.BaseAddress + 0xE934C, [0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90]);
        Memory.Instance.SafeWrite(Mod.BaseAddress + 0xECB67, [0x90, 0x90, 0x90, 0x90, 0x90, 0x90]);
    }

    public static void RestoreShopTextCode()
    {
        Memory.Instance.SafeWrite(Mod.BaseAddress + 0x2D75A8, [0x88, 0x08]);
        Memory.Instance.SafeWrite(Mod.BaseAddress + 0x2D75B3, [0xC6, 0x00, 0x00]);
        Memory.Instance.SafeWrite(Mod.BaseAddress + 0xE934C, [0xC6, 0x05, 0xA8, 0xCC, 0x9C, 0x00, 0x00]);
        Memory.Instance.SafeWrite(Mod.BaseAddress + 0xECB67, [0x88, 0x0D, 0xA8, 0xCC, 0x9C, 0x00]);
    }

    private static unsafe byte* ShopTextAddress => (byte*)(Mod.BaseAddress + 0x5CCCA8);

    public static unsafe void HandleShopText(int itemSelected)
    {
        if (Mod.LB1_AP == null || Mod.LB1_AP.ScoutedLocations == null)
        {
            return;
        }

        long index = ArchipelagoHandler.gameOffset + itemSelected;
        if (!Mod.LB1_AP.ScoutedLocations.TryGetValue(index, out ScoutedItemInfo? item) || item == null)
        {
            Mod.PrintToLog($"Error retrieving scouted location for ID {itemSelected}: index not found");
            return;
        }

        try
        {
            string message = item.Player + "'s " + item.ItemDisplayName;
            if (message.Length > 64)
            {
                message = message[..64]; // Truncate message if it exceeds selected max shop character length
            }
            HintSystem.WriteTextToMemory(message, (uint)ShopTextAddress);
        }
        catch (Exception ex)
        {
            Mod.PrintToLog($"Error retrieving scouted location for ID {itemSelected}: {ex.Message}");
        }
    }
}