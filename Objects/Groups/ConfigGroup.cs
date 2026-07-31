using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Architect.Behaviour.Abilities;
using Architect.Behaviour.Custom;
using Architect.Behaviour.Fixers;
using Architect.Behaviour.Utility;
using Architect.Config;
using Architect.Config.Types;
using Architect.Content.Custom;
using Architect.Editor;
using Architect.Objects.Placeable;
using Architect.Prefabs;
using Architect.Storage;
using GlobalEnums;
using HutongGames.PlayMaker.Actions;
using MonoMod.RuntimeDetour;
using Satchel.Futils.Serialiser;
using UnityEngine;
using UnityEngine.Video;
using FsmEvent = HutongGames.PlayMaker.FsmEvent;
using Object = UnityEngine.Object;

namespace Architect.Objects.Groups;

public static class ConfigGroup
{
    public static readonly List<ConfigType> Generic =
    [
        ConfigurationManager.RegisterConfigType(
            new BoolConfigType("Active", "start_active", (o, value) =>
            {
                if (!value.GetValue()) o.SetActive(false);
            }))
    ];
    
    public static readonly List<ConfigType> Disabler = GroupUtils.Merge(Generic,
    [
        ConfigurationManager.RegisterConfigType(
            new BoolConfigType("Disable All", "disabler_all", (o, value) =>
            {
                if (value.GetValue()) o.GetComponent<ObjectRemover>().all = true;
            }).WithDefaultValue(false))
    ]);
    
    public static readonly List<ConfigType> DisableEnemy = GroupUtils.Merge(Disabler,
    [
        ConfigurationManager.RegisterConfigType(
            new BoolConfigType("Include Shade", "disabler_shade", (o, value) =>
            {
                if (value.GetValue()) o.GetComponent<ObjectRemover>().shade = true;
            }).WithDefaultValue(false))
    ]);
    
    public static readonly List<ConfigType> Prefab = GroupUtils.Merge(Generic,
    [
        ConfigurationManager.RegisterConfigType(
            new ChoiceConfigType("Preview Visibility", "prefab_visibility", (o, value) =>
            {
                o.GetComponent<Prefab>().visibility = value.GetValue();
            }).WithOptions("All", "Unlocked", "None").WithDefaultValue(0))
    ]);
    
    public static readonly List<ConfigType> ObjectLayerer = [
        ConfigurationManager.RegisterConfigType(
            new IdConfigType("Target ID", "layerer_target", (o, value) =>
            {
                o.GetComponent<Layerer>().target = value.GetValue();
            })),
        ConfigurationManager.RegisterConfigType(
            new StringConfigType("Set Layer", "layerer_name", (o, value) =>
            {
                o.GetComponent<Layerer>().layer = LayerMask.NameToLayer(value.GetValue());
            })),
        ConfigurationManager.RegisterConfigType(
            new BoolConfigType("Start Applied", "layerer_start_applied", (o, value) =>
            {
                o.GetComponent<Layerer>().start = value.GetValue();
            }).WithDefaultValue(true)),
        ConfigurationManager.RegisterConfigType(
            new BoolConfigType("Recursive", "layerer_recursive", (o, value) =>
            {
                o.GetComponent<Layerer>().recursive = value.GetValue();
            }).WithDefaultValue(true))
    ];

    private static readonly ConfigType Hook = ConfigurationManager.RegisterConfigType(
        new IdConfigType("Path", "enemy_path", (o, value) =>
        {
            var hook = o.GetComponent<EnemyHook>();
            if (hook) hook.path = value.GetUnmodifiedValue();
            else
            {
                var oh = o.GetComponent<ObjectHook>();
                oh.path = value.GetUnmodifiedValue();
                oh.prefabPath = value.GetValue();
            }
        }));
    
    public static readonly List<ConfigType> ObjectHook = [
        Hook,
        ConfigurationManager.RegisterConfigType(
            new StringConfigType("Child Path", "object_hook_child_path", (o, value) =>
            {
                o.GetComponent<ObjectHook>().childPath = value.GetValue();
            })),
        ConfigurationManager.RegisterConfigType(
            new ChoiceConfigType("Start Mode", "object_hook_start", (o, value) =>
            {
                o.GetComponent<ObjectHook>().start = value.GetValue();
            }).WithOptions("Normal", "Inactive", "Active").WithDefaultValue(0)),
        ConfigurationManager.RegisterConfigType(
            new IntConfigType("Index", "object_hook_index", (o, value) =>
            {
                o.GetComponent<ObjectHook>().index = value.GetValue();
            }).WithDefaultValue(0)),
        ConfigurationManager.RegisterConfigType(
            new BoolConfigType("Remove Normal Activators", "remove_vanilla_disablers", (o, value) =>
            {
                if (value.GetValue())
                {
                    o.RemoveComponent<ActivateIfPlayerdataTrue>();
                    o.RemoveComponent<DeactivateIfPlayerdataTrue>();
                    o.RemoveComponent<DeactivateIfPlayerdataFalse>();
                    o.RemoveComponent<DeactivateIfPlayerdataFalseDelayed>();
                    o.AddComponent<IgnoreDisablers>();
                    o.RemoveComponent<Disabler>();
                }
            }).WithDefaultValue(false))
    ];
    
    public static readonly List<ConfigType> EnemyHook = GroupUtils.Merge(Generic, [
        Hook,
        ConfigurationManager.RegisterConfigType(
            new IntConfigType("Index", "enemy_hook_index", (o, value) =>
            {
                o.GetComponent<EnemyHook>().index = value.GetValue();
            }).WithDefaultValue(0))
    ]);
    
    public static readonly List<ConfigType> Darkness = GroupUtils.Merge(Generic,
    [
        ConfigurationManager.RegisterConfigType(
            new IntConfigType("Dark Level", "darkness_level", (o, value) =>
            {
                o.GetComponent<Darkness>().amount = value.GetValue();
            }).WithDefaultValue(2))
    ]);
    
    public static readonly List<ConfigType> AudioPlayer = [
        ConfigurationManager.RegisterConfigType(
            new BoolConfigType("Play on Start", "audio_player_start", (o, value) =>
            {
                o.GetComponent<AudioPlayer>().playOnStart = value.GetValue();
            }).WithDefaultValue(true)),
        ConfigurationManager.RegisterConfigType(
            new ChoiceConfigType("Mode", "audio_player_cue_type", (o, value) =>
            {
                o.GetComponent<AudioPlayer>().isAtmos = value.GetValue() == 1;
            }).WithDefaultValue(0).WithOptions("Music", "Atmos")),
        ConfigurationManager.RegisterConfigType(
            new FloatConfigType("Fade Time", "audio_player_fade_time", (o, value) =>
            {
                o.GetComponent<AudioPlayer>().fadeTime = value.GetValue();
            }).WithDefaultValue(0)),
        ConfigurationManager.RegisterConfigType(
            new BoolConfigType("Lock", "audio_player_lock", (o, value) =>
            {
                o.GetComponent<AudioPlayer>().lockMusic = value.GetValue();
            }).WithDefaultValue(true)),
        ConfigurationManager.RegisterConfigType(
            new StringConfigType("Audio Cue", "audio_player_cue", (o, value) =>
            {
                o.GetComponent<AudioPlayer>().cueId = value.GetValue();
            }).WithDefaultValue("None"))
    ];
    
    public static readonly List<ConfigType> CollisionChanger = [
        ConfigurationManager.RegisterConfigType(
            new BoolConfigType("Disable on Start", "collision_changer_disable", (o, value) =>
            {
                o.GetComponent<CollisionChanger>().startDisabled = value.GetValue();
            }).WithDefaultValue(true)),
        ConfigurationManager.RegisterConfigType(
            new IdConfigType("First Object", "collision_changer_a", (o, value) =>
            {
                o.GetComponent<CollisionChanger>().id1 = value.GetValue();
            })),
        ConfigurationManager.RegisterConfigType(
            new IdConfigType("Second Object", "collision_changer_b", (o, value) =>
            {
                o.GetComponent<CollisionChanger>().id2 = value.GetValue();
            }))
    ];
    
    public static readonly List<ConfigType> Visible = GroupUtils.Merge(Generic, [
        ConfigurationManager.RegisterConfigType(
            new BoolConfigType("Visible", "is_visible", (o, value) =>
            {
                if (value.GetValue()) return;
                if (o.GetComponent<MiscFixers.AlphaClamp>()) return;
                o.RemoveComponent<MiscFixers.ColorLock>();
                
                foreach (var renderer in o.GetComponentsInChildren<tk2dSprite>(true))
                {
                    var col = renderer.color;
                    col.a = 0;
                    renderer.color = col;
                }
                foreach (var renderer in o.GetComponentsInChildren<SpriteRenderer>(true))
                {
                    var col = renderer.color;
                    col.a = 0;
                    renderer.color = col;
                }
                
                o.AddComponent<MiscFixers.ColorLock>().permanent = true;
            }))
    ]);

    public static readonly List<ConfigType> CameraBorder = GroupUtils.Merge(Generic, [
        ConfigurationManager.RegisterConfigType(
            new ChoiceConfigType("Border Type", "camera_border_type", (o, value) =>
            {
                o.GetComponent<CameraBorder>().type = value.GetValue();
            }).WithOptions("Left", "Right", "Top", "Bottom", "Lock").WithDefaultValue(4)),
        ConfigurationManager.RegisterConfigType(
            new ChoiceConfigType("Active Mode", "camera_border_mode", (o, value) =>
            {
                o.GetComponent<CameraBorder>().activeType = value.GetValue();
            }).WithOptions("Both", "Gameplay", "Binoculars").WithDefaultValue(0))
    ]);

    public static readonly List<ConfigType> Water = GroupUtils.Merge(Generic, [
        ConfigurationManager.RegisterConfigType(new Vector2ConfigType(
                "Scale", "water_scale",
                (o, value) =>
                {
                    var ns = o.transform.localScale * value.GetValue();
                    o.transform.SetScaleX(ns.x);
                    o.transform.SetScaleY(ns.y);

                    var fsm = o.GetComponentsInChildren<PlayMakerFSM>()
                        .FirstOrDefault(f => f.FsmName == "Surface Water Region");
                    if (fsm) fsm.FsmVariables.FindFsmFloat("Hero Offset").Value = ns.y * 3;

                    var ab = o.transform.Find("Acid Box");
                    if (ab) ab.localPosition /= ns.y;
                },
                (o, value, ctx) =>
                {
                    if (ctx != ConfigurationManager.PreviewContext.Cursor) return;
                    if (EditManager.CurrentObject is not PlaceableObject placeable) return;
                    var ns = placeable.Prefab.transform.localScale * value.GetValue() * EditManager.CurrentScale;
                    o.transform.SetScaleX(EditManager.CurrentlyFlipped ? -ns.x : ns.x);
                    o.transform.SetScaleY(ns.y);
                })
            .WithDefaultValue(Vector2.one)),
        ConfigurationManager.RegisterConfigType(new BoolConfigType(
                "Black Water", "water_black", (o, value) =>
                {
                    var fsm = o.GetComponentsInChildren<PlayMakerFSM>()
                        .FirstOrDefault(f => f.FsmName == "Surface Water Region");
                    if (fsm) fsm.FsmVariables.FindFsmBool("Black").value = value.GetValue();
                }).WithDefaultValue(false))
    ]);

    public static readonly List<ConfigType> Item = GroupUtils.Merge(Generic, [
        ConfigurationManager.RegisterConfigType(
            new StringConfigType("Item ID", "item_id", (o, value) =>
            {
                o.GetComponent<CustomPickup>().itemId = value.GetValue();
            }).WithDefaultValue("Wayward_Compass"))
    ]);
    
    public static readonly List<ConfigType> Mutable =  GroupUtils.Merge(Visible, [
        ConfigurationManager.RegisterConfigType(
            new BoolConfigType("Mute Sound", "mutable_muted", (o, value) =>
            {
                o.GetComponentInChildren<SoundMaker>().muted = value.GetValue();
            }).WithDefaultValue(false).WithPriority(-1))
    ]);
    
    public static readonly List<ConfigType> Feather =  GroupUtils.Merge(Visible, [
        ConfigurationManager.RegisterConfigType(
            new FloatConfigType("Flight Time", "feather_flight_time", (o, value) =>
            {
                o.GetComponent<Feather>().featherTime = value.GetValue();
            }).WithDefaultValue(5)),
        ConfigurationManager.RegisterConfigType(
            new FloatConfigType("Respawn Time", "feather_respawn_time", (o, value) =>
            {
                o.GetComponent<Feather>().respawnTime = value.GetValue();
            }).WithDefaultValue(6))
    ]);
    
    public static readonly List<ConfigType> Cocoon =  GroupUtils.Merge(Visible, [
        ConfigurationManager.RegisterConfigType(
            new IntConfigType("Lifeseed Count", "lifeseed_count", (o, value) =>
            {
                o.GetComponent<HealthCocoon>().SetScuttlerAmount(value.GetValue());
            }).WithDefaultValue(3))
    ]);
    
    public static readonly List<ConfigType> ColoWall =  GroupUtils.Merge(Visible, [
        ConfigurationManager.RegisterConfigType(
            new FloatConfigType("Move Distance", "colo_wall_move", (o, value) =>
            {
                o.GetComponent<MiscFixers.ColoWall>().moveDistance = value.GetValue();
            }).WithDefaultValue(5)),
        ConfigurationManager.RegisterConfigType(
            new FloatConfigType("Move Speed", "colo_wall_speed", (o, value) =>
            {
                o.GetComponent<MiscFixers.ColoWall>().moveSpeed = value.GetValue();
            }).WithDefaultValue(28))
    ]);
    
    public static readonly List<ConfigType> ColoPlat =  GroupUtils.Merge(Visible, [
        ConfigurationManager.RegisterConfigType(
            new BoolConfigType("Start Up", "colo_plat_start_up", (o, value) =>
            {
                if (!value.GetValue()) return;
                var fsm = o.LocateMyFSM("Control");
                var goneUp = false;
                fsm.GetState("Retracted").AddAction(() =>
                {
                    if (goneUp) return;
                    goneUp = true;
                    fsm.SendEvent("PLAT EXPAND");
                });
            }).WithDefaultValue(true))
    ]);
    
    public static readonly List<ConfigType> LaserCrystal = GroupUtils.Merge(Visible, [
        ConfigurationManager.RegisterConfigType(
            new FloatConfigType("Start Pause", "laser_crystal_pause", (o, value) =>
            {
                o.LocateMyFSM("Laser Bug").FsmVariables.FindFsmFloat("Start Pause").Value = value.GetValue();
            }).WithDefaultValue(0.5f)),
        ConfigurationManager.RegisterConfigType(
            new FloatConfigType("Idle Time", "laser_crystal_idle", (o, value) =>
            {
                o.LocateMyFSM("Laser Bug").FsmVariables.FindFsmFloat("Idle Time").Value = value.GetValue();
            }).WithDefaultValue(1)),
        ConfigurationManager.RegisterConfigType(
            new FloatConfigType("Charge Time", "laser_crystal_charge", (o, value) =>
            {
                o.LocateMyFSM("Laser Bug").FsmVariables.FindFsmFloat("Antic Time").Value = value.GetValue();
            }).WithDefaultValue(0.75f)),
        ConfigurationManager.RegisterConfigType(
            new FloatConfigType("Beam Time", "laser_crystal_beam", (o, value) =>
            {
                o.LocateMyFSM("Laser Bug").FsmVariables.FindFsmFloat("Beam Time").Value = value.GetValue();
            }).WithDefaultValue(1.5f))
    ]);
    
    public static readonly List<ConfigType> AbilityCrystal =  GroupUtils.Merge(Mutable, [
        ConfigurationManager.RegisterConfigType(
            new FloatConfigType("Regen Time", "crystal_regen_time", (o, value) =>
            {
                o.GetComponent<AbilityCrystal>().regenTime = value.GetValue();
            }).WithDefaultValue(2.5f)),
        ConfigurationManager.RegisterConfigType(
            new BoolConfigType("Single Use", "crystal_single_use", (o, value) =>
            {
                o.GetComponent<AbilityCrystal>().singleUse = value.GetValue();
            }).WithDefaultValue(false).WithPriority(-1))
    ]);
    
    public static readonly List<ConfigType> Duplicator =  GroupUtils.Merge(Generic, [
        ConfigurationManager.RegisterConfigType(
            new IdConfigType("Object ID", "duplicator_id", (o, value) =>
            {
                o.GetComponent<ObjectDuplicator>().id = value.GetValue();
            }))
    ]);
    
    public static readonly List<ConfigType> Stretchable = GroupUtils.Merge(Generic, [
        ConfigurationManager.RegisterConfigType(new Vector2ConfigType("Scale", "object_scale",
                (o, value) =>
                {
                    var ns = o.transform.localScale * value.GetValue();
                    o.transform.SetScaleX(ns.x);
                    o.transform.SetScaleY(ns.y);
                },
                (o, value, ctx) =>
                {
                    if (ctx != ConfigurationManager.PreviewContext.Cursor) return;
                    if (EditManager.CurrentObject is not PlaceableObject placeable) return;
                    var ns = (placeable.IgnoreScale ? Vector2.one : placeable.Prefab.transform.localScale) 
                             * value.GetValue() * EditManager.CurrentScale;
                    o.transform.SetScaleX(EditManager.CurrentlyFlipped ? -ns.x : ns.x);
                    o.transform.SetScaleY(ns.y);
                })
            .WithDefaultValue(Vector2.one))
    ]);
    
    public static readonly List<ConfigType> Fireball = GroupUtils.Merge(Stretchable, GroupUtils.Merge(Visible, [
        ConfigurationManager.RegisterConfigType(
            new IntConfigType("Enemy Damage", "fireball_enemy_damage", (o, value) =>
            {
                var fsm = o.LocateMyFSM("damages_enemy");
                if (value.GetValue() == 0) fsm.enabled = false;
                else fsm.FsmVariables.FindFsmInt("damageDealt").value = value.GetValue();
            }).WithDefaultValue(15)),
        ConfigurationManager.RegisterConfigType(
            new IntConfigType("Player Damage", "fireball_player_damage", (o, value) =>
            {
                if (value.GetValue() == 0) return;
                o.AddComponent<DamageHero>().damageDealt = value.GetValue();
            }).WithDefaultValue(0)),
        ConfigurationManager.RegisterConfigType(
            new BoolConfigType("Can Pogo", "fireball_can_pogo", (o, value) =>
            {
                if (value.GetValue()) return;
                o.AddComponent<NonBouncer>();
            }).WithDefaultValue(true))
    ]));
    
    public static readonly List<ConfigType> Scream = GroupUtils.Merge(Stretchable, GroupUtils.Merge(Visible, [
        ConfigurationManager.RegisterConfigType(
            new IntConfigType("Enemy Damage", "scream_enemy_damage", (o, value) =>
            {
                var fsms = o.GetComponentsInChildren<PlayMakerFSM>()
                    .Where(f => f.FsmName == "damages_enemy");
                foreach (var fsm in fsms)
                {
                    if (value.GetValue() == 0) fsm.enabled = false;
                    else fsm.FsmVariables.FindFsmInt("damageDealt").value = value.GetValue();
                }
            }).WithDefaultValue(20)),
        ConfigurationManager.RegisterConfigType(
            new IntConfigType("Player Damage", "scream_player_damage", (o, value) =>
            {
                if (value.GetValue() == 0) return;
                
                var fsms = o.GetComponentsInChildren<PlayMakerFSM>()
                    .Where(f => f.FsmName == "damages_enemy");
                foreach (var fsm in fsms)
                {
                    fsm.gameObject.AddComponent<DamageHero>().damageDealt = value.GetValue();
                }
            }).WithDefaultValue(0)),
        ConfigurationManager.RegisterConfigType(
            new BoolConfigType("Can Pogo", "scream_can_pogo", (o, value) =>
            {
                if (value.GetValue()) return;
                
                var fsms = o.GetComponentsInChildren<PlayMakerFSM>()
                    .Where(f => f.FsmName == "damages_enemy");
                foreach (var fsm in fsms)
                {
                    fsm.gameObject.AddComponent<NonBouncer>();
                }
            }).WithDefaultValue(true))
    ]));
    
    public static readonly List<ConfigType> Conveyor = GroupUtils.Merge(Stretchable, GroupUtils.Merge(Visible, [
        ConfigurationManager.RegisterConfigType(
            new FloatConfigType("Belt Speed", "conveyor_speed", (o, value) =>
            {
                var val = value.GetValue();
                if (val < 0)
                {
                    o.transform.SetScaleX(-o.transform.GetScaleX());
                }

                o.GetComponentInChildren<ConveyorBelt>().speed *= val;
                foreach (var anim in o.GetComponentsInChildren<Animator>())
                {
                    anim.speed *= Math.Abs(val);
                }
            }).WithDefaultValue(1))
    ]));

    public static readonly List<ConfigType> DreamBlock = GroupUtils.Merge(Visible, GroupUtils.Merge(Stretchable, [
        ConfigurationManager.RegisterConfigType(
            new BoolConfigType("Show Particles", "dream_block_particles", (o, value) =>
            {
                if (!value.GetValue()) return;
                o.GetComponent<DreamBlock>().SetupParticles();
            }).WithDefaultValue(true))
    ]));
    
    public static readonly List<ConfigType> Goams = GroupUtils.Merge(Visible, [
        ConfigurationManager.RegisterConfigType(new BoolConfigType("Start Up", "goam_start_up",
                (o, value) =>
                {
                    if (value.GetValue()) return; 
                    o.LocateMyFSM("Worm Control").FsmVariables.FindFsmBool("Start Down").Value = true;
                })
            .WithDefaultValue(false))
    ]);
    
    public static readonly List<ConfigType> Garpede = GroupUtils.Merge(Visible, [
        ConfigurationManager.RegisterConfigType(new FloatConfigType("Speed", "garpede_speed",
                (o, value) =>
                {
                    o.GetComponent<BigCentipede>().moveSpeed = value.GetValue();
                })
            .WithDefaultValue(20)),
        ConfigurationManager.RegisterConfigType(new FloatConfigType("Burrow Time", "garpede_burrow_time",
                (o, value) =>
                {
                    o.GetComponent<BigCentipede>().burrowTime = value.GetValue();
                })
            .WithDefaultValue(1.7f)),
        ConfigurationManager.RegisterConfigType(new FloatConfigType("Entry Offset", "garpede_entry_offset",
                (o, value) =>
                {
                    o.transform.Find("Entry").SetLocalPositionX(value.GetValue());
                })
            .WithDefaultValue(-12)),
        ConfigurationManager.RegisterConfigType(new FloatConfigType("Exit Offset", "garpede_exit_offset",
                (o, value) =>
                {
                    o.transform.Find("Exit").SetLocalPositionX(value.GetValue());
                })
            .WithDefaultValue(6)),
        ConfigurationManager.RegisterConfigType(new IntConfigType("Segments", "garpede_segments",
                (o, value) =>
                {
                    var garpede = o.GetComponent<EnemyFixers.Garpede>();
                    garpede.segments = Math.Max(0, value.GetValue());
                    garpede.Setup();
                },
                (o, value, ctx) =>
                {
                    if (ctx == ConfigurationManager.PreviewContext.Placement) return;
                    var garpede = o.GetComponent<EnemyFixers.Garpede>();
                    garpede.segments = Math.Max(0, value.GetValue());
                    garpede.Setup();
                })
            .WithDefaultValue(3))
    ]);
    
    public static readonly List<ConfigType> Interaction = GroupUtils.Merge(Stretchable, [
        ConfigurationManager.RegisterConfigType(
            new ChoiceConfigType("Label", "interaction_label",
                (o, value) =>
                {
                    o.GetComponent<CustomInteraction>().prompt = value.GetStringValue();
                }).WithOptions(CustomInteraction.Labels).WithDefaultValue(0)),
        ConfigurationManager.RegisterConfigType(
            new BoolConfigType("Hide on Interact", "interaction_hide",
                (o, value) =>
                {
                    o.GetComponent<CustomInteraction>().hideOnInteract = value.GetValue();
                }).WithDefaultValue(true)),
        ConfigurationManager.RegisterConfigType(
            new Vector2ConfigType("Offset", "interaction_offset",
                (o, value) =>
                {
                    var ci = o.GetComponent<CustomInteraction>();
                    var val = value.GetValue();
                    ci.xOffset = val.x;
                    ci.yOffset = val.y;
                }).WithDefaultValue(Vector2.zero))
    ]);
    
    public static readonly List<ConfigType> Wind =  GroupUtils.Merge(Stretchable, [
        ConfigurationManager.RegisterConfigType(
            new BoolConfigType("Show Particles", "wind_particles", (o, value) =>
            {
                if (!value.GetValue()) return;
                o.GetComponent<Wind>().SetupParticles();
            }).WithDefaultValue(true)),
        ConfigurationManager.RegisterConfigType(
            new FloatConfigType("Wind Speed", "wind_speed", (o, value) =>
            {
                o.GetComponent<Wind>().speed = value.GetValue() * 10;
            }).WithDefaultValue(3).WithPriority(-1)),
        ConfigurationManager.RegisterConfigType(
            new BoolConfigType("Affects Player", "wind_affects_player", (o, value) =>
            {
                o.GetComponent<Wind>().affectsPlayer = value.GetValue();
            }).WithDefaultValue(true)),
        ConfigurationManager.RegisterConfigType(
            new BoolConfigType("Affects Enemies", "wind_affects_enemies", (o, value) =>
            {
                o.GetComponent<Wind>().affectsEnemies = value.GetValue();
            }).WithDefaultValue(true)),
        ConfigurationManager.RegisterConfigType(
            new BoolConfigType("Affects Projectiles", "wind_affects_projectiles", (o, value) =>
            {
                o.GetComponent<Wind>().affectsProjectiles = value.GetValue();
            }).WithDefaultValue(true)),
        ConfigurationManager.RegisterConfigType(
            new ColourConfigType("Particle Colours", "wind_colour_particles", (o, value) =>
            {
                var wind = o.GetComponent<Wind>();
                var val = value.GetValue();
                wind.r = val.r;
                wind.g = val.g;
                wind.b = val.b;
                wind.a = val.a;
            }, true, (o, value, arg3) =>
            {
                if (arg3 == ConfigurationManager.PreviewContext.Cursor) return;
                o.GetComponent<SpriteRenderer>().color = value.GetValue().Where(a: 1);
            }).WithDefaultValue(Color.white).WithPriority(-1))
    ]);

    private static readonly ConfigType RenderLayer = ConfigurationManager.RegisterConfigType(
        new IntConfigType("Render Layer", "obj_layer",
                (o, value) =>
                {
                    foreach (var comp in o.GetComponentsInChildren<Renderer>())
                        comp.sortingOrder = value.GetValue();
                }, (o, value, _) =>
                {
                    foreach (var comp in o.GetComponentsInChildren<Renderer>())
                        comp.sortingOrder = value.GetValue();
                })
            .WithDefaultValue(0));

    public static readonly List<ConfigType> Decorations = GroupUtils.Merge(Visible, [
        RenderLayer
    ]);
    
    public static readonly List<ConfigType> Camera =  GroupUtils.Merge(Generic, [
        ConfigurationManager.RegisterConfigType(
            new IdConfigType("Object ID", "camera_view_id", (o, value) =>
            {
                o.GetComponent<CameraObjects.CustomCamera>().id = value.GetValue();
            })),
        ConfigurationManager.RegisterConfigType(
            new IntConfigType("Resolution", "camera_resolution", (o, value) =>
            {
                o.GetComponent<CameraObjects.CustomCamera>().resolution = value.GetValue();
            }).WithDefaultValue(1024)),
        ConfigurationManager.RegisterConfigType(
            new FloatConfigType("Zoom", "camera_zoom_amount", (o, value) =>
            {
                o.GetComponent<tk2dCamera>().zoomFactor = value.GetValue();
            }).WithDefaultValue(1))
    ]);

    public static readonly List<ConfigType> CameraView = GroupUtils.Merge(Stretchable, [
        RenderLayer
    ]);

    public static readonly List<ConfigType> StretchDecor = GroupUtils.Merge(Stretchable, GroupUtils.Merge(Decorations, []));
    
    public static readonly List<ConfigType> StretchColourDecor = GroupUtils.Merge(StretchDecor, [
        ConfigurationManager.RegisterConfigType(
            new ColourConfigType("Colour", "png_sprite_colour",
                (o, value) =>
                {
                    var col = value.GetValue();
                    if (o.GetComponent<MiscFixers.AlphaClamp>()) col.a = Mathf.Max(col.a, 0.1f);
                    o.GetComponentInChildren<SpriteRenderer>().color = col;
                }, true).WithDefaultValue(Color.white).WithPriority(-1))
    ]);

    public static readonly List<ConfigType> TrackPoint = [
        ConfigurationManager.RegisterConfigType(
            new IdConfigType("Track ID", "track_id", (o, value) =>
            {
                o.GetComponent<SplineObjects.SplinePoint>().id = value.GetValue();
            }).WithDefaultValue("1").WithPriority(-1))
    ];

    public static readonly List<ConfigType> TrackStartPoint = GroupUtils.Merge(Visible, GroupUtils.Merge(TrackPoint, [
        ConfigurationManager.RegisterConfigType(
            new FloatConfigType("Speed", "track_speed", (o, value) =>
            {
                o.GetComponent<SplineObjects.Spline>().speed = value.GetValue();
            }).WithDefaultValue(10)),
        ConfigurationManager.RegisterConfigType(
            new ColourConfigType("Colour", "track_colour", (o, value) =>
            {
                o.GetComponent<SplineObjects.Spline>().colour = value.GetValue();
            }, true).WithDefaultValue(new Color(1, 1, 1, 0.1f)))
    ]));
    
    public static readonly List<ConfigType> Vines = GroupUtils.Merge(Decorations, [
        ConfigurationManager.RegisterConfigType(
            new BoolConfigType("Damage Player", "vines_hurt_player",
                (o, value) =>
                {
                    if (value.GetValue()) return;
                    o.RemoveComponent<DamageHero>();
                })
                .WithDefaultValue(true))
    ]);

    public static readonly List<ConfigType> PersistentBreakable = GroupUtils.Merge(Visible, [
        ConfigurationManager.RegisterConfigType(MakePersistenceConfigType("Stay Broken", "breakable_stay"))
    ]);

    public static readonly List<ConfigType> Levers = GroupUtils.Merge(Visible, [
        ConfigurationManager.RegisterConfigType(MakePersistenceConfigType("Stay Pulled", "lever_stay"))
    ]);

    public static readonly List<ConfigType> Chest = GroupUtils.Merge(Visible, [
        ConfigurationManager.RegisterConfigType(MakePersistenceConfigType("Stay Opened", "chest_stay")),
        ConfigurationManager.RegisterConfigType(
            new IntConfigType("Small Geo", "chest_geo_small", (o, value) =>
            {
                o.LocateMyFSM("Chest Control").FsmVariables.FindFsmInt("Geo Small").value = value.GetValue();
            }).WithDefaultValue(50)),
        ConfigurationManager.RegisterConfigType(
            new IntConfigType("Medium Geo", "chest_geo_med", (o, value) =>
            {
                o.LocateMyFSM("Chest Control").FsmVariables.FindFsmInt("Geo Med").value = value.GetValue();
            }).WithDefaultValue(0)),
        ConfigurationManager.RegisterConfigType(
            new IntConfigType("Large Geo", "chest_geo_large", (o, value) =>
            {
                o.LocateMyFSM("Chest Control").FsmVariables.FindFsmInt("Geo Large").value = value.GetValue();
            }).WithDefaultValue(0))
    ]);

    public static readonly List<ConfigType> QgDoor = GroupUtils.Merge(Visible, [
        ConfigurationManager.RegisterConfigType(
            new Vector2ConfigType("Open Offset", "qg_door_open_offset", (o, value) =>
            {
                ((iTweenMoveBy)o.LocateMyFSM("Control").GetState("Slide").actions[0]).vector.value = value.GetValue();
            }).WithDefaultValue(new Vector2(4.7f, 0)))
    ]);

    public static readonly List<ConfigType> BattleGate = GroupUtils.Merge(Visible, [
        ConfigurationManager.RegisterConfigType(
            new BoolConfigType("Start Closed", "bg_start_closed", (o, value) =>
            {
                if (value.GetValue()) return;
                o.LocateMyFSM("BG Control").FsmVariables.FindFsmBool("Start Closed").Value = false;
            }).WithDefaultValue(true))
    ]);

    public static readonly List<ConfigType> WpGate = GroupUtils.Merge(Visible, [
        ConfigurationManager.RegisterConfigType(
            new BoolConfigType("Start Closed", "wp_gate_start_closed", (o, value) =>
            {
                if (!value.GetValue()) return;
                o.LocateMyFSM("FSM").FsmVariables.FindFsmBool("Auto Up").Value = true;
            }).WithDefaultValue(true)),
        ConfigurationManager.RegisterConfigType(
            new FloatConfigType("Fade Up Time", "wp_gate_fade_up", (o, value) =>
            {
                o.LocateMyFSM("FSM").FsmVariables.FindFsmFloat("Up Time").Value = value.GetValue();
            }).WithDefaultValue(1)),
        ConfigurationManager.RegisterConfigType(
            new FloatConfigType("Fade Down Time", "wp_gate_fade_down", (o, value) =>
            {
                o.LocateMyFSM("FSM").FsmVariables.FindFsmFloat("Down Time").Value = value.GetValue();
            }).WithDefaultValue(1))
    ]);

    public static readonly List<ConfigType> WpLift = GroupUtils.Merge(Visible, [
        ConfigurationManager.RegisterConfigType(
            new Vector2ConfigType("Travel Distance", "wp_lift_distance", (o, value) =>
            {
                o.GetComponent<MiscFixers.WpLift>().move = value.GetValue();
            }).WithDefaultValue(new Vector2(0, 20)))
    ]);

    private static readonly ConfigType IsBreakable = ConfigurationManager.RegisterConfigType(new BoolConfigType(
        "Breakable", "breakable_on", (o, value) =>
        {
            if (!value.GetValue()) o.RemoveComponentsInChildren<Breakable>();
        }).WithDefaultValue(true));

    private static readonly int Terrain = LayerMask.NameToLayer("Terrain");
    private static readonly int Default = LayerMask.NameToLayer("Default");

    public static readonly List<ConfigType> Colliders = GroupUtils.Merge(Stretchable, GroupUtils.Merge(Decorations, [
        ConfigurationManager.RegisterConfigType(
            new ChoiceConfigType("Collision Type", "collider_type", (o, value) =>
                {
                    switch (value.GetValue())
                    {
                        case 0:
                            o.RemoveComponentsInChildren<Collider2D>();
                            break;
                        case 1:
                            o.layer = (int)PhysLayers.ENEMIES;
                            var collider = o.GetComponentInChildren<Collider2D>();
                            collider.isTrigger = false;
                            collider.gameObject.AddComponent<NonBouncer>();
                            var dh = collider.gameObject.AddComponent<DamageHero>();
                            dh.damageDealt = 1;
                            dh.hazardType = 2;
                            break;
                        case 2:
                            o.GetComponentInChildren<Collider2D>().isTrigger = false;
                            o.layer = Terrain;
                            break;
                        case 3:
                            o.layer = Terrain;
                            var col = o.GetComponentInChildren<Collider2D>();
                            col.gameObject.AddComponent<PlatformEffector2D>().surfaceArc = 120;
                            col.isTrigger = false;
                            col.usedByEffector = true;
                            break;
                        case 4:
                            o.layer = Default;
                            foreach (var c in o.GetComponentsInChildren<Collider2D>())
                            {
                                c.isTrigger = false;
                                c.gameObject.layer = Default;
                            }
                            break;
                    }
                }
            ).WithOptions("None", "Hazard", "Solid", "Semi-Solid", "Barrier")),
        
        ConfigurationManager.RegisterConfigType(
            new FloatConfigType("Fallthrough Time", "collider_fallthrough",
                (o, value) =>
                {
                    o.GetComponentInChildren<Collider2D>().gameObject
                        .AddComponent<Fallthrough>().fallthroughTime = value.GetValue();
                }
            ))
    ]));

    private static readonly ConfigType AlphaColour = ConfigurationManager.RegisterConfigType(
        new FloatConfigType("Colour A", "colour_alpha", (o, value) =>
        {
            var sr = o.GetComponent<SpriteRenderer>();
            var color = sr.color;
            color.a = value.GetValue();
            sr.color = color;
        }).WithDefaultValue(1));
    public static readonly List<ConfigType> ColouredShapes = GroupUtils.Merge(Stretchable, GroupUtils.Merge(Colliders, [
        ConfigurationManager.RegisterConfigType(
            new ColourConfigType("Colour", "sprite_colour", (o, value) =>
            {
                var col = value.GetValue();
                if (o.GetComponent<MiscFixers.AlphaClamp>()) col.a = Mathf.Max(col.a, 0.1f);
                o.GetComponent<SpriteRenderer>().color = col;
            }, true).WithDefaultValue(Color.white)),
        ConfigurationManager.RegisterConfigType(
            new BoolConfigType("Has Physics", "coloured_shapes_has_physics", (o, value) =>
            {
                if (!value.GetValue()) return;
                o.AddComponent<Rigidbody2D>().collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            }).WithDefaultValue(false)),
        ConfigurationManager.RegisterConfigType(
            new FloatConfigType("Gravity Scale (Physics)", "coloured_shapes_gravity_scale", (o, value) =>
            {
                var rb2d = o.GetComponent<Rigidbody2D>();
                if (!rb2d) return;
                rb2d.gravityScale = value.GetValue();
            }).WithPriority(2)),
        ConfigurationManager.RegisterConfigType(
            new FloatConfigType("Mass (Physics)", "coloured_shapes_mass", (o, value) =>
            {
                var rb2d = o.GetComponent<Rigidbody2D>();
                if (!rb2d) return;
                rb2d.mass = value.GetValue();
            }).WithPriority(2)),
        ConfigurationManager.RegisterConfigType(
            new FloatConfigType("Friction (Physics)", "coloured_shapes_friction", (o, value) =>
            {
                var rb2d = o.GetComponent<Rigidbody2D>();
                if (!rb2d) return;
                if (!rb2d.sharedMaterial) rb2d.sharedMaterial = new PhysicsMaterial2D();
                rb2d.sharedMaterial.friction = value.GetValue();
            }).WithPriority(2))
    ]));
    
    public static readonly List<ConfigType> Line = GroupUtils.Merge(Colliders, [
        ConfigurationManager.RegisterConfigType(
            new IdConfigType("Point Set ID", "point_id", (o, value) =>
            {
                o.GetComponent<LineObject>().id = value.GetValue();
            }).WithDefaultValue("1").WithPriority(-1)),
        ConfigurationManager.RegisterConfigType(
            new FloatConfigType("Visual Width", "line_width", (o, value) =>
            {
                o.GetComponent<LineObject>().width = value.GetValue();
            }).WithDefaultValue(0.2f)),
        ConfigurationManager.RegisterConfigType(
            new ColourConfigType("Colour", "line_colour", (o, value) =>
            {
                o.GetComponent<LineObject>().LineColour = value.GetValue();
            }, true).WithDefaultValue(Color.white))
    ]);
    
    public static readonly List<ConfigType> Colourer = GroupUtils.Merge(Generic, [
        ConfigurationManager.RegisterConfigType(
            new IdConfigType("Object ID", "colourer_target", (o, value) =>
            {
                o.GetComponent<ObjectColourer>().targetId = value.GetValue();
            })),
        ConfigurationManager.RegisterConfigType(
            new BoolConfigType("Start Applied", "colourer_active", (o, value) =>
            {
                o.GetComponent<ObjectColourer>().startApplied = value.GetValue();
            }).WithDefaultValue(true)),
        ConfigurationManager.RegisterConfigType(
            new ColourConfigType("Colour RGB", "colourer_colour_set", (o, value) =>
            {
                o.GetComponent<ObjectColourer>().Colour = value.GetValue();
            }, false).WithDefaultValue(Color.white)),
        ConfigurationManager.RegisterConfigType(
            new FloatConfigType("Colour A", "colourer_alpha", (o, value) =>
            {
                var oc = o.GetComponent<ObjectColourer>();
                oc.useAlphaByDefault = true;
                oc.a = value.GetValue();
            })),
        ConfigurationManager.RegisterConfigType(
            new ChoiceConfigType("Colour Particles", "colourer_particles", (o, value) =>
            {
                o.GetComponent<ObjectColourer>().particles = value.GetValue();
            }).WithOptions("True", "False", "Only").WithDefaultValue(0)),
        ConfigurationManager.RegisterConfigType(
            new ChoiceConfigType("Tint Mode", "colourer_mode", (o, value) =>
            {
                o.GetComponent<ObjectColourer>().mode = value.GetValue();
            }).WithOptions("Multiply", "Set").WithDefaultValue(0))
    ]);

    public static readonly List<ConfigType> Gravity = GroupUtils.Merge(Visible, [
        ConfigurationManager.RegisterConfigType(new FloatConfigType("Gravity Scale", "gravity_scale",
            (o, value) =>
            {
                o.AddComponent<GravityLock>().level = value.GetValue();
            }
        ))
    ]);

    public class GravityLock : MonoBehaviour
    {
        public float level;
        public Rigidbody2D rb2d;

        private void Start()
        {
            rb2d = gameObject.GetComponentInChildren<Rigidbody2D>(true);
            if (!rb2d) rb2d = gameObject.AddComponent<Rigidbody2D>();
        }

        private void Update()
        {
            rb2d.gravityScale = level;
            rb2d.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }
    }

    public static readonly List<ConfigType> TriggerActivator = GroupUtils.Merge(Gravity, [
        ConfigurationManager.RegisterConfigType(new IntConfigType("Trigger Layer", "activator_layer",
            (o, value) =>
            {
                o.GetComponent<MiscFixers.TriggerActivator>().layer = value.GetValue();
            }
        ))
    ]);
    public static readonly List<ConfigType> Shielder = GroupUtils.Merge(Generic, [
        ConfigurationManager.RegisterConfigType(
            new IdConfigType("Object ID", "shielder_id", (o, value) =>
            {
                o.GetComponent<Shielder>().id = value.GetValue();
            })),
        ConfigurationManager.RegisterConfigType(
            new BoolConfigType("Start Applied", "shielder_start_applied", (o, value) =>
            {
                o.GetComponent<Shielder>().startApplied = value.GetValue();
            }).WithDefaultValue(true)),
        ConfigurationManager.RegisterConfigType(
            new BoolConfigType("Immune to Generic", "shielder_generic", (o, value) =>
            {
                if (!value.GetValue()) return;
                o.GetComponent<Shielder>().extraImmunities.Add(AttackTypes.Generic);
            }).WithDefaultValue(false)),
        ConfigurationManager.RegisterConfigType(
            new BoolConfigType("Immune to Nail", "shielder_nail", (o, value) =>
            {
                if (!value.GetValue()) return;
                o.GetComponent<Shielder>().extraImmunities.Add(AttackTypes.Nail);
            }).WithDefaultValue(false)),
        ConfigurationManager.RegisterConfigType(
            new BoolConfigType("Immune to Spell", "shielder_spell", (o, value) =>
            {
                if (!value.GetValue()) return;
                o.GetComponent<Shielder>().extraImmunities.Add(AttackTypes.Spell);
            }).WithDefaultValue(false)),
        ConfigurationManager.RegisterConfigType(
            new BoolConfigType("Immune to Acid", "shielder_acid", (o, value) =>
            {
                if (!value.GetValue()) return;
                o.GetComponent<Shielder>().extraImmunities.Add(AttackTypes.Acid);
            }).WithDefaultValue(false)),
        ConfigurationManager.RegisterConfigType(
            new BoolConfigType("Immune to Splatter", "shielder_splatter", (o, value) =>
            {
                if (!value.GetValue()) return;
                o.GetComponent<Shielder>().extraImmunities.Add(AttackTypes.Splatter);
            }).WithDefaultValue(false)),
        ConfigurationManager.RegisterConfigType(
            new BoolConfigType("Immune to Water", "shielder_water", (o, value) =>
            {
                if (!value.GetValue()) return;
                o.GetComponent<Shielder>().extraImmunities.Add(AttackTypes.RuinsWater);
            }).WithDefaultValue(false)),
        ConfigurationManager.RegisterConfigType(
            new BoolConfigType("Immune to Sharp Shadow", "shielder_sharp_shadow", (o, value) =>
            {
                if (!value.GetValue()) return;
                o.GetComponent<Shielder>().extraImmunities.Add(AttackTypes.SharpShadow);
            }).WithDefaultValue(false)),
        ConfigurationManager.RegisterConfigType(
            new BoolConfigType("Immune to Nail Beam", "shielder_nail_beam", (o, value) =>
            {
                if (!value.GetValue()) return;
                o.GetComponent<Shielder>().extraImmunities.Add(AttackTypes.NailBeam);
            }).WithDefaultValue(false))
    ]);

    public static readonly List<ConfigType> Benches = GroupUtils.Merge(Visible, [
        ConfigurationManager.RegisterConfigType(new BoolConfigType("Save Respawn", "bench_spawn",
            (o, value) =>
            {
                if (value.GetValue()) return;
                var burst = o.GetComponentsInChildren<PlayMakerFSM>()
                    .First(fsm => fsm.FsmName == "Bench Control");
                burst.FsmVariables.FindFsmBool("Set Respawn").Value = false;
            }
        ).WithDefaultValue(true)),
        ConfigurationManager.RegisterConfigType(new Vector2ConfigType("Sit Offset", "bench_offset",
            (o, value) =>
            {
                var pos = o.GetComponentsInChildren<PlayMakerFSM>()
                    .First(fsm => fsm.FsmName == "Bench Control").FsmVariables.FindFsmVector3("Adjust Vector");
                pos.Value += (Vector3)value.GetValue();
            }
        ).WithDefaultValue(Vector2.zero))
    ]);

    public static readonly List<ConfigType> TollBench = GroupUtils.Merge(Benches, [
        ConfigurationManager.RegisterConfigType(new IntConfigType("Cost", "toll_bench_cost", 
                (o, value) =>
                {
                    var fsm = o.LocateMyFSM("Toll Machine Bench");
                    fsm.GetState("Get Price")
                        .AddAction(() => fsm.FsmVariables.FindFsmInt("Toll Cost").value = value.GetValue());
                }
            ).WithDefaultValue(30))
    ]);

    public static readonly List<ConfigType> WalkTarget = GroupUtils.Merge(Generic, [
        ConfigurationManager.RegisterConfigType(new FloatConfigType("Speed", "walk_speed", 
                (o, value) =>
                {
                    o.GetComponent<WalkTarget>().speed = Mathf.Abs(value.GetValue());
                }
            ).WithDefaultValue(5).WithPriority(1)),
        ConfigurationManager.RegisterConfigType(new ChoiceConfigType("Mode", "walk_anim", 
            (o, value) =>
            {
                var wt = o.GetComponent<WalkTarget>();
                wt.anim = value.GetStringValue();
            }
        ).WithDefaultValue(0).WithOptions("Walk", "Run", "Sprint"))
    ]);

    public static readonly List<ConfigType> GrubBottle = GroupUtils.Merge(PersistentBreakable, [
        ConfigurationManager.RegisterConfigType(new BoolConfigType("Breakable", "bottle_can_break", 
                (o, value) =>
                {
                    if (!value.GetValue()) o.RemoveComponent<PlayMakerFSM>();
                }
            ).WithDefaultValue(true)),
        ConfigurationManager.RegisterConfigType(new BoolConfigType("Contains Grub", "bottle_has_grub", 
                (o, value) =>
                {
                    if (!value.GetValue())
                    {
                        o.transform.GetChild(0).GetChild(0).gameObject.SetActive(false);
                        o.transform.GetChild(1).gameObject.SetActive(false);
                    }
                }
            ).WithDefaultValue(true)),
        ConfigurationManager.RegisterConfigType(new BoolConfigType("Add to Grubs", "bottle_add_grub", 
                (o, value) =>
                {
                    if (!value.GetValue())
                    {
                        o.LocateMyFSM("Bottle Control").GetState("Set").DisableAction(0);
                    }
                }
            ).WithDefaultValue(false))
    ]);

    public static readonly List<ConfigType> ObjectSpinner = GroupUtils.Merge(Generic, [
        ConfigurationManager.RegisterConfigType(new IdConfigType("Object ID", "spinner_target", 
                (o, value) => 
                {
                    o.GetComponent<ObjectSpinner>().targetId = value.GetValue(); 
                }
            )),
            ConfigurationManager.RegisterConfigType(new FloatConfigType("Rotation Speed", "spinner_speed", 
                (o, value) =>
                {
                    o.GetComponent<ObjectSpinner>().speed = value.GetValue();
                }
            ).WithDefaultValue(100))
    ]);

    public static readonly List<ConfigType> FsmHook = GroupUtils.Merge(Generic, [
            ConfigurationManager.RegisterConfigType(new IdConfigType("Object ID", "fsm_hook_target", 
                (o, value) => 
                {
                    o.GetComponent<FsmHook>().targetId = value.GetValue();
                }
            )),
            ConfigurationManager.RegisterConfigType(
                new IntConfigType("Index", "fsm_hook_index", (o, value) =>
                {
                    o.GetComponent<FsmHook>().index = value.GetValue();
                }).WithDefaultValue(0)),
            ConfigurationManager.RegisterConfigType(new StringConfigType("FSM Name", "fsm_hook_name", 
                (o, value) => 
                {
                    o.GetComponent<FsmHook>().fsmName = value.GetValue();
                }
            )),
            ConfigurationManager.RegisterConfigType(new StringConfigType("Target State", "fsm_hook_state", 
                (o, value) => 
                {
                    o.GetComponent<FsmHook>().stateName = value.GetValue();
                }
            )),
            ConfigurationManager.RegisterConfigType(new ChoiceConfigType("Detection Mode", "fsm_hook_target_mode", 
                (o, value) => 
                {
                    o.GetComponent<FsmHook>().inject = value.GetValue() == 1;
                }
            ).WithOptions("Observe", "Inject").WithDefaultValue(0)),
            ConfigurationManager.RegisterConfigType(new StringConfigType("FsmMaster Edits", "fsm_hook_json_edits", 
                (o, value) =>
                {
                    o.GetComponent<FsmHook>().fsmMasterData = value.GetValue();
                }
            ))
    ]);

    public static readonly List<ConfigType> ComponentHook = GroupUtils.Merge(Generic, [
            ConfigurationManager.RegisterConfigType(new IdConfigType("Object ID", "component_hook_target", 
                (o, value) => 
                {
                    o.GetComponent<ComponentHook>().id = value.GetValue();
                }
            )),
            ConfigurationManager.RegisterConfigType(new StringConfigType("Component Name", "component_hook_name", 
                (o, value) => 
                {
                    o.GetComponent<ComponentHook>().componentName = value.GetValue();
                }
            )),
            ConfigurationManager.RegisterConfigType(new BoolConfigType("Recursive", "component_hook_recursive", 
                (o, value) => 
                {
                    o.GetComponent<ComponentHook>().recursive = value.GetValue();
                }
            ).WithDefaultValue(true)),
            ConfigurationManager.RegisterConfigType(new ChoiceConfigType("Mode", "component_hook_mode", 
                (o, value) =>
                {
                    o.GetComponent<ComponentHook>().mode = value.GetValue();
                }
            ).WithOptions("Destroy", "Disable", "Enable", "None").WithDefaultValue(0)),
            ConfigurationManager.RegisterConfigType(new StringConfigType("Field Name", "component_hook_field", 
                (o, value) => 
                {
                    o.GetComponent<ComponentHook>().fieldName = value.GetValue();
                }
            ))
    ]);

    public static readonly List<ConfigType> ObjectAnchor = GroupUtils.Merge(Generic, [
            ConfigurationManager.RegisterConfigType(new IdConfigType("Object ID", "anchor_target", 
                (o, value) => 
                {
                    o.GetComponent<ObjectAnchor>().targetId = value.GetValue();
                }
            )),
            ConfigurationManager.RegisterConfigType(new IdConfigType("Parent ID (Optional)", "anchor_parent", 
                (o, value) => 
                {
                    o.GetComponent<ObjectAnchor>().parentId = value.GetValue();
                }
            )),
            ConfigurationManager.RegisterConfigType(new FloatConfigType("Path Distance", "anchor_dist", 
                (o, value) => 
                {
                    o.GetComponent<ObjectAnchor>().trackDistance = value.GetValue();
                }
            ).WithDefaultValue(10)),
            ConfigurationManager.RegisterConfigType(new FloatConfigType("Start Distance", "anchor_start_dist", 
                (o, value) =>
                {
                    o.GetComponent<ObjectAnchor>().startOffset = value.GetValue();
                }
            ).WithDefaultValue(0)),
            ConfigurationManager.RegisterConfigType(new FloatConfigType("Move Speed", "anchor_speed", 
                (o, value) =>
                {
                    o.GetComponent<ObjectAnchor>().speed = value.GetValue();
                }
            ).WithDefaultValue(0)),
            ConfigurationManager.RegisterConfigType(new FloatConfigType("Path Rotation", "anchor_rot", 
                (o, value) =>
                {
                    o.GetComponent<ObjectAnchor>().startRotation = value.GetValue();
                }
            ).WithDefaultValue(0)),
            ConfigurationManager.RegisterConfigType(new FloatConfigType("Path Rotation over Time", "anchor_rot_speed", 
                (o, value) =>
                {
                    o.GetComponent<ObjectAnchor>().rotationSpeed = value.GetValue();
                }
            ).WithDefaultValue(0)),
            ConfigurationManager.RegisterConfigType(new FloatConfigType("Smoothing", "anchor_smoothing", 
                (o, value) =>
                {
                    o.GetComponent<ObjectAnchor>().smoothing = value.GetValue();
                }
            ).WithDefaultValue(0.5f)),
            ConfigurationManager.RegisterConfigType(new FloatConfigType("Pause Time", "anchor_pause_time", 
                (o, value) =>
                {
                    o.GetComponent<ObjectAnchor>().pauseTime = value.GetValue();
                }
            ).WithDefaultValue(0)),
            ConfigurationManager.RegisterConfigType(new BoolConfigType("Stick Player", "anchor_stick", 
                (o, value) =>
                {
                    o.GetComponent<ObjectAnchor>().stickPlayer = value.GetValue();
                }
            )),
            ConfigurationManager.RegisterConfigType(new BoolConfigType("Start Moving", "anchor_moving", 
                (o, value) =>
                {
                    o.GetComponent<ObjectAnchor>().moving = value.GetValue();
                }
            ).WithDefaultValue(true))
    ]);

    public static readonly List<ConfigType> ObjectExtractor = GroupUtils.Merge(Generic, [
            ConfigurationManager.RegisterConfigType(new StringConfigType("Scene ID", "extractor_scene_id", 
                (o, value) => 
                {
                    o.GetComponent<ObjectExtractor>().sceneName = value.GetValue();
                }
            )),
            ConfigurationManager.RegisterConfigType(new IdConfigType("Object Path", "extractor_path", 
                (o, value) => 
                {
                    o.GetComponent<ObjectExtractor>().path = value.GetValue();
                }
            ))
    ]);

    public static readonly List<ConfigType> ObjectMover = GroupUtils.Merge(Generic, [
            ConfigurationManager.RegisterConfigType(new IdConfigType("Object ID", "mover_target", 
                (o, value) => 
                {
                    o.GetComponent<ObjectMover>().targetId = value.GetValue();
                }
            )),
            ConfigurationManager.RegisterConfigType(new Vector2ConfigType("Offset", "mover_offset", 
                (o, value) =>
                {
                    var om = o.GetComponent<ObjectMover>();
                    var val = value.GetValue();
                    om.xOffset = val.x;
                    om.yOffset = val.y;
                }
            ).WithDefaultValue(Vector2.zero)),
            ConfigurationManager.RegisterConfigType(new FloatConfigType("Rotation", "mover_rot", 
                (o, value) => 
                {
                    o.GetComponent<ObjectMover>().rotation = value.GetValue();
                }
            ).WithDefaultValue(0)),
            ConfigurationManager.RegisterConfigType(new BoolConfigType("Clear Velocity", "mover_clear_vel", 
                (o, value) => 
                {
                    o.GetComponent<ObjectMover>().clearVelocity = value.GetValue();
                }
            ).WithDefaultValue(true)),
            ConfigurationManager.RegisterConfigType(new BoolConfigType("Move X", "mover_do_x", 
                (o, value) => 
                {
                    o.GetComponent<ObjectMover>().moveX = value.GetValue();
                }
            ).WithDefaultValue(true)),
            ConfigurationManager.RegisterConfigType(new BoolConfigType("Move Y", "mover_do_y", 
                (o, value) => 
                {
                    o.GetComponent<ObjectMover>().moveY = value.GetValue();
                }
            ).WithDefaultValue(true)),
            ConfigurationManager.RegisterConfigType(new ChoiceConfigType("Position Source", "mover_mode", 
                (o, value) => 
                {
                    o.GetComponent<ObjectMover>().moveMode = value.GetValue();
                }
            ).WithOptions("Mover", "Self", "Player", "Absolute").WithDefaultValue(0)),
            ConfigurationManager.RegisterConfigType(new IdConfigType("Position Source ID", "mover_mode_2", 
                (o, value) =>
                {
                    o.GetComponent<ObjectMover>().moveTarget = value.GetValue();
                }
            ).WithPriority(1))
    ]);

    private static readonly ConfigType Invincible =
        ConfigurationManager.RegisterConfigType(
            new BoolConfigType("Invincible", "invulnerable",
                (o, value) =>
                {
                    if (!value.GetValue()) return;
                    o.AddComponent<EnemyInvulnerabilityMarker>();
                }));

    public static readonly List<ConfigType> SimpleEnemies = GroupUtils.Merge(Visible, [
        ConfigurationManager.RegisterConfigType(
            new IntConfigType("Small Geo Drops", "small_money",
                (o, value) => { o.GetComponentInChildren<HealthManager>(true).SetGeoSmall(value.GetValue()); })),
        ConfigurationManager.RegisterConfigType(
            new IntConfigType("Medium Geo Drops", "med_money",
                (o, value) => { o.GetComponentInChildren<HealthManager>(true).SetGeoMedium(value.GetValue()); })),
        ConfigurationManager.RegisterConfigType(
            new IntConfigType("Large Geo Drops", "large_money",
                (o, value) => { o.GetComponentInChildren<HealthManager>(true).SetGeoLarge(value.GetValue()); })),
        ConfigurationManager.RegisterConfigType(
            new BoolConfigType("Fling Drops", "fling_money",
                (o, value) => { o.GetComponentInChildren<HealthManager>(true).megaFlingGeo = value.GetValue(); })),
        ConfigurationManager.RegisterConfigType(
            new BoolConfigType("Give Soul", "give_silk",
                (o, value) =>
                {
                    o.GetComponentInChildren<HealthManager>(true).enemyType = value.GetValue() ? 1 : 6;
                })),
        ConfigurationManager.RegisterConfigType(
            new BoolConfigType("Can Pogo On", "enemy_can_pogo",
                (o, value) => {
                    if (!value.GetValue())
                    {
                        foreach (var t in o.GetComponentsInChildren<Transform>(true)) 
                            t.gameObject.AddComponent<NonBouncer>();
                    } })),
        ConfigurationManager.RegisterConfigType(
            new FloatConfigType("Recoil Speed", "enemy_recoil_speed",
                (o, value) =>
                {
                    var rc = o.GetComponent<Recoil>();
                    if (!rc) return;
                    rc.SetRecoilSpeed(value.GetValue());
                })),
        ConfigurationManager.RegisterConfigType(
            new BoolConfigType("Increment Journal", "enemy_increment_journal",
                (o, value) => {
                {
                    if (!value.GetValue()) o.AddComponent<JournalKillDisablerMarker>();
                } })),
        Invincible
    ]);

    public static readonly List<ConfigType> Wingsmould = GroupUtils.Merge(SimpleEnemies, [
        ConfigurationManager.RegisterConfigType(
            new BoolConfigType("Stationary", "wingsmould_stationary",
                (o, value) =>
                {
                    if (!value.GetValue()) return;
                    o.LocateMyFSM("Control").FsmVariables.FindFsmBool("Stationary").Value = true;
                }).WithDefaultValue(true))
    ]);

    public static readonly List<ConfigType> WhiteSaw = GroupUtils.Merge(Visible, [
        ConfigurationManager.RegisterConfigType(
            new BoolConfigType("Accurate Hitbox", "wp_saw_accurate",
                (o, value) =>
                {
                    if (value.GetValue()) return;
                    CircleCollider2D cc2d = null;
                    foreach (var c in o.GetComponents<CircleCollider2D>())
                    {
                        if (!cc2d || c.radius > cc2d.radius) cc2d = c;
                    }

                    if (cc2d) cc2d.enabled = false;
                }).WithDefaultValue(false))
    ]);

    public static readonly List<ConfigType> NonPersistentEnemies = GroupUtils.Merge(SimpleEnemies, [
        ConfigurationManager.RegisterConfigType(
            new IntConfigType("Health", "enemy_hp",
                (o, value) =>
                {
                    o.GetComponentInChildren<HealthManager>(true).hp = value.GetValue();
                }).WithPriority(-1))
    ]);

    public static readonly List<ConfigType> Enemies = GroupUtils.Merge(NonPersistentEnemies, [
        ConfigurationManager.RegisterConfigType(MakePersistenceConfigType("Stay Dead", "enemy_stay_dead", 
            (o, item) => {
                item.OnSetSaveState += b =>
                {
                    o.GetComponent<HealthManager>().isDead = b;
                };
                item.OnGetSaveState += (ref b) => { b = o.GetComponent<HealthManager>().isDead; };
            }))
    ]);

    public static readonly List<ConfigType> Duranda = GroupUtils.Merge(Enemies, [
        ConfigurationManager.RegisterConfigType(
            new Vector2ConfigType("Move Offset", "duranda_move_offset",
                (o, value) =>
                {
                    o.LocateMyFSM("Tween").FsmVariables.FindFsmVector3("Move Vector").Value = value.GetValue();
                }).WithDefaultValue(new Vector2(0, 5)))
    ]);

    public static readonly List<ConfigType> Gorb = GroupUtils.Merge(Enemies, [
        ConfigurationManager.RegisterConfigType(
            new ChoiceConfigType("Position Mode", "gorb_pos_mode",
                (o, value) =>
                {
                    if (value.GetValue() == 0) return;
                    o.GetComponent<EnemyFixers.Gorb>().posPlayer = true;
                }).WithOptions("Spawn", "Player").WithDefaultValue(0))
    ]);
    
    public static readonly List<ConfigType> OomaCore =  GroupUtils.Merge(Visible, [
        ConfigurationManager.RegisterConfigType(
            new BoolConfigType("Can Pogo Explosion", "can_pogo_ooma_core_explosion", (o, value) =>
            {
                if (!value.GetValue()) return;
                ((SpawnObjectFromGlobalPool)o.LocateMyFSM("Lil Jelly").GetState("Die").actions[1])
                    .gameObject = MiscFixers.PogoableExplosionL;
            }).WithDefaultValue(false))
    ]);
    
    public static readonly List<ConfigType> SporgBullet =  GroupUtils.Merge(Visible, [
        ConfigurationManager.RegisterConfigType(
            new BoolConfigType("Can Pogo Explosion", "can_pogo_sporg_bullet_explosion", (o, value) =>
            {
                if (!value.GetValue()) return;
                ((SpawnObjectFromGlobalPool)o.LocateMyFSM("Spore Bomb").GetState("Explode").actions[1])
                    .gameObject = MiscFixers.PogoableExplosionM;
            }).WithDefaultValue(false))
    ]);
    
    public static readonly List<ConfigType> Sporg =  GroupUtils.Merge(Enemies, [
        ConfigurationManager.RegisterConfigType(
            new BoolConfigType("Can Pogo Bullet Explosion", "can_pogo_sporg_explosion", (o, value) =>
            {
                if (!value.GetValue()) return;
                var fsm = o.LocateMyFSM("Shroom Turret");
                var projectile = fsm.FsmVariables.FindFsmGameObject("Projectile");
                fsm.GetState("Fire").AddAction(() =>
                {
                    var pObj = projectile.value;
                    if (!pObj) return;
                    ((SpawnObjectFromGlobalPool)pObj.LocateMyFSM("Spore Bomb").GetState("Explode").actions[1])
                        .gameObject = MiscFixers.PogoableExplosionM;
                }, 3);
            }).WithDefaultValue(false)),
        ConfigurationManager.RegisterConfigType(
            new Vector2ConfigType("Alert Range", "sporg_alert_range", (o, value) =>
            {
                o.transform.Find("Hero Detect").GetComponent<BoxCollider2D>().size = value.GetValue();
            }).WithDefaultValue(new Vector2(25, 10)))
    ]);

    public static readonly List<ConfigType> Teleplane = GroupUtils.Merge(Enemies, [
        ConfigurationManager.RegisterConfigType(
            new Vector2ConfigType("Teleplane Size", "teleplane_size",
                (o, value) =>
                {
                    o.GetComponent<EnemyFixers.Teleplane>().collider.size = value.GetValue();
                }).WithDefaultValue(new Vector2(10, 10)))
    ]);

    public static readonly List<ConfigType> ShadeSibling = GroupUtils.Merge(Visible, [
        ConfigurationManager.RegisterConfigType(
            new BoolConfigType("Ignore Void Heart", "shade_ignore_vh",
                (o, value) =>
                {
                    if (!value.GetValue()) return;
                    var friendly = o.LocateMyFSM("Control").GetState("Friendly?");
                    for (var i = 2; i <= 7; i++) friendly.DisableAction(i);
                }).WithDefaultValue(false))
    ]);

    public static readonly List<ConfigType> Shade = GroupUtils.Merge(Visible, [
        ConfigurationManager.RegisterConfigType(
            new IntConfigType("Health Override", "player_shade_hp",
                (o, value) =>
                {
                    o.GetComponent<EnemyFixers.Shade>().hp = value.GetValue();
                })),
        ConfigurationManager.RegisterConfigType(
            new ChoiceConfigType("Fireball Level", "player_shade_fireball",
                (o, value) =>
                {
                    o.GetComponent<EnemyFixers.Shade>().spirit = value.GetValue();
                }).WithOptions("Default", "None", "1", "2")),
        ConfigurationManager.RegisterConfigType(
            new ChoiceConfigType("Dive Level", "player_shade_dive",
                (o, value) =>
                {
                    o.GetComponent<EnemyFixers.Shade>().dive = value.GetValue();
                }).WithOptions("Default", "None", "1", "2")),
        ConfigurationManager.RegisterConfigType(
            new ChoiceConfigType("Wraiths Level", "player_shade_wraths",
                (o, value) =>
                {
                    o.GetComponent<EnemyFixers.Shade>().wraiths = value.GetValue();
                }).WithOptions("Default", "None", "1", "2")),
        ConfigurationManager.RegisterConfigType(
            new ChoiceConfigType("Friendly", "player_shade_friendly",
                (o, value) =>
                {
                    if (value.GetValue() == 0) return;
                    o.GetComponent<EnemyFixers.Shade>().friendly = value.GetValue();
                }).WithOptions("Default", "Friendly", "Unfriendly").WithDefaultValue(0)),
        ConfigurationManager.RegisterConfigType(
            new BoolConfigType("Shade Death Counts", "player_shade_counts",
                (o, value) =>
                {
                    if (!value.GetValue()) return;
                    o.GetComponent<EnemyFixers.Shade>().countDead = true;
                }).WithDefaultValue(false))
    ]);
    
    public static readonly List<ConfigType> AbyssTendrils = GroupUtils.Merge(Generic, [
        ConfigurationManager.RegisterConfigType(
            new BoolConfigType("Ignore Void Heart", "tendrils_ignore_vh",
                (o, value) =>
                {
                    if (!value.GetValue()) return;
                    o.LocateMyFSM("Black Charm").enabled = false;
                }).WithDefaultValue(true))
    ]);
    
    public static readonly List<ConfigType> BounceShroom = GroupUtils.Merge(Visible, [
        ConfigurationManager.RegisterConfigType(
            new BoolConfigType("Side Bounces", "bounce_shroom_randomness",
                (o, value) =>
                {
                    if (value.GetValue()) return;
                    o.AddComponent<BounceRandomnessDisabler>();
                }).WithDefaultValue(true))
    ]);

    public static readonly List<ConfigType> Wakeable = GroupUtils.Merge(Enemies, [
        ConfigurationManager.RegisterConfigType(
            new BoolConfigType("Start Awake", "enemy_starts_awake",
                (o, value) =>
                {
                    if (!value.GetValue()) return;
                    o.GetComponent<EnemyFixers.Wakeable>().Wake();
                }).WithDefaultValue(true))
    ]);

    public static readonly List<ConfigType> Swooper = GroupUtils.Merge(Enemies, [
        ConfigurationManager.RegisterConfigType(
            new BoolConfigType("Swoop In", "enemy_swoop_in",
                (o, value) =>
                {
                    if (!value.GetValue()) return;
                    o.GetComponent<EnemyFixers.Swooper>().swoopIn = true;
                }).WithDefaultValue(false).WithPriority(-1))
    ]);

    public static readonly List<ConfigType> Mantis = GroupUtils.Merge(Enemies, [
        ConfigurationManager.RegisterConfigType(
            new BoolConfigType("Ignore Respect", "mantis_ignore_respect",
                (o, value) =>
                {
                    if (!value.GetValue()) return;
                    var fsm = o.LocateMyFSM("Mantis");
                    fsm.GetState("Lords Defeated?").AddAction(() => fsm.SendEvent("FALSE"), 0);
                }).WithDefaultValue(false))
    ]);

    public static readonly List<ConfigType> MantisChild = GroupUtils.Merge(Enemies, [
        ConfigurationManager.RegisterConfigType(
            new BoolConfigType("Ignore Respect", "mantis_child_ignore_respect",
                (o, value) =>
                {
                    if (!value.GetValue()) return;
                    var fsm = o.LocateMyFSM("Mantis Flyer");
                    fsm.GetState("Lords Defeated").AddAction(() => fsm.SendEvent("TOOK DAMAGE"), 0);
                }).WithDefaultValue(false))
    ]);

    public static readonly List<ConfigType> LittleWeaver = GroupUtils.Merge(Enemies, [
        ConfigurationManager.RegisterConfigType(
            new BoolConfigType("Start Alert", "little_weaver_starts_awake",
                (o, value) =>
                {
                    if (!value.GetValue()) return;
                    o.LocateMyFSM("Control").FsmVariables.FindFsmBool("Start Alert").Value = true;
                }).WithDefaultValue(true))
    ]);

    public static readonly List<ConfigType> GruzMother = GroupUtils.Merge(Wakeable, [
        ConfigurationManager.RegisterConfigType(
            new BoolConfigType("Spawn Gruzzers", "gruz_mother_spawns",
                (o, value) =>
                {
                    if (value.GetValue()) return;
                    o.GetComponent<EnemyFixers.GruzMother>().spawnGruzzers = false;
                }).WithDefaultValue(true))
    ]);

    public static readonly List<ConfigType> Thk = GroupUtils.Merge(Wakeable, [
        ConfigurationManager.RegisterConfigType(
            new BoolConfigType("Phase Roar Stun", "thk_roar_stun",
                (o, value) =>
                {
                    if (value.GetValue()) return;
                    var fsm = o.LocateMyFSM("Control");
                    fsm.GetState("Roar").DisableActions(4, 5);
                    fsm.GetState("Roar P3").DisableActions(5, 6);
                }).WithDefaultValue(false))
    ]);

    public static readonly List<ConfigType> Oblobble = GroupUtils.Merge(Swooper, [
        ConfigurationManager.RegisterConfigType(
            new BoolConfigType("Cause Anger on Death", "oblobble_cause_anger",
                (o, value) =>
                {
                    if (!value.GetValue()) return;
                    o.GetComponent<EnemyFixers.Oblobble>().angerOthers = true;
                })
                .WithDefaultValue(false).WithPriority(-1)),
        ConfigurationManager.RegisterConfigType(
            new ChoiceConfigType("Anger Mode", "oblobble_self_anger",
                (o, value) =>
                {
                    o.GetComponent<EnemyFixers.Oblobble>().getAngry = value.GetValue();
                }).WithOptions("None", "Vanilla", "Start")
                .WithDefaultValue(0).WithPriority(-1))
    ]);

    public static readonly List<ConfigType> Npcs = GroupUtils.Merge(Visible, [
        ConfigurationManager.RegisterConfigType(new StringConfigType("Dialogue", "npc_dialogue", (o, value) =>
        {
            o.GetComponent<MiscFixers.Npc>().dialogue = value.GetValue();
        }).WithDefaultValue("Sample Text"))
    ]);

    public static readonly List<ConfigType> GhostNpcs = GroupUtils.Merge(Npcs, [
        ConfigurationManager.RegisterConfigType(MakePersistenceConfigType("Stay Dead", "ghost_stay_dead"))
    ]);

    public static readonly List<ConfigType> Midwife = GroupUtils.Merge(Npcs, [
        ConfigurationManager.RegisterConfigType(new BoolConfigType("Start Angered", "midwife_angry", (o, value) =>
        {
            if (!value.GetValue()) return;
            var fsm = o.transform.GetChild(1).gameObject.LocateMyFSM("Conversation Control");
            fsm.GetState("Idle").AddAction(() => fsm.SetState("Talk Finish"));
        }).WithDefaultValue(false))
    ]);

    public static readonly List<ConfigType> TollMachine = GroupUtils.Merge(Npcs, [
        ConfigurationManager.RegisterConfigType(new IntConfigType("Cost", "toll_machine_cost", 
            (o, value) =>
            {
                var fsm = o.LocateMyFSM("Toll Machine");
                fsm.GetState("Get Price")
                    .AddAction(() => fsm.FsmVariables.FindFsmInt("Toll Cost").value = value.GetValue());
            }
        ).WithDefaultValue(30)),
        ConfigurationManager.RegisterConfigType(MakePersistenceConfigType("Stay Paid", "toll_stay"))
    ]);

    public static readonly List<ConfigType> RadiancePlat = GroupUtils.Merge(Visible, [
        ConfigurationManager.RegisterConfigType(new BoolConfigType("Start Up", "radiant_plat_start_up", (o, value) =>
        {
            if (!value.GetValue()) return;
            o.LocateMyFSM("radiant_plat").SendEvent("APPEAR");
        }).WithDefaultValue(true))
    ]);

    public static readonly List<ConfigType> Elderbug = GroupUtils.Merge(Npcs, [
        ConfigurationManager.RegisterConfigType(new BoolConfigType("Has Flower", "elderbug_flower", (o, value) =>
        {
            var flower = o.transform.GetChild(3).gameObject;
            flower.RemoveComponent<PlayMakerFSM>();
            flower.SetActive(value.GetValue());
        }).WithDefaultValue(false))
    ]);

    public class BounceRandomnessDisabler : MonoBehaviour;

    static ConfigGroup()
    {
        ConfigurationManager.RegisterConfigType(
            new FloatConfigType("Z Offset", "obj_z",
                (o, value) => { o.transform.SetPositionZ(o.transform.GetPositionZ() + value.GetValue()); },
                (o, value, arg3) =>
                {
                    if (arg3 == ConfigurationManager.PreviewContext.Cursor)
                    {
                        CursorManager.Offset.z += value.GetValue();
                    }
                    else o.transform.SetPositionZ(o.transform.GetPositionZ() + value.GetValue());
                }).WithDefaultValue(0));
        
        ConfigurationManager.RegisterConfigType(
            new FloatConfigType("Z Offset", "enemy_z",
                (o, value) =>
                {
                    var sz = o.GetComponent<SetZ>();
                    if (sz) sz.z += value.GetValue();
                    o.transform.SetPositionZ(o.transform.GetPositionZ() + value.GetValue());
                },
                (o, value, arg3) =>
                {
                    if (arg3 == ConfigurationManager.PreviewContext.Cursor)
                    {
                        CursorManager.Offset.z += value.GetValue();
                    }
                    else o.transform.SetPositionZ(o.transform.GetPositionZ() + value.GetValue());
                }).WithDefaultValue(0));
        
        typeof(HealthManager).Hook(nameof(HealthManager.IsBlockingByDirection),
            (Func<HealthManager, int, AttackTypes, bool> orig, HealthManager self, int cardinalDirection,
                    AttackTypes attackType) =>
                self.GetComponentInParent<EnemyInvulnerabilityMarker>() ||
                orig(self, cardinalDirection, attackType));

        _ = new Hook(typeof(HealthManager).GetProperty(nameof(HealthManager.IsInvincible))!.GetGetMethod(),
            (Func<HealthManager, bool> orig, HealthManager self) => 
                self.GetComponentInParent<EnemyInvulnerabilityMarker>() || orig(self));
        
        typeof(EnemyDeathEffects).Hook(nameof(EnemyDeathEffects.RecordKillForJournal),
            (Action<EnemyDeathEffects> orig, EnemyDeathEffects self) =>
            {
                if (self.GetComponentInParent<JournalKillDisablerMarker>()) return;
                orig(self);
            });

        On.BounceShroom.Start += (orig, self) =>
        {
            if (!self.GetComponent<BounceRandomnessDisabler>())
            {
                orig(self);
                return;
            }
            
            if (!self.active) return;
            var gameObject = Object.Instantiate(self.idleParticlePrefab);
            if ((bool) (Object) gameObject)
            {
                gameObject.transform.SetPositionX(self.transform.position.x);
                gameObject.transform.SetPositionY(self.transform.position.y);
            }
            self.idleRoutine = self.StartCoroutine(self.Idle());
            foreach (var cee in self.GetComponentsInChildren<CollisionEnterEvent>())
            {
                cee.OnCollisionEnteredDirectional += (direction, _) =>
                {
                    if (direction == CollisionEnterEvent.Direction.Top)
                        HeroController.instance.SendMessage("Bounce");
                    self.BounceSmall();
                };
            }
        };
    }

    public class EnemyInvulnerabilityMarker : MonoBehaviour;
    public class JournalKillDisablerMarker : MonoBehaviour;
    
    public static readonly List<ConfigType> Velocity = GroupUtils.Merge(Gravity, [
        ConfigurationManager.RegisterConfigType(
            new FloatConfigType("X Velocity", "velocity_apply_x", (o, value) =>
            {
                o.GetOrAddComponent<VelocityApplier>().x = value.GetValue();
            }).WithDefaultValue(0)),
        ConfigurationManager.RegisterConfigType(
            new FloatConfigType("Y Velocity", "velocity_apply_y", (o, value) =>
            {
                o.GetOrAddComponent<VelocityApplier>().y = value.GetValue();
            }).WithDefaultValue(0))
    ]);

    public static readonly List<ConfigType> Stomper = GroupUtils.Merge(Generic, [
        ConfigurationManager.RegisterConfigType(
            new FloatConfigType("Speed", "stomper_speed", (o, value) =>
            {
                o.GetComponentInChildren<Animator>().speed *= value.GetValue();
            }).WithDefaultValue(1)),
        ConfigurationManager.RegisterConfigType(
            new FloatConfigType("Offset", "stomper_offset", (o, value) =>
            {
                var anim = o.GetComponentInChildren<Animator>();
                ArchitectPlugin.Instance.StartCoroutine(ToggleAnimator());

                return;

                IEnumerator ToggleAnimator()
                {
                    if (!anim) yield break;
                    anim.enabled = false;
                    yield return new WaitForSeconds(value.GetValue());
                    if (!anim) yield break;
                    anim.enabled = true;
                }
            }).WithDefaultValue(0))
    ]);

    public static readonly List<ConfigType> ChargedLumaflies = GroupUtils.Merge(Generic, [
        ConfigurationManager.RegisterConfigType(
            new FloatConfigType("Offset", "lumaflies_offset", (o, value) =>
            {
                if (value.GetValue() == 0) return;
                var fsm = o.LocateMyFSM("zap control");
                var pause = fsm.GetState("Pause");
                pause.DisableAction(0);
                pause.AddAction(new Wait
                {
                    finishEvent = FsmEvent.Finished,
                    time = value.GetValue()
                });
            }).WithDefaultValue(0).WithPriority(-1)),
        ConfigurationManager.RegisterConfigType(
            new FloatConfigType("Delay", "lumaflies_delay", (o, value) =>
            {
                o.LocateMyFSM("zap control").FsmVariables.FindFsmFloat("Wait Time").Value = value.GetValue();
            }).WithDefaultValue(0.8f))
    ]);

    public static readonly ConfigType PngUrl = ConfigurationManager.RegisterConfigType(
        new StringConfigType("PNG URL", "png_url",
            (o, value) => { o.GetComponentInChildren<PngObject>().url = value.GetValue(); }, (o, value, _) =>
            {
                var prev = o.GetOrAddComponent<PngPreview>();
                var point = (prev?.point).GetValueOrDefault(true);
                var ppu = (prev?.ppu).GetValueOrDefault(100);
                var vcount = (prev?.vcount).GetValueOrDefault(1);
                var hcount = (prev?.hcount).GetValueOrDefault(1);
                CustomAssetManager.DoLoadSprite(value.GetValue(), point, ppu, hcount, vcount,
                    sprites =>
                    {
                        if (o) o.GetComponent<SpriteRenderer>().sprite = sprites[0];
                        EditorUI.RefreshItem();
                    });
            }).WithPriority(-1));

    public static readonly ConfigType Aa = ConfigurationManager.RegisterConfigType(
        new BoolConfigType("Anti Aliasing", "png_antialias",
                (o, value) => { o.GetComponentInChildren<PngObject>().point = !value.GetValue(); },
                (o, value, _) => { o.GetOrAddComponent<PngPreview>().point = !value.GetValue(); })
            .WithDefaultValue(true).WithPriority(-2));

    public static readonly List<ConfigType> Png = GroupUtils.Merge(Generic, [
        PngUrl,
        Aa,
        ConfigurationManager.RegisterConfigType(
            new FloatConfigType("Pixels Per Unit", "png_ppu",
                    (o, value) => { o.GetComponentInChildren<PngObject>().ppu = value.GetValue(); },
                    (o, value, _) => { o.GetOrAddComponent<PngPreview>().ppu = value.GetValue(); })
                .WithDefaultValue(100)
                .WithPriority(-2)),
        ConfigurationManager.RegisterConfigType(
            new DoubleIntConfigType("Frame Counts", "png_allframecount",
                    (o, value) =>
                    {
                        var p = o.GetComponentInChildren<PngObject>();
                        var val = value.GetValue();
                        p.vcount = val.Item1;
                        p.hcount = val.Item2;
                    },
                    (o, value, _) =>
                    {
                        var p = o.GetOrAddComponent<PngPreview>();
                        var val = value.GetValue();
                        p.vcount = val.Item1;
                        p.hcount = val.Item2;
                    })
                .WithDefaultValue((1, 1))
                .WithPriority(-2)),
        ConfigurationManager.RegisterConfigType(
            new IntConfigType("Empty Frame Count", "png_eframecount",
                    (o, value) => { o.GetComponentInChildren<PngObject>().dummy = value.GetValue(); })
                .WithDefaultValue(0)
                .WithPriority(-2)),
        ConfigurationManager.RegisterConfigType(
            new IntConfigType("Start Frame", "png_sframe",
                    (o, value) =>
                    {
                        o.GetComponentInChildren<PngObject>().frame = Math.Max(0, value.GetValue());
                    })
                .WithDefaultValue(0)
                .WithPriority(-2)),
        ConfigurationManager.RegisterConfigType(
            new FloatConfigType("Frames per Second", "png_frametime",
                    (o, value) =>
                    {
                        var png = o.GetComponentInChildren<PngObject>();
                        if (value.GetValue() == 0) png.frameTime = 0;
                        else png.frameTime = 1 / Mathf.Max(0.01f, value.GetValue());
                    })
                .WithDefaultValue(10)
                .WithPriority(-2)),
        ConfigurationManager.RegisterConfigType(
            new BoolConfigType("Loop", "png_loop",
                (o, value) =>
                {
                    o.GetComponentInChildren<PngObject>().loop = value.GetValue();
                }).WithDefaultValue(true).WithPriority(-2)),
        ConfigurationManager.RegisterConfigType(
            new BoolConfigType("Play on Start", "png_start_playing",
                (o, value) =>
                {
                    o.GetComponentInChildren<PngObject>().playing = value.GetValue();
                }).WithDefaultValue(true).WithPriority(-2))
    ]);

    public static readonly List<ConfigType> PhysicalPng = GroupUtils.Merge(Png,
        GroupUtils.Merge(StretchColourDecor, []));

    public static readonly List<ConfigType> ZoteHead = GroupUtils.Merge(Png,
        GroupUtils.Merge(TriggerActivator, []));

    public static readonly List<ConfigType> FullPng = GroupUtils.Merge(PhysicalPng, 
        [
            ConfigurationManager.RegisterConfigType(
                new BoolConfigType("Light Reflection", "png_glow",
                        (o, value) => { o.GetComponentInChildren<PngObject>().glow = value.GetValue(); })
                    .WithDefaultValue(false)
                    .WithPriority(-2))
        ]);

    public static readonly List<ConfigType> PngUI = GroupUtils.Merge(Png, [
        RenderLayer,
        ConfigurationManager.RegisterConfigType(
            new Vector2ConfigType("Offset", "ui_png_offset",
                (o, value) =>
                {
                    var uo = o.GetOrAddComponent<UIPngObject>();
                    var val = value.GetValue();
                    uo.xOffset = val.x;
                    uo.yOffset = val.y;
                }).WithDefaultValue(Vector2.zero).WithPriority(-1)),
        ConfigurationManager.RegisterConfigType(
            new ChoiceConfigType("Shift With", "ui_png_anchor",
                (o, value) =>
                {
                    o.GetOrAddComponent<UIPngObject>().anchorTo = value.GetValue();
                }).WithOptions("None", "LastMask").WithDefaultValue(0).WithPriority(-1))
    ]);

    private static readonly ConfigType WavUrl = ConfigurationManager.RegisterConfigType(
        new StringConfigType("WAV URL", "wav_url",
            (o, value) => { o.GetComponentInChildren<WavObject>().url = value.GetValue(); }).WithPriority(-1));
    
    public static readonly List<ConfigType> Wav = GroupUtils.Merge(Generic, [
        WavUrl,
        ConfigurationManager.RegisterConfigType(
            new FloatConfigType("Volume", "wav_volume",
                (o, value) => { o.GetComponent<WavObject>().Volume = value.GetValue(); })
                .WithDefaultValue(1)),
        ConfigurationManager.RegisterConfigType(
            new FloatConfigType("Pitch", "wav_pitch",
                (o, value) => { o.GetComponent<WavObject>().pitch = value.GetValue(); })
                .WithDefaultValue(1)),
        ConfigurationManager.RegisterConfigType(
            new ChoiceConfigType("Sound Type", "wav_mode",
                (o, value) => { o.GetComponent<WavObject>().globalSound = value.GetValue() == 1; })
                .WithOptions("Local", "Global").WithDefaultValue(1)),
        ConfigurationManager.RegisterConfigType(
            new ChoiceConfigType("Volume Type", "wav_volume_mode",
                (o, value) => { o.GetComponent<WavObject>().soundType = value.GetValue(); })
                .WithOptions("Master", "Music", "Sound").WithDefaultValue(0)
                .WithPriority(-1)),
        ConfigurationManager.RegisterConfigType(
            new BoolConfigType("Loop Sound", "wav_loop",
                (o, value) => { o.GetComponent<WavObject>().loop = value.GetValue(); })
                .WithDefaultValue(false)),
        ConfigurationManager.RegisterConfigType(
            new StringConfigType("Sync ID", "wav_sync_id",
                (o, value) => { o.GetComponent<WavObject>().syncId = value.GetValue(); }))
    ]);

    public static readonly List<ConfigType> Mp4 = GroupUtils.Merge(Decorations, [
        ConfigurationManager.RegisterConfigType(
            new StringConfigType("MP4 URL", "mp4_url",
                (o, value) => { o.GetOrAddComponent<Mp4Object>().url = value.GetValue(); }, (o, value, context) =>
                {
                    var player = o.GetOrAddComponent<VideoPlayer>();
                    if (player.playbackSpeed > 0) player.playbackSpeed = 0;
                    CustomAssetManager.DoLoadVideo(player,
                        context == ConfigurationManager.PreviewContext.Cursor ? null : o.transform.GetScaleX(),
                        value.GetValue());
                }).WithPriority(-1)),
        ConfigurationManager.RegisterConfigType(
            new BoolConfigType("Play on Start", "mp4_start_playing",
                (o, value) =>
                {
                    o.GetOrAddComponent<Mp4Object>().playOnStart = value.GetValue();
                }).WithDefaultValue(true).WithPriority(-1)),
        ConfigurationManager.RegisterConfigType(
            new BoolConfigType("Loop Video", "mp4_loop",
                    (o, value) =>
                    {
                        if (!value.GetValue()) return;
                        o.GetComponent<VideoPlayer>().isLooping = value.GetValue();
                    })
                .WithDefaultValue(false)),
        ConfigurationManager.RegisterConfigType(
            new FloatConfigType("Playback Speed", "mp4_speed",
                (o, value) => { o.GetComponent<VideoPlayer>().playbackSpeed = value.GetValue(); })
                .WithDefaultValue(1)),
        ConfigurationManager.RegisterConfigType(
            new BoolConfigType("Light Reflection", "mp4_glow",
                    (o, value) => { o.GetComponentInChildren<Mp4Object>().glow = value.GetValue(); })
                .WithDefaultValue(false)
                .WithPriority(-2)),
        AlphaColour
    ]);

    private static readonly int ActiveRegion = LayerMask.NameToLayer("ActiveRegion");
    private static readonly int SoftTerrain = LayerMask.NameToLayer("Soft Terrain");

    public static readonly List<ConfigType> TriggerZones = GroupUtils.Merge(Stretchable, [
        ConfigurationManager.RegisterConfigType(
            new ChoiceConfigType("Trigger Type", "trigger_type",
                    (o, value) =>
                    {
                        var val = value.GetValue();
                        o.GetComponent<TriggerZone>().mode = val;

                        if (val == 3)
                        {
                            o.layer = ActiveRegion;
                            o.AddComponent<Rigidbody2D>().constraints = RigidbodyConstraints2D.FreezeAll;
                        }
                        else o.layer = SoftTerrain;
                    })
                .WithOptions("Player", "Nail Swing", "Enemy", "Other Zone", "Activator").WithDefaultValue(0).WithPriority(-1)),
        ConfigurationManager.RegisterConfigType(
            new ChoiceConfigType("Shape", "trigger_shape",
                    (o, value) =>
                    {
                        if (value.GetValue() == 0) return;
                        o.GetComponent<PolygonCollider2D>().enabled = true;
                        o.GetComponent<BoxCollider2D>().enabled = false;
                    }, (o, value, _) =>
                    {
                        o.GetComponent<SpriteRenderer>().sprite =
                            value.GetValue() == 0 ? TriggerZone.SquareZone : TriggerZone.CircleZone;
                    })
                .WithOptions("Square", "Circle").WithDefaultValue(0).WithPriority(-1)),
        ConfigurationManager.RegisterConfigType(
            new IntConfigType("Trigger Layer", "trigger_layer",
                (o, value) =>
                {
                    var zone = o.GetComponent<TriggerZone>();
                    zone.layer = value.GetValue();
                    zone.usingLayer = true;
                }).WithPriority(-1))
    ]);

    public static readonly List<ConfigType> HitResponder = GroupUtils.Merge(Stretchable, [
        ConfigurationManager.RegisterConfigType(
            new ChoiceConfigType("Shape", "hit_responder_shape",
                    (o, value) =>
                    {
                        if (value.GetValue() == 0) return;
                        o.GetComponent<PolygonCollider2D>().enabled = true;
                        o.GetComponent<BoxCollider2D>().enabled = false;
                    }, (o, value, _) =>
                    {
                        o.GetComponent<SpriteRenderer>().sprite =
                            value.GetValue() == 0 ? Behaviour.Utility.HitResponder.SquareZone : Behaviour.Utility.HitResponder.CircleZone;
                    })
                .WithOptions("Square", "Circle").WithDefaultValue(0).WithPriority(-1))
    ]);

    public static readonly List<ConfigType> EnemyDamager = GroupUtils.Merge(Stretchable, [
        ConfigurationManager.RegisterConfigType(
            new ChoiceConfigType("Shape", "enemy_damager_shape",
                    (o, value) =>
                    {
                        if (value.GetValue() == 0) return;
                        o.GetComponent<PolygonCollider2D>().enabled = true;
                        o.GetComponent<BoxCollider2D>().enabled = false;
                    }, (o, value, _) =>
                    {
                        o.GetComponent<SpriteRenderer>().sprite =
                            value.GetValue() == 0 ? UtilityObjects.SquareDamager : UtilityObjects.CircleDamager;
                    })
                .WithOptions("Square", "Circle").WithDefaultValue(0)),
        ConfigurationManager.RegisterConfigType(
            new ChoiceConfigType("Damage Type", "enemy_damager_damage_type", 
                    (o, value) =>
                    {
                        var val = value.GetStringValue();
                        o.GetComponent<DamageEnemies>().attackType = val switch
                        {
                            "Water" => AttackTypes.RuinsWater,
                            _ => (AttackTypes)Enum.Parse(typeof(AttackTypes), val)
                        };
                    }).WithDefaultValue(1)
                .WithOptions(
                    "Nail",
                    "Generic",
                    "Spell",
                    "Acid",
                    "Splatter",
                    "Water",
                    "SharpShadow",
                    "NailBeam")
        ),
        ConfigurationManager.RegisterConfigType(
            new IntConfigType("Amount", "enemy_damager_hp_amount", 
                (o, value) =>
                {
                    o.GetComponent<DamageEnemies>().damageDealt = value.GetValue();
                }).WithDefaultValue(10)
        )
    ]);
    
    public static readonly List<ConfigType> HazardRespawn = GroupUtils.Merge(Generic, [
        ConfigurationManager.RegisterConfigType(
            new BoolConfigType("Collision Trigger", "hrp_collision", (o, value) =>
            {
                if (value.GetValue()) return;
                o.RemoveComponent<Collider2D>();
            }).WithDefaultValue(true)),
        ConfigurationManager.RegisterConfigType(
            new FloatConfigType("Collision Width", "hrp_width", (o, value) =>
                {
                    if (EditManager.IsEditing) return;
                    var bc2d = o.GetComponent<BoxCollider2D>();
                    bc2d.size = bc2d.size.Where(x: value.GetValue());
                }, (o, value, _) => o.transform.SetScaleX(o.transform.GetScaleX() * value.GetValue()))
                .WithDefaultValue(1)),
        ConfigurationManager.RegisterConfigType(
            new FloatConfigType("Collision Height", "hrp_height", (o, value) =>
                {
                    if (EditManager.IsEditing) return;
                    var bc2d = o.GetComponent<BoxCollider2D>();
                    bc2d.size = bc2d.size.Where(y: value.GetValue());
                }, (o, value, _) => o.transform.SetScaleY(o.transform.GetScaleY() * value.GetValue()))
                .WithDefaultValue(1))
    ]);

    public static readonly List<ConfigType> Transitions = GroupUtils.Merge(Stretchable, [
        ConfigurationManager.RegisterConfigType(
            new ChoiceConfigType("Gate Type", "trans_type",
                    (o, value) =>
                    {
                        o.GetComponent<CustomTransitionPoint>().pointType = value.GetValue();
                    })
                .WithOptions("Door", "Left", "Right", "Top", "Bottom").WithDefaultValue(0).WithPriority(-1)),
        ConfigurationManager.RegisterConfigType(
            new StringConfigType("Door ID", "trans_id", (o, value) =>
            {
                o.name = value.GetValue();
            }).WithPriority(-1)),
        ConfigurationManager.RegisterConfigType(
            new StringConfigType("Target Door ID", "trans_other_id",
                (o, value) =>
                {
                    o.GetComponent<TransitionPoint>().entryPoint = value.GetValue();
                }).WithPriority(-1)),
        ConfigurationManager.RegisterConfigType(
            new StringConfigType("Target Scene", "trans_other_scene",
                (o, value) =>
                {
                    o.GetComponent<TransitionPoint>().targetScene = value.GetValue();
                }).WithPriority(-1)),
        ConfigurationManager.RegisterConfigType(
            new FloatConfigType("Entry Delay", "trans_delay",
                (o, value) =>
                {
                    o.GetComponent<TransitionPoint>().entryDelay = value.GetValue();
                }).WithPriority(-1)),
        ConfigurationManager.RegisterConfigType(
            new ChoiceConfigType("Trigger Type", "trans_collide",
                (o, value) =>
                {
                    var tp = o.GetComponent<TransitionPoint>();
                    tp.isADoor = value.GetValue() == 1;
                    if (value.GetValue() == 1)
                    {
                        var ci = o.AddComponent<CustomInteraction>();
                        ci.door = tp;
                        ci.prompt = "Enter";
                    }
                    else if (value.GetValue() == 2) o.GetComponent<BoxCollider2D>().offset = new Vector2(-9999, -9999);
                }).WithOptions("Collision", "Door", "None").WithDefaultValue(0).WithPriority(-1)),
        ConfigurationManager.RegisterConfigType(
            new BoolConfigType("Active in Editor", "trans_edit_mode_active",
                (o, value) =>
                {
                    o.GetComponent<CustomTransitionPoint>().applyInEditMode = value.GetValue();
                }).WithDefaultValue(true).WithPriority(-1))
    ]);

    public static readonly List<ConfigType> Bindings = GroupUtils.Merge(Mutable, [
        ConfigurationManager.RegisterConfigType(
            new BoolConfigType("Binding Active", "binding_active",
                (o, value) => { o.GetComponent<Binding>().active = value.GetValue(); }).WithDefaultValue(true)),
        ConfigurationManager.RegisterConfigType(
            new BoolConfigType("Reversible", "binding_toggle",
                (o, value) => { o.GetComponent<Binding>().reversible = value.GetValue(); }).WithDefaultValue(false)),
        ConfigurationManager.RegisterConfigType(
            new BoolConfigType("Contact Toggle", "binding_contact_toggle",
                (o, value) =>
                {
                    if (!value.GetValue()) o.RemoveComponent<CircleCollider2D>();
                }).WithDefaultValue(true)),
        ConfigurationManager.RegisterConfigType(
            new BoolConfigType("Appear in UI", "binding_ui",
                (o, value) => { o.GetComponent<Binding>().uiVisible = value.GetValue(); }).WithDefaultValue(true))
    ]);

    public static readonly List<ConfigType> KeyListener = GroupUtils.Merge(Generic, [
        ConfigurationManager.RegisterConfigType(
            new StringConfigType("Keybind", "key_listener_key",
                (o, value) =>
                {
                    if (!Enum.TryParse<KeyCode>(value.GetValue(), true, out var key)) return;
                    o.GetComponent<KeyListener>().key = key;
                }))
    ]);
    
    public static readonly List<ConfigType> Bumpers = GroupUtils.Merge(Visible, [
        ConfigurationManager.RegisterConfigType(
            new ChoiceConfigType("Mode", "bumper_mode",
                (o, value) =>
                {
                    o.GetComponent<Bumper>().SetEvil(value.GetValue() == 1);
                },
                (o, value, _) =>
                {
                    o.GetComponent<SpriteRenderer>().sprite =
                        value.GetValue() == 0 ? Bumper.NormalIcon : Bumper.EvilIcon;
                }).WithOptions("Regular", "Fire").WithDefaultValue(0)),
        ConfigurationManager.RegisterConfigType(
            new ChoiceConfigType("Activation", "bumper_activation",
                (o, value) =>
                {
                    o.GetComponent<Bumper>().activation = value.GetValue();
                }).WithOptions("Contact", "Hit").WithDefaultValue(0))
    ]);

    public static readonly List<ConfigType> Remover = GroupUtils.Merge(Generic, [
        ConfigurationManager.RegisterConfigType(
            new StringConfigType("Filter", "remover_filter",
                (o, value) =>
                {
                    o.GetComponent<ObjectRemover>().filter = value.GetValue();
                }))
    ]);

    public static readonly List<ConfigType> DisableRenderer = GroupUtils.Merge(Remover, [
        ConfigurationManager.RegisterConfigType(
            new BoolConfigType("Disable All In Range", "remover_all_in_range",
                (o, value) =>
                {
                    o.GetComponent<ObjectRemover>().allInRange = value.GetValue();
                })),
        ConfigurationManager.RegisterConfigType(
            new FloatConfigType("Range", "remover_range",
                (o, value) =>
                {
                    o.GetComponent<ObjectRemover>().range = value.GetValue();
                }).WithDefaultValue(25))
    ]);

    public static readonly List<ConfigType> RoomClearer = GroupUtils.Merge(Remover, [
        ConfigurationManager.RegisterConfigType(
            new BoolConfigType("Remove Transitions", "remove_transitions",
                    (o, value) => { o.GetOrAddComponent<RoomClearerConfig>().removeTransitions = value.GetValue(); })
                .WithDefaultValue(false).WithPriority(-1)
        ),
        ConfigurationManager.RegisterConfigType(
            new BoolConfigType("Remove Benches", "remove_benches",
                    (o, value) => { o.GetOrAddComponent<RoomClearerConfig>().removeBenches = value.GetValue(); })
                .WithDefaultValue(true).WithPriority(-1)
        ),
        ConfigurationManager.RegisterConfigType(
            new BoolConfigType("Remove Blur", "remove_blur",
                    (o, value) => { o.GetOrAddComponent<RoomClearerConfig>().removeBlur = value.GetValue(); })
                .WithDefaultValue(true).WithPriority(-1)
        ),
        ConfigurationManager.RegisterConfigType(
            new BoolConfigType("Remove Music", "remove_music",
                    (o, value) => { o.GetOrAddComponent<RoomClearerConfig>().removeMusic = value.GetValue(); })
                .WithDefaultValue(true).WithPriority(-1)
        ),
        ConfigurationManager.RegisterConfigType(
            new BoolConfigType("Remove Tilemap", "remove_tilemap",
                    (o, value) => { o.GetOrAddComponent<RoomClearerConfig>().removeTilemap = value.GetValue(); })
                .WithDefaultValue(true).WithPriority(-1)
        ),
        ConfigurationManager.RegisterConfigType(
            new BoolConfigType("Remove Other", "remove_other",
                    (o, value) => { o.GetOrAddComponent<RoomClearerConfig>().removeOther = value.GetValue(); })
                .WithDefaultValue(true).WithPriority(-1)
        ),
        ConfigurationManager.RegisterConfigType(
            new BoolConfigType("Recursive", "recursive_clear_room",
                    (o, value) => { o.GetOrAddComponent<RoomClearerConfig>().recursive = value.GetValue(); })
                .WithDefaultValue(false).WithPriority(-1)
        )
    ]);

    public static readonly List<ConfigType> ObjectRemover = GroupUtils.Merge(Generic, [
        ConfigurationManager.RegisterConfigType(
            new StringConfigType("Path", "remover_path",
                (o, value) => { o.GetOrAddComponent<ObjectRemoverConfig>().objectPath = value.GetValue(); })
                .WithPriority(-1)
        ),
        ConfigurationManager.RegisterConfigType(
            new IntConfigType("Index", "remover_index",
                (o, value) => { o.GetOrAddComponent<ObjectRemoverConfig>().index = value.GetValue(); })
                .WithDefaultValue(0).WithPriority(-1)
        )
    ]);

    public static readonly List<ConfigType> ObjectEnabler = GroupUtils.Merge(Generic, [
        ConfigurationManager.RegisterConfigType(
            new StringConfigType("Path", "enabler_path",
                (o, value) => { o.GetComponent<ObjectEnabler>().objectPath = value.GetValue(); })
                .WithPriority(-1)
        ),
        ConfigurationManager.RegisterConfigType(
            new IntConfigType("Index", "enabler_index",
                    (o, value) => { o.GetComponent<ObjectEnabler>().index = value.GetValue(); })
                .WithDefaultValue(0).WithPriority(-1)
        )
    ]);
    
    public static readonly List<ConfigType> Binoculars = GroupUtils.Merge(Visible, [
        ConfigurationManager.RegisterConfigType(
            new FloatConfigType("Camera Speed", "freecam_speed",
                (o, value) => { o.GetComponent<Binoculars>().speed = value.GetValue() * 10; }).WithDefaultValue(2)),
        ConfigurationManager.RegisterConfigType(
            new FloatConfigType("Start Zoom", "freecam_start_zoom",
                (o, value) => { o.GetComponent<Binoculars>().startZoom = value.GetValue(); })),
        ConfigurationManager.RegisterConfigType(
            new FloatConfigType("Zoom Minimum", "freecam_min_zoom",
                (o, value) => { o.GetComponent<Binoculars>().minZoom = value.GetValue(); })),
        ConfigurationManager.RegisterConfigType(
            new FloatConfigType("Zoom Maximum", "freecam_max_zoom",
                (o, value) => { o.GetComponent<Binoculars>().maxZoom = value.GetValue(); })),
        ConfigurationManager.RegisterConfigType(
            new Vector2ConfigType("Start Offset", "freecam_offset",
                (o, value) => { o.GetComponent<Binoculars>().startOffset = value.GetValue(); }))
    ]);

    private static ChoiceConfigType MakePersistenceConfigType(string name, string id,
        Action<GameObject, PersistentBoolItem> action = null)
    {
        var cc = new ChoiceConfigType(name, id, (o, value) =>
        {
            var val = value.GetValue();

            if (val == 0)
            {
                o.RemoveComponentsInChildren<PersistentBoolItem>();
            }
            else
            {
                var item = o.GetComponentInChildren<PersistentBoolItem>();

                if (!item)
                {
                    item = o.AddComponent<PersistentBoolItem>();
                    item.persistentBoolData = new PersistentBoolData
                    {
                        id = o.name,
                        sceneName = o.scene.name
                    };
                    item.enabled = true;
                    action?.Invoke(o, item);
                }

                item.semiPersistent = val == 1;
                item.persistentBoolData.semiPersistent = val == 1;
            }
        }).WithOptions("False", "Bench", "True");
        cc.WithPriority(-1);
        return cc;
    }
}
