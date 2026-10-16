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
    public SlotData? SlotDataInstance;
    // public static DateTime LastDeathLinkPacketTime = DateTime.Now;
    private string Server { get; set; }
    private int Port { get; set; }
    private string Slot { get; set; }
    private string? Seed { get; set; }
    private string Password { get; set; }
    public Dictionary<long, ScoutedItemInfo>? ScoutedLocations { get; private set; }

    public static bool IsConnected;
    public static bool IsConnecting;
    public static readonly int gameOffset = 400000;

    private Thread? _locationsThread;
    private Thread? _hintThread;
    // private Thread? _queuedItemsThread;

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
        _session.MessageLog.OnMessageReceived += OnMessageReceived;
        _session.Socket.SocketClosed += OnSocketClosed;
        _session.Socket.ErrorReceived += OnSessionErrorReceived;
        // _session.Socket.PacketReceived += PacketReceived;
        _session.Items.ItemReceived += ItemReceived;
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


    // Something went wrong or we need to properly disconnect from the server. Cleanup and re null our session
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
            HintSystem.UpdateConnectionStatusText("Connecting. Please wait");
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
            HintSystem.UpdateConnectionStatusText("Failed To Connect");
        }

        if (result.Successful)
        {
            _loginSuccessful = (LoginSuccessful)result;
            // Sets up slot Data
            SlotDataInstance = new(_loginSuccessful.SlotData);
            SlotDataInstance.PrintData();
            var locationIDs = BuildLocationIds();
            var scouting = Session.Locations.ScoutLocationsAsync(HintCreationPolicy.None, locationIDs);
            HintSystem.UpdateConnectionStatusText("Hooking, Please Wait");
            // Modify the game now that we are connected
            bool isHooked = Mod.InitOnMenu();
            // Store our slot name in game to access later
            // Mod.GameInstance!.PlayerName = Slot;
            if (isHooked)
            {
                HintSystem.UpdateConnectionStatusText("Connected. Ready to Play");
            }
            else
            {
                HintSystem.UpdateConnectionStatusText("Failed To Hook");
            }
            EnsureBackgroundThreads();
            scouting.Wait();
            ScoutedLocations = scouting.Result;
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

    // Function to build an array of all location IDs in the game for scouting purposes. 
    private static long[] BuildLocationIds()
    {
        var ids = new List<long>();
        ids.AddRange(Enumerable.Range(400000, Game.MaxItemID).Select(i => (long)i));
        return [.. ids];
    }

    /* 
    Tells archi want we want to do when an item is received. 
    Majority of items besides the groupings in this function are given to the player when they sync up with the "item" state or "location" state. 
    However, there are a few we want the player to have instantaneously so we handle those instances here.
    */
    private void ItemReceived(ReceivedItemsHelper helper)
    {
        // while (helper.Any())
        // {
        // var item = helper.DequeueItem();
        // int gameID = (int)item.ItemId - gameOffset;
        //     bool prevInShop = false, prevInLevelSelect = false;

        // Verify if the player is in a shop or in the level selector since we give them certain items instantly if they are
        // if (Mod.GameInstance != null)
        // {
        //     lock (Mod.GameInstance.StateLock)
        //     {
        //         prevInShop = Mod.GameInstance.PrevInShop;
        //         prevInLevelSelect = Mod.GameInstance.PrevInLevelSelect;
        //     }
        // }

        // if (Mod.GameInstance != null && prevInShop && gameID != 699)
        // {
        //     // Token or red brick purchasable
        //     if ((gameID >= 900 && gameID <= 935) || (gameID >= 213 && gameID <= 425))
        //     {
        //         Game.ManageItem(gameID);
        //     }
        //     return;
        // }
        // If in level select, we give them any item but purple stud
        // if (Mod.GameInstance != null && prevInLevelSelect && gameID != 699)
        // {
        //     Game.ManageItem(gameID);
        //     return;
        // }
        // If the player is controllable, we give them purple studs, any spells/abilities, and Horcruxes
        // if (Mod.GameInstance != null && Game.IsPlayerControllable())
        // {
        //     // Handle Spells/Abilities
        //     if (gameID >= Game.SpellPurchOffset)
        //     {
        //         Game.ManageItem(gameID);
        //         int mapID;
        //         lock (Mod.GameInstance!.MapLock)
        //         {
        //             mapID = Mod.GameInstance!.MapID;
        //         }
        //         SpellHandler.SpellMapLogic(mapID);
        //         return;
        //     }
        // Handle Purple Studs
        // if (gameID == 699)
        // {
        //     HubHandler.HandlePurpleStud();
        //     return;
        // }
        // }
        // Handle Purple Stud if player isn't controllable (could be in the menu)
        // if (Mod.GameInstance != null && !Game.IsPlayerControllable() && gameID == 699)
        // {
        //     _queuedItems.Enqueue(gameID);
        //     return;
        // }
        // }
    }

    // Once Goal is completed, this function is called
    public void Release()
    {
        Session.SetGoalAchieved();
    }

    // This is the function we use to send completed locations. Pass the ID to this function and it will be marked complete.
    public void CheckLocation(Int64 id)
    {
        _locationsToCheck.Enqueue(id + gameOffset);
    }

    private ConcurrentQueue<Int64> _locationsToCheck = new();
    private ConcurrentQueue<Int64> _queuedItems = new();
    private readonly object _locationsLock = new();
    private readonly object _itemsLock = new();

    // This functions is a separate thread that will tell the server that our player has complete their location checks. Done in a separate thread to keep the game running and not relying on the server.
    public void RunCheckLocationsFromList()
    {
        while (true)
        {
            if (_locationsToCheck.TryPeek(out _))
            {
                if (IsConnected && _session != null)
                {
                    if (_locationsToCheck.TryDequeue(out long locationId))
                    {
                        Session.Locations.CompleteLocationChecks(locationId);
                    }
                }
                else
                {
                    Thread.Sleep(100);
                }
            }
            else
            {
                Thread.Sleep(100);
            }
        }
    }

    // This function is to ensure that our threads are active and running. Set up like this in case there is a disconnect and reconnect to the server.
    private void EnsureBackgroundThreads()
    {
        if (_locationsThread == null || !_locationsThread.IsAlive)
        {
            _locationsThread = new Thread(RunCheckLocationsFromList)
            {
                IsBackground = true,
                Name = "LocationThread"
            };
            _locationsThread.Start();
        }

        if (_hintThread == null || !_hintThread.IsAlive)
        {
            _hintThread = new Thread(HintSystem.HandleMessages)
            {
                IsBackground = true,
                Name = "HintThread"
            };
            _hintThread.Start();
        }

        // if (_queuedItemsThread == null || !_queuedItemsThread.IsAlive)
        // {
        //     _queuedItemsThread = new Thread(HandleQueuedItems)
        //     {
        //         IsBackground = true
        //     };
        //     _queuedItemsThread.Start();
        // }
    }

    // If there are instantenous items (like purple studs and in the future traps) that we queued up because the player wasn't controllable, this is a separate thread that waits until the player is controllable to pass them the item. 
    public void HandleQueuedItems()
    {
        // while (true)
        // {
        //     if (_queuedItems.TryPeek(out var itemId))
        //     {
        //         if (Mod.GameInstance != null && Game.IsPlayerControllable())
        //         {
        //             if (_queuedItems.TryDequeue(out itemId))
        //             {
        //                 // Console.WriteLine($"Handling queued item with game ID {itemId}");
        //                 if (itemId == 699)
        //                 {
        //                     HubHandler.HandlePurpleStud();
        //                 }
        //             }
        //         }
        //         else
        //         {
        //             Thread.Sleep(100);
        //         }
        //     }
        //     else
        //     {
        //         Thread.Sleep(100);
        //     }
        // }
    }

    // Helper function to verify if a location has already been completed (includes if it was collected by server)
    public bool IsLocationChecked(Int64 id)
    {
        lock (_locationsLock)
        {
            return Session.Locations.AllLocationsChecked.Contains(id + gameOffset);
        }
    }

    /*
    This archi is designed to sync the game state to either the items received state, the locations completed state, or a mixture of both.
    These following functions take the range of archi IDs as parameters and will update the game state to match whether the item has been received or the location checked.
    */

    // Updates Game state to Items
    public void UpdateBasedOnItems(Int64 minItemId, Int64 maxItemId)
    {
        lock (_itemsLock)
        {
            foreach (var item in Session.Items.AllItemsReceived)
            {
                // Only process items whose AP item ID falls within the desired range
                if (item.ItemId - gameOffset < minItemId || item.ItemId - gameOffset > maxItemId)
                    continue;

                var gameId = item.ItemId - gameOffset;
                Game.ManageItem((int)gameId);
            }
        }
    }

    // Updates Game state to locations
    public void UpdateBasedOnLocations(Int64 minLocationId, Int64 maxLocationId)
    {
        lock (_locationsLock)
        {
            foreach (var location in Session.Locations.AllLocationsChecked)
            {
                // Only handle locations within the desired ID range
                if (location - gameOffset < minLocationId || location - gameOffset > maxLocationId)
                    continue;

                var gameId = location - gameOffset;
                Game.ManageItem((int)gameId);
            }
        }
    }

    // Helper Function to count how many items inbetween a certain archi ID range have been received
    public int CountItemsReceivedInRange(Int64 start, Int64 end)
    {
        lock (_itemsLock)
        {
            var startId = start + gameOffset;
            var endId = end + gameOffset;
            return Session.Items.AllItemsReceived.Count(item => item.ItemId >= startId && item.ItemId <= endId);
        }
    }

    // Helper Function to count how many locations inbetween a certain archi ID range have been checked
    public int CountLocationsCheckedInRange(Int64 start, Int64 end)
    {
        lock (_locationsLock)
        {
            var startId = start + gameOffset;
            var endId = end + gameOffset;
            return Session.Locations.AllLocationsChecked.Count(location => location >= startId && location <= endId);
        }
    }

    // Helper Function to count how many items with a specific archi ID (i.e.  purple stud) have been received
    public int CountItemsReceivedWithId(Int64 gameId)
    {
        lock (_itemsLock)
        {
            var targetId = gameId + gameOffset;
            return Session.Items.AllItemsReceived.Count(item => item.ItemId == targetId);
        }
    }

    // We pass the Map ID to data storage for auto tracking purposes
    public void SendMapID(int MapID)
    {
        Session.DataStorage[Scope.Slot, "map"] = MapID;
    }

    // Helper function to help determine what type of item (progression/Trap/Filler) was sent/received. Used to determine the in game text color.
    private static byte GetItemFlag(ItemInfo item)
    {
        Mod.PrintToLog($"Item flags are: {item.Flags}");
        if ((item.Flags & ItemFlags.Trap) == ItemFlags.Trap)
        {
            return 0; // 0 is Red
        }
        if ((item.Flags & ItemFlags.Advancement) == ItemFlags.Advancement)
        {
            return 3; // 3 is Purple
        }
        if ((item.Flags & ItemFlags.NeverExclude) == ItemFlags.NeverExclude)
        {
            return 8; // 8 is Dark Blue
        }
        if ((item.Flags & ItemFlags.None) == ItemFlags.None)
        {
            return 2; // 2 is Light Blue
        }
        return 5; // Default flag which is gray
    }

    // Helper Function to handle Messages received from the server based on their type (hint/Item/Goal/Cheat)
    private static void OnMessageReceived(LogMessage message)
    {
        byte itemFlag;
        switch (message)
        {
            case HintItemSendLogMessage hintMessage:
                Mod.PrintToLog($"Hint Message Received: {hintMessage.ToString() ?? string.Empty}");
                itemFlag = GetItemFlag(hintMessage.Item);
                string hntmsg = hintMessage.ToString() ?? string.Empty;

                if (!hintMessage.IsRelatedToActivePlayer)
                {
                    Mod.PrintToLog("Hint not related to active player, skipping");
                    return;
                }

                HintSystem.EnqueueMessage(hntmsg, itemFlag);
                break;
            case ItemSendLogMessage itemMessage:
                Mod.PrintToLog($"Item Message Received: {itemMessage.ToString() ?? string.Empty}");
                itemFlag = GetItemFlag(itemMessage.Item);
                string itmmsg = itemMessage.ToString() ?? string.Empty;

                if (!itemMessage.IsRelatedToActivePlayer)
                {
                    Mod.PrintToLog($"Item not related to active player, skipping");
                    return;
                }

                HintSystem.EnqueueMessage(itmmsg, itemFlag);
                break;
            case GoalLogMessage goalLogMessage:
                HintSystem.EnqueueMessage(goalLogMessage.ToString(), 3);
                break;
            default:
                /* 
                Printing all messages in case it breaks something, but don't think that will be the case.
                Prints the message type too in case there is something we want to print in the future
                */
                Mod.PrintToLog($"{message.GetType().Name} Received: {message.ToString() ?? string.Empty}");
                // If it is a Cheat Console message, we want to print that out too. The message type in the library doesn't seem to work so we do a string search.
                bool isCheatMessage = message.ToString() != null && message.ToString().Contains("Cheat console");
                if (isCheatMessage)
                {
                    HintSystem.EnqueueMessage(message.ToString(), 3);
                }
                break;
        }
    }

    // Saving for death link implementation later. used to update archi tags
    // public void UpdateTags(string[] tags)
    // {
    //     if (!IsConnected || _session == null)
    //     {
    //         Mod.PrintToLog("Not connected to Archipelago server. Cannot update tags.");
    //         return;
    //     }
    //     var packet = new ConnectUpdatePacket
    //     {
    //         Tags = tags,
    //         ItemsHandling = ItemsHandlingFlags.AllItems
    //     };
    //     _session.Socket.SendPacket(packet);
    // }
}