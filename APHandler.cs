using System.Collections.Concurrent;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Helpers;
using Archipelago.MultiClient.Net.MessageLog.Messages;
using Archipelago.MultiClient.Net.Packets;
using System.Diagnostics.CodeAnalysis;
using Archipelago.MultiClient.Net.Models;
using LB1_AP_Mod.Configuration;
using Newtonsoft.Json.Linq;
using Archipelago.MultiClient.Net.Converters;

namespace LB1_AP_Mod;

public class ArchipelagoHandler
{
    private const string GAME_NAME = "Lego Batman The Video Game";
    private ArchipelagoSession? _session;
    public ArchipelagoSession Session => _session ?? throw new InvalidOperationException("Session has not been initialized.");
    private LoginSuccessful? _loginSuccessful;
    // public SlotData? SlotDataInstance;
    // public static DateTime LastDeathLinkPacketTime = DateTime.Now;
    private string Server { get; set; }
    private int Port { get; set; }
    private string Slot { get; set; }
    private string? Seed { get; set; }
    private string Password { get; set; }
    public Dictionary<long, ScoutedItemInfo>? ScoutedLocations { get; private set; }

    public static bool IsConnected;
    public static bool IsConnecting;
    public static int gameOffset = 400000;

    private Thread? _locationsThread;
    private Thread? _hintThread;
    private Thread? _queuedItemsThread;

    public ArchipelagoHandler(string server, int port, string slot, string password)
    {
        Server = server;
        Port = port;
        Slot = slot;
        Password = password;
        CreateSession();
    }

    [MemberNotNull(nameof(_session))]
    private void CreateSession()
    {
        _session = ArchipelagoSessionFactory.CreateSession(Server, Port);
        // _session.MessageLog.OnMessageReceived += OnMessageReceived;
        _session.Socket.SocketClosed += OnSocketClosed;
        _session.Socket.ErrorReceived += OnSessionErrorReceived;
        // _session.Socket.PacketReceived += PacketReceived;
        // _session.Items.ItemReceived += ItemReceived;
    }

    public void InitConnect()
    {
        IsConnecting = true;
        CreateSession();
        IsConnected = Connect();
        IsConnecting = false;
    }

    // Tells the game what we want done when the connection is closed
    private void OnSocketClosed(string reason = "")
    {
        Mod.PrintToLog($"Connection closed ({reason}) Attempting reconnect...");
        Mod.PrintToLog("Socket Closed");
        Disconnect();
    }

    // Tells the game what we want done when there is a session error
    private void OnSessionErrorReceived(Exception e, string message)
    {
        Mod.PrintToLog($"Connection error ({message}).");
        Mod.PrintToLog($"Session Error: {e.GetBaseException().Message}");
        Disconnect();
    }


    // something we wrong or we need to properly disconnect from the server. cleanup and re null our session
    public void Disconnect()
    {
        Mod.PrintToLog("Disconnected from server and attempting to reconnect. If you close the application, checks will not be sent.");
        _ = _session?.Socket.DisconnectAsync();
        _session = null;
        IsConnected = false;
    }

    // Attempts to connect to the server
    private bool Connect()
    {
        LoginResult result;

        try
        {
            // Check to see if Game/Menu is loaded before trying to connect, we do this to mitigate impact of null values and the game changing things later.
            Game.IsGameLoaded();
            // HintSystem.UpdateConnectionStatusText("Connecting. Please wait");
            Seed = Session.ConnectAsync()?.Result?.SeedName;
            Mod.PrintToLog(Seed + Slot);

            // string[] tags = Mod.Configuration?.ArchipelagoOptions.DeathLink == Config.DeathLinkTag.On
            //     ? ["DeathLink"]
            //     : Array.Empty<string>();

            result = Session.LoginAsync(
                game: GAME_NAME,
                name: Slot,
                itemsHandlingFlags: ItemsHandlingFlags.AllItems,
                version: new Version(1, 0, 0),
                tags: [],
                password: Password
            ).Result;
        }
        catch (Exception e)
        {
            result = new LoginFailure(e.GetBaseException().Message);
            // HintSystem.UpdateConnectionStatusText("Failed To Connect");
        }

        if (result.Successful)
        {
            _loginSuccessful = (LoginSuccessful)result;
            // Sets up slot Data
            // SlotDataInstance = new(_loginSuccessful.SlotData);
            // SlotDataInstance.PrintData();
            // var locationIDs = BuildLocationIds();
            // var scouting = Session.Locations.ScoutLocationsAsync(HintCreationPolicy.None, locationIDs);
            // HintSystem.UpdateConnectionStatusText("Hooking, Please Wait");
            // Modify the game now that we are connected
            bool isHooked = Mod.InitOnMenu();
            // Store our slot name in game to access later
            // Mod.GameInstance!.PlayerName = Slot;
            // Tell DataStorage we are on the menu
            // Session.DataStorage[Scope.Slot, "map"] = 402;
            // if (isHooked)
            // {
            //     HintSystem.UpdateConnectionStatusText("Ready to Play");
            // }
            // else
            // {
            //     HintSystem.UpdateConnectionStatusText("Failed To Hook");
            // }
            // EnsureBackgroundThreads();
            // scouting.Wait();
            // ScoutedLocations = scouting.Result;
            //resync here
            return true;
        }
        var failure = (LoginFailure)result;
        var errorMessage = $"Failed to Connect to {Server}:{Port} as {Slot}";
        errorMessage = failure.Errors.Aggregate(errorMessage, (current, error) => current + $"\n    {error}");
        errorMessage = failure.ErrorCodes.Aggregate(errorMessage, (current, error) => current + $"\n    {error}");
        Mod.PrintToLog(errorMessage);
        Mod.PrintToLog($"Attempting reconnect...");
        return false;
    }
}