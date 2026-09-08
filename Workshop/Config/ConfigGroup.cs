using System.Collections.Generic;
using System.Linq;
using Architect.Workshop.Items;
using Architect.Workshop.Types;
using UnityEngine;

namespace Architect.Workshop.Config;

public static class ConfigGroup
{
    public static readonly List<ConfigType> SpriteItem =
    [
        ConfigurationManager.RegisterConfigType(
            new StringConfigType<SpriteItem>("Icon URL", "png_url", (item, value) =>
            {
                item.IconUrl = value.GetValue();
            })
        ),
        ConfigurationManager.RegisterConfigType(
            new BoolConfigType<SpriteItem>("Anti Aliasing", "png_antialias", (item, value) =>
            {
                item.Point = !value.GetValue();
            }).WithDefaultValue(true)
        ),
        ConfigurationManager.RegisterConfigType(
            new FloatConfigType<SpriteItem>("Pixels Per Unit", "png_ppu", (item, value) =>
            {
                item.Ppu = value.GetValue();
            }).WithDefaultValue(100)
        )
    ];
    
    public static readonly List<ConfigType> Cue =
    [
        ConfigurationManager.RegisterConfigType(
            new StringConfigType<CustomCue>("WAV URL", "music_cue_wav_url", (item, value) =>
            {
                item.WavUrl = value.GetValue();
            })
        )
    ];
    
    public static readonly List<ConfigType> Scene = [
        ConfigurationManager.RegisterConfigType(
            new StringConfigType<CustomScene>("Group ID", "scene_group", (item, value) =>
            {
                item.Group = value.GetValue();
            }).WithDefaultValue("None")
        ),
        ConfigurationManager.RegisterConfigType(
            new IntConfigType<CustomScene>("Scene Width", "scene_tilemap_width", (item, value) =>
            {
                item.TilemapWidth = value.GetValue();
            }).WithDefaultValue(500)
        ),
        ConfigurationManager.RegisterConfigType(
            new IntConfigType<CustomScene>("Scene Height", "scene_tilemap_height", (item, value) =>
            {
                item.TilemapHeight = value.GetValue();
            }).WithDefaultValue(500)
        ),
        ConfigurationManager.RegisterConfigType(
            new ChoiceConfigType<CustomScene>("Environment Type", "scene_enviro_type", (item, value) =>
            {
                item.Environment = value.GetValue();
            }).WithOptions("Dust", "Grass", "Bone", "Spa", "Metal", "None", "Wet").WithDefaultValue(0)
        ),
        ConfigurationManager.RegisterConfigType(
            new ColourConfigType<CustomScene>("Hero Light", "scene_hero_light", (item, value) =>
            {
                item.HeroLight = value.GetValue();
            }, false).WithDefaultValue(Color.white)
        ),
        ConfigurationManager.RegisterConfigType(
            new ColourConfigType<CustomScene>("Ambient Light", "scene_ambient_light", (item, value) =>
            {
                item.AmbientLight = value.GetValue();
            }, false).WithDefaultValue(Color.white)
        ),
        ConfigurationManager.RegisterConfigType(
            new FloatConfigType<CustomScene>("Saturation", "scene_saturation", (item, value) =>
            {
                item.Saturation = value.GetValue();
            }).WithDefaultValue(1)
        )
    ];
    
    public static readonly List<ConfigType> SceneMap = [
        (NoteConfigType) "Only applies if the scene's group has a map",
        ConfigurationManager.RegisterConfigType(
            new Vector2ConfigType<CustomScene>("Map Offset", "scene_map_offset", (item, value) =>
            {
                item.MapPos = value.GetValue();
            }).WithDefaultValue(Vector2.zero)
        ),
        ConfigurationManager.RegisterConfigType(
            new StringConfigType<SpriteItem>("Full Map URL", "scene_full_map_url", (item, value) =>
            {
                item.IconUrl = value.GetValue();
            })
        ),
        ConfigurationManager.RegisterConfigType(
            new BoolConfigType<SpriteItem>("Anti Aliasing", "scene_full_map_antialias", (item, value) =>
            {
                item.Point = !value.GetValue();
            }).WithDefaultValue(true)
        ),
        ConfigurationManager.RegisterConfigType(
            new FloatConfigType<SpriteItem>("Pixels Per Unit", "scene_full_map_ppu", (item, value) =>
            {
                item.Ppu = value.GetValue();
            }).WithDefaultValue(100)
        ),
        ConfigurationManager.RegisterConfigType(
            new StringConfigType<CustomScene>("Empty Room URL", "scene_empty_map_url", (item, value) =>
            {
                item.RoughMapUrl = value.GetValue();
            })
        ),
        ConfigurationManager.RegisterConfigType(
            new BoolConfigType<CustomScene>("Anti Aliasing", "scene_empty_map_antialias", (item, value) =>
            {
                item.MPoint = !value.GetValue();
            }).WithDefaultValue(true)
        ),
        ConfigurationManager.RegisterConfigType(
            new FloatConfigType<CustomScene>("Pixels Per Unit", "scene_empty_map_ppu", (item, value) =>
            {
                item.MPpu = value.GetValue();
            }).WithDefaultValue(100)
        )
    ];
    
    public static readonly List<ConfigType> SceneGroup = [
        ConfigurationManager.RegisterConfigType(
            new StringConfigType<SceneGroup>("Area Name", "scene_group_name", (item, value) =>
            {
                item.GroupName = value.GetValue();
            })
        )
    ];
    
    public static readonly List<ConfigType> Charm = [
        ConfigurationManager.RegisterConfigType(
            new StringConfigType<CustomCharm>("Charm Name", "charm_name", (item, value) =>
            {
                item.CharmName = value.GetValue();
            }).WithDefaultValue("Sample Name")
        ),
        ConfigurationManager.RegisterConfigType(
            new StringConfigType<CustomCharm>("Charm Desc", "charm_desc", (item, value) =>
            {
                item.CharmDesc = value.GetValue();
            }).WithDefaultValue("Sample Desc")
        ),
        ConfigurationManager.RegisterConfigType(
            new IntConfigType<CustomCharm>("Charm Cost", "charm_cost", (item, value) =>
            {
                item.Cost = value.GetValue();
            }).WithDefaultValue(1)
        )
    ];
    
    public static readonly List<ConfigType> Item = [
        ConfigurationManager.RegisterConfigType(
            new StringConfigType<CustomItem>("Item Name", "item_name", (item, value) =>
            {
                item.Name = value.GetValue();
            }).WithDefaultValue("Sample Name")
        ),
        ConfigurationManager.RegisterConfigType(
            new StringConfigType<CustomItem>("Item Desc", "item_desc", (item, value) =>
            {
                item.Desc = value.GetValue();
            }).WithDefaultValue("Sample Desc")
        ),
        ConfigurationManager.RegisterConfigType(
            new BoolConfigType<CustomItem>("Show Count", "item_count", (item, value) =>
            {
                item.HasCount = value.GetValue();
            }).WithDefaultValue(false)
        )
    ];
    
    public static readonly List<ConfigType> SceneGroupIcon = [
        ConfigurationManager.RegisterConfigType(
            new StringConfigType<SpriteItem>("Save Icon URL", "scene_group_url", (item, value) =>
            {
                item.IconUrl = value.GetValue();
            })
        ),
        ConfigurationManager.RegisterConfigType(
            new BoolConfigType<SpriteItem>("Anti Aliasing", "scene_group_antialias", (item, value) =>
            {
                item.Point = !value.GetValue();
            }).WithDefaultValue(true)
        )
    ];
    
    public static readonly List<ConfigType> SceneGroupMap = [
        ConfigurationManager.RegisterConfigType(
            new BoolConfigType<SceneGroup>("Has Map", "scene_group_has_map", (item, value) =>
            {
                item.HasMap = value.GetValue();
            }).WithDefaultValue(false)
        ),
        (NoteConfigType) "The map is unlocked when this global variable is true (if set)",
        ConfigurationManager.RegisterConfigType(
            new StringConfigType<SceneGroup>("Required Variable", "scene_group_map_var", (item, value) =>
            {
                item.Variable = value.GetValue();
            })
        ),
        ConfigurationManager.RegisterConfigType(
            new StringConfigType<SceneGroup>("Map Icon URL", "scene_group_map_url", (item, value) =>
            {
                item.MapUrl = value.GetValue();
            })
        ),
        ConfigurationManager.RegisterConfigType(
            new BoolConfigType<SceneGroup>("Anti Aliasing", "scene_group_map_antialias", (item, value) =>
            {
                item.MPoint = !value.GetValue();
            }).WithDefaultValue(true)
        ),
        ConfigurationManager.RegisterConfigType(
            new FloatConfigType<SceneGroup>("Pixels Per Unit", "scene_group_map_ppu", (item, value) =>
            {
                item.MPpu = value.GetValue();
            }).WithDefaultValue(100)
        ),
        ConfigurationManager.RegisterConfigType(
            new ColourConfigType<SceneGroup>("Map Colour", "scene_group_map_colour", (item, value) =>
            {
                item.MapColour = value.GetValue();
            }, false).WithDefaultValue(Color.white)
        )
    ];
    
    public static readonly List<ConfigType> SceneGroupMapPos = [
        (NoteConfigType) "The position of the map, label, zoomed in map and compass icon",
        ConfigurationManager.RegisterConfigType(
            new Vector2ConfigType<SceneGroup>("Map Pos", "scene_group_map_pos", (item, value) =>
            {
                item.Pos = value.GetValue();
            }).WithDefaultValue(Vector2.zero)
        ),
        ConfigurationManager.RegisterConfigType(
            new Vector2ConfigType<SceneGroup>("Title Offset", "scene_group_map_title_pos", (item, value) =>
            {
                item.TextPos = value.GetValue();
            }).WithDefaultValue(Vector2.zero)
        ),
        ConfigurationManager.RegisterConfigType(
            new Vector2ConfigType<SceneGroup>("Zoom Pos", "scene_group_map_zoom_pos", (item, value) =>
            {
                item.ZoomPos = value.GetValue();
            }).WithDefaultValue(Vector2.zero)
        ),
        ConfigurationManager.RegisterConfigType(
            new Vector2ConfigType<SceneGroup>("Area Label", "scene_group_map_label_pos", (item, value) =>
            {
                item.AreaNamePos = value.GetValue();
            }).WithDefaultValue(Vector2.zero)
        ),
        ConfigurationManager.RegisterConfigType(
            new Vector2ConfigType<SceneGroup>("Compass Pos", "scene_group_map_compass_pos", (item, value) =>
            {
                item.CompassPos = value.GetValue() + new Vector2(4.2f, -7.61f);
            }).WithDefaultValue(Vector2.zero)
        )
    ];
    
    public static readonly List<ConfigType> SceneGroupMapDirIn = [
        (NoteConfigType) "Make vanilla maps lead to this one",
        ConfigurationManager.RegisterConfigType(
            new StringConfigType<SceneGroup>("Enter Below", "scene_group_map_b", (item, value) =>
            {
                item.InverseDirectionalZones[(int)MapUtils.Dir.Down] = value.GetValue().Split(',');
            })
        ),
        ConfigurationManager.RegisterConfigType(
            new StringConfigType<SceneGroup>("Enter Above", "scene_group_map_a", (item, value) =>
            {
                item.InverseDirectionalZones[(int)MapUtils.Dir.Up] = value.GetValue().Split(',');
            })
        ),
        ConfigurationManager.RegisterConfigType(
            new StringConfigType<SceneGroup>("Enter Left", "scene_group_map_l", (item, value) =>
            {
                item.InverseDirectionalZones[(int)MapUtils.Dir.Left] = value.GetValue().Split(',');
            })
        ),
        ConfigurationManager.RegisterConfigType(
            new StringConfigType<SceneGroup>("Enter Right", "scene_group_map_r", (item, value) =>
            {
                item.InverseDirectionalZones[(int)MapUtils.Dir.Right] = value.GetValue().Split(',');
            })
        )
    ];
    
    public static readonly List<ConfigType> SceneGroupMapDirOut = [
        (NoteConfigType) "Make this map lead to others",
        ConfigurationManager.RegisterConfigType(
            new StringConfigType<SceneGroup>("Exit Below", "scene_group_map_b2", (item, value) =>
            {
                item.DirectionalZones[(int)MapUtils.Dir.Down] = value.GetValue().Split(',').ToList();
            })
        ),
        ConfigurationManager.RegisterConfigType(
            new StringConfigType<SceneGroup>("Exit Above", "scene_group_map_a2", (item, value) =>
            {
                item.DirectionalZones[(int)MapUtils.Dir.Up] = value.GetValue().Split(',').ToList();
            })
        ),
        ConfigurationManager.RegisterConfigType(
            new StringConfigType<SceneGroup>("Exit Left", "scene_group_map_l2", (item, value) =>
            {
                item.DirectionalZones[(int)MapUtils.Dir.Left] = value.GetValue().Split(',').ToList();
            })
        ),
        ConfigurationManager.RegisterConfigType(
            new StringConfigType<SceneGroup>("Exit Right", "scene_group_map_r2", (item, value) =>
            {
                item.DirectionalZones[(int)MapUtils.Dir.Right] = value.GetValue().Split(',').ToList();
            })
        )
    ];
    
    public static readonly List<ConfigType> MenuStyle =
    [
        ConfigurationManager.RegisterConfigType(
            new StringConfigType<CustomMenuStyle>("Name", "menu_style_name", (item, value) =>
            {
                item.Name = value.GetValue();
            })
        ),
        (NoteConfigType) "The style is unlocked when this universal variable is true (if set)",
        ConfigurationManager.RegisterConfigType(
            new StringConfigType<CustomMenuStyle>("Required Variable", "menu_style_var", (item, value) =>
            {
                item.RequiredBool = value.GetValue();
            })
        )
    ];
}