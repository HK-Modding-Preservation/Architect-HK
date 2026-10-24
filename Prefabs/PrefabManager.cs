using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Architect.Behaviour.Utility;
using Architect.Editor;
using Architect.Events.Blocks;
using Architect.Objects.Categories;
using Architect.Placements;
using tk2dRuntime.TileMap;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Architect.Prefabs;

public static class PrefabManager
{
    public static readonly Dictionary<string, LevelData> Prefabs = [];
    public static bool InPrefabScene;
    public static string Last;

    public static readonly Sprite PrefabIcon = ResourceUtils.LoadSpriteResource("prefab", ppu: 256);
    
    private static string _oldScene;
    private static Vector3 _oldPos;

    public static void Init()
    {
        PrefabObject.Init();
        
        typeof(HeroController).Hook(nameof(HeroController.SceneInit),
            (Action<HeroController> orig, HeroController self) =>
            {
                orig(self);

                if (GameManager.instance.sceneName.StartsWith("Prefab_")) return;
                InPrefabScene = false;
                ScriptEditorUI.ToggleParent.SetActive(true);
            });
    }
    
    public static void Toggle(string prefabName)
    {
        var pNames = PrefabsCategory.Prefabs.Select(p => p.Name).ToArray();
        if (pNames.Contains(prefabName, StringComparer.InvariantCultureIgnoreCase))
        {
            prefabName = pNames.First(k => k.Equals(prefabName, StringComparison.InvariantCultureIgnoreCase));
        }
        Last = prefabName;
        if (GameManager.instance.isPaused)
        {
            GameManager.instance.StartCoroutine(GameManager.instance.PauseGameToggle());
        }
        
        InPrefabScene = !InPrefabScene;
        
        GameManager.instance.entryGateName = "";
        if (!InPrefabScene)
        {
            ScriptEditorUI.ToggleParent.SetActive(true);
            EditManager.NoclipPos = _oldPos;
            GameManager.instance.ChangeToScene(_oldScene, "", 0);
            GameCameras.instance.cameraController.SetMode(CameraController.CameraMode.FOLLOWING);
        }
        else
        {
            foreach (var obj in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
            {
                foreach (var gate in obj.GetComponentsInChildren<TransitionPoint>()) 
                    gate.gameObject.SetActive(false);
            }
            ScriptManager.IsLocal = true;
            ScriptEditorUI.ToggleParent.SetActive(false);
            _oldScene = GameManager.instance.sceneName;
            _oldPos = HeroController.instance.transform.position;
            ArchitectPlugin.Instance.StartCoroutine(LoadScene($"Prefab_{prefabName}"));
        }
    }

    private static IEnumerator LoadScene(string sceneName)
    {
        var current = GameManager.instance.sceneName;
        
        var scene = UnityEngine.SceneManagement.SceneManager.CreateScene(sceneName);
        UnityEngine.SceneManagement.SceneManager.SetActiveScene(scene);
        FsmHook.FsmMaster.OnSceneLoaded(scene, LoadSceneMode.Single);

        var unload2 = UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(current);
        if (unload2 != null) while (!unload2.isDone) yield return null;

        var sm = SceneUtils.CreateSceneManager();
        SceneUtils.CreateGradeMarker(Color.white, Color.white, 1, 1);
        
        sm.transform.position = new Vector3(100, 100, 1);
        sm.transform.localScale = Vector3.one;

        var sr = sm.AddComponent<SpriteRenderer>();
        sr.sprite = PrefabIcon;
        
        CreateTilemap();
        
        GameManager.instance.SetupSceneRefs(true);

        EditManager.NoclipPos.x = 100;
        EditManager.NoclipPos.y = 100;
        HeroController.instance.transform.SetPosition2D(new Vector2(100, 100));
        GameCameras.instance.mainCamera.transform.SetPosition2D(100, 100);
        GameCameras.instance.cameraTarget.transform.SetPosition2D(100, 100);
        
        GameCameras.instance.SceneInit();
    }
    
    public static void CreateTilemap()
    {
        var tm = Object.Instantiate(SceneUtils.TilemapPrefab);
        tm.name = "Tilemap";

        SceneUtils.Tilemap = tm.GetComponent<tk2dTileMap>();
        SceneUtils.Tilemap.width = 9999;
        SceneUtils.Tilemap.height = 9999;

        SceneUtils.Tilemap.layers[0].width = 9999;
        SceneUtils.Tilemap.layers[0].height = 9999;

        GameManager.instance.tilemap = SceneUtils.Tilemap;
        GameManager.instance.sceneWidth = 9999;
        GameManager.instance.sceneHeight = 9999;

        SceneUtils.Tilemap.layers[0].numColumns = Mathf.CeilToInt(9999 / 32f);
        SceneUtils.Tilemap.layers[0].numRows = Mathf.CeilToInt(9999 / 32f);

        var nsc = new List<SpriteChunk>();
        for (var row = 0; row < SceneUtils.Tilemap.layers[0].numRows; row++)
        {
            for (var col = 0; col < SceneUtils.Tilemap.layers[0].numColumns; col++)
            {
                nsc.Add(new SpriteChunk());
            }
        }
        SceneUtils.Tilemap.layers[0].spriteChannel.chunks = nsc.ToArray();
    }
}