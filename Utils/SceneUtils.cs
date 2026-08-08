using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Architect.Api;
using USceneManager = UnityEngine.SceneManagement.SceneManager;
using Architect.Behaviour.Utility;
using Architect.Content.Preloads;
using Architect.Placements;
using Architect.Storage;
using Architect.Workshop.Items;
using GlobalEnums;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;
using tk2dRuntime.TileMap;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Architect.Utils;

public static class SceneUtils
{
    public static GameObject TilemapPrefab;
    private static GameObject _manager;
    private static GameObject _sceneManager;
    private static GameObject _gradeMarker;
    private static GameObject _borderPrefab;

    public static tk2dTileMap Tilemap;

    public static readonly Dictionary<string, CustomScene> CustomScenes = [];
    public static readonly Dictionary<string, SceneGroup> SceneGroups = [];
    
    public static void Init()
    {
        _borderPrefab = new GameObject("[Architect] Border Replacement");
        _borderPrefab.SetActive(false);
        Object.DontDestroyOnLoad(_borderPrefab);
        
        PreloadManager.RegisterPreload(new BasicPreload("Tutorial_01", "_Managers", o =>
        {
            o = Object.Instantiate(o);
            o.SetActive(false);
            Object.DontDestroyOnLoad(o);
            o.name = "[Architect] Managers Preload";
            _manager = o;
        }));
        PreloadManager.RegisterPreload(new BasicPreload("Tutorial_01", "_SceneManager", o =>
        {
            o = Object.Instantiate(o);
            o.SetActive(false);
            Object.DontDestroyOnLoad(o);
            o.name = "[Architect] Scene Manager Preload";
            _sceneManager = o;
        }));
        PreloadManager.RegisterPreload(new BasicPreload("Tutorial_01", "_Markers/GradeMarker", o =>
        {
            o = Object.Instantiate(o);
            o.SetActive(false);
            Object.DontDestroyOnLoad(o);
            o.name = "[Architect] Grade Marker Preload";
            _gradeMarker = o;
        }));
        PreloadManager.RegisterPreload(new BasicPreload("Tutorial_01", "TileMap", o =>
        {
            o = Object.Instantiate(o);
            o.SetActive(false);
            Object.DontDestroyOnLoad(o);
            
            var tm = o.GetComponent<tk2dTileMap>();
            for (var x = 0; x < tm.width; x++)
            {
                for (var y = 0; y < tm.height; y++)
                {
                    tm.ClearTile(x, y, 0);
                }
            }
            
            o.name = "[Architect] Tilemap Preload";
            TilemapPrefab = o;
            
            Object.Destroy(tm.renderData);
        }));
        
        typeof(SceneLoad).Hook(nameof(SceneLoad.BeginRoutine), RedirectLoad);
        
        _ = new ILHook(typeof(SceneLoad)
                .GetMethod(nameof(SceneLoad.BeginRoutine), BindingFlags.NonPublic | BindingFlags.Instance)
                .GetStateMachineTarget(),
            il =>
            {
                var cursor = new ILCursor(il);
                
                // Go to line before trying to load scene
                cursor.GotoNext(
                    MoveType.Before,
                    instr => instr.MatchLdarg(0),
                    instr => instr.MatchLdloc(1),
                    instr => instr.OpCode == OpCodes.Ldfld,
                    instr => instr.MatchLdcI4(1),
                    instr => instr.OpCode == OpCodes.Call
                );
                
                // Mark this point
                var beforeLoad = cursor.MarkLabel();
                
                // Go to the line after trying to load the scene
                cursor.GotoNext(
                    MoveType.After,
                    instr => instr.OpCode == OpCodes.Blt_S
                );
                // Mark this point
                var afterLoad = cursor.MarkLabel();

                // Return to line before loading scene
                cursor.GotoLabel(beforeLoad);
                
                // Skip scene loading if loading a custom scene
                cursor.Emit(OpCodes.Call, typeof(SceneUtils).GetMethod(nameof(Check)));
                cursor.Emit(OpCodes.Brtrue, afterLoad);
                
                // Go to the line before trying to activate the scene
                cursor.GotoNext(
                    MoveType.Before,
                    instr => instr.MatchLdarg(0),
                    instr => instr.OpCode == OpCodes.Ldfld,
                    instr => instr.MatchLdcI4(1)
                );
                
                // Mark this point
                var beforeActivate = cursor.MarkLabel();
                
                // Go to the line after trying to activate the scene
                cursor.GotoNext(
                    MoveType.After,
                    instr => instr.MatchLdarg(0),
                    instr => instr.MatchLdarg(0),
                    instr => instr.OpCode == OpCodes.Ldfld,
                    instr => instr.OpCode == OpCodes.Stfld
                );
                
                // Mark this point
                var afterActivate = cursor.MarkLabel();
                
                // Return to line before activating scene
                cursor.GotoLabel(beforeActivate);
                
                // Skip scene activation if loading a custom scene
                cursor.Emit(OpCodes.Call, typeof(SceneUtils).GetMethod(nameof(Check)));
                cursor.Emit(OpCodes.Brtrue, afterActivate);
            });
        
        typeof(GameManager).Hook(nameof(GameManager.LoadScene),
            (Action<GameManager, string> orig, GameManager self, string destScene) =>
            {
                if (CustomScenes.ContainsKey(destScene))
                {
                    StorageManager.SaveScene(self.sceneName, PlacementManager.GetLevelData());
                    StorageManager.SaveScene(StorageManager.GLOBAL, PlacementManager.GetGlobalData());
                    
                    self.tilemapDirty = true;
                    self.startedOnThisScene = false;
                    self.nextSceneName = destScene;
                    LoadScene(destScene);
                    return;
                }
                orig(self, destScene);
            });
    }

    private static bool _loadingCustomScene;

    public static bool Check()
    {
        return _loadingCustomScene;
    }
    
    private static IEnumerator RedirectLoad(
        Func<SceneLoad, IEnumerator> orig,
        SceneLoad self)
    {
        var o = orig(self);
        
        if (CustomScenes.ContainsKey(self.TargetSceneName))
        {
            _loadingCustomScene = true;
            self.ActivationComplete += Load;
        }
        
        while (o.MoveNext()) yield return o.Current;
        self.ActivationComplete -= Load;
        
        yield break;
        
        void Load()
        {
            _loadingCustomScene = false;
            LoadScene(self.TargetSceneName);
        }
    }

    public static void LoadScene(string sceneName)
    {
        var current = GameManager.instance.sceneName;

        if (current == sceneName)
        {
            // Merges existing scene into temp to avoid name clash
            USceneManager.MergeScenes(USceneManager.GetActiveScene(), USceneManager.CreateScene("Temp"));
            current = "Temp";
        }

        var info = CustomScenes[sceneName];
        
        var scene = USceneManager.CreateScene(sceneName);
        var sm = CreateSceneManager();
        sm.GetComponent<SceneManager>().environmentType = info.Environment;
        
        USceneManager.MoveGameObjectToScene(sm, scene);
        USceneManager.MoveGameObjectToScene(CreateManager(), scene);
        
        USceneManager.SetActiveScene(scene);
        FsmHook.FsmMaster.OnSceneLoaded(scene, LoadSceneMode.Single);
        
        CreateGradeMarker(info.HeroLight, info.AmbientLight, info.Saturation);
        CreateTileMap(info);
        
        sm.AddComponent<CustomTransitionPoint>();
        var point = sm.AddComponent<TransitionPoint>();
        point.nonHazardGate = true;
        point.targetScene = "Town";
        point.entryPoint = "left1";

        var col = sm.AddComponent<BoxCollider2D>();
        col.size = new Vector2(0, 0);
        col.offset = new Vector2(-9999, -9999);
        col.isTrigger = true;
        
        PlayMakerFSM.BroadcastEvent("LEVEL LOADED");

        var osm = GameManager.instance.sm;
        if (osm) osm.tag = "Untagged";
        try
        {
            USceneManager.UnloadSceneAsync(current);
        }
        catch
        {
            //
        }
        try
        {
            USceneManager.UnloadSceneAsync($"{current}_boss");
        }
        catch
        {
            //
        }
    }
    
    private static AtmosCue NoAtmosCue => field ??=
        Resources.FindObjectsOfTypeAll<AtmosCue>().FirstOrDefault(o => o.name == "None");
    
    public static AudioMixerSnapshot AtmosSnapshot => field ??=
        Resources.FindObjectsOfTypeAll<AudioMixerSnapshot>()
            .FirstOrDefault(o => o.name == "at All Layers");
    
    public static GameObject CreateSceneManager()
    {
        var sm = Object.Instantiate(_sceneManager);
        sm.name = "_SceneManager";
        var csm = sm.GetComponent<SceneManager>();
        csm.borderPrefab = _borderPrefab;
        csm.overrideParticlesWith = MapZone.NONE;
        csm.noParticles = true;
        csm.mapZone = MapZone.NONE;
        csm.environmentType = 5;
        csm.actorSnapshot = GameManager.instance.actorSnapshotUnpaused;
        csm.atmosSnapshot = AtmosSnapshot;
        csm.atmosCue = NoAtmosCue;
        csm.musicCue = GameManager.instance.noMusicCue;
        csm.darknessLevel = 0;
        
        sm.AddComponent<HazardRespawnMarker>();
        sm.SetActive(true);
        
        return sm;
    }
    
    public static GameObject CreateManager()
    {
        var m = Object.Instantiate(_manager);
        m.name = "_Managers";
        m.SetActive(true);
        return m;
    }
    
    public static void CreateGradeMarker(Color heroLight, Color ambient, float saturation)
    {
        var m = Object.Instantiate(_gradeMarker);
        var gm = m.GetComponent<GradeMarker>();

        gm.heroLightColor = heroLight;
        gm.ambientColor = ambient;
        gm.saturation = saturation;

        gm.maxIntensityRadius = 0;
        gm.cutoffRadius = float.PositiveInfinity;
        
        m.name = "GradeMarker";
        m.SetActive(true);
    }
    
    public static void CreateTileMap(CustomScene scene)
    {
        var tm = Object.Instantiate(TilemapPrefab);
        tm.name = "TileMap";
        
        Tilemap = tm.GetComponent<tk2dTileMap>();
        
        Tilemap.renderData = null;
        
        Tilemap.width = scene.TilemapWidth;
        Tilemap.height = scene.TilemapHeight;

        Tilemap.layers[0].width = scene.TilemapWidth;
        Tilemap.layers[0].height = scene.TilemapHeight;

        GameManager.instance.tilemap = Tilemap;
        GameManager.instance.sceneWidth = scene.TilemapWidth;
        GameManager.instance.sceneHeight = scene.TilemapHeight;

        Tilemap.layers[0].numColumns = Mathf.CeilToInt(scene.TilemapWidth / 32f);
        Tilemap.layers[0].numRows = Mathf.CeilToInt(scene.TilemapHeight / 32f);

        var nsc = new List<SpriteChunk>();
        for (var row = 0; row < Tilemap.layers[0].numRows; row++)
        {
            for (var col = 0; col < Tilemap.layers[0].numColumns; col++)
            {
                nsc.Add(new SpriteChunk());
            }
        }

        Tilemap.layers[0].gameObject = null;
        Tilemap.layers[0].spriteChannel.chunks = nsc.ToArray();
        
        var ext = MapLoader.GetModData(scene.Id);
        
        if (ext != null && !ext.TilemapChanges.IsNullOrEmpty())
        {
            foreach (var (x, y) in ext.TilemapChanges)
            {
                try
                {
                    if (Tilemap.GetTile(x, y, 0) == -1) Tilemap.SetTile(x, y, 0, 0);
                    else Tilemap.ClearTile(x, y, 0);
                }
                catch (Exception)
                {
                    // Out of bounds
                }
            }
        }

        var ld = PlacementManager.GetLevelData();
        if (!ld.TilemapChanges.IsNullOrEmpty()) foreach (var (x, y) in ld.TilemapChanges)
        {
            try
            {
                if (Tilemap.GetTile(x, y, 0) == -1) Tilemap.SetTile(x, y, 0, 0);
                else Tilemap.ClearTile(x, y, 0);
            }
            catch (Exception)
            {
                // Out of bounds
            }
        }
        
        tm.SetActive(true);
        Tilemap.Build();
    }
}