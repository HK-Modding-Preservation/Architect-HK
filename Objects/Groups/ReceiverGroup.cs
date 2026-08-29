using System.Collections.Generic;
using Architect.Behaviour.Custom;
using Architect.Behaviour.Fixers;
using Architect.Behaviour.Utility;
using Architect.Content.Custom;
using Architect.Events;
using Architect.Prefabs;
using UnityEngine;

namespace Architect.Objects.Groups;

public static class ReceiverGroup
{
    public static readonly List<EventReceiverType> Generic = [
        EventManager.RegisterReceiverType(new EventReceiverType("disable", "Disable", o =>
        {
            o.SetActive(false);
        })),
        EventManager.RegisterReceiverType(new EventReceiverType("enable", "Enable", o =>
        {
            o.SetActive(true);
        }, true))
    ];
    
    public static readonly List<EventReceiverType> Shielder = GroupUtils.Merge(Generic, [
        EventManager.RegisterReceiverType(new EventReceiverType("do_shield_apply", "Apply", o =>
        {
            o.GetComponent<Shielder>().Shield();
        }))
    ]);
    
    public static readonly List<EventReceiverType> Feather = GroupUtils.Merge(Generic, [
        EventManager.RegisterReceiverType(new EventReceiverType("set_feather_rot", "SetRotation", (o, b) =>
        {
            Behaviour.Custom.Feather.SetRotation(b?.GetVariable<float>("Rotation", 0) ?? 0);
        }))
    ]);
    
    public static readonly List<EventReceiverType> Bumpers = GroupUtils.Merge(Generic, [
        EventManager.RegisterReceiverType(new EventReceiverType("bouncer_evil", "SetFire", o =>
        {
            o.GetComponent<Bumper>().SetEvil(true);
        })),
        EventManager.RegisterReceiverType(new EventReceiverType("bouncer_normal", "SetNormal", o =>
        {
            o.GetComponent<Bumper>().SetEvil(false);
        }))
    ]);
    
    public static readonly List<EventReceiverType> Velocity = GroupUtils.Merge(Generic, [
        EventManager.RegisterReceiverType(new EventReceiverType("set_velocity", "SetVelocity", (o, b) =>
        {
            if (b == null) return;
            o.GetComponent<Rigidbody2D>().velocity = new Vector2(
                b.GetVariable<float>("New X"),
                b.GetVariable<float>("New Y")
            );
        }))
    ]);
    
    public static readonly List<EventReceiverType> ColoWall = GroupUtils.Merge(Generic, [
        EventManager.RegisterReceiverType(new EventReceiverType("colo_wall_move", "Move", (o, b) =>
        {
            if (b == null) return;
            var fsm = o.LocateMyFSM("Control");
            var dist = b.GetVariable<float>("Distance", o.GetComponent<MiscFixers.ColoWall>().moveDistance);
            fsm.FsmVariables.FindFsmFloat("Distance").Value = dist;
            fsm.SendEvent("MOVE");
        })),
        EventManager.RegisterReceiverType(new EventReceiverType("colo_wall_return", "Return", o =>
        {
            var fsm = o.LocateMyFSM("Control");
            fsm.FsmVariables.FindFsmFloat("Distance").Value = 0;
            fsm.SendEvent("MOVE");
        }))
    ]);
    
    public static readonly List<EventReceiverType> ColoPlat = GroupUtils.Merge(Generic, [
        EventManager.RegisterReceiverType(new EventReceiverType("colo_plat_up", "Up", o =>
        {
            o.LocateMyFSM("Control").SendEvent("PLAT EXPAND");
        })),
        EventManager.RegisterReceiverType(new EventReceiverType("colo_plat_down", "Down", o =>
        {
            o.LocateMyFSM("Control").SendEvent("PLAT RETRACT");
        })),
        EventManager.RegisterReceiverType(new EventReceiverType("colo_plat_down_slow", "DownSlow", o =>
        {
            o.LocateMyFSM("Control").SendEvent("SLOW RETRACT");
        }))
    ]);
    
    public static readonly List<EventReceiverType> Particles = GroupUtils.Merge(Generic, [
        EventManager.RegisterReceiverType(new EventReceiverType("particle_burst", "Burst", (o, b) =>
        {
            o.ApplyToAllComponents<ParticleSystem>(ps => 
                ps.Emit(Mathf.RoundToInt(b?.GetVariable<float>("Count", 200) ?? 200)));
        })),
        EventManager.RegisterReceiverType(new EventReceiverType("particle_play", "Play", o =>
        {
            o.ApplyToAllComponents<ParticleSystem>(ps => ps.Play());
        })),
        EventManager.RegisterReceiverType(new EventReceiverType("particle_pause", "Pause", o =>
        {
            o.ApplyToAllComponents<ParticleSystem>(ps => ps.Pause());
        })),
        EventManager.RegisterReceiverType(new EventReceiverType("particle_stop", "Stop", o =>
        {
            o.ApplyToAllComponents<ParticleSystem>(ps => ps.Stop());
        }))
    ]);
    
    public static readonly List<EventReceiverType> JellyEgg = GroupUtils.Merge(Generic, [
        EventManager.RegisterReceiverType(new EventReceiverType("egg_respawn", "Respawn", o =>
        {
            var egg = o.GetComponent<JellyEgg>();
            egg.meshRenderer.enabled = true;
            egg.circleCollider.enabled = true;
        })),
        EventManager.RegisterReceiverType(new EventReceiverType("egg_explode", "Explode", o =>
        {
            o.GetComponent<JellyEgg>().Burst();
        }))
    ]);
    
    public static readonly List<EventReceiverType> Ghosts = GroupUtils.Merge(Generic, [
        EventManager.RegisterReceiverType(new EventReceiverType("ghost_die", "Die", o =>
        {
            o.LocateMyFSM("ghost_npc_death").SendEvent("DREAMNAIL");
        }))
    ]);
    
    public static readonly List<EventReceiverType> GradeMarker = [
        EventManager.RegisterReceiverType(new EventReceiverType("grade_marker_off", "Deactivate", o =>
        {
            o.GetComponent<GradeMarker>().Deactivate();
        })),
        EventManager.RegisterReceiverType(new EventReceiverType("grade_marker_on", "Activate", o =>
        {
            o.GetComponent<GradeMarker>().Activate();
        })),
        EventManager.RegisterReceiverType(new EventReceiverType("grade_marker_off_gradual", "DeactivateGradual", o =>
        {
            o.GetComponent<GradeMarker>().DeactivateGradual();
        })),
        EventManager.RegisterReceiverType(new EventReceiverType("grade_marker_on_gradual", "ActivateGradual", o =>
        {
            o.GetComponent<GradeMarker>().ActivateGradual();
        }))
    ];
    
    public static readonly List<EventReceiverType> ObjectHook = [
        EventManager.RegisterReceiverType(new EventReceiverType("hook_disable_target", "Disable", o =>
        {
            var oh = o.GetComponent<ObjectHook>();
            oh.FindObject();
            var target = oh.o;
            if (target) target.SetActive(false);
        })),
        EventManager.RegisterReceiverType(new EventReceiverType("hook_enable_target", "Enable", o =>
        {
            var oh = o.GetComponent<ObjectHook>();
            oh.FindObject();
            var target = oh.o;
            if (target) target.SetActive(true);
        })),
        EventManager.RegisterReceiverType(new EventReceiverType("hook_rescan", "FindObject", o =>
        {
            var oh = o.GetComponent<ObjectHook>();
            oh.FindObject(true);
        }))
    ];
    
    public static readonly List<EventReceiverType> AudioPlayer = [
        EventManager.RegisterReceiverType(new EventReceiverType("audio_player_play", "Play", o =>
        {
            o.GetComponent<AudioPlayer>().Play();
        }))
    ];
    
    public static readonly List<EventReceiverType> CollisionChanger = [
        EventManager.RegisterReceiverType(new EventReceiverType("collision_changer_disable", "DisableCollision", o =>
        {
            o.GetComponent<CollisionChanger>().DisableCollision();
        })),
        EventManager.RegisterReceiverType(new EventReceiverType("collision_changer_enable", "EnableCollision", o =>
        {
            o.GetComponent<CollisionChanger>().EnableCollision();
        }))
    ];
    
    public static readonly List<EventReceiverType> Prefab = [
        EventManager.RegisterReceiverType(new EventReceiverType("prefab_start", "Activate", o =>
        {
            o.SetActive(true);
        }, true)),
        EventManager.RegisterReceiverType(new EventReceiverType("prefab_destroy", "Destroy", o =>
        {
            o.GetComponent<Prefab>().Destroy();
        }, true))
    ];
    
    public static readonly List<EventReceiverType> AbilityCrystal = GroupUtils.Merge(Generic, [
        EventManager.RegisterReceiverType(new EventReceiverType("crystal_clear", "ClearAll", _ =>
        {
            AbilityObjects.ActiveCrystals.Clear();
            AbilityObjects.RefreshCrystalUI();
        }))
    ]);
    
    public static readonly List<EventReceiverType> ObjectLayerer = [
        EventManager.RegisterReceiverType(new EventReceiverType("layerer_apply", "Apply", o =>
        {
            o.GetComponent<Layerer>().Apply();
        }))
    ];
    
    public static readonly List<EventReceiverType> MagmaRocks = GroupUtils.Merge(Generic, [
        EventManager.RegisterReceiverType(new EventReceiverType("magma_go", "Away", o =>
        {
            o.LocateMyFSM("Control").SendEvent("AWAY");
        })),
        EventManager.RegisterReceiverType(new EventReceiverType("magma_back", "Return", o =>
        {
            o.LocateMyFSM("Control").SendEvent("RETURN");
        }))
    ]);
    
    public static readonly List<EventReceiverType> Blast = GroupUtils.Merge(Generic, [
        EventManager.RegisterReceiverType(new EventReceiverType("servitor_blast", "Fire", o =>
        {
            o.LocateMyFSM("Control").SetState("Shoot");
        }))
    ]);
    
    public static readonly List<EventReceiverType> TriggerZone = GroupUtils.Merge(Generic, [
        EventManager.RegisterReceiverType(new EventReceiverType("set_tz_layer", "SetLayer", (o, b) =>
        {
            if (b == null) return;
            var tz = o.GetComponent<TriggerZone>();
            tz.usingLayer = true;
            tz.layer = Mathf.RoundToInt(b.GetVariable<float>("Layer"));
        })),
        EventManager.RegisterReceiverType(new EventReceiverType("disable_tz", "DisableImmediate", o =>
        {
            o.GetComponent<TriggerZone>().block = true;
            o.SetActive(false);
        }))
    ]);
    
    public static readonly List<EventReceiverType> Gate = GroupUtils.Merge(Generic, [
        EventManager.RegisterReceiverType(new EventReceiverType("generic_gate_open", "Open", o =>
        {
            o.LocateMyFSM("Control").SendEvent("OPEN");
        }))
    ]);
    
    public static readonly List<EventReceiverType> MantisGate = GroupUtils.Merge(Generic, [
        EventManager.RegisterReceiverType(new EventReceiverType("mantis_gate_open", "Open", o =>
        {
            o.LocateMyFSM("Mantis Gate").SendEvent("OPEN");
        }))
    ]);
    
    public static readonly List<EventReceiverType> BoneGate = GroupUtils.Merge(Generic, [
        EventManager.RegisterReceiverType(new EventReceiverType("bone_gate_open", "Open", o =>
        {
            o.LocateMyFSM("Bone Gate").SendEvent("OPEN");
        }))
    ]);
    
    public static readonly List<EventReceiverType> TollGate = GroupUtils.Merge(Generic, [
        EventManager.RegisterReceiverType(new EventReceiverType("toll_gate_open", "Open", o =>
        {
            o.LocateMyFSM("Toll Gate").SendEvent("OPEN");
        }))
    ]);
    
    public static readonly List<EventReceiverType> MetalGate = GroupUtils.Merge(Generic, [
        EventManager.RegisterReceiverType(new EventReceiverType("metal_gate_open", "Open", o =>
        {
            o.LocateMyFSM("Gate").SendEvent("OPEN");
        }))
    ]);
    
    public static readonly List<EventReceiverType> CityGate = GroupUtils.Merge(Generic, [
        EventManager.RegisterReceiverType(new EventReceiverType("ruins_gate_open", "Open", o =>
        {
            o.LocateMyFSM("Toll Gate").SendEvent("OPEN");
        }))
    ]);
    
    public static readonly List<EventReceiverType> BattleGate = GroupUtils.Merge(Generic, [
        EventManager.RegisterReceiverType(new EventReceiverType("battle_gate_open", "Open", o =>
        {
            o.LocateMyFSM("BG Control").SendEvent("BG OPEN");
        })),
        EventManager.RegisterReceiverType(new EventReceiverType("battle_gate_close", "Close", o =>
        {
            o.LocateMyFSM("BG Control").SendEvent("BG CLOSE");
        }))
    ]);
    
    public static readonly List<EventReceiverType> WpGate = GroupUtils.Merge(Generic, [
        EventManager.RegisterReceiverType(new EventReceiverType("wp_gate_open", "Open", o =>
        {
            o.LocateMyFSM("FSM").SendEvent("DOWN");
        })),
        EventManager.RegisterReceiverType(new EventReceiverType("wp_gate_close", "Close", o =>
        {
            o.LocateMyFSM("FSM").SendEvent("UP");
        }))
    ]);
    
    public static readonly List<EventReceiverType> RadiancePlat = GroupUtils.Merge(Generic, [
        EventManager.RegisterReceiverType(new EventReceiverType("radiance_plat_up", "Up", o =>
        {
            var fsm = o.GetComponent<PlayMakerFSM>();
            fsm.SendEvent("APPEAR");
            fsm.SendEvent("PLAT EXPAND");
        })),
        EventManager.RegisterReceiverType(new EventReceiverType("radiance_plat_down", "Down", o =>
        {
            var fsm = o.GetComponent<PlayMakerFSM>();
            fsm.SendEvent("DISAPPEAR");
            fsm.SendEvent("PLAT RETRACT");
        }))
    ]);
    
    public static readonly List<EventReceiverType> FsmHook = GroupUtils.Merge(Generic, [
        EventManager.RegisterReceiverType(new EventReceiverType("fsm_set_state", "SetState", o =>
        {
            o.GetComponent<FsmHook>().SetState();
        })),
        EventManager.RegisterReceiverType(new EventReceiverType("fsm_clear_transitions", "WipeAllTransitions", o =>
        {
            o.GetComponent<FsmHook>().ClearEvents(true);
        })),
        EventManager.RegisterReceiverType(new EventReceiverType("fsm_remove_transition", "WipeTransitions", o =>
        {
            o.GetComponent<FsmHook>().ClearEvents();
        })),
        EventManager.RegisterReceiverType(new EventReceiverType("fsm_send_event", "SendEvent", (o, b) =>
        {
            o.GetComponent<FsmHook>().SendEvent(b?.GetVariable<string>("Event") ?? "");
        })),
        EventManager.RegisterReceiverType(new EventReceiverType("fsm_disable_action", "DisableAction", (o, b) =>
        {
            o.GetComponent<FsmHook>().DisableAction(Mathf.RoundToInt(b?.GetVariable<float>("Action") ?? 0));
        })),
        EventManager.RegisterReceiverType(new EventReceiverType("fsm_enable_action", "EnableAction", (o, b) =>
        {
            o.GetComponent<FsmHook>().EnableAction(Mathf.RoundToInt(b?.GetVariable<float>("Action") ?? 0));
        }))
    ]);
    
    public static readonly List<EventReceiverType> ComponentHook = GroupUtils.Merge(Generic, [
        EventManager.RegisterReceiverType(new EventReceiverType("component_hook_apply", "Apply", o =>
        {
            o.GetComponent<ComponentHook>().Setup();
        })),
        EventManager.RegisterReceiverType(new EventReceiverType("component_hook_set", "Set", (o, b) =>
        {
            if (b == null) return;
            o.GetComponent<ComponentHook>().SetValue(b.GetVariable<object>("New Value"));
        }))
    ]);
    
    public static readonly List<EventReceiverType> EnemyDamager = GroupUtils.Merge(Generic, [
        EventManager.RegisterReceiverType(new EventReceiverType("set_enemy_damager", "SetDamage", (o, b) =>
        {
            if (b == null) return;
            o.GetComponent<DamageEnemies>().damageDealt = (int)b.GetVariable<float>("New Damage");
        }))
    ]);
    
    public static readonly List<EventReceiverType> ObjectSpinner = GroupUtils.Merge(Generic, [
        EventManager.RegisterReceiverType(new EventReceiverType("spinner_reverse", "Reverse", o =>
        {
            o.GetComponent<ObjectSpinner>().speed *= -1;
        }))
    ]);
    
    public static readonly List<EventReceiverType> WalkTarget = GroupUtils.Merge(Generic, [
        EventManager.RegisterReceiverType(new EventReceiverType("start_walk", "Start", o =>
        {
            o.GetComponent<WalkTarget>().StartWalk();
        })),
        EventManager.RegisterReceiverType(new EventReceiverType("end_walk", "Cancel", o =>
        {
            o.GetComponent<WalkTarget>().StopWalk();
        }))
    ]);
    
    public static readonly List<EventReceiverType> TeleportPoint = GroupUtils.Merge(Generic, [
        EventManager.RegisterReceiverType(new EventReceiverType("do_tp", "Teleport", o =>
        {
            HeroController.instance.transform.position = o.transform.position;
        }))
    ]);
    
    public static readonly List<EventReceiverType> Playable = GroupUtils.Merge(Generic, [
        EventManager.RegisterReceiverType(new EventReceiverType("start_play", "Play", o =>
        {
            o.GetComponent<IPlayable>().Play();
        })),
        EventManager.RegisterReceiverType(new EventReceiverType("stop_play", "Pause", o =>
        {
            o.GetComponent<IPlayable>().Pause();
        })),
        EventManager.RegisterReceiverType(new EventReceiverType("reset_play", "Reset", o =>
        {
            o.GetComponent<IPlayable>().Reset();
        }))
    ]);
    
    public static readonly List<EventReceiverType> Png = GroupUtils.Merge(Playable, [
        EventManager.RegisterReceiverType(new EventReceiverType("png_flip_x", "FlipX", o =>
        {
            o.transform.SetScaleX(-o.transform.GetScaleX());
        })),
        EventManager.RegisterReceiverType(new EventReceiverType("png_flip_y", "FlipY", o =>
        {
            o.transform.SetScaleY(-o.transform.GetScaleY());
        })),
        EventManager.RegisterReceiverType(new EventReceiverType("png_set_width", "SetWidth", (o, b) =>
        {
            if (b == null) return;
            o.transform.SetScaleX(b.GetVariable<float>("New Width", 1));
        })),
        EventManager.RegisterReceiverType(new EventReceiverType("png_set_height", "SetHeight", (o, b) =>
        {
            if (b == null) return;
            o.transform.SetScaleY(b.GetVariable<float>("New Height", 1));
        })),
        EventManager.RegisterReceiverType(new EventReceiverType("png_set_frame", "SetFrame", (o, b) =>
        {
            if (b == null) return;
            o.GetComponent<PngObject>().SetFrame(Mathf.RoundToInt(b.GetVariable<float>("New Frame", 1)));
        })),
        EventManager.RegisterReceiverType(new EventReceiverType("png_set_fps", "SetFPS", (o, b) =>
        {
            if (b == null) return;
            var png = o.GetComponentInChildren<PngObject>();
            var val = b.GetVariable<float>("New FPS", 1);
            if (val == 0) png.frameTime = 0;
            else png.frameTime = 1 / Mathf.Max(0.01f, val);
        }))
    ]);
    
    public static readonly List<EventReceiverType> Wav = GroupUtils.Merge(Playable, [
        EventManager.RegisterReceiverType(new EventReceiverType("set_volume", "SetVolume", (o, b) =>
        {
            if (b == null) return;
            o.GetComponent<WavObject>().Volume = b.GetVariable<float>("New Volume");
        }))
    ]);
    
    public static readonly List<EventReceiverType> Duplicator = GroupUtils.Merge(Generic, [
        EventManager.RegisterReceiverType(new EventReceiverType("duplicate", "SpawnObject", o =>
        {
            o.GetComponent<ObjectDuplicator>().Duplicate();
        }))
    ]);
    
    public static readonly List<EventReceiverType> HazardRespawn = GroupUtils.Merge(Generic, [
        EventManager.RegisterReceiverType(new EventReceiverType("set_spawn", "SetSpawn", o =>
        {
            PlayerData.instance.SetHazardRespawn(o.GetComponent<HazardRespawnMarker>());
        }))
    ]);
    
    public static readonly List<EventReceiverType> Respawn = GroupUtils.Merge(Generic, [
        EventManager.RegisterReceiverType(new EventReceiverType("set_bench_spawn", "SetSpawn", o =>
        {
            PlayerData.instance.SetBenchRespawn(
                o.GetComponent<RespawnMarker>(), 
                GameManager.instance.sceneName, 
                0);
        }))
    ]);
    
    public static readonly List<EventReceiverType> ObjectAnchor = GroupUtils.Merge(Generic, [
        EventManager.RegisterReceiverType(new EventReceiverType("anchor_reset", "Reset", o =>
        {
            o.GetComponent<ObjectAnchor>().Reset();
        })),
        EventManager.RegisterReceiverType(new EventReceiverType("anchor_stop", "StopMoving", o =>
        {
            o.GetComponent<ObjectAnchor>().moving = false;
        })),
        EventManager.RegisterReceiverType(new EventReceiverType("anchor_start", "StartMoving", o =>
        {
            o.GetComponent<ObjectAnchor>().moving = true;
        }))
    ]);
    
    public static readonly List<EventReceiverType> ObjectMover = GroupUtils.Merge(Generic, [
        EventManager.RegisterReceiverType(new EventReceiverType("mover_move", "Move", (o, b) =>
        {
            o.GetComponent<ObjectMover>().Move(
                b?.GetVariable<float>("Extra X") ?? 0,
                b?.GetVariable<float>("Extra Y") ?? 0,
                b?.GetVariable<float>("Extra Rot") ?? 0);
        }))
    ]);

    private class DeathMarker : MonoBehaviour
    {
        public float time;
    }
    
    public static readonly List<EventReceiverType> Enemies = GroupUtils.Merge(Generic, [
        EventManager.RegisterReceiverType(new EventReceiverType("enemy_die", "Die", o =>
        {
            var hm = o.GetComponentInChildren<HealthManager>();
            if (!hm)
            {
                var eh = o.GetComponent<EnemyHook>();
                if (!eh) return;
                if (!eh.hm) eh.DoCheck();
                hm = eh.hm;
            }
            if (!hm) return;
            var dm = hm.gameObject.GetOrAddComponent<DeathMarker>();
            if (Time.time - dm.time < 0.1f) return;
            dm.time = Time.time;
            hm.Hit(new HitInstance
            {
                Source = o,
                AttackType = AttackTypes.Generic,
                DamageDealt = 999999999,
                SpecialType = SpecialTypes.None,
                IgnoreInvulnerable = true,
                Multiplier = 1
            });
        })),
        EventManager.RegisterReceiverType(new EventReceiverType("enemy_invul", "MakeInvulnerable", o =>
        {
            var hm = o.GetComponentInChildren<HealthManager>();
            if (!hm)
            {
                var eh = o.GetComponent<EnemyHook>();
                if (!eh) return;
                if (!eh.hm) eh.DoCheck();
                hm = eh.hm;
            }
            if (!hm) return;
            hm.gameObject.GetOrAddComponent<ConfigGroup.EnemyInvulnerabilityMarker>();
        })),
        EventManager.RegisterReceiverType(new EventReceiverType("enemy_vul", "MakeVulnerable", o =>
        {
            var hm = o.GetComponentInChildren<HealthManager>();
            if (!hm)
            {
                var eh = o.GetComponent<EnemyHook>();
                if (!eh) return;
                if (!eh.hm) eh.DoCheck();
                hm = eh.hm;
            }
            if (!hm) return;
            hm.gameObject.RemoveComponent<ConfigGroup.EnemyInvulnerabilityMarker>();
        }))
    ]);
    
    public static readonly List<EventReceiverType> Wakeable = GroupUtils.Merge(Enemies, [
        EventManager.RegisterReceiverType(new EventReceiverType("do_awake", "Wake", o =>
        {
            o.GetComponent<EnemyFixers.Wakeable>().Wake();
        }))
    ]);
    
    public static readonly List<EventReceiverType> Oblobble = GroupUtils.Merge(Enemies, [
        EventManager.RegisterReceiverType(new EventReceiverType("oblobble_rage", "Rage", o =>
        {
            var fsm = o.LocateMyFSM("Set Rage");
            if (fsm.ActiveStateName == "Idle") fsm.SetState("Set");
        }))
    ]);
    
    public static readonly List<EventReceiverType> EnemyHook = GroupUtils.Merge(Enemies, [
        EventManager.RegisterReceiverType(new EventReceiverType("enemy_hook_set", "SetEnemy", (o, b) =>
        {
            var eh = o.GetComponent<EnemyHook>();
            if (!eh) return;
            var hm = b.GetVariable<HealthManager>("New Enemy");
            eh.Set(hm);
        }))
    ]);
    
    public static readonly List<EventReceiverType> Binoculars = GroupUtils.Merge(Generic, [
        EventManager.RegisterReceiverType(new EventReceiverType("start_using", "StartUsing", o =>
        {
            o.GetComponent<Binoculars>().StartUsing();
        }))
    ]);
    
    public static readonly List<EventReceiverType> Colourer = GroupUtils.Merge(Generic, [
        EventManager.RegisterReceiverType(new EventReceiverType("do_colour", "Colour", (o, b) =>
        {
            o.GetComponent<ObjectColourer>().Apply(Mathf.Max(0, b?.GetVariable<float>("Fade Time") ?? 0));
        })),
        EventManager.RegisterReceiverType(new EventReceiverType("do_colour_dynamic", "DynamicColour", (o, b) =>
        {
            o.GetComponent<ObjectColourer>().Apply(
                Mathf.Max(0, b.GetVariable<float>("Fade Time")),
                new Color(
                    b.GetVariable<float>("R", 1), 
                    b.GetVariable<float>("G", 1), 
                    b.GetVariable<float>("B", 1), 
                    b.GetVariable<float>("A", 1))
            );
        }))
    ]);
}