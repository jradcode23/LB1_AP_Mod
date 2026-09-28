using Reloaded.Hooks.ReloadedII.Interfaces;
using Reloaded.Mod.Interfaces;
using LB1_AP_Mod.Template;
using LB1_AP_Mod.Configuration;
using System.Diagnostics;
// #if DEBUG

// #endif

namespace LB1_AP_Mod;

/// <summary>
/// Your mod logic goes here.
/// </summary>
public class Mod : ModBase // <= Do not Remove.
{
    /// <summary>
    /// Provides access to the mod loader API.
    /// </summary>
    // private readonly IModLoader _modLoader;

    /// <summary>
    /// Provides access to the Reloaded.Hooks API.
    /// </summary>
    /// <remarks>This is null if you remove dependency on Reloaded.SharedLib.Hooks in your mod.</remarks>
    private static IReloadedHooks? _hooks;

    /// <summary>
    /// Provides access to the Reloaded logger.
    /// </summary>
    private static ILogger? Logger;

    /// <summary>
    /// Entry point into the mod, instance that created this class.
    /// </summary>
    // private readonly IMod _owner;

    /// <summary>
    /// Provides access to this mod's configuration.
    /// </summary>
    public static Config? Configuration;

    /// <summary>
    /// The configuration of the currently executing mod.
    /// </summary>
    // private readonly IModConfig _modConfig;
    public static ArchipelagoHandler? LB1_AP;
    public static Game? GameInstance;
    public static uint BaseAddress;

    public Mod(ModContext context)
    {
        // _modLoader = context.ModLoader;
        _hooks = context.Hooks;
        Logger = context.Logger;
        // _owner = context.Owner;
        Configuration = context.Configuration;
        // _modConfig = context.ModConfig;

#if DEBUG
        // Attaches debugger in debug mode; ignored in release.
        Debugger.Launch();
#endif

        GameInstance = new Game();
        BaseAddress = (uint)Process.GetCurrentProcess().MainModule!.BaseAddress;

        if (Configuration == null)
        {
            PrintToLog("Configuration is null. Terminating Mod.");
            return;
        }
        SetUpAP(Configuration.ArchipelagoOptions.Server, Configuration.ArchipelagoOptions.Port, Configuration.ArchipelagoOptions.Slot, Configuration.ArchipelagoOptions.Password);
        PrintToLog("Mod Version: LB1_AP_mod version 1.0.0 Source Code");

        var thread1 = new Thread(start: () =>
        {
            while (true)
            {
                if (!ArchipelagoHandler.IsConnecting && !ArchipelagoHandler.IsConnected)
                {
                    LB1_AP!.InitConnect();
                }
                Thread.Sleep(2500);
            }
        });
        thread1.Start();
    }

    // Helper Function to help print to the terminal and log file with a consistent prefix
    public static void PrintToLog(string message)
    {
        Logger!.WriteLineAsync("[LB1.AP.mod] " + message);
    }

    #region Standard Overrides
    public override void ConfigurationUpdated(Config configuration)
    {
        if (Configuration == null)
        {
            PrintToLog("Configuration is null, cannot update.");
            return;
        }

        if (Configuration.ArchipelagoOptions.Port != configuration.ArchipelagoOptions.Port || Configuration.ArchipelagoOptions.Slot != configuration.ArchipelagoOptions.Slot)
        {
            Configuration = configuration;
            PrintToLog($"Config Updated: Applying");
            LB1_AP!.Disconnect();
            SetUpAP(Configuration.ArchipelagoOptions.Server, Configuration.ArchipelagoOptions.Port, Configuration.ArchipelagoOptions.Slot, Configuration.ArchipelagoOptions.Password);
        }
    }
    #endregion

    public static void SetUpAP(string server, int port, string slot, string password)
    {
        LB1_AP = new ArchipelagoHandler(server, port, slot, password);
        PrintToLog($"Mod Initialized with Server: {server}, Port: {port}, Slot: {slot}");
    }

    public static bool InitOnMenu()
    {
        if (_hooks == null)
        {
            PrintToLog("Hooks are Null. Please do not proceed and report this to the Dev.");
            return false;
        }

        int hookCount = Game._asmHooks.Count;
        if (hookCount > 0)
        {
            PrintToLog($"Hooks already set up. Count: {hookCount}, skipping setup.");
            return true;
        }

        PrintToLog("Modifying instructions for hook setup.");
        Game.ModifyInstructions();

        if (_hooks != null)
        {
            PrintToLog("Game loaded, setting up hooks. Please wait for hook setup before loading a save file.");
            GameInstance!.SetupHooks(_hooks);
            PrintToLog("Hooks set up complete. You may now load a save file.");
            return true;
        }

        return false;
    }

    #region For Exports, Serialization etc.
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public Mod() { }
#pragma warning restore CS8618
    #endregion
}