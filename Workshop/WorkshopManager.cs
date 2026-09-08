using System;
using System.Collections.Generic;
using System.Linq;
using Architect.Editor;
using Architect.Storage;
using Architect.Workshop.Config;
using Architect.Workshop.Items;
using Architect.Workshop.Types;
using UnityEngine;

namespace Architect.Workshop;

public static class WorkshopManager
{
    public static WorkshopData WorkshopData;

    public static readonly Dictionary<string, (Vector2, Func<string, WorkshopItem>)> WorkshopItems = [];

    public static void Init()
    {
        SceneGroup.Init();
        Register<SceneGroup>("Scene Group",
            new Vector2(-300, -225),
            ConfigGroup.SceneGroup,
            ConfigGroup.SceneGroupIcon,
            ConfigGroup.SceneGroupMap,
            ConfigGroup.SceneGroupMapPos,
            ConfigGroup.SceneGroupMapDirIn,
            ConfigGroup.SceneGroupMapDirOut);
        
        Register<CustomScene>("Scene",
            new Vector2(-100, -225),
            ConfigGroup.Scene,
            ConfigGroup.SceneMap);
        
        CustomCharm.Init();
        Register<CustomCharm>("Charm",
            new Vector2(-300, -262.5f),
            ConfigGroup.SpriteItem,
            ConfigGroup.Charm);
        
        CustomItem.Init();
        Register<CustomItem>("Item",
            new Vector2(-100, -262.5f),
            ConfigGroup.SpriteItem,
            ConfigGroup.Item);
        
        Register<CustomCue>("Audio Cue",
            new Vector2(-300, -300),
            ConfigGroup.Cue);
        
        CustomMenuStyle.Init();
        Register<CustomMenuStyle>("Menu Style",
            new Vector2(-300, -337.5f),
            ConfigGroup.MenuStyle);
    }

    private static readonly List<WorkshopData> ExtWorkshops = [];
    
    public static void Setup()
    {
        StorageManager.LoadWorkshopData();
        foreach (var item in ExtWorkshops.SelectMany(data => data.Items))
        {
            if (WorkshopData.Items.Any(i => i.Id == item.Id)) continue;
            foreach (var cfg in item.CurrentConfig.Values) cfg.Setup(item);
            item.Register();
        }
    }

    public static void LoadExtWorkshop(WorkshopData data)
    {
        ExtWorkshops.Add(data);
    }

    public static void LoadWorkshop(WorkshopData data)
    {
        data ??= new WorkshopData();
        if (WorkshopData != null) foreach (var item in WorkshopData.Items) item.Unregister();

        WorkshopData = data;
        foreach (var item in WorkshopData.Items)
        {
            foreach (var cfg in item.CurrentConfig.Values) cfg.Setup(item);
            item.Register();
        }
        WorkshopUI.Refresh();
    }
    
    private static void Register<T>(string type, Vector2 pos, params List<ConfigType>[] config) where T : WorkshopItem, new()
    {
        pos.y += 5;
        WorkshopItems[type] = (pos, s => new T
        {
            Id = s,
            Type = type,
            Config = config,
            CurrentConfig = []
        });
    }
}