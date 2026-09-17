using Architect.Storage;
using UnityEngine;

namespace Architect.Workshop.Items;

public class CustomScene : SpriteItem
{
    public string Group = "None";
    
    public int TilemapWidth = 500;
    public int TilemapHeight = 500;

    public Color HeroLight = Color.white;
    public Color AmbientLight = Color.white;
    public float Saturation = 1;
    
    public Vector3 MapPos;
    
    public int Environment = 0;

    public SpriteRenderer MapRenderer;
    private GameObject _mapPiece;
    private RoughMapRoom _mapRough;
    
    public override string LoadScene => Id;
    
    public string RoughMapUrl = string.Empty;
    public bool MPoint;
    public float MPpu = 100;
    
    public override (string, string)[] FilesToDownload => [
        (IconUrl, "png"),
        (RoughMapUrl, "png")
    ];

    public override void Register()
    {
        SceneUtils.CustomScenes[Id] = this;
        if (SceneUtils.SceneGroups.TryGetValue(Group, out var g)) SetupMap(g);
        
        base.Register();
        RefreshRoughMapSprite();
    }

    public override void Unregister()
    {
        SceneUtils.CustomScenes.Remove(Id);
    }

    public GameObject GetGameMapPiece()
    {
        return _mapPiece;
    }

    public void SetupMap(SceneGroup group)
    {
        if (!group.HasMap) return;
        if (_mapPiece) Object.Destroy(_mapPiece);

        var groupMap = group.GetGameMap();
        if (!groupMap) return;
        
        if (!GameCameras.instance) return;
        var gm = GameCameras.instance.GetComponentInChildren<GameMap>(true);
        if (!gm) return;

        var parent = groupMap.transform;
        _mapPiece = Object.Instantiate(gm.transform.Find("Crossroads").Find("Crossroads_03").gameObject, parent);
        _mapPiece.transform.localPosition = MapPos - group.ZoomPos * 0.2307692308f;
        _mapPiece.name = Id;
        
        MapRenderer = _mapPiece.GetComponent<SpriteRenderer>();
        _mapRough = _mapPiece.GetComponent<RoughMapRoom>();

        MapRenderer.sprite = _roughMapSprite;
        _mapRough.fullSprite = Sprite;
        _mapRough.fullSpriteDisplayed = false;
    }

    private Sprite _roughMapSprite;

    private void RefreshRoughMapSprite()
    {
        if (RoughMapUrl.IsNullOrWhiteSpace()) return;
        CustomAssetManager.DoLoadSprite(RoughMapUrl, MPoint, MPpu, 1, 1, sprites =>
        {
            if (sprites.IsNullOrEmpty()) return;
            _roughMapSprite = sprites[0];
            if (MapRenderer && _mapRough)
            {
                MapRenderer.sprite = _roughMapSprite;
                _mapRough.fullSpriteDisplayed = false;
            }
        });
    }

    protected override void OnReadySprite()
    {
        if (MapRenderer && _mapRough)
        {
            _mapRough.fullSprite = Sprite;
            _mapRough.fullSpriteDisplayed = false;
        }
    }
}