using System.Collections;
using Architect.Content.Preloads;
using Architect.Objects.Categories;
using Architect.Objects.Placeable;
using UnityEngine;

namespace Architect.Workshop.Items;

public class CustomPreloadObject : WorkshopItem
{
    public override bool RegisterEarly => true;

    private static bool _loading;

    public string Name = string.Empty;
    public string Scene = string.Empty;
    public string Path = string.Empty;

    private PreloadObject _preloadObject;

    public override void Register()
    {
        if (Scene.IsNullOrWhiteSpace() || Path.IsNullOrWhiteSpace() || Name.IsNullOrWhiteSpace()) return;
        
        if (_preloadObject != null) return;
        
        _preloadObject = new PreloadObject(Name, $"custom_{Id}", (Scene, Path));
        Categories.Misc.Add(_preloadObject);

        if (PreloadManager.HasPreloaded)
        {
            ArchitectPlugin.Instance.StartCoroutine(EnsureLoaded());
        }
    }

    private IEnumerator EnsureLoaded()
    {
        yield return new WaitUntil(() => _loading == false);
        yield return _preloadObject.EnsureLoaded();

        if (!_preloadObject.Loaded) yield return PreloadManager.Repack(Scene);
        
        _loading = false;
    }

    public override void Unregister()
    {
        if (_preloadObject == null) return;
        
        PreloadManager.UnregisterPreload(_preloadObject);
        PlaceableObject.RegisteredObjects.Remove(Id);
        Categories.Misc.Remove(_preloadObject);

        _preloadObject = null;
    }
    
    private static readonly Sprite Icon = ResourceUtils.LoadSpriteResource("object_extractor", FilterMode.Point);

    public override Sprite GetIcon() => Icon;
}