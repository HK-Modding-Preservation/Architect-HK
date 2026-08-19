using Architect.Behaviour.Custom;
using Architect.Behaviour.Fixers;
using Architect.Behaviour.Utility;
using Architect.Objects.Categories;
using Architect.Objects.Groups;
using Architect.Objects.Placeable;
using GlobalEnums;
using UnityEngine;
using UnityEngine.Video;
using Object = UnityEngine.Object;

namespace Architect.Content.Custom;

public static class MiscObjects
{
    public static void Init()
    {
        Categories.Misc.AddStart(CreateLine());
        Categories.Misc.AddStart(CreateTriangle());
        Categories.Misc.AddStart(CreateCircle());
        Categories.Misc.AddStart(CreateSquare());
        
        Categories.Effects.AddStart(CreateAudioObject());
        
        //Categories.Effects.AddStart(CreateAudioObject());
        
        Categories.Effects.AddStart(CreateAsset<Mp4Object>("MP4", "custom_mp4", true, true)
            .WithConfigGroup(ConfigGroup.Mp4)
            .WithReceiverGroup(ReceiverGroup.Playable)
            .WithBroadcasterGroup(BroadcasterGroup.Finishable));
        
        Categories.Effects.AddStart(CreateAsset<WavObject>("WAV", "custom_wav", false, false)
            .WithConfigGroup(ConfigGroup.Wav)
            .WithReceiverGroup(ReceiverGroup.Wav)
            .WithInputGroup(InputGroup.Wav));
        
        Categories.Effects.AddStart(CreateAsset<UIPngObject>("PNG (HUD)", "custom_png_ui", true, false,
                "\n\nFrame Count options can be used to split a sprite sheet into an animation.\n" +
                "Broadcasts 'OnFinish' when the animation ends.\n\n" +
                "Renders on the HUD, can be used inside of a prefab and\n" +
                "made to appear in every room using the Spawn Prefab block.", true)
            .WithConfigGroup(ConfigGroup.PngUI)
            .WithReceiverGroup(ReceiverGroup.Png)
            .WithBroadcasterGroup(BroadcasterGroup.Finishable)
            .WithInputGroup(InputGroup.Png)
            .WithOutputGroup(OutputGroup.Png));
        
        Categories.Effects.AddStart(CreateAsset<PngObject>("PNG", "custom_png", true, false,
                "\n\nFrame Count options can be used to split a sprite sheet into an animation.\n" +
                "Broadcasts 'OnFinish' when the animation ends.")
            .WithConfigGroup(ConfigGroup.FullPng)
            .WithReceiverGroup(ReceiverGroup.Png)
            .WithBroadcasterGroup(BroadcasterGroup.Png)
            .WithInputGroup(InputGroup.Png)
            .WithOutputGroup(OutputGroup.Png));
        
        Categories.Platforming.Add(CreateWind());
        Categories.Platforming.Add(CreateBumper());
        Categories.Platforming.Add(CreateDreamBlock());
        Categories.Platforming.Add(CreateFeather());
        Categories.Platforming.Add(CreateBubble());

        Categories.Hazards.Add(CreateCustomHazard("White Thorns", "white_thorns",
        [
            new Vector2(-3.672f, -1.265f),
            new Vector2(-3.011f, 0.066f),
            new Vector2(-1.469f, 0.777f),
            new Vector2(0.474f, 1.282f),
            new Vector2(2.353f, 0.813f),
            new Vector2(3.754f, -0.674f)
        ]));
    }

    public static PlaceableObject CreateCustomHazard(string name, string id, Vector2[] points)
    {
        var obj = new GameObject(name);
        obj.SetActive(false);
        Object.DontDestroyOnLoad(obj);

        obj.layer = (int)PhysLayers.ENEMIES;
        
        obj.transform.SetPositionZ(0.01f);

        var col = obj.AddComponent<EdgeCollider2D>();

        col.points = points;

        var dh = obj.AddComponent<DamageHero>();
        obj.AddComponent<NonBouncer>();
        dh.damageDealt = 1;
        dh.hazardType = 2;
        
        obj.AddComponent<SpriteRenderer>().sprite = ResourceUtils.LoadSpriteResource(id, ppu:64);

        return new CustomObject(name, id, obj)
            .WithRotationGroup(RotationGroup.All)
            .WithConfigGroup(ConfigGroup.Vines);
    }

    private static PlaceableObject CreateWind()
    {
        Wind.Init();

        var windObj = new GameObject("Wind")
        {
            layer = 6
        };
        
        var collider = windObj.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(10, 10);
        collider.isTrigger = true;

        windObj.AddComponent<Wind>();

        windObj.SetActive(false);
        Object.DontDestroyOnLoad(windObj);
        
        return new CustomObject("Wind", "wind_zone",
                windObj,
                sprite: ResourceUtils.LoadSpriteResource("wind", FilterMode.Point, ppu:3.2f),
                description: "Applies a force to players and some objects when inside the wind's hitbox.")
            .WithRotationGroup(RotationGroup.All)
            .WithConfigGroup(ConfigGroup.Wind);
    }

    private static PlaceableObject CreateDreamBlock()
    {
        DreamBlock.Init();

        var obj = new GameObject("Dream Block");
        Object.DontDestroyOnLoad(obj);

        obj.layer = LayerMask.NameToLayer("Terrain");

        obj.AddComponent<BoxCollider2D>().size *= 10;

        var col = obj.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size *= 9.8f;

        obj.SetActive(false);
        obj.AddComponent<DreamBlock>();

        obj.AddComponent<SpriteRenderer>().sprite =
            ResourceUtils.LoadSpriteResource("DreamBlock.dream_block", FilterMode.Point);

        obj.transform.SetPositionZ(0.01f);

        return new CustomObject("Dream Block", "dream_block", obj)
            .WithConfigGroup(ConfigGroup.DreamBlock);
    }

    private static PlaceableObject CreateBubble()
    {
        Bubble.Init();
        
        var bubble = new GameObject("Bubble")
        {
            layer = (int)PhysLayers.INTERACTIVE_OBJECT
        };

        var particles = new GameObject("Particles")
        {
            transform =
            {
                parent = bubble.transform,
                localPosition = new Vector3(0, 0, 0.05f)
            }
        };

        var ps = particles.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetimeMultiplier = 1.5f;
        main.startSpeedMultiplier = 0;

        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        sol.size = new ParticleSystem.MinMaxCurve(1, 
            new AnimationCurve(new Keyframe(0, 0.5f), new Keyframe(1, 0)));
        
        var emission = ps.emission;
        emission.rateOverTimeMultiplier = 0;

        var psr = particles.GetComponent<ParticleSystemRenderer>();
        psr.material = new Material(Shader.Find("Sprites/Default"))
        {
            mainTexture = ResourceUtils.LoadSpriteResource("Bubble.particle", FilterMode.Point).texture
        };
        
        bubble.SetActive(false);
        Object.DontDestroyOnLoad(bubble);
        
        bubble.transform.position = new Vector3(0, 0, 0.005f);
        
        var detector = new GameObject("Detector")
        {
            transform = { parent = bubble.transform },
            layer = LayerMask.NameToLayer("Hero Detector")
        };
        
        var detectCol = detector.AddComponent<CircleCollider2D>();
        detectCol.isTrigger = true;
        detectCol.radius = 0.7f;

        var td = new GameObject("Terrain Detector")
        {
            transform = { parent = bubble.transform },
            layer = (int)PhysLayers.TERRAIN_DETECTOR
        };
        
        var tdCol = td.AddComponent<CircleCollider2D>();
        tdCol.isTrigger = false;
        tdCol.radius = 1;
        
        td.AddComponent<Rigidbody2D>().constraints = RigidbodyConstraints2D.FreezeAll;
        
        td.AddComponent<Bubble.TerrainDetector>().bubble = 
            detector.AddComponent<Bubble.Detector>().bubble = bubble.AddComponent<Bubble>();

        var col = bubble.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.7f;

        bubble.AddComponent<SpriteRenderer>();

        return new CustomObject("Bubble", "celeste_bubble", bubble, sprite: Bubble.Red.Idle[0],
            description: "Carries a player in the direction they hold down until they hit a wall or dash out,\n" +
                         "refreshing their dash and double jump.")
            .WithConfigGroup(ConfigGroup.Bubble)
            .WithBroadcasterGroup(BroadcasterGroup.Activatable);
    }
    
    private static PlaceableObject CreateFeather()
    {
        Feather.Init();

        var obj = new GameObject("Feather");
        Object.DontDestroyOnLoad(obj);

        var col = obj.AddComponent<CircleCollider2D>();
        col.radius = 0.5f;
        col.isTrigger = true;

        obj.SetActive(false);
        obj.AddComponent<Feather>().Setup();

        obj.transform.position = new Vector3(0, 0, 0.004f);

        return new CustomObject("Feather", "feather", obj)
            .WithConfigGroup(ConfigGroup.Feather)
            .WithInputGroup(InputGroup.Feather)
            .WithOutputGroup(OutputGroup.Feather)
            .WithReceiverGroup(ReceiverGroup.Feather)
            .WithBroadcasterGroup(BroadcasterGroup.Feather);
    }

    private static PlaceableObject CreateAsset<T>(string name, string id, bool addRenderer, 
        bool addVideo, string extDesc = "", bool preview = false) where T : MonoBehaviour
    {
        var asset = new GameObject("Custom Asset");

        if (addRenderer) asset.AddComponent<SpriteRenderer>().sprite = ArchitectPlugin.BlankSprite;
        if (addVideo) asset.AddComponent<VideoPlayer>();
        
        asset.AddComponent<T>();
        Object.DontDestroyOnLoad(asset);
        asset.SetActive(false);

        return new CustomObject($"Custom {name}", id,
                asset,
                sprite: ResourceUtils.LoadSpriteResource(id, ppu: 300),
                description:
                $"Places a custom {name} in the game.\n\n" +
                "URL should be a direct download anyone can access\n" +
                "in order to work with the level sharer." + extDesc)
            .WithRotationGroup(RotationGroup.All);
    }

    private static PlaceableObject CreateAudioObject()
    {
        AudioPlayer.Init();
        
        var obj = new GameObject("Audio Player");
        obj.SetActive(false);
        Object.DontDestroyOnLoad(obj);

        obj.AddComponent<AudioPlayer>();

        return new CustomObject("Audio Player", "vanilla_audio_player", obj, 
                "Can play vanilla atmosphere or music cues.\n" +
                "A list of all cues can be found in the guide.",
                sprite: ResourceUtils.LoadSpriteResource("audio_player", ppu: 300))
            .WithConfigGroup(ConfigGroup.AudioPlayer)
            .WithReceiverGroup(ReceiverGroup.AudioPlayer);
    }

    private static PlaceableObject CreateSquare()
    {
        var square = CreateShape("square");
        
        square.AddComponent<MiscFixers.Landable>();

        var collider = square.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = new Vector2(10, 10);

        return new CustomObject("Coloured Square", "coloured_square", square, 
                "A square that can be coloured or given a hitbox for custom collision.\n\n" +
                "RGBA colour values should be between 0 and 1.")
            .WithConfigGroup(ConfigGroup.ColouredShapes)
            .WithBroadcasterGroup(BroadcasterGroup.Landable)
            .WithRotationGroup(RotationGroup.All);
    }

    private static PlaceableObject CreateCircle()
    {
        var circle = CreateShape("circle");
        
        circle.AddComponent<MiscFixers.Landable>();

        var collider = circle.AddComponent<PolygonCollider2D>();
        collider.isTrigger = true;

        var points = new Vector2[24];
        for (var i = 0; i < 24; i++)
        {
            var angle = 2 * Mathf.PI * i / 24;
            var x = Mathf.Cos(angle) * 5;
            var y = Mathf.Sin(angle) * 5;
            points[i] = new Vector2(x, y);
        }

        collider.pathCount = 1;
        collider.SetPath(0, points);

        return new CustomObject("Coloured Circle", "coloured_circle", circle, 
                "A circle that can be coloured or given a hitbox for custom collision." +
                "\n\nRGBA colour values should be between 0 and 1.")
            .WithConfigGroup(ConfigGroup.ColouredShapes)
            .WithBroadcasterGroup(BroadcasterGroup.Landable)
            .WithRotationGroup(RotationGroup.All);
    }
    
    public static readonly Material LineMaterial = new(Shader.Find("Sprites/Default"));

    private static PlaceableObject CreateLine()
    {
        var line = new GameObject("Shape (Line)");

        line.SetActive(false);
        Object.DontDestroyOnLoad(line);

        line.AddComponent<LineObject>();

        var lr = line.AddComponent<LineRenderer>();
        lr.material = LineMaterial;

        var collider = line.AddComponent<PolygonCollider2D>();
        collider.isTrigger = true;

        return new CustomObject("Coloured Line Point", "coloured_line", line, 
                "A point that can be combined with others to form lines that\n" +
                "can be coloured or given a hitbox for custom collision.\n\n" +
                "Follows the config options of the first point of its ID that is placed." +
                "\n\nRGBA colour values should be between 0 and 1.",
                sprite: ResourceUtils.LoadSpriteResource("line_point", ppu:200))
            .WithConfigGroup(ConfigGroup.Line)
           
            .WithRotationGroup(RotationGroup.All);
    }

    private static PlaceableObject CreateTriangle()
    {
        var triangle = CreateShape("triangle");

        triangle.AddComponent<MiscFixers.Landable>();
        
        var collider = triangle.AddComponent<EdgeCollider2D>();
        collider.isTrigger = true;
        collider.points =
        [
            new Vector2(-5, -4.17f),
            new Vector2(0, 4.45f),
            new Vector2(5, -4.17f),
            new Vector2(-5, -4.17f)
        ];

        return new CustomObject("Coloured Triangle", "coloured_triangle", triangle, 
                "A triangle that can be coloured or given a hitbox for custom collision." +
                "\n\nRGBA colour values should be between 0 and 1.")
            .WithConfigGroup(ConfigGroup.ColouredShapes)
            .WithBroadcasterGroup(BroadcasterGroup.Landable)
            .WithRotationGroup(RotationGroup.All);
    }

    private static GameObject CreateShape(string name)
    {
        var sprite = ResourceUtils.LoadSpriteResource(name);

        var point = new GameObject("Shape (" + name + ")");
        point.transform.localScale /= 3;

        point.AddComponent<SpriteRenderer>().sprite = sprite;

        point.SetActive(false);
        Object.DontDestroyOnLoad(point);

        return point;
    }
    
    private static PlaceableObject CreateBumper()
    {
        Bumper.Init();
        
        var bumper = new GameObject("Bumper")
        {
            layer = LayerMask.NameToLayer("Terrain")
        };
        
        bumper.SetActive(false);
        Object.DontDestroyOnLoad(bumper);
        
        bumper.transform.position = new Vector3(0, 0, 0.005f);
        
        bumper.AddComponent<Bumper>();
        bumper.AddComponent<SpriteRenderer>().sprite = Bumper.NormalIcon;

        var col = bumper.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.7f;

        var damager = new GameObject("Damager")
        {
            transform = { parent = bumper.transform },
            layer = LayerMask.NameToLayer("Attack")
        };
        
        var damageCol = damager.AddComponent<CircleCollider2D>();
        damageCol.isTrigger = true;
        damageCol.radius = 0.7f;
        
        damager.AddComponent<DamageHero>().hazardType = 1;
        damager.AddComponent<DamageEnemies>().damageDealt = 20;
        
        return new CustomObject("Bumper", "celeste_bumper", bumper)
            .WithConfigGroup(ConfigGroup.Bumpers)
            .WithReceiverGroup(ReceiverGroup.Bumpers);
    }
}