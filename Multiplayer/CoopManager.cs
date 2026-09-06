using System.Collections.Generic;
using Architect.Multiplayer.Hkmp;
using Architect.Placements;
using UnityEngine;
using Settings = Architect.Storage.Settings;

namespace Architect.Multiplayer;

public abstract class CoopManager
{
    public static CoopManager Instance;
    
    public static void Init()
    {
        Instance = new DummyManager();

        if (!Settings.CoopMode.Value) return;
        
        if (ModHooks.GetMod("HKMP") is Mod) HkmpManagerInitializer.Init();
    }

    public abstract string Name { get; }
    
    public abstract bool IsActive();
    
    public abstract void ResetRoom(string room);
    
    public abstract void MoveObjects(string room, List<(string, Vector3)> movements);
    
    public abstract void EraseObjects(string room, List<string> ids);
    
    public abstract void ToggleTiles(string room, List<(int, int)> tiles, bool empty);
    
    public abstract void ToggleLock(string room, string id);
    
    public abstract void PlaceObjects(string room, List<ObjectPlacement> placements);
    
    public abstract void ShareScene(string room, bool scriptOnly, LevelData data);
    
    public abstract void RefreshRoom();
    
    public abstract void ShareEvent(string room, string name);
}