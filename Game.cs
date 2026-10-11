using Reloaded.Memory;
using Reloaded.Memory.Interfaces;
using Reloaded.Hooks.Definitions;
using Reloaded.Hooks.Definitions.Enums;
using Reloaded.Hooks.Definitions.X86;
using System.Numerics;
using LB1_AP_Mod.Configuration;
using LB1_AP_Mod;

namespace LB1_AP_Mod;

public class Game
{
    private ushort MapID = 0;
    // This lock is used for isInShop, isInMenu because of the Hint System
    public readonly object StateLock = new();
    public bool isMenuOpen = false;
    public bool isShopOpen = false;
    // After Connecting, this function reads initial game variables and NOPs code that we don't want running
    public static unsafe void ModifyInstructions()
    {
        ushort* mapPointer = (ushort*)(Mod.BaseAddress + 0x6CA8C4);
        if (mapPointer == null)
        {
            Mod.PrintToLog("Error determining intiial map");
            return;
        }
        Mod.GameInstance!.MapID = *mapPointer;
        Mod.PrintToLog($"Initial Map Address is: {*mapPointer}");
        Mod.LB1_AP!.SendMapID(*mapPointer);

        // NOP the code that writes to the Hint Interpreter
        Memory.Instance.SafeWrite(Mod.BaseAddress + 0x1D522D, [0x90, 0x90, 0x90, 0x90, 0x90, 0x90]);
        // NOP the code that writes to the Minikit on screen timer (keeping the pop but removing the write
        Memory.Instance.SafeWrite(Mod.BaseAddress + 0xDC040, [0xDD, 0xD8, 0x90, 0x90, 0x90, 0x90]);
    }

    // Used to check if the game menu is loaded before connecting and trying to set up hooks
    public static void IsGameLoaded()
    {
        Mod.PrintToLog("Checking to see if game is loaded");
        int rewriteNumber = 0;
        while (!AreGameFilesLoaded())
        {
            if (rewriteNumber % 10 == 0)
                Mod.PrintToLog("Waiting for game to load");
            rewriteNumber++;
            Thread.Sleep(500);
        }
    }

    public static unsafe bool IsPlayerControllable()
    {
        byte* playerFlagBase = *(byte**)(Mod.BaseAddress + 0x6B364C);
        if (playerFlagBase == null)
        {
            return false;
        }
        return *(playerFlagBase + 0x258) == 1;
    }

    // Helper Function to check if menu is loaded or player is controllable
    public static unsafe bool AreGameFilesLoaded()
    {
        try
        {
            if (Mod.GameInstance!.MapID > 0 || IsPlayerControllable())
                return true;
            uint* basePtr = (uint*)(Mod.BaseAddress + 0x6B364C);
            if (basePtr == null || *basePtr < Mod.BaseAddress)
                return false;

            return true;
        }
        catch (Exception ex)
        {
            Mod.PrintToLog($"Error in checking if game is loaded: {ex.Message}");
            return false;
        }
    }

    private const int SuitOffset = 80;
    private const int MinikitOffset = 100;
    private const int HostageOffset = 400;
    private const int LevelOffset = 425;
    private const int TrueSuperHeroOffset = 455;
    public const int RedBrickOffset = 485;
    private const int MaxItemID = 521;
    public static void ManageItem(int itemID)
    {
        switch (itemID)
        {
            case < SuitOffset: // Handle Character
                break;
            case < MinikitOffset: // Handle Suit Unlock
                Suits.UnlockSuit(itemID - SuitOffset);
                break;
            case < HostageOffset: // Handle Minikit
                break;
            case < LevelOffset: // Handle Hostage
                LevelHandler.UnlockHostage(itemID - HostageOffset);
                break;
            case < TrueSuperHeroOffset: // Handle Level
                LevelHandler.UnlockLevel(itemID - LevelOffset);
                break;
            case < RedBrickOffset: // Handle True Super Hero
                LevelHandler.UnlockTrueSuperHero(itemID - TrueSuperHeroOffset);
                break;
            case < MaxItemID: // Handle Red Brick
                Shop.UnlockExtra(itemID - RedBrickOffset);
                break;
        }

    }

    public static List<IAsmHook> _asmHooks = [];
    private static IReverseWrapper<IncreaseMinikitCount> _reverseWrapOnIncreaseMinikitCount = default!;
    private static IReverseWrapper<CharacterUnlock> _reverseWrapOnCharacterUnlock = default!;
    private static IReverseWrapper<UpdateMap> _reverseWrapOnMapUpdate = default!;
    private static IReverseWrapper<OpenMenu> _reverseWrapOnOpenMenu = default!;
    private static IReverseWrapper<CloseMenu> _reverseWrapOnCloseMenu = default!;
    private static IReverseWrapper<NewGameCutscene> _reverseWrapOnNewGameCutscene = default!;
    private static IReverseWrapper<HostageRescue> _reverseWrapOnHostageRescue = default!;
    private static IReverseWrapper<TrueSuperHero> _reverseWrapOnTrueSuperHero = default!;
    private static IReverseWrapper<ChangeSuitWorn> _reverseWrapOnChangeSuitWorn = default!;
    private static IReverseWrapper<ChangeSuitSignal> _reverseWrapOnChangeSuitSignal = default!;
    private static IReverseWrapper<PurchaseRedBrick> _reverseWrapOnPurchaseRedBrick = default!;
    private static IReverseWrapper<HandleInterruptedMessage> _reverseWrapOnHandleInterruptedMessage = default!;

    // Modifying the associated assembly of our game to call our functions
    // TODO: Future proof this from game updates by implementing signature scanning
    public void SetupHooks(IReloadedHooks hooks)
    {
        string[] minikitCountIncreaseHook =
        {
            "use32",
            "pushfd",
            "pushad",
            $"{hooks.Utilities.GetAbsoluteCallMnemonics(OnIncreaseMinikitCount, out _reverseWrapOnIncreaseMinikitCount)}",
            "popad",
            "popfd",
        };
        _asmHooks.Add(hooks.CreateAsmHook(minikitCountIncreaseHook, (int)(Mod.BaseAddress + 0x8CCAE), AsmHookBehaviour.ExecuteFirst).Activate());

        string[] characterUnlockHook =
        {
            "use32",
            "pushfd",
            "pushad",
            $"{hooks.Utilities.GetAbsoluteCallMnemonics(OnCharacterUnlock, out _reverseWrapOnCharacterUnlock)}",
            "popad",
            "popfd",
        };
        // _asmHooks.Add(hooks.CreateAsmHook(characterUnlockHook, (int)(Mod.BaseAddress + 0xB0F9A), AsmHookBehaviour.ExecuteFirst).Activate());

        string[] updateMapIDHook =
        [
            "use32",
            "pushfd",
            "pushad",
            $"{hooks.Utilities.GetAbsoluteCallMnemonics(OnMapUpdate, out _reverseWrapOnMapUpdate)}",
            "popad",
            "popfd",
        ];
        _asmHooks.Add(hooks.CreateAsmHook(updateMapIDHook, Mod.BaseAddress + 0xB0FE2, AsmHookBehaviour.ExecuteFirst).Activate());

        string[] openMenuHook =
        [
            // Move EBP Because we need it for our function
            "use32",
            "push ecx",
            "mov ecx, ebp",
            "pushfd",
            "pushad",
            $"{hooks.Utilities.GetAbsoluteCallMnemonics(OnOpenMenu, out _reverseWrapOnOpenMenu)}",
            "popad",
            "popfd",
            "pop ecx",
        ];
        _asmHooks.Add(hooks.CreateAsmHook(openMenuHook, Mod.BaseAddress + 0x2BF6E3, AsmHookBehaviour.ExecuteFirst).Activate());

        string[] closeMenuHook =
        {
            "use32",
            "pushfd",
            "pushad",
            $"{hooks.Utilities.GetAbsoluteCallMnemonics(OnCloseMenu, out _reverseWrapOnCloseMenu)}",
            "popad",
            "popfd",
        };
        _asmHooks.Add(hooks.CreateAsmHook(closeMenuHook, (int)(Mod.BaseAddress + 0x2BF570), AsmHookBehaviour.ExecuteAfter).Activate());

        string[] newGameCutsceneHook =
        {
            "use32",
            "pushfd",
            "pushad",
            $"{hooks.Utilities.GetAbsoluteCallMnemonics(OnNewGameCutscene, out _reverseWrapOnNewGameCutscene)}",
            "popad",
            "popfd",
        };
        _asmHooks.Add(hooks.CreateAsmHook(newGameCutsceneHook, (int)(Mod.BaseAddress + 0x20F88F), AsmHookBehaviour.ExecuteAfter).Activate());

        string[] hostageHook =
        {
            "use32",
            "pushfd",
            "pushad",
            $"{hooks.Utilities.GetAbsoluteCallMnemonics(OnHostageRescue, out _reverseWrapOnHostageRescue)}",
            "popad",
            "popfd",
        };
        _asmHooks.Add(hooks.CreateAsmHook(hostageHook, (int)(Mod.BaseAddress + 0x253177), AsmHookBehaviour.ExecuteAfter).Activate());

        string[] trueSuperHeroHook =
        {
            "use32",
            "pushfd",
            "pushad",
            $"{hooks.Utilities.GetAbsoluteCallMnemonics(OnTrueSuperHero, out _reverseWrapOnTrueSuperHero)}",
            "popad",
            "popfd",
        };
        _asmHooks.Add(hooks.CreateAsmHook(trueSuperHeroHook, (int)(Mod.BaseAddress + 0xDC35B), AsmHookBehaviour.ExecuteAfter).Activate());

        string[] changeSuitWornHook =
        {
            "use32",
            "pushfd",
            "pushad",
            $"{hooks.Utilities.GetAbsoluteCallMnemonics(OnChangeSuitWorn, out _reverseWrapOnChangeSuitWorn)}",
            "popad",
            "popfd",
        };
        _asmHooks.Add(hooks.CreateAsmHook(changeSuitWornHook, (int)(Mod.BaseAddress + 0xF241D), AsmHookBehaviour.ExecuteFirst).Activate()); // Change Suit that the person is wearing

        string[] changeSuitSignalHook =
        {
            // Push and Pop all Registers except for the EAX register since we need to adjust it in our function
            "use32",
            "pushfd",
            "push ebx",
            "push ecx",
            "push edx",
            "push esi",
            "push edi",
            "push ebp",
            $"{hooks.Utilities.GetAbsoluteCallMnemonics(OnChangeSuitSignal, out _reverseWrapOnChangeSuitSignal)}",
            "pop ebp",
            "pop edi",
            "pop esi",
            "pop edx",
            "pop ecx",
            "pop ebx",
            "popfd",
        };
        // Change Suit that the signal is showing in a level
        _asmHooks.Add(hooks.CreateAsmHook(changeSuitSignalHook, (int)(Mod.BaseAddress + 0xF0818), AsmHookBehaviour.ExecuteFirst).Activate());

        string[] purchaseRedBrick =
        {
            "use32",
            "pushfd",
            "pushad",
            $"{hooks.Utilities.GetAbsoluteCallMnemonics(OnPurchaseRedBrick, out _reverseWrapOnPurchaseRedBrick)}",
            "popad",
            "popfd",
        };
        _asmHooks.Add(hooks.CreateAsmHook(purchaseRedBrick, (int)(Mod.BaseAddress + 0x1CEE67), AsmHookBehaviour.ExecuteFirst).Activate());

        string[] handleInterruptedMessageHook =
        {
            "use32",
            "pushfd",
            "pushad",
            $"{hooks.Utilities.GetAbsoluteCallMnemonics(OnHandleInterruptedMessage, out _reverseWrapOnHandleInterruptedMessage)}",
            "popad",
            "popfd",
        };
        _asmHooks.Add(hooks.CreateAsmHook(handleInterruptedMessageHook, (int)(Mod.BaseAddress + 0x1D4E0E), AsmHookBehaviour.ExecuteFirst).Activate());
    }

    [Function([FunctionAttribute.Register.eax, FunctionAttribute.Register.edx],
    FunctionAttribute.Register.eax, FunctionAttribute.StackCleanup.Callee)]
    public delegate void IncreaseMinikitCount(uint eax, uint edx);

    private static void OnIncreaseMinikitCount(uint eax, uint edx)
    {
        uint minikitCount = eax;
        ushort mapId = (ushort)(edx & 0xFFFFu);
        Mod.PrintToLog($"Total Minikits collected: {minikitCount}. Map ID is: 0x{mapId:X}.");
        minikitCount -= 1; // Adjust for pointer arithmetic
        ReadMinikitValue(minikitCount, mapId);
    }

    private static unsafe byte* MinikitCodeBaseAddress => (byte*)(Mod.BaseAddress + 0x6D10E0);

    private static unsafe void ReadMinikitValue(uint minikitNum, ushort mapId)
    {
        if (MinikitCodeBaseAddress == null)
        {
            Mod.PrintToLog("Please report to a Dev. Minikit Address is Null");
            return;
        }
        byte* mostRecentMinikit = MinikitCodeBaseAddress + (minikitNum * 0xC);
        Mod.PrintToLog($"MostRecentMiniki: 0x{(uint)mostRecentMinikit:X}");


        string currentCode = new((sbyte*)mostRecentMinikit); // Read the current message from memory
        int MaxLength = 8;

        if (string.IsNullOrEmpty(currentCode))
        {
            Mod.PrintToLog("Minikit Code has a null value");
            return;
        }

        if (currentCode.Length > MaxLength)
        {
            Mod.PrintToLog("Unexpected Behavior, code exceeded max length");
            return;
        }
        Mod.PrintToLog($"Minikit Code: {currentCode}. Map ID is: 0x{mapId:X}.");

        if (MinikitHandler.kitTable.TryGetValue((mapId, currentCode), out int kitId))
        {
            CheckAndReportLocation(kitId + MinikitOffset);
            return;
        }
        Mod.PrintToLog($"Minikit Code not recognized: {currentCode}. Map ID is: 0x{mapId:X}.");
    }

    private static unsafe void ClearMinikitCodes()
    {
        if (MinikitCodeBaseAddress == null)
        {
            Mod.PrintToLog("Please report to a Dev. Minikit Address is Null");
            return;
        }

        for (int i = 0; i < 10; i++)
        {
            byte* minikitCode = MinikitCodeBaseAddress + (i * 0xC);
            *minikitCode = 0x0;
        }
    }

    [Function([FunctionAttribute.Register.edx],
    FunctionAttribute.Register.eax, FunctionAttribute.StackCleanup.Callee)]

    public delegate void CharacterUnlock(uint edx);

    private static void OnCharacterUnlock(uint edx)
    {
        int archiID = CharacterHandler.GetPurchaseCharacterID((IntPtr)edx, 0x0);
        Mod.PrintToLog($"Character Unlocked: {archiID}");
        if (archiID != -1)
        {
            CheckAndReportLocation(archiID);
        }
    }

    private static bool IsInHub()
    {
        return HubMapIDs.Contains(Mod.GameInstance!.MapID);
    }

    [Function([FunctionAttribute.Register.ecx],
    FunctionAttribute.Register.eax, FunctionAttribute.StackCleanup.Callee)]
    public delegate void UpdateMap(ushort ecx);
    private static unsafe void OnMapUpdate(ushort ecx)
    {
        Mod.PrintToLog($"Map ID Updated to: {ecx}");
        Mod.GameInstance!.MapID = ecx;

        int levelCompletedID = LevelHandler.CheckStatusMap(ecx);
        if (levelCompletedID != -1)
        {
            CheckAndReportLocation(levelCompletedID + LevelOffset);
        }
        ResetItems();
        Mod.LB1_AP!.SendMapID(Mod.GameInstance!.MapID);
        // TODO: This code may be a little repetive right now with the subsequent functions, once other items are implemented, can see what we want to do
        if (IsInHub())
        {
            ClearMinikitCodes();
            Mod.LB1_AP!.UpdateBasedOnLocations(0, MinikitOffset - 1);
            Mod.LB1_AP!.UpdateBasedOnItems(MinikitOffset, HostageOffset - 1);
            Mod.LB1_AP!.UpdateBasedOnLocations(HostageOffset, LevelOffset - 1);
            Mod.LB1_AP!.UpdateBasedOnItems(LevelOffset, TrueSuperHeroOffset - 1);
            Mod.LB1_AP!.UpdateBasedOnLocations(TrueSuperHeroOffset, RedBrickOffset - 1);
            Mod.LB1_AP!.UpdateBasedOnItems(RedBrickOffset, MaxItemID);
        }
        else
        {
            Mod.LB1_AP!.UpdateBasedOnItems(SuitOffset, MinikitOffset - 1);
            Mod.LB1_AP!.UpdateBasedOnLocations(HostageOffset, RedBrickOffset - 1);
            Mod.LB1_AP!.UpdateBasedOnItems(TrueSuperHeroOffset, MaxItemID);
        }
    }

    private static readonly ushort[] HubMapIDs = [0x9, 0xA, 0x5];

    [Function([FunctionAttribute.Register.edx, FunctionAttribute.Register.ecx],
    FunctionAttribute.Register.eax, FunctionAttribute.StackCleanup.Callee)]
    public delegate void OpenMenu(ushort edx, byte ecx);
    private static void OnOpenMenu(ushort edx, byte ecx)
    {

        // ecx: 0x21 is menu, 0x2C is shop, & 0x2F is level
        if (edx != 1 || Mod.GameInstance!.MapID == 0)
        {
            return;
        }
        Mod.PrintToLog($"ecx for Menu Opened is: 0x{ecx:X}");
        if (IsInHub() && ecx == 0x21) // Open menu
        {
            ResetItems();
            lock (Mod.GameInstance!.StateLock)
            {
                Mod.GameInstance!.isMenuOpen = true;
            }
            Mod.PrintToLog("Menu In Hub Opened");
            Mod.LB1_AP!.UpdateBasedOnItems(0, MaxItemID);
        }
        else if (IsInHub() && ecx == 0x2C) // Open Shop
        {
            ResetItems();
            lock (Mod.GameInstance!.StateLock)
            {
                Mod.GameInstance!.isShopOpen = true;
            }
            Mod.PrintToLog("Shop In Hub Opened");
            Mod.LB1_AP!.UpdateBasedOnLocations(0, MaxItemID);
            Shop.ResetShopPurchaseLocks();
        }
        else if (IsInHub() && ecx == 0x2F) // Open Level Selector
        {
            Mod.PrintToLog("Level Selector In Hub Opened");
            Mod.LB1_AP!.UpdateBasedOnLocations(0, MinikitOffset - 1);
            Mod.LB1_AP!.UpdateBasedOnItems(MinikitOffset, HostageOffset - 1);
            Mod.LB1_AP!.UpdateBasedOnLocations(HostageOffset, LevelOffset - 1);
            Mod.LB1_AP!.UpdateBasedOnItems(LevelOffset, TrueSuperHeroOffset - 1);
            Mod.LB1_AP!.UpdateBasedOnLocations(TrueSuperHeroOffset, MaxItemID);
        }
        else if (LevelHandler.CheckStatusMap(Mod.GameInstance!.MapID) == -1)
        {
            ResetItems();
            lock (Mod.GameInstance!.StateLock)
            {
                Mod.GameInstance!.isMenuOpen = true;
            }
            Mod.PrintToLog("Menu in Level Opened");
            Mod.LB1_AP!.UpdateBasedOnItems(SuitOffset, MinikitOffset - 1);
            Mod.LB1_AP!.UpdateBasedOnLocations(HostageOffset, RedBrickOffset - 1);
            HintSystem.MakeMinikitCountAppear();
        }
    }

    [Function(CallingConventions.Fastcall)]
    public delegate void CloseMenu();
    private static void OnCloseMenu()
    {
        bool isInShop;
        bool isInMenu;
        lock (Mod.GameInstance!.StateLock)
        {
            isInShop = Mod.GameInstance!.isShopOpen;
            isInMenu = Mod.GameInstance!.isMenuOpen;
        }
        if (isInMenu || isInShop)
        {
            lock (Mod.GameInstance!.StateLock)
            {
                Mod.GameInstance!.isMenuOpen = false;
                Mod.GameInstance!.isShopOpen = false;
            }
            ResetItems();
            if (IsInHub())
            {
                Mod.LB1_AP!.UpdateBasedOnLocations(0, MinikitOffset - 1);
                Mod.LB1_AP!.UpdateBasedOnItems(MinikitOffset, HostageOffset - 1);
                Mod.LB1_AP!.UpdateBasedOnLocations(HostageOffset, LevelOffset - 1);
                Mod.LB1_AP!.UpdateBasedOnItems(LevelOffset, TrueSuperHeroOffset - 1);
                Mod.LB1_AP!.UpdateBasedOnLocations(TrueSuperHeroOffset, RedBrickOffset - 1);
                Mod.LB1_AP!.UpdateBasedOnItems(RedBrickOffset, MaxItemID);
            }
            else
            {
                Mod.LB1_AP!.UpdateBasedOnItems(0, MinikitOffset - 1);
                Mod.LB1_AP!.UpdateBasedOnLocations(HostageOffset, RedBrickOffset - 1);
                Mod.LB1_AP!.UpdateBasedOnItems(TrueSuperHeroOffset, MaxItemID);
            }
            HintSystem.MakeMinikitCountDisappear();
        }
    }

    [Function(CallingConventions.Fastcall)]
    public delegate void NewGameCutscene();
    private static unsafe void OnNewGameCutscene()
    {
        Mod.PrintToLog("New Game Cutscene Written");
        uint* levelToBeLoadedPTR = (uint*)(Mod.BaseAddress + 0x6CA89C);
        uint* hubConstantPTRAddress = (uint*)(Mod.BaseAddress + 0x6CA8B4);
        if (*levelToBeLoadedPTR != *hubConstantPTRAddress)
        {
            *levelToBeLoadedPTR = *hubConstantPTRAddress;
        }
    }

    [Function([FunctionAttribute.Register.edx],
    FunctionAttribute.Register.eax, FunctionAttribute.StackCleanup.Callee)]
    public delegate void HostageRescue(uint edx);
    private static void OnHostageRescue(uint edx)
    {
        int itemId = BitOperations.TrailingZeroCount(edx);
        CheckAndReportLocation(itemId + HostageOffset);
    }

    [Function(CallingConventions.Fastcall)]
    public delegate void TrueSuperHero();
    private static void OnTrueSuperHero()
    {
        int archiID = LevelHandler.ConvertLevelIDToArchiID();
        if (archiID != -1)
        {
            CheckAndReportLocation(archiID + TrueSuperHeroOffset);
        }
    }

    [Function([FunctionAttribute.Register.edx],
    FunctionAttribute.Register.eax, FunctionAttribute.StackCleanup.Callee)]
    public delegate void ChangeSuitWorn(ushort edx);
    private static void OnChangeSuitWorn(ushort edx)
    {
        Mod.PrintToLog($"Worn Suit Changed. edx is: 0x{edx:X}");
        if (!IsInHub())
        {
            int archiId = Suits.GetSuitID(edx);
            if ((archiId > 0 && archiId < 5) || (archiId > 5 && archiId < 10))
                CheckAndReportLocation(archiId + SuitOffset);
        }
    }

    [Function([FunctionAttribute.Register.eax,],
    FunctionAttribute.Register.eax, FunctionAttribute.StackCleanup.Callee)]
    public delegate uint ChangeSuitSignal(uint eax);
    private static uint OnChangeSuitSignal(uint eax)
    {
        Mod.PrintToLog($"Suit Signal Changed. eax is: 0x{eax:X}");
        if (eax == 0)
        {
            // TODO: If this works, break it into a function to avoid duplicate code
            ushort blockedCode = Suits.GetSuitCode(10);
            eax = (eax & 0xFFFF0000u) | blockedCode;
            Mod.PrintToLog($"Suit is not unlocked. Blocking. Blocked Code: 0x{blockedCode:X}. eax: 0x{eax:X}");
        }
        ushort shortenedCode = (ushort)eax;
        int archiId = Suits.GetSuitID(shortenedCode);
        if (archiId == 0 || archiId == 5 || archiId == 10) // Suit is Bat Suit, Robin Suit or default (unknown)
        {
            return eax;
        }
        bool isUnlocked = Mod.LB1_AP!.CountItemsReceivedWithId(archiId + SuitOffset) > 0;
        if (!isUnlocked)
        {
            ushort blockedCode = Suits.GetSuitCode(10);
            eax = (eax & 0xFFFF0000u) | blockedCode;
            Mod.PrintToLog($"Suit is not unlocked. Blocking. Blocked Code: 0x{blockedCode:X}. eax: 0x{eax:X}");
        }
        return eax;
    }

    [Function([FunctionAttribute.Register.eax, FunctionAttribute.Register.edx],
    FunctionAttribute.Register.eax, FunctionAttribute.StackCleanup.Callee)]
    public delegate void PurchaseRedBrick(uint eax, uint edx);
    private static unsafe void OnPurchaseRedBrick(uint eax, uint edx)
    {
        bool isInShop;
        lock (Mod.GameInstance!.StateLock)
        {
            isInShop = Mod.GameInstance!.isShopOpen;
        }
        if (isInShop)
        {
            int itemId = BitOperations.TrailingZeroCount(edx);
            uint redBrickAddress = (uint)*(byte**)(Mod.BaseAddress + 0x6BE2C0);
            if ((eax - redBrickAddress) == 0x4)
            {
                itemId += 0x20; // For the last 4 red bricks, eax is a different address. This accounts for that
            }
            CheckAndReportLocation(itemId + RedBrickOffset);
        }
    }

    [Function(CallingConventions.Fastcall)]
    private delegate void HandleInterruptedMessage();
    private static void OnHandleInterruptedMessage()
    {
        HintSystem.HandleInterruptedMessage();
    }


    public static void CheckAndReportLocation(int apID)
    {
        if (Mod.LB1_AP!.IsLocationChecked(apID))
        {
            Mod.PrintToLog($"Location for AP ID: {apID} already checked");
            return;
        }
        Mod.PrintToLog($"Checking location for AP ID: {apID}");
        Mod.LB1_AP!.CheckLocation(apID);
    }

    private static void ResetItems()
    {
        LevelHandler.ResetLevels();
        LevelHandler.ResetHostages();
        LevelHandler.ResetTrueSuperHeroes();
        Suits.ResetSuits();
        Shop.ResetRedBricks();
    }

    // public static void CheckWinCon()
    // {
    // Levels Beaten
    //     if (Mod.LB1_AP!.SlotDataInstance!.EndGoal == 2)
    //     {
    //         int levelsCompleted = Mod.LB1_AP!.CountLocationsCheckedInRange(450, 473);
    //         int requiredLevels = Mod.LB1_AP!.SlotDataInstance!.NumberOfRequiredLevels;
    //         if (requiredLevels == -1)
    //         {
    //             Mod.PrintToLog("Can't Determine if the game is completed, Level slot data not available.");
    //             return;
    //         }
    //         Mod.PrintToLog($"Player Has Completed {levelsCompleted} Levels");
    //         if (levelsCompleted >= requiredLevels)
    //         {
    //             Mod.LB1_AP!.Release();
    //         }
    //         return;
    //     }
    // }
}