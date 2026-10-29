using Reloaded.Memory;
using Reloaded.Memory.Interfaces;
using Reloaded.Hooks.Definitions;
using Reloaded.Hooks.Definitions.Enums;
using Reloaded.Hooks.Definitions.X86;
using System.Numerics;

namespace LB1_AP_Mod;

public class Game
{
    private ushort MapID = 0;
    // This lock is used for isInShop, isInMenu because of the Hint System
    public readonly object StateLock = new();
    public bool isMenuOpen = false;
    public bool isShopOpen = false;
    public bool isLevelSelectOpen = false;

    // After Connected, this function reads initial game variables and NOPs code that we don't want running
    public static unsafe void ModifyInstructions()
    {
        // Set the Initial Map ID
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
        // NOP the code that moves the in level stud total to be moved into global total
        Memory.Instance.SafeWrite(Mod.BaseAddress + 0x24D3B3, [0x90, 0x90, 0x90, 0x90, 0x90, 0x90]);
        // NOP the code that turns the Hint color light blue
        Memory.Instance.SafeWrite(Mod.BaseAddress + 0x1D4E43, [0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90]);
        // NOP the code that turns the hint color red
        Memory.Instance.SafeWrite(Mod.BaseAddress + 0x1D526E, [0x90, 0x90, 0x90, 0x90, 0x90]);
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

    // This address seems to be 1 when the player is controllable and 0 when not (with few exceptions like menu). This function checks that variable and returns true/false
    public static unsafe bool IsPlayerControllable()
    {
        byte* playerFlagBase = *(byte**)(Mod.BaseAddress + 0x6B364C);
        if (playerFlagBase == null)
        {
            return false;
        }
        return *(playerFlagBase + 0x258) == 1;
    }

    // This helper Function checks if menu is loaded or player is controllable so we can connect and overwrite the assembly without the game reverting it
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

    // Constants for Archi ID Offsets
    private const int SuitOffset = 80;
    public const int PurpleStudOffset = 90;
    public const int MinikitOffset = 100;
    private const int HostageOffset = 400;
    public const int LevelOffset = 425;
    private const int TrueSuperHeroOffset = 455;
    public const int RedBrickOffset = 485;
    public const int MaxItemID = 521;

    // This is our general function that handles items. The way the archi is set up is we switch the game flags between the "Location" state and the "item" state depending on what the player is doing.
    public static void ManageItem(int itemID)
    {
        switch (itemID)
        {
            case < SuitOffset: // Handle Character
                CharacterHandler.UnlockCharacter(itemID);
                break;
            case < PurpleStudOffset: // Handle Suit Unlock
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

    // These are the reverse wrapper for the reloaded hooks. To make modding the game easier, we insert our functions at specific parts of the game's assembly and perform our own functions.
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
    private static IReverseWrapper<StudCollected> _reverseWrapOnStudCollected = default!;
    private static IReverseWrapper<ShopItemSelected> _reverseWrapOnShopItemSelected = default!;
    private static IReverseWrapper<OpenShopSubMenu> _reverseWrapOnOpenShopSubMenu = default!;
    private static IReverseWrapper<CloseShopSubMenu> _reverseWrapOnCloseShopSubMenu = default!;
    private static IReverseWrapper<SelectFreeplayCharacters> _reverserWrapOnSelectFreeplayCharacters = default!;

    // This section is where we write the assembly code that is going to get injected.
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
        _asmHooks.Add(hooks.CreateAsmHook(characterUnlockHook, (int)(Mod.BaseAddress + 0x240A37), AsmHookBehaviour.ExecuteFirst).Activate());

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
        _asmHooks.Add(hooks.CreateAsmHook(hostageHook, (int)(Mod.BaseAddress + 0x253177), AsmHookBehaviour.ExecuteFirst).Activate());

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
        _asmHooks.Add(hooks.CreateAsmHook(changeSuitSignalHook, (int)(Mod.BaseAddress + 0xF0818), AsmHookBehaviour.ExecuteFirst).Activate()); // Change Suit that the signal is showing in a level

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
        _asmHooks.Add(hooks.CreateAsmHook(handleInterruptedMessageHook, (int)(Mod.BaseAddress + 0x1D4E0E), AsmHookBehaviour.ExecuteFirst).Activate()); // Max hint timer
        _asmHooks.Add(hooks.CreateAsmHook(handleInterruptedMessageHook, (int)(Mod.BaseAddress + 0x1D4E50), AsmHookBehaviour.ExecuteFirst).Activate()); // Walk through Door

        string[] collectStudHook =
        {
            "use32",
            "pushfd",
            "pushad",
            $"{hooks.Utilities.GetAbsoluteCallMnemonics(OnStudCollected, out _reverseWrapOnStudCollected)}",
            "popad",
            "popfd",
        };
        _asmHooks.Add(hooks.CreateAsmHook(collectStudHook, (int)(Mod.BaseAddress + 0x1B22A9), AsmHookBehaviour.ExecuteFirst).Activate());

        string[] shopItemSelectedHook =
        {
            "use32",
            "pushfd",
            "pushad",
            $"{hooks.Utilities.GetAbsoluteCallMnemonics(OnShopItemSelected, out _reverseWrapOnShopItemSelected)}",
            "popad",
            "popfd",
        };
        _asmHooks.Add(hooks.CreateAsmHook(shopItemSelectedHook, (int)(Mod.BaseAddress + 0xE8FD8), AsmHookBehaviour.ExecuteFirst).Activate()); // Characters
        _asmHooks.Add(hooks.CreateAsmHook(shopItemSelectedHook, (int)(Mod.BaseAddress + 0xE90AE), AsmHookBehaviour.ExecuteFirst).Activate()); // Extras
        _asmHooks.Add(hooks.CreateAsmHook(shopItemSelectedHook, (int)(Mod.BaseAddress + 0xE9229), AsmHookBehaviour.ExecuteFirst).Activate()); // Suits

        string[] openShopSubMenuHook =
        {
            "use32",
            "pushfd",
            "pushad",
            $"{hooks.Utilities.GetAbsoluteCallMnemonics(OnOpenShopSubMenu, out _reverseWrapOnOpenShopSubMenu)}",
            "popad",
            "popfd",
        };
        _asmHooks.Add(hooks.CreateAsmHook(openShopSubMenuHook, (int)(Mod.BaseAddress + 0xEF590), AsmHookBehaviour.ExecuteFirst).Activate());

        string[] closeShopSubMenuHook =
        {
            "use32",
            "pushfd",
            "pushad",
            $"{hooks.Utilities.GetAbsoluteCallMnemonics(OnCloseShopSubMenu, out _reverseWrapOnCloseShopSubMenu)}",
            "popad",
            "popfd",
        };
        _asmHooks.Add(hooks.CreateAsmHook(closeShopSubMenuHook, (int)(Mod.BaseAddress + 0xECB13), AsmHookBehaviour.ExecuteFirst).Activate());

        string[] freeplayCharacterHook =
        {
            "use32",
            "pushfd",
            "pushad",
            $"{hooks.Utilities.GetAbsoluteCallMnemonics(OnSelectFreeplayCharacters, out _reverserWrapOnSelectFreeplayCharacters)}",
            "popad",
            "popfd",
        };
        _asmHooks.Add(hooks.CreateAsmHook(freeplayCharacterHook, (int)(Mod.BaseAddress + 0xA6AB3), AsmHookBehaviour.ExecuteFirst).Activate());
    }

    // This function is what we want to happen when the in level minikit count increases.
    // We will read the minikit code that the game stored and send the location
    // eax is the minikit count and edx is the Map ID it was collected in.
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

    // Start of the minikit code array in memory
    private static unsafe byte* MinikitCodeBaseAddress => (byte*)(Mod.BaseAddress + 0x6D10E0);

    // Function to read the minikit array and send the associated check.
    private static unsafe void ReadMinikitValue(uint minikitNum, ushort mapId)
    {
        if (MinikitCodeBaseAddress == null)
        {
            Mod.PrintToLog("Please report to a Dev. Minikit Address is Null");
            return;
        }
        byte* mostRecentMinikit = MinikitCodeBaseAddress + (minikitNum * 0xC); // Each code and Map ID is 0xC bytes long
        Mod.PrintToLog($"MostRecentMiniki: 0x{(uint)mostRecentMinikit:X}");

        string currentCode = new((sbyte*)mostRecentMinikit); // Read the current message from memory
        int MaxLength = 8; // Each minikit code is max 8 bytes

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
        // Lookup minikit code and report the location to Archi
        if (MinikitHandler.kitTable.TryGetValue((mapId, currentCode), out int kitId))
        {
            CheckAndReportLocation(kitId + MinikitOffset);
            return;
        }

        Mod.PrintToLog($"Minikit Code not recognized: {currentCode}. Map ID is: 0x{mapId:X}.");
    }

    // Since the game doesn't, we clear out the minikit codes when returning to hub
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

    // This function is what we want to happen when the player purchases a character.
    // The function converts the character offset (edi) to archi ID and reports the location to archi
    [Function([FunctionAttribute.Register.edi],
    FunctionAttribute.Register.eax, FunctionAttribute.StackCleanup.Callee)]
    public delegate void CharacterUnlock(int edi);

    private static void OnCharacterUnlock(int edi)
    {
        bool isInShop;
        lock (Mod.GameInstance!.StateLock)
        {
            isInShop = Mod.GameInstance!.isShopOpen;
        }
        int archiID = CharacterHandler.GetCharacterArchiID(edi);
        if (archiID != -1 && isInShop)
        {
            Mod.PrintToLog($"Character Unlocked: {archiID}");
            CheckAndReportLocation(archiID);
        }
    }

    // Helper function to verify if the player is in the Hub or in a level based on map ID
    private static readonly ushort[] HubMapIDs = [0x6, 0x9, 0xA, 0x5];
    public static bool IsInHub()
    {
        return HubMapIDs.Contains(Mod.GameInstance!.MapID);
    }

    // Helper function to update Game Flags to Items/Locations when in hub
    private static void UpdateHubItemsAndLocations()
    {
        Mod.LB1_AP!.UpdateBasedOnItems(0, SuitOffset - 1);
        Mod.LB1_AP!.UpdateBasedOnLocations(SuitOffset, MinikitOffset - 1);
        LevelHandler.UpdateMinikitCounts(true);
        Mod.LB1_AP!.UpdateBasedOnLocations(HostageOffset, LevelOffset - 1);
        Mod.LB1_AP!.UpdateBasedOnItems(LevelOffset, TrueSuperHeroOffset - 1);
        Mod.LB1_AP!.UpdateBasedOnLocations(TrueSuperHeroOffset, RedBrickOffset - 1);
        Mod.LB1_AP!.UpdateBasedOnItems(RedBrickOffset, MaxItemID);
    }

    // Helper function to update Game Flags to Items/Locations when in level
    private static void UpdateLevelItemsAndLocations()
    {
        Mod.LB1_AP!.UpdateBasedOnItems(0, MinikitOffset - 1);
        LevelHandler.UpdateMinikitCounts(false);
        Mod.LB1_AP!.UpdateBasedOnLocations(HostageOffset, RedBrickOffset - 1);
        Mod.LB1_AP!.UpdateBasedOnItems(RedBrickOffset, MaxItemID);
    }

    // Since the archi switches the game flags between location & item states, we have a custom function that runs when a map changes.
    // ecx is the new map that the player is going to.
    [Function([FunctionAttribute.Register.ecx],
    FunctionAttribute.Register.eax, FunctionAttribute.StackCleanup.Callee)]
    public delegate void UpdateMap(ushort ecx);
    private static void OnMapUpdate(ushort ecx)
    {
        // If coming from menu remove our connection status message
        if (Mod.GameInstance!.MapID == 0x0)
        {
            HintSystem.RestoreNewGameText();
        }

        // If the player enters the trophy room, check win con
        if (Mod.GameInstance!.MapID == 0x6)
        {
            CheckWinCon();
        }

        // Update Map ID
        Mod.PrintToLog($"Map ID Updated to: {ecx}");
        Mod.GameInstance!.MapID = ecx;

        // If going to menu, display connection status
        if (ecx == 0x0)
        {
            string message = ArchipelagoHandler.IsConnected ? "Connected. Ready to Play" : "Connecting. Please wait";
            HintSystem.UpdateConnectionStatusText(message);
        }

        // Check to see if the map we are on is the end of level status screen
        // If so, send the level completed location to archi
        int levelCompletedID = LevelHandler.CheckStatusMap(ecx);
        if (levelCompletedID != -1)
        {
            CheckAndReportLocation(levelCompletedID + LevelOffset);
            CharacterHandler.SendVanillaCharacters(levelCompletedID);
        }

        // Report the Map ID to Data storage for Auto Map Tracking
        Mod.LB1_AP!.SendMapID(Mod.GameInstance!.MapID);

        // Handle Item & location state for the new map.
        ResetItems();
        if (IsInHub())
        {
            ClearMinikitCodes();
            UpdateHubItemsAndLocations();
        }
        else
        {
            UpdateLevelItemsAndLocations();
        }
    }

    // Since the archi switches the game flags between location & item states, we have a custom function that runs when a menu is opened.
    // edx is how deep of a menu is open and ecx is what type of menu was opened
    [Function([FunctionAttribute.Register.edx, FunctionAttribute.Register.ecx],
    FunctionAttribute.Register.eax, FunctionAttribute.StackCleanup.Callee)]
    public delegate void OpenMenu(ushort edx, byte ecx);
    private static void OnOpenMenu(ushort edx, byte ecx)
    {
        // Sometimes we need to change the character selected by Player 2 (i.e. villain level or vehicle level)
        bool isInLevelSelect;
        lock (Mod.GameInstance!.StateLock)
        {
            isInLevelSelect = Mod.GameInstance!.isLevelSelectOpen;
        }
        if (isInLevelSelect && edx == 3) // 3 is when the freeplay character selector is to open
        {
            CharacterHandler.HandleCharacterSelect();
        }

        // If the player is deeper than level 1 or Map ID is menu, abandon our function
        if (edx != 1 || Mod.GameInstance!.MapID == 0)
        {
            return;
        }

        Mod.PrintToLog($"ecx for Menu Opened is: 0x{ecx:X}");

        // Handle Game Flags between Item & Location states
        ResetItems();
        // ecx: 0x21 is menu, 0x2C is shop, & 0x2F is level
        if (IsInHub() && ecx == 0x21) // Open menu
        {
            lock (Mod.GameInstance!.StateLock)
            {
                Mod.GameInstance!.isMenuOpen = true;
            }
            Mod.PrintToLog("Menu In Hub Opened");

            // Update Game Flags
            Mod.LB1_AP!.UpdateBasedOnItems(0, MaxItemID);
            LevelHandler.UpdateMinikitCounts(true);

            // Print the wincon status on the menu
            HintSystem.UpdateWinConText();
        }
        else if (IsInHub() && ecx == 0x2C) // Open Shop
        {
            lock (Mod.GameInstance!.StateLock)
            {
                Mod.GameInstance!.isShopOpen = true;
            }
            Mod.PrintToLog("Shop In Hub Opened");

            // Update Game flags
            Mod.LB1_AP!.UpdateBasedOnLocations(0, MaxItemID);
            LevelHandler.UpdateMinikitCounts(false);

            // Handle the shop flags that prevent purchases, adjust shop prices, & determine special character purchasability
            Shop.ResetShopPurchaseLocks();
            Shop.SetShopPrices(Mod.LB1_AP!.SlotDataInstance!.CheaperShops, false);
            if (Mod.LB1_AP!.SlotDataInstance!.SimpleEpisodeCharacters == 1)
            {
                CharacterHandler.HandleSpecialCharacters();
            }
        }
        else if (IsInHub() && ecx == 0x2F) // Open Level Selector
        {
            Mod.PrintToLog("Level Selector In Hub Opened");
            lock (Mod.GameInstance!.StateLock)
            {
                Mod.GameInstance!.isLevelSelectOpen = true;
            }

            // Update Game Flags
            Mod.LB1_AP!.UpdateBasedOnItems(0, MinikitOffset - 1);
            LevelHandler.UpdateMinikitCounts(false);
            Mod.LB1_AP!.UpdateBasedOnLocations(HostageOffset, LevelOffset - 1);
            Mod.LB1_AP!.UpdateBasedOnItems(LevelOffset, TrueSuperHeroOffset - 1);
            Mod.LB1_AP!.UpdateBasedOnLocations(TrueSuperHeroOffset, MaxItemID);
        }
        else if (LevelHandler.CheckStatusMap(Mod.GameInstance!.MapID) == -1) // In Level
        {
            lock (Mod.GameInstance!.StateLock)
            {
                Mod.GameInstance!.isMenuOpen = true;
            }
            Mod.PrintToLog("Menu in Level Opened");

            // Update Game Flags
            Mod.LB1_AP!.UpdateBasedOnItems(0, MinikitOffset - 1);
            LevelHandler.UpdateMinikitCounts(false);
            Mod.LB1_AP!.UpdateBasedOnLocations(HostageOffset, RedBrickOffset - 1);
            Mod.LB1_AP!.UpdateBasedOnItems(RedBrickOffset, MaxItemID);

            // Make the minikit count & Win Con text appear
            HintSystem.MakeMinikitCountAppear();
            HintSystem.UpdateWinConText();
        }
    }


    // Since the archi switches the game flags between location & item states, we have a custom function that runs when a menu is closed. We don't need any registers for this function
    [Function(CallingConventions.Fastcall)]
    public delegate void CloseMenu();
    private static void OnCloseMenu()
    {
        bool isInShop;
        bool isInMenu;
        bool isInLevelSelect;
        lock (Mod.GameInstance!.StateLock)
        {
            isInShop = Mod.GameInstance!.isShopOpen;
            isInMenu = Mod.GameInstance!.isMenuOpen;
            isInLevelSelect = Mod.GameInstance!.isLevelSelectOpen;
        }

        // Since you can't be in 2 of these at once and game flags need to be the same after, we update all 3 when closed
        if (isInMenu || isInShop || isInLevelSelect)
        {
            lock (Mod.GameInstance!.StateLock)
            {
                Mod.GameInstance!.isMenuOpen = false;
                Mod.GameInstance!.isShopOpen = false;
                Mod.GameInstance!.isLevelSelectOpen = false;
            }

            // Reverse the shop price decrease
            if (isInShop)
            {
                Shop.SetShopPrices(Mod.LB1_AP!.SlotDataInstance!.CheaperShops, true);
            }

            // Handle Game Flags
            ResetItems();
            if (IsInHub())
            {
                UpdateHubItemsAndLocations();
            }
            else
            {
                UpdateLevelItemsAndLocations();
            }
            // Make the Minikit Count on menu disappear and win con text go back to vanilla
            HintSystem.MakeMinikitCountDisappear();
            HintSystem.RestoreWinConText();
        }
    }

    // This custom function runs whenever a player clicks new game. A cutscene will play showing the villains escaping
    // This function will change the map to be loaded after to Arkham instead of You Can Bank on Batman
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

    // This custom function runs when a hostage is collected, determines the level and reports the archi location
    // eax is the address to write to and edx is the bit
    [Function([FunctionAttribute.Register.eax, FunctionAttribute.Register.edx],
    FunctionAttribute.Register.eax, FunctionAttribute.StackCleanup.Callee)]
    public delegate void HostageRescue(int eax, uint edx);
    private static unsafe void OnHostageRescue(int eax, uint edx)
    {
        byte* hostageArray = *(byte**)(Mod.BaseAddress + 0x6B09B0);
        int byteOffset = (eax - (int)hostageArray) * 8;
        int bitOffset = BitOperations.TrailingZeroCount(edx);
        int itemId = byteOffset + bitOffset;
        if (itemId < 0 || itemId > 25)
        {
            Mod.PrintToLog($"Cannot send hostage unlock. eax: 0x{eax:X}. hostageArray: 0x{(uint)hostageArray:X}. byteOffset: 0x{byteOffset:X}. bit offset: 0x{bitOffset:X}. itemID: {itemId}");
            return;
        }
        CheckAndReportLocation(itemId + HostageOffset);
    }

    // This function funs when the player achieves True Super hero in a level. It will check to see what level the player is in and report the location to archipelago.
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

    // This function runs when a player changes suit by entering a suit changer. It will report the suit as worn to archipelago.
    // edx is the suit offset that the player is going to wear
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

    // This function runs when the game tries to change the suit appearing on a suit changer. If the player doesn't have it unlocked, it changes the last 2 bytes to the "empty/blocked" suit code.
    // eax contains the suit value that is going to be written and we change it in game code if it isn't unlocked yet
    [Function([FunctionAttribute.Register.eax,],
    FunctionAttribute.Register.eax, FunctionAttribute.StackCleanup.Callee)]
    public delegate uint ChangeSuitSignal(uint eax);
    private static uint OnChangeSuitSignal(uint eax)
    {
        Mod.PrintToLog($"Suit Signal Changed. eax is: 0x{eax:X}");
        if (eax == 0)
        {
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

    // This function runs when the player purchases a red brick (extra/suit upgrade). It performs a bit operation to determine which one was purchased and reports the archi location
    // eax is the address & edx is the bit
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

    // This function runs when the player picks up a stud. As a QOL, we save any stud collected by the player regardless of whether they can beat the level or not.
    // ecx is the address to write to and edi is the stud value picked up
    [Function([FunctionAttribute.Register.ecx, FunctionAttribute.Register.edi],
    FunctionAttribute.Register.edi, FunctionAttribute.StackCleanup.Callee)]
    // edi is the stud value picked up and ecx is the address it is being written to
    public delegate void StudCollected(uint ecx, uint edi);
    private static unsafe void OnStudCollected(uint ecx, uint edi)
    {
        uint* studTotalAddress = *(uint**)(Mod.BaseAddress + 0x5C59CC) + (0x7BD0 / 4);
        uint* inLevelP1StudAddress = (uint*)(Mod.BaseAddress + 0x5C594C);
        uint* inLevelP2StudAddress = (uint*)(Mod.BaseAddress + 0x5C595C);

        if (ecx == (uint)inLevelP1StudAddress || ecx == (uint)inLevelP2StudAddress)
        {
            *studTotalAddress += edi;
        }

        return;
    }

    // This function runs when the game sets the timer to max. It is used to determine if one of our messages was interupted and add it back to the queue.
    [Function(CallingConventions.Fastcall)]
    private delegate void HandleInterruptedMessage();
    private static void OnHandleInterruptedMessage()
    {
        HintSystem.HandleInterruptedMessage();
    }

    // The custom function is inserted where the game constantly (once per frame likely) updates the item name in the shop.
    // This function will check to see where in the shop you are and update the text to show the archi location name.
    // We NOP the code that forces the text to update, so only ours runs
    [Function([FunctionAttribute.Register.eax],
    FunctionAttribute.Register.eax, FunctionAttribute.StackCleanup.Callee)]
    public delegate void ShopItemSelected(int eax);
    private static unsafe void OnShopItemSelected(int eax)
    {
        byte* shopMenuOpenedAddress = (byte*)(Mod.BaseAddress + 0x5CCCE8);
        int shopMenuOpened = *shopMenuOpenedAddress;
        int itemOffset = shopMenuOpened switch
        {
            1 => Shop.shopCharacters[eax],
            2 => RedBrickOffset + 1 + eax,
            5 => RedBrickOffset + 21 + eax,
            _ => -1
        };
        if (itemOffset == -1) // Other menu open
        {
            return;
        }
        Shop.HandleShopText(itemOffset);
    }

    // This custom function runs whenever you open a sub menu in the shop (extras, characters, data, etc.)
    // It will NOP the assembly code that constantly writes to the address showing shop text on screen.
    // We NOP & revert to original whenever a sub menu is open cause it affects other text in game too outside of the shop.
    [Function(CallingConventions.Fastcall)]
    private delegate void OpenShopSubMenu();
    private static unsafe void OnOpenShopSubMenu()
    {
        byte* shopMenuOpenedAddress = (byte*)(Mod.BaseAddress + 0x5CCCE8);
        int shopMenuOpened = *shopMenuOpenedAddress;
        if (shopMenuOpened == 1 || shopMenuOpened == 2 || shopMenuOpened == 5)
        {
            Mod.PrintToLog("Shop Sub menu opened. NOP'ing code");
            Shop.NOPShopTextCode();
        }
    }

    // Reverts the effects of the function above.
    [Function(CallingConventions.Fastcall)]
    private delegate void CloseShopSubMenu();
    private static void OnCloseShopSubMenu()
    {
        Mod.PrintToLog("Shop Sub Menu closed. Restoring code");
        Shop.RestoreShopTextCode();
    }

    // The game does not give the player a glide, dive, or tech character cause it assumes they have suits.
    // Whenever the game adjusts the freeplay character array, we make sure to force it to have a glide, dive & tech character if unlocked.
    [Function(CallingConventions.Fastcall)]
    private delegate void SelectFreeplayCharacters();
    private static void OnSelectFreeplayCharacters()
    {
        CharacterHandler.HandleFreeplayCharacters();
    }

    // Helper function to report an Archi location as completed.
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

    // Helper function that resets all game flags to locked state.
    private static void ResetItems()
    {
        CharacterHandler.ResetUnlockedCharacters();
        LevelHandler.ResetLevels();
        LevelHandler.ResetHostages();
        LevelHandler.ResetMinikitCount();
        LevelHandler.ResetTrueSuperHeroes();
        Shop.ResetRedBricks();
        Suits.ResetSuits();
    }

    // Helper function to determine if the player has achieved their win con
    public static void CheckWinCon()
    {
        // Minikits
        if (Mod.LB1_AP!.SlotDataInstance!.EndGoal == 0)
        {
            int minikitCount = MinikitHandler.GetMinikitsReceivedCount();
            int minikitsToWin = Mod.LB1_AP!.SlotDataInstance!.MinikitsToWin;
            if (minikitsToWin == -1)
            {
                Mod.PrintToLog("Can't determine if the game is completed, minikits to win slot data not available");
                return;
            }
            Mod.PrintToLog($"Minikit Count: {minikitCount}. Minikits To Win: {minikitsToWin}");
            if (minikitCount >= minikitsToWin)
            {
                Mod.LB1_AP!.Release();
            }
            return;
        }

        // Levels Beaten
        if (Mod.LB1_AP!.SlotDataInstance!.EndGoal == 1)
        {
            int levelsCompleted = Mod.LB1_AP!.CountLocationsCheckedInRange(LevelOffset, TrueSuperHeroOffset - 1);
            int requiredLevels = Mod.LB1_AP!.SlotDataInstance!.LevelsToWin;
            if (requiredLevels == -1)
            {
                Mod.PrintToLog("Can't Determine if the game is completed, Level slot data not available.");
                return;
            }
            Mod.PrintToLog($"Level Completed Count: {levelsCompleted}. Required Number of Levels To Win: {requiredLevels}");
            if (levelsCompleted >= requiredLevels)
            {
                Mod.LB1_AP!.Release();
            }
            return;
        }
    }
}