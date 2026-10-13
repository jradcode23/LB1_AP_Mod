namespace LB1_AP_Mod;

public class SlotData(Dictionary<string, object> slotData)
{
    private Dictionary<string, object> SlotDataDictionary { get; set; } = slotData ?? [];
    public int EndGoal => GetIntSlotValue("EndGoal");
    public int MinikitSanity => GetIntSlotValue("MinikitSanity");
    public int MinikitsToWin => GetIntSlotValue("MinikitsToWin");
    public int MinikitGrouping => GetIntSlotValue("MinikitGrouping");
    public int LevelsToWin => GetIntSlotValue("LevelsToWin");
    public int CheaperShops => GetIntSlotValue("cheaper_shops");
    public int ShuffleHushAndRas => GetIntSlotValue("ShuffleHushAndRas");
    public int HushUnlockCondition => GetIntSlotValue("HushUnlockCondition");
    public int RasUnlockCondition => GetIntSlotValue("RasUnlockCondition");
    public int SimpleEpisodeCharacters => GetIntSlotValue("SimpleEpisodeCharacters");

    private int GetIntSlotValue(string key)
    {
        if (SlotDataDictionary?.TryGetValue(key, out var value) != true || value == null)
            return -1;

        if (!int.TryParse(value.ToString(), out int result))
        {
            Mod.PrintToLog($"[SlotData] Failed to parse {key} value '{value}'");
            return -1;
        }

        return result;
    }


    // Helper Function that prints the slot data to console for debugging/verification.
    public void PrintData()
    {
        Mod.PrintToLog("=== Slot Data ===");
        Mod.PrintToLog($"EndGoal: {EndGoal}");
        Mod.PrintToLog($"MinikitSanity: {MinikitSanity}");
        Mod.PrintToLog($"MinikitsToWin: {MinikitsToWin}");
        Mod.PrintToLog($"MinikitGrouping: {MinikitGrouping}");
        Mod.PrintToLog($"LevelsToWin: {LevelsToWin}");
        Mod.PrintToLog($"cheaper_shops: {CheaperShops}");
        Mod.PrintToLog($"ShuffleHushAndRas: {ShuffleHushAndRas}");
        Mod.PrintToLog($"HushUnlockCondition: {HushUnlockCondition}");
        Mod.PrintToLog($"RasUnlockCondition: {RasUnlockCondition}");
        Mod.PrintToLog($"SimpleEpisodeCharacters: {SimpleEpisodeCharacters}");


        if (EndGoal == -1 || MinikitSanity == -1 || MinikitsToWin == -1 || MinikitGrouping == -1 || LevelsToWin == -1 || CheaperShops == -1 || ShuffleHushAndRas == -1 || HushUnlockCondition == -1 || RasUnlockCondition == -1 || SimpleEpisodeCharacters == -1)
        {
            Mod.PrintToLog("One or more values not found or invalid. Please report to the devs.");
        }
    }
}