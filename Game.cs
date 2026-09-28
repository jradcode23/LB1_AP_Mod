using Reloaded.Memory;
using Reloaded.Memory.Interfaces;
using Reloaded.Hooks.Definitions;
using Reloaded.Hooks.Definitions.Enums;
using Reloaded.Hooks.Definitions.X86;
using System.Numerics;
using LB1_AP_Mod.Configuration;

namespace LB1_AP_Mod;

public class Game
{
    private ushort MapID = 0;
    public bool isMenuOpen = false;
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

    // Helper Function to check if menu is loaded or player is controllable
    public static unsafe bool AreGameFilesLoaded()
    {
        try
        {
            if (Mod.GameInstance!.MapID > 0)
                return true;
            // TODO: Use a better address for this. It loads a little early right now which could cause some issues
            uint* basePtr = (uint*)(Mod.BaseAddress + 0x6957F8);
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

    private const int LevelOffset = 425;
    private const int TrueStatusOffset = 455;
    public static void ManageItem(int itemID)
    {
        switch (itemID)
        {
            case < LevelOffset:
                break;
            case < TrueStatusOffset: // Handle Level
                LevelHandler.UnlockLevel(itemID - LevelOffset);
                break;
        }

    }

    public static List<IAsmHook> _asmHooks = [];
    private static IReverseWrapper<UpdateMap> _reverseWrapOnMapUpdate = default!;
    private static IReverseWrapper<OpenMenu> _reverseWrapOnOpenMenu = default!;
    private static IReverseWrapper<CloseMenu> _reverseWrapOnCloseMenu = default!;

    // Modifying the associated assembly of our game to call our functions
    // TODO: Future proof this from game updates by implementing signature scanning
    public void SetupHooks(IReloadedHooks hooks)
    {
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
            "use32",
            "pushfd",
            "pushad",
            $"{hooks.Utilities.GetAbsoluteCallMnemonics(OnOpenMenu, out _reverseWrapOnOpenMenu)}",
            "popad",
            "popfd",
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
        Mod.LB1_AP!.UpdateBasedOnLocations(LevelOffset, TrueStatusOffset - 1);
    }

    private static readonly ushort[] HubMapIDs = [0x9, 0xA, 0x5];

    [Function([FunctionAttribute.Register.edx],
    FunctionAttribute.Register.eax, FunctionAttribute.StackCleanup.Callee)]
    public delegate void OpenMenu(ushort edx);
    private static void OnOpenMenu(ushort edx)
    {

        if (edx != 1)
        {
            return;
        }
        bool isHub = HubMapIDs.Contains(Mod.GameInstance!.MapID);
        if (isHub)
        {
            Mod.PrintToLog("Menu or Level Selector In Hub Opened");
            Mod.GameInstance!.isMenuOpen = true;
            ResetItems();
            Mod.LB1_AP!.UpdateBasedOnItems(LevelOffset, TrueStatusOffset - 1);
        }
        else if (Mod.GameInstance!.MapID != 0 && LevelHandler.CheckStatusMap(Mod.GameInstance!.MapID) == -1)
        {
            Mod.PrintToLog("Menu or Level Selector In Level Opened");
            Mod.GameInstance!.isMenuOpen = true;
            ResetItems();
            Mod.LB1_AP!.UpdateBasedOnLocations(LevelOffset, TrueStatusOffset - 1);
        }
    }

    [Function(CallingConventions.Fastcall)]
    public delegate void CloseMenu();
    private static void OnCloseMenu()
    {
        if (Mod.GameInstance!.isMenuOpen)
        {
            Mod.GameInstance!.isMenuOpen = false;
            ResetItems();
            Mod.LB1_AP!.UpdateBasedOnLocations(LevelOffset, TrueStatusOffset - 1);
            Mod.PrintToLog("Menu or Level Selector Closed");
        }
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