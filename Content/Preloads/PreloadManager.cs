using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Architect.Storage;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.UI;

namespace Architect.Content.Preloads;

public static class PreloadManager
{
    public const string PRELOAD_BUNDLE_NAME = "architect_asset_bundle";
    
    public static bool HasPreloaded;
    public static readonly Dictionary<string, List<(string, IPreload)>> ToPreload = [];
    
    public static readonly Dictionary<string, AssetBundle> Bundles = [];

    private static GameObject _canvasObj;
    private static Text _status;

    public static void RegisterPreload(IPreload obj)
    {
        if (!ToPreload.ContainsKey(obj.Scene)) ToPreload[obj.Scene] = [];
        ToPreload[obj.Scene].Add((obj.Path, obj));
    }
    
    public static void UnregisterPreload(IPreload obj)
    {
        if (ToPreload.TryGetValue(obj.Scene, out var value)) 
            value.Remove((obj.Path, obj));
    }

    public static void DoPreload(bool repack)
    {
        SetupCanvas();
        ArchitectPlugin.Instance.StartCoroutine(Preload(repack));
    }

    private static void SetupCanvas()
    {
        _canvasObj = new GameObject("[Architect] Preload Status");
        Object.DontDestroyOnLoad(_canvasObj);

        _canvasObj.AddComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = _canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        _canvasObj.AddComponent<GraphicRaycaster>();

        UIUtils.MakeImage("Preload BG", _canvasObj, Vector2.zero,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(3000, 3000))
            .sprite = ResourceUtils.LoadSpriteResource("preloader_bg");

        var label = UIUtils.MakeLabel("Progress", _canvasObj, Vector2.zero, new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f));
        label.font = "TrajanPro-Bold";
        label.transform.SetAsLastSibling();
        _status = label.textComponent;
        _status.alignment = TextAnchor.UpperCenter;
        _status.fontSize = 20;
        _status.verticalOverflow = VerticalWrapMode.Overflow;
    }

    public static IEnumerator Repack(string sceneName)
    {
        if (Bundles.TryGetValue(sceneName, out var old)) old.Unload(false);
        
        var preloadJson = $"{{\"{sceneName}\":" +
                          $"{JsonConvert.SerializeObject(ToPreload[sceneName].Select(p => p.Item1))}}}";

        var (bundleData, _) = UnitySceneRepacker.Repack(
            $"{PRELOAD_BUNDLE_NAME}_{sceneName}",
            Preloader.DataPath,
            preloadJson,
            UnitySceneRepacker.Mode.AssetBundle
        );
        
        var op = AssetBundle.LoadFromMemoryAsync(bundleData);
        yield return op;

        Bundles[sceneName] = op.assetBundle;
    }

    private static IEnumerator Preload(bool repack)
    {
        if (repack)
        {
            var sceneCount = ToPreload.Count;
            var i = 0;
            foreach (var sceneName in ToPreload.Keys)
            {
                _status.text = "Repacking Scenes\n" +
                               $"{i} / {sceneCount}\n" +
                               $"{sceneName}";
                yield return Repack(sceneName);
                i++;
            }
        }

        HashSet<AssetBundleRequest> queue = [];
        
        StorageManager.FindLoadRequirements();
        foreach (var (sceneName, v) in ToPreload)
        {
            var bundle = Bundles[sceneName];
            foreach (var (path, preload) in v)
            {
                if (preload.Loaded) continue;
                
                var assetName = $"{sceneName}/{path}.prefab";
                if (!preload.ShouldAlwaysLoad)
                {
                    continue;
                }
                
                preload.MarkLoaded();

                var request = bundle.LoadAssetAsync<GameObject>(assetName);

                request.completed += _ =>
                {
                    queue.Remove(request);

                    var go = (GameObject)request.asset;
                    var modGo = Object.Instantiate(go);
                    Object.DontDestroyOnLoad(modGo);
                    modGo.SetActive(false);

                    preload.OnPreload(modGo);
                };
                
                queue.Add(request);
            }
        }

        var total = queue.Count;
        while (queue.Count > 0)
        {
            _status.text = $"Loading Assets\n" +
                           $"{total-queue.Count} / {total}";
            yield return null;
        }
        ArchitectPlugin.Instance.Log("Done Preloading");
        HasPreloaded = true;
        Object.Destroy(_canvasObj);
        ArchitectPlugin.PreloadingDone();
    }
}
