using System.Collections.Generic;
using Architect.Storage;
using Modding.Converters;
using Newtonsoft.Json;
using UnityEngine;

namespace Architect;

public class ArchitectData
{
    public static ArchitectData Instance => ArchitectPlugin.Instance.SaveData;

    public Dictionary<string, string> StringVariables = [];
    public Dictionary<string, float> FloatVariables = [];
    public Dictionary<string, bool> BoolVariables = [];

    public HashSet<string> GotCharms = [];
    public HashSet<string> NewCharms = [];
    public HashSet<string> EquippedCharms = [];

    public Dictionary<string, int> CustomItems = [];
}

public class GlobalArchitectData
{
    public static GlobalArchitectData Instance => ArchitectPlugin.Instance.GlobalData;
    
    public string CurrentMap = "";
    public string CurrentMapId = "";

    public Dictionary<string, (List<string>, int)> CherryScores = [];
    public Dictionary<string, (List<string>, int)> GoldCherryScores = [];
    
    public Dictionary<string, string> StringVariables = [];
    public Dictionary<string, float> FloatVariables = [];
    public Dictionary<string, bool> BoolVariables = [];

    public List<string> SavedMapNames = [];

    public Dictionary<int, string> SaveSlotGroups = [];
    
    public Dictionary<string, string> ConfigData = [];

    public string MenuStyle = string.Empty;
    
    [JsonConverter(typeof(PlayerActionSetConverter))]
    public Settings.ArchitectBinds Binds = new();

    public static bool LegacyMode;
    public static int UIMode;
}
