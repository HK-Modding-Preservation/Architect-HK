using System.Collections.Generic;
using System.IO;
using System.Linq;
using Architect.Config.Types;
using Architect.Objects.Categories;
using Architect.Placements;
using Architect.Storage;
using Newtonsoft.Json;
using Satchel.BetterMenus;
using UnityEngine;
using ConfigurationManager = Architect.Config.ConfigurationManager;

namespace Architect.Updater;

public static class DcmPorter
{
    // Register config types
    public static void Init()
    {
        DcmSawMovement.Init();
        
        ConfigurationManager.RegisterConfigType(
            new FloatConfigType("Colour R", "colour_red", (o, value) =>
            {
                var sr = o.GetComponent<SpriteRenderer>();
                var color = sr.color;
                color.r = value.GetValue();
                sr.color = color;
            }).WithDefaultValue(1));
        ConfigurationManager.RegisterConfigType(
            new FloatConfigType("Colour G", "colour_green", (o, value) =>
            {
                var sr = o.GetComponent<SpriteRenderer>();
                var color = sr.color;
                color.g = value.GetValue();
                sr.color = color;
            }).WithDefaultValue(1));
        ConfigurationManager.RegisterConfigType(
            new FloatConfigType("Colour B", "colour_blue", (o, value) =>
            {
                var sr = o.GetComponent<SpriteRenderer>();
                var color = sr.color;
                color.b = value.GetValue();
                sr.color = color;
            }).WithDefaultValue(1));
        
        ConfigurationManager.RegisterConfigType(
            new FloatConfigType("Velocity X", "particles_velocity_x",
                (o, value) =>
                {
                    var val = value.GetValue();

                    o.ApplyToAllComponents<ParticleSystem>(ps =>
                    {
                        var vol = ps.velocityOverLifetime;
                        vol.enabled = true;
                        vol.x = val;
                        var fol = ps.forceOverLifetime;
                        fol.enabled = false;
                    });
                }).WithPriority(-1));
        
        ConfigurationManager.RegisterConfigType(
            new FloatConfigType("Scale X", "object_scale_x",
                (o, value) =>
                {
                    o.transform.SetScaleX(o.transform.GetScaleX() * value.GetValue());
                }));
        ConfigurationManager.RegisterConfigType(
            new FloatConfigType("Scale Y", "object_scale_y",
                (o, value) =>
                {
                    o.transform.SetScaleY(o.transform.GetScaleY() * value.GetValue());
                }));
    }

    public static void Port()
    {
        Port(Path.Combine(StorageManager.DataPath, "DecorationMasterData"), true);
    }

    private static void Port(string dmdPath, bool delete)
    {
        if (!Directory.Exists(dmdPath)) return;
        
        ArchitectPlugin.Instance.Log("DcM Data found");
        
        ArchitectPlugin.Instance.Log("Saving DcM object prefabs");
        string[] prefabs = ["Line", "RespawnPlat", "Sporg", "Wall", "Conveyor", "Spikes", "Jarcol", "Lever", "Gate", "ZoteMachine", "ZoteGate", "Stomper", "StomperLever", "Tp", "Twinkle", "Plat", "MarioPlat", "Dash", "Spirit", "Dive", "Wraiths", "Pogo", "Claw", "Wings", "Cdash", "Mana", "Piano", "ManaMachine", "ManaGate"];
        foreach (var s in prefabs)
        {
            var path = Path.Combine(StorageManager.DataPath, "Prefabs", $"Prefab_DCM_{s}.architect.json");
            if (File.Exists(path)) File.Delete(path);
            File.WriteAllText(path, ResourceUtils.LoadTextResource($"Updater.Prefab_DCM_{s}.architect.json"));
        }
        string[] salPrefabs = ["Totem", "Bumper", "Coin", "Gate"];
        foreach (var s in salPrefabs)
        {
            var path = Path.Combine(StorageManager.DataPath, "Prefabs", $"Prefab_SaL_{s}.architect.json");
            if (File.Exists(path)) File.Delete(path);
            File.WriteAllText(path, ResourceUtils.LoadTextResource($"Updater.Prefab_SaL_{s}.architect.json"));
        }
        PrefabsCategory.Prefabs = StorageManager.LoadPrefabs(StorageManager.DataPath);
        
        ArchitectPlugin.Instance.Log("Saving DcM mana item");

        var wPath = Path.Combine(StorageManager.DataPath, "workshop.json");
        if (File.Exists(wPath)) File.Delete(wPath);
        File.WriteAllText(wPath, ResourceUtils.LoadTextResource("Updater.workshop.json"));
        
        ArchitectPlugin.Instance.Log("Attempting to port data");

        var converter = new DcmObject.DcmObjectConverter();

        var globalPath = Path.Combine(dmdPath, "global.json");

        Dictionary<string, List<DcmObject>> globals = [];
        if (File.Exists(globalPath))
        {
            var text = File.ReadAllText(globalPath);
            var dcmObjects = JsonConvert.DeserializeObject<List<DcmObject>>(text, converter)
                .Where(d => d != null).ToArray();
            Dictionary<string, string> sceneLookup = [];
            foreach (var o in dcmObjects)
            {
                sceneLookup[o.GetConfig("Identity")] = o.GetConfig("sceneName");
            }
            foreach (var o in dcmObjects)
            {
                o.SetConfig("DestinationScene", sceneLookup[o.GetConfig("Destination")]);
                var sn = o.GetConfig("sceneName");
                if (!globals.TryGetValue(sn, out var v)) v = globals[sn] = [];
                v.Add(o);
                
                Dictionary<string, string> d = [];
                d["door_id"] = o.GetConfig("Identity");
                d["trigger_type"] = "2";
                v.Add(new DcmObject(
                    DcmObject.DcmObjectConverter.GetObjectType("transgate"),
                    0,
                    1,
                    d,
                    o.Pos));
            }
        }
        
        foreach (var file in Directory.GetFiles(dmdPath))
        {
            if (!file.EndsWith(".json") || file.StartsWith("global")) continue;
            var text = File.ReadAllText(file);
            var dcmObjects = JsonConvert.DeserializeObject<List<DcmObject>>(text, converter)
                .Where(d => d != null);

            var scene = Path.GetFileNameWithoutExtension(file);

            if (globals.TryGetValue(scene, out var objects))
            {
                globals.Remove(scene);
                dcmObjects = objects.Aggregate(dcmObjects, (current, o) => current.Append(o));
            }
            
            var ld = new LevelData(dcmObjects.Select(d => d.GetPlacement()).ToList(), 
                [], [], []);
            StorageManager.SaveScene(scene, ld);
        }

        foreach (var (scene, objects) in globals)
        {
            var ld = new LevelData(objects.Select(d => d.GetPlacement()).ToList(), 
                [], [], []);
            StorageManager.SaveScene(scene, ld);
        }

        if (delete)
        {
            ArchitectPlugin.Instance.Log("Deleting old data");
            Directory.Delete(dmdPath, true);
        }

        ArchitectPlugin.Instance.Log("Porting complete");
    }
    
    private static readonly List<Element> Elements = [];

    public static MenuScreen GetMenuScreen(MenuScreen returnMenu)
    {
        Elements.Clear();
        var current = "";
        Elements.Add(new InputField("DcM Map Path", s => current = s, () => "", "", int.MaxValue));

        Elements.Add(new MenuButton("Load Map", "Will wipe your current map", _ =>
        {
            StorageManager.WipeLevelData();
            Port(current, false);
            StorageManager.LateLoad();
            StorageManager.LoadWorkshopData();
        }));
        
        return new Menu(
            name: "DcM Updater",
            elements: Elements.ToArray()
        ).GetMenuScreen(returnMenu);
    }
}