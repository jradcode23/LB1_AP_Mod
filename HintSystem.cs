using Reloaded.Memory;
using Reloaded.Memory.Interfaces;
using System.Collections.Concurrent;

namespace LB1_AP_Mod;

// We use a record container to hold the hint messages and their associated type (i.e. progression, filler, trap, useful).
public record HintMessage(string Text, byte MessageType);

public class HintSystem
{
    private static readonly ConcurrentQueue<HintMessage> MessageQueue = new();
    // Used a link list for interupted messages so you can add to front of list if it can't be displayed
    private static readonly LinkedList<HintMessage> InterruptedMessageList = new();
    private static readonly object interruptedMessageLock = new();

    // Helper function to add a message to the queue 
    public static void EnqueueMessage(string message, byte messageType = 5)
    {
        if (message.Contains('"'))
        {
            message = message.Replace("\"", string.Empty);
        }
        if (!string.IsNullOrEmpty(message))
        {
            MessageQueue.Enqueue(new HintMessage(message, messageType));
        }
    }

    // Helper function to add a message to the front of the interrupted message queue
    public static void AddInterruptedMessageToFront(string message, byte messageType)
    {
        if (string.IsNullOrEmpty(message))
        {
            return;
        }

        lock (interruptedMessageLock)
        {
            if (!InterruptedMessageList.Any(m => m.Text == message))
            {
                InterruptedMessageList.AddFirst(new HintMessage(message, messageType));
            }
        }
    }

    // Timer for how long hints stay on screen (7.5 seconds)
    private static unsafe float* HintTimerAddress => (float*)(Mod.BaseAddress + 0x6C6F34);
    // Place the hint code in this address to make the respective text appear on screen
    private static unsafe uint* HintCodeInterpreterAddress => (uint*)(Mod.BaseAddress + 0x6C6F24);
    // The byte here determines what color the hint text prints as
    private static unsafe byte* HintColorAdress => (byte*)(Mod.BaseAddress + 0x6C6F29);
    private const uint BatmanAttackCode = 0x93D22C;
    private const uint GrappleCode = 0x93CE04;
    private static unsafe uint* HintMessageBaseAddress => *(uint**)(Mod.BaseAddress + 0x6957F8);
    private static unsafe uint* BatmanAttackHintAddress => *(uint**)(HintMessageBaseAddress + 0x19C);
    private static unsafe uint* BatmanGrappleHintAddress => *(uint**)(HintMessageBaseAddress + 0x191);
    private static unsafe uint* Data1Message => *(uint**)(HintMessageBaseAddress + 0x35C);

    // Limiting the text to 255 characters. Can potentially handle more, but this should be sufficient and prevent unexpected crashes.
    private const int MaxMessageLength = 255;

    // Main function we use (in a separate thread) to print a message on screen
    public static unsafe void HandleMessages()
    {
        while (true)
        {
            // Several checks we run to determine if something can be printed on screen
            bool playerControllable = Game.IsPlayerControllable();
            bool notInShop;
            bool notInMenu;
            bool notInLevelSelect;
            lock (Mod.GameInstance!.StateLock)
            {
                notInShop = Mod.GameInstance!.isShopOpen == false;
                notInMenu = Mod.GameInstance!.isMenuOpen == false;
                notInLevelSelect = Mod.GameInstance!.isLevelSelectOpen == false;
            }

            // Timer for when a story (H1, V1, etc.) name is present on screen
            float* storyNameTimer = (float*)(Mod.BaseAddress + 0x6CB6AC);
            bool hasStoryTimerCleared = *storyNameTimer == 0;

            // This is seems to be greater than 0 when there is a cutscene present
            uint* inLevelCutscene = (uint*)(Mod.BaseAddress + 0x5C5828);
            bool haveCutscenesCleared = *inLevelCutscene == 0;

            // This byte determines whether enemies can attack you at the beginning of YCBOB
            byte* YcbobEnemiesAddress = (byte*)(Mod.BaseAddress + 0x6CB68C);
            bool enemiesCanAttack = *YcbobEnemiesAddress == 0;

            if (playerControllable && notInShop && notInMenu && notInLevelSelect && hasStoryTimerCleared && enemiesCanAttack && haveCutscenesCleared)
            {
                // verify if there is no message currently being printed or if the timer is maxed (at 7.5 seconds)
                if (*HintCodeInterpreterAddress == 0 || *HintTimerAddress >= 7.5f)
                {
                    HintMessage? message = null;
                    lock (interruptedMessageLock)
                    {
                        // If there is a message that was interrupted, we want that to print first
                        if (InterruptedMessageList.Count > 0)
                        {
                            message = InterruptedMessageList.First!.Value;
                            InterruptedMessageList.RemoveFirst();
                        }
                    }
                    // If there wasn't anything in the interrupted queue, try to get something from the message queue
                    if (message == null)
                    {
                        MessageQueue.TryDequeue(out var dequeuedMessage);
                        message = dequeuedMessage;
                    }

                    // If there was something in the message queue, print it out
                    if (message != null)
                    {
                        WriteTextToMemory(message.Text, (uint)BatmanAttackHintAddress); // Set the designated messaged
                        *HintCodeInterpreterAddress = BatmanAttackCode; // Set hint system pointer to our message
                        *HintColorAdress = message.MessageType; // Set Color based on item progression
                        *HintTimerAddress = 0f; // Restart Hint timer, shows for 5 seconds
                    }
                }
                Thread.Sleep(100);
            }
            else
            {
                Thread.Sleep(100);
            }
        }
    }

    /* 
    This function is called when the game removes the hint message from the screen and adds it to the interrupted queue.
    This can happen due to opening a shop, walking through a loading zone, pausing, etc.
    */
    public static unsafe void HandleInterruptedMessage()
    {
        if (*HintTimerAddress > 6f || *HintCodeInterpreterAddress == 0) // If timer is greater than 6 seconds or if there is nothing on screen, we can return
        {
            *HintCodeInterpreterAddress = 0;
            return;
        }

        string currentMessage = new((sbyte*)BatmanAttackHintAddress); // Read the current message from memory
        byte currentMessageType = *HintColorAdress; // Read the message type from memory

        if (string.IsNullOrEmpty(currentMessage))
        {
            Mod.PrintToLog("Message was interrupted, but had a null value");
            return;
        }

        if (currentMessage.Length > MaxMessageLength)
        {
            Mod.PrintToLog("Unexpected Behavior, hint message exceeded max length");
            return;
        }

        if (!string.IsNullOrEmpty(currentMessage))
        {
            AddInterruptedMessageToFront(currentMessage, currentMessageType);
        }
    }

    // Helper function to convert a string to UTF-8 encoded bytes
    public static void WriteTextToMemory(string newText, uint memoryAddress)
    {

        // The game draws UTF-8: accents in player and item names display as they are
        var bytes = System.Text.Encoding.ASCII.GetBytes(newText + '\0');

        // Ensure that our message isn't too large to print.
        if (bytes.Length > MaxMessageLength)
        {
            // keep the terminator, and don't cut a UTF-8 character in half
            int cut = MaxMessageLength - 1;
            while (cut > 0 && (bytes[cut] & 0xC0) == 0x80)
                cut--;
            var full = new byte[cut + 1];
            Array.Copy(bytes, full, cut);
            bytes = full;
        }

        // Write the message directly to memory
        Memory.Instance.SafeWrite(memoryAddress, bytes);
    }

    private static uint OriginalPausedAddress = 0;
    private static unsafe uint* PausedTextBaseAddress => *(uint**)(Mod.BaseAddress + 0x6957F8) + 0x100;

    // // This is a helper function that verifies the count of horcruxes received and updates the on screen text
    public static unsafe void UpdateWinConText()
    {
        if (OriginalPausedAddress == 0)
        {
            OriginalPausedAddress = (uint)*(byte**)PausedTextBaseAddress;
            Mod.PrintToLog($"Set OriginalPausedAddress to: 0x{OriginalPausedAddress:X}");
        }

        *PausedTextBaseAddress = (uint)BatmanGrappleHintAddress;

        if (Mod.LB1_AP!.SlotDataInstance!.EndGoal == 0)
        {
            int minikitCount = MinikitHandler.GetMinikitsReceivedCount();
            DisplayMinikitCount(minikitCount);
        }
        if (Mod.LB1_AP!.SlotDataInstance!.EndGoal == 1)
        {
            byte levelsBeaten = (byte)Mod.LB1_AP!.CountLocationsCheckedInRange(425, 454);
            DisplayLevelsBeaten(levelsBeaten);
        }
    }

    public static unsafe void RestoreWinConText()
    {
        if (OriginalPausedAddress == 0)
        {
            Mod.PrintToLog("Pause Address not set yet. Returning");
            return;
        }
        *PausedTextBaseAddress = OriginalPausedAddress;
    }

    // Helper function to write the received Minikit Count to the pause text on menu
    private static unsafe void DisplayMinikitCount(int count)
    {
        string message = $"Minikits Received: {count}/{Mod.LB1_AP!.SlotDataInstance!.MinikitsToWin}";
        WriteTextToMemory(message, (uint)BatmanGrappleHintAddress);
    }

    // Helper function to write the Levels Beaten to the pause text on menu
    private static unsafe void DisplayLevelsBeaten(byte count)
    {
        string message = $"Levels Beaten: {count}/{Mod.LB1_AP!.SlotDataInstance!.LevelsToWin}";
        WriteTextToMemory(message, (uint)BatmanGrappleHintAddress);
    }

    // Helper Function to write the Ras & Hush progress to Data 1 Message
    public static unsafe void DisplayHushAndRasProgress(int hushProgress, int rasProgress)
    {
        string message = $"Hush progress: {hushProgress}/{Mod.LB1_AP!.SlotDataInstance!.HushUnlockCondition}. Ras Progress: {rasProgress}/{Mod.LB1_AP!.SlotDataInstance!.RasUnlockCondition}";
        WriteTextToMemory(message, (uint)Data1Message);
    }

    // New Game text on the main menu. We show connection status here.
    private static byte[] OriginalNewGameText = new byte[30];
    private static unsafe byte* NewGameTextAddress => *(byte**)(HintMessageBaseAddress + 0xFC);

    public static unsafe void UpdateConnectionStatusText(string status)
    {
        if (OriginalNewGameText[0] == 0)
        {
            for (int i = 0; i < OriginalNewGameText.Length; i++)
            {
                OriginalNewGameText[i] = *(NewGameTextAddress + i);
            }
        }
        WriteTextToMemory(status, (uint)NewGameTextAddress);
    }

    public static unsafe void RestoreNewGameText()
    {
        for (int i = 0; i < OriginalNewGameText.Length; i++)
        {
            *(NewGameTextAddress + i) = OriginalNewGameText[i];
        }
    }

    private static unsafe float* MinikitTimer => (float*)(Mod.BaseAddress + 0x6D10DC);

    public static unsafe void MakeMinikitCountAppear()
    {
        if (MinikitTimer == null)
        {
            Mod.PrintToLog("Cannot Show Minikit Count, Minikit Timer is null");
            return;
        }
        *MinikitTimer = 1;
    }

    public static unsafe void MakeMinikitCountDisappear()
    {
        if (MinikitTimer == null)
        {
            Mod.PrintToLog("Cannot Clear Minikit Count, Minikit Timer is null");
            return;
        }
        if (*MinikitTimer > 0)
        {
            *MinikitTimer = 0;
        }
    }
}