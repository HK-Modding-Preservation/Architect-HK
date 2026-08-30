using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Architect.Api;
using Architect.Behaviour.Fixers;
using Architect.Content;
using Architect.Content.Custom;
using Architect.Content.Preloads;
using Architect.Editor;
using Architect.Events;
using Architect.Events.Blocks;
using Architect.Events.Blocks.Operators;
using Architect.Multiplayer;
using Architect.Objects.Categories;
using Architect.Placements;
using Architect.Prefabs;
using Architect.Sharer;
using Architect.Storage;
using Architect.Updater;
using Architect.Workshop;
using ItemChanger;
using Newtonsoft.Json;
using Satchel;
using Satchel.BetterMenus;
using UnityEngine;
using Object = UnityEngine.Object;
using SceneUtils = Architect.Utils.SceneUtils;
using Settings = Architect.Storage.Settings;

namespace Architect;

public class ArchitectPlugin : Mod, 
    ILocalSettings<ArchitectData>, 
    IGlobalSettings<GlobalArchitectData>,
    ICustomMenuMod
{
    internal static ArchitectPlugin Instance;

    public static readonly Sprite BlankSprite = ResourceUtils.LoadSpriteResource("blank", ppu:300);

    public static GameObject ArrowPromptNew;
    
    public override List<(string, string)> GetPreloadNames()
    {
        Instance = this;
        
        Log("Architect has loaded!");

        var mo = new GameObject("[Architect] Manager Object");
        Object.DontDestroyOnLoad(mo);
        _manager = mo.AddComponent<Manager>();
        
        SceneUtils.Init();
        PrefabManager.Init();
        
        HookUtils.Init();
        TitleUtils.Init();
        MapUtils.Init();
        
        StorageManager.Init();
        
        MiscFixers.Init();
        EnemyFixers.Init();
        
        Categories.Init();
        
        ActionManager.Init();
        CoopManager.Init();
        
        WorkshopManager.Init();
        ScriptManager.Init();
        EditManager.Init();
        
        VanillaObjects.Init();
        SplineObjects.Init();
        UtilityObjects.Init();
        AbilityObjects.Init();
        MiscObjects.Init();
        ParticleObjects.Init();
        CameraObjects.Init();
        LegacyObjects.Init();
        
        RespawnMarkerManager.Init();
        
        PlacementManager.Init();
        
        BroadcasterHooks.Init();
        
        EditorUI.Setup();
        
        StorageManager.MakeBackup(DateTime.Now.ToString("yy-MM-dd-HH-mm-ss"));
        ProjectManager.Init();

        typeof(GameManager).Hook(nameof(GameManager.ResetSemiPersistentItems),
            (Action<GameManager> orig, GameManager self) =>
            {
                BoolVarBlock.SemiVars.Clear();
                NumVarBlock.SemiVars.Clear();
                StringVarBlock.SemiVars.Clear();
                orig(self);
            });

        PrefabsCategory.Prefabs = StorageManager.LoadPrefabs(StorageManager.DataPath);
        DcmPorter.Init();

        try
        {
            DcmPorter.Port();
        }
        catch
        {
            Log("Error porting DcM map");
        }

        if (Settings.UseMapiPreloads.Value) return PreloadManager.ToPreload.SelectMany(kvp =>
        {
            List<(string, string)> s = [];
            foreach (var (val, _) in kvp.Value) s.Add((kvp.Key, val));
            return s;
        }).Distinct().ToList();
        
        return [("Crossroads_47", "RestBench")];
    }

    public override string GetVersion()
    {
        return Assembly.GetExecutingAssembly().GetName().Version.ToString();
    }
    
    public override void Initialize(Dictionary<string, Dictionary<string, GameObject>> preloadedObjects)
    {
        try
        {
            FindMaps();
        }
        catch
        {
            Log("Could not load external maps");
        }
        
        if (Settings.UseMapiPreloads.Value)
        {
            foreach (var (scene, items) in PreloadManager.ToPreload)
            {
                foreach (var (path, preload) in items)
                {
                    preload.MarkLoaded();
                    if (preloadedObjects[scene].TryGetValue(path, out var obj))
                    {
                        Object.DontDestroyOnLoad(obj);
                        preload.OnPreload(obj);
                    }
                    else preload.OnPreload(null);
                }
            }

            PreloadManager.HasPreloaded = true;
            PreloadingDone();
        } else PreloadManager.DoPreload(true);
        
        SharerManager.Init();
        EditorUI.SetupCategories();

        ModHooks.SavegameLoadHook += _ =>
        {
            ItemChangerMod.CreateSettingsProfile(false, false);
        };

        ArrowPromptNew = preloadedObjects["Crossroads_47"]["RestBench"].LocateMyFSM("Bench Control")
            .GetAction<ShowPromptMarker>("In Range", 0).prefab.Value;
    }

    public static void PreloadingDone()
    {
        WorkshopManager.Setup();
        FavouritesCategory.Favourites = StorageManager.LoadFavourites();
        SavedCategory.Objects = StorageManager.LoadSavedObjects();
    }

    private Manager _manager;
    
    private static void FindMaps()
    {
        var modsPath = Path.GetFullPath(Assembly.GetExecutingAssembly().Location);
        while (!modsPath.EndsWith("Mods"))
        {
            modsPath = Directory.GetParent(modsPath)?.FullName;
            if (modsPath == null) return;
        }
        foreach (var dir in Directory.GetDirectories(
                     modsPath, 
                     "Architect",
                     SearchOption.AllDirectories))
        {
            if (dir == Path.Combine(modsPath, "Architect")) continue;

            var parent = dir;
            var disabled = false;
            while (!parent.EndsWith("Mods"))
            {
                parent = Directory.GetParent(parent)?.FullName;
                if (parent == null || parent.EndsWith("Disabled"))
                {
                    disabled = true;
                    break;
                }
            }

            if (disabled) continue;

            var scenes = Path.Combine(dir, "Scenes");
            if (Directory.Exists(scenes)) foreach (var path in Directory.GetFiles(scenes))
            {
                if (!path.EndsWith(".architect.json")) continue;
                var sceneName = Path.GetFileNameWithoutExtension(path).Replace(".architect", "");
                MapLoader.LoadStandaloneMap(sceneName, path);
            }

            var prefabs = Path.Combine(dir, "Prefabs");
            if (Directory.Exists(prefabs)) foreach (var path in Directory.GetFiles(prefabs))
            {
                if (!path.EndsWith(".architect.json")) continue;
                var sceneName = Path.GetFileNameWithoutExtension(path).Replace(".architect", "");
                MapLoader.LoadStandalonePrefab(sceneName, path);
            }
            
            var assets = Path.Combine(dir, "Assets");
            if (Directory.Exists(assets)) CustomAssetManager.AssetPaths.Add(assets);
            StorageManager.LoadPrefabs(dir);
            
            var workshop = Path.Combine(dir, "workshop.json");
            if (File.Exists(workshop))
            {
                var data = File.ReadAllText(workshop);
                WorkshopManager.LoadExtWorkshop(JsonConvert.DeserializeObject<WorkshopData>(data, Converters.All));
            }

            StorageManager.Directories.Add(dir);
        }
    }

    public void StartCoroutine(IEnumerator routine)
    {
        _manager.StartCoroutine(routine);
    }

    public void StopCoroutine(IEnumerator routine)
    {
        if (routine != null) _manager.StopCoroutine(routine);
    }

    public class Manager : MonoBehaviour
    {
        private void Update()
        {
            if (HeroController.SilentInstance) EditManager.Update();
            CursorManager.Update();
            SharerManager.Update();
            AbilityObjects.Update();
        }
    }

    public ArchitectData SaveData { get; set; } = new();
    public GlobalArchitectData GlobalData { get; set; } = new();
    
    public void OnLoadLocal(ArchitectData s)
    {
        SaveData = s;
    }

    public ArchitectData OnSaveLocal()
    {
        return SaveData;
    }

    public void OnLoadGlobal(GlobalArchitectData s)
    {
        GlobalData = s;
    }
    
    public GlobalArchitectData OnSaveGlobal()
    {
        return GlobalData;
    }
    
    private Menu _menuRef; 

    public MenuScreen GetMenuScreen(MenuScreen modListMenu, ModToggleDelegates? toggleDelegates)
    {
        _menuRef ??= new Menu(
            name: "Architect", 
            elements: Settings.ArchitectBinds.Keybinds
                .Select(o => new KeyBind(o.Code.Name, o.Action))
                .Concat(Settings.MiscConfig)
                .Prepend(Blueprints.NavigateToMenu("DcM Converter", "Converter from DecorationMaster to Architect",
                    () => DcmPorter.GetMenuScreen(_menuRef.GetCachedMenuScreen(modListMenu))))
                .Prepend(Blueprints.NavigateToMenu("Layout", "Configure how Architect's editor UI is structured",
                    () => LayoutManager.GetMenuScreen(_menuRef.GetCachedMenuScreen(modListMenu))))
                .Prepend(Blueprints.NavigateToMenu("Projects", "Open the Architect project manager",
                    () => ProjectManager.GetMenuScreen(_menuRef.GetCachedMenuScreen(modListMenu))))
                .ToArray()
        );
        
        return _menuRef.GetMenuScreen(modListMenu);
    }

    public bool ToggleButtonInsideMenu => false;
}
