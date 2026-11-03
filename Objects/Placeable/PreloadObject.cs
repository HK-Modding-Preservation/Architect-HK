using System;
using System.Collections;
using Architect.Content.Preloads;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Architect.Objects.Placeable;

public class PreloadObject : PlaceableObject, IPreload
{
    public bool Loaded { get; protected set; }
    public string Scene { get; }
    public string Path { get; }

    public void MarkLoaded() { }

    public bool ShouldLoad;
    public bool ShouldAlwaysLoad => ShouldLoad;

    private readonly Action<GameObject> _preloadAction;
    private readonly Func<GameObject, GameObject> _extraction;

    public PreloadObject(
        string name, 
        string id, 
        (string, string) path,
        string description = null,
        Action<GameObject> postSpawnAction = null, 
        Action<GameObject> preloadAction = null,
        Func<GameObject, GameObject> extraction = null,
        Sprite sprite = null,
        Sprite uiSprite = null)
        : base(name, id, description, postSpawnAction, sprite, uiSprite)
    {
        Scene = path.Item1;
        Path = path.Item2;

        _preloadAction = preloadAction;
        _extraction = extraction;
        
        PreloadManager.RegisterPreload(this);
    }

    public override IEnumerator EnsureLoaded()
    {
        if (Loaded) yield break;

        var assetName = $"{Scene}/{Path}.prefab";
        if (!PreloadManager.Bundles.TryGetValue(Scene, out var bundle)) yield break;
        if (!bundle.Contains(assetName)) yield break;
        
        var request = bundle.LoadAssetAsync<GameObject>(assetName);

        yield return request;
        if (Loaded) yield break;
        
        Loaded = true;
        
        var go = (GameObject)request.asset;
        var modGo = Object.Instantiate(go);
        Object.DontDestroyOnLoad(modGo);
        modGo.SetActive(false);
        
        OnPreload(go);
    }

    public virtual void OnPreload(GameObject preload)
    {
        if (_extraction != null) preload = _extraction(preload);
        
        _preloadAction?.Invoke(preload);
        FinishSetup(preload);
        Loaded = true;
    }
}