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

    public static List<IAsmHook> _asmHooks = [];
    private static IReverseWrapper<UpdateMap> _reverseWrapOnMapUpdate = default!;

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
    }

    [Function([FunctionAttribute.Register.ecx],
    FunctionAttribute.Register.eax, FunctionAttribute.StackCleanup.Callee)]
    public delegate void UpdateMap(ushort ecx);
    private static unsafe void OnMapUpdate(ushort ecx)
    {
        Mod.PrintToLog($"Map ID Updated to: {ecx}");
        Mod.GameInstance!.MapID = ecx;
    }
}