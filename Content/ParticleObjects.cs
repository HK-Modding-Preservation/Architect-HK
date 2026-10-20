using System.Collections.Generic;
using Architect.Behaviour.Custom;
using Architect.Behaviour.Fixers;
using Architect.Config;
using Architect.Config.Types;
using Architect.Objects.Categories;
using Architect.Objects.Groups;
using Architect.Objects.Placeable;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Architect.Content;

public static class ParticleObjects
{
    private static bool _setup;
    
    public static void Init()
    {
        Categories.Effects.Add(new PreloadObject("Falling Crystals", "falling_crystals",
            ("Mines_31", "Pt Crystal Dropping (13)"),
            preloadAction: MiscFixers.AddComponent<ParticleObject>,
            sprite: ResourceUtils.LoadSpriteResource("falling_crystal"))
            .WithConfigGroup(Particle)
            .WithInputGroup(InputGroup.Particles)
            .WithReceiverGroup(ReceiverGroup.Particles));
        
        Categories.Effects.Add(new PreloadObject("Dust Effect", "dust_effect",
            ("Deepnest_East_16", "Giant Hopper Summon/Antic Dust 1"),
            preloadAction: MiscFixers.AddComponent<ParticleObject>,
            sprite: ResourceUtils.LoadSpriteResource("dust_particles"))
            .WithConfigGroup(Particle)
            .WithInputGroup(InputGroup.Particles)
            .WithReceiverGroup(ReceiverGroup.Particles));
        
        Categories.Effects.Add(new PreloadObject("Rocks Effect", "rocks_effect",
            ("Deepnest_East_16", "Giant Hopper Summon/Antic Rocks 1"),
            preloadAction: MiscFixers.AddComponent<ParticleObject>,
            sprite: ResourceUtils.LoadSpriteResource("rock_particles"))
            .WithConfigGroup(Particle)
            .WithInputGroup(InputGroup.Particles)
            .WithReceiverGroup(ReceiverGroup.Particles));
        
        Categories.Effects.Add(new PreloadObject("Damaging Falling Crystals", "falling_crystals_dmg",
            ("Mines_31", "Pt Crystal Dropping (13)"),
            preloadAction: MiscFixers.AddComponent<ParticleObject>,
            postSpawnAction: MiscFixers.FixDamagingCrystals,
            sprite: ResourceUtils.LoadSpriteResource("falling_crystal"))
            .WithConfigGroup(Particle)
            .WithInputGroup(InputGroup.Particles)
            .WithReceiverGroup(ReceiverGroup.Particles));
        
        On.GameCameras.SetupGameRefs += (orig, self) =>
        {
            orig(self);
            if (_setup) return;

            _setup = true;
            var sp = self.sceneParticles.gameObject;
            CreateParticleObject(sp, "Generic", "default_particles");
            CreateParticleObject(sp, "Queen's Gardens", "royal_garden_particles");
            CreateParticleObject(sp, "White Palace", "white_palace_particles");
            CreateParticleObject(sp, "Dirtmouth", "town_particle_set");
            CreateParticleObject(sp, "Kingdom's Edge", "outskirts_particles");
            CreateParticleObject(sp, "Fungus", "fungus_particles");
            CreateParticleObject(sp, "Crystal Peak", "mines_particles");
            CreateParticleObject(sp, "Fog Canyon", "fog_canyon_particles");
            CreateParticleObject(sp, "Waterways", "waterways_particles");
            CreateParticleObject(sp, "City of Tears", "ruins_interior_particles");
            CreateParticleObject(sp, "Dream", "dream_particles");
            CreateParticleObject(sp, "Resting Grounds", "resting_grounds_particles");
            CreateParticleObject(sp, "Hive", "hive_drip_particles");
            CreateParticleObject(sp, "Fungal Wastes", "fungal_wastes_particles");
            CreateParticleObject(sp, "Deepnest", "Deepnest Particles");
            CreateParticleObject(sp, "Abyss", "abyss particles");
        };
    }

    public class FollowCamera : MonoBehaviour
    {
        private void Update()
        {
            transform.SetPosition2D(GameCameras.instance.tk2dCam.transform.position);
        }
    }

    private static void CreateParticleObject(GameObject particles, string name, string path)
    {
        var o = Object.Instantiate(particles.transform.Find(path).gameObject);
        o.SetActive(false);
        Object.DontDestroyOnLoad(o);

        o.AddComponent<FollowCamera>();
        o.AddComponent<ParticleObject>();

        var id = path.ToLower().Replace(" ", "_");
        var co = new CustomObject($"{name} Effect", id, o,
            sprite: ResourceUtils.LoadSpriteResource(id, FilterMode.Point, ppu: 75.5f),
            description: "Affects the whole room.")
        {
            ParentScale = Vector3.one,
            LossyScale = Vector3.one,
            Offset = new Vector3(0, 0, -o.transform.GetPositionZ())
        };
        Categories.Effects.Add(co
            .WithConfigGroup(Particle)
            .WithInputGroup(InputGroup.Particles)
            .WithReceiverGroup(ReceiverGroup.Particles))
            .DoIgnoreScale();
    }

    public static readonly List<ConfigType> Particle = GroupUtils.Merge(ConfigGroup.Stretchable, [
        ConfigurationManager.RegisterConfigType(
            new BoolConfigType("Play on Start", "particles_play_on_awake",
                    (o, value) =>
                    {
                        o.ApplyToAllComponents<ParticleSystem>(ps =>
                        {
                            var main = ps.main;
                            main.playOnAwake = value.GetValue();
                        });
                    }).WithPriority(-1)),
        ConfigurationManager.RegisterConfigType(
            new IntConfigType("Emission Rate", "particles_rate",
                    (o, value) =>
                    {
                        o.ApplyToAllComponents<ParticleSystem>(ps =>
                        {
                            var emission = ps.emission;
                            emission.rateOverTime = value.GetValue();
                        });
                    }).WithPriority(-1)),
        ConfigurationManager.RegisterConfigType(
            new IntConfigType("Max Particles", "particles_max",
                    (o, value) =>
                    {
                        o.ApplyToAllComponents<ParticleSystem>(ps =>
                        {
                            var main = ps.main;
                            main.maxParticles = value.GetValue();
                        });
                    }).WithPriority(-1)),
        ConfigurationManager.RegisterConfigType(
            new FloatConfigType("Playback Speed", "particles_speed",
                    (o, value) =>
                    {
                        o.ApplyToAllComponents<ParticleSystem>(ps =>
                        {
                            var main = ps.main;
                            main.simulationSpeed = value.GetValue();
                        });
                    }).WithPriority(-1)),
        ConfigurationManager.RegisterConfigType(
            new Vector3ConfigType("Velocity", "particles_velocity",
                    (o, value) =>
                    {
                        var val = value.GetValue();
                        
                        o.ApplyToAllComponents<ParticleSystem>(ps =>
                        {
                            var vol = ps.velocityOverLifetime;
                            vol.enabled = true;
                            
                            vol.x = val.x;
                            vol.y = val.y;
                            vol.z = val.z;

                            var fol = ps.forceOverLifetime;
                            fol.enabled = false;
                        });
                    }).WithPriority(-1)),
        ConfigurationManager.RegisterConfigType(
            new FloatConfigType("Gravity", "particles_velocity_gravity",
                    (o, value) =>
                    {
                        o.ApplyToAllComponents<ParticleSystem>(ps =>
                        {
                            var main = ps.main;
                            main.gravityModifier = value.GetValue();
                        });
                    }).WithPriority(-1)),
        ConfigurationManager.RegisterConfigType(
            new FloatConfigType("Size Multiplier", "particles_size_mul",
                    (o, value) =>
                    {
                        o.ApplyToAllComponents<ParticleSystem>(ps =>
                        {
                            var sol = ps.sizeOverLifetime;
                            sol.enabled = true;
                            sol.sizeMultiplier *= value.GetValue();
                        });
                    }).WithPriority(-1)),
        ConfigurationManager.RegisterConfigType(
            new FloatConfigType("Lifetime", "particles_lifetime",
                    (o, value) =>
                    {
                        o.ApplyToAllComponents<ParticleSystem>(ps =>
                        {
                            var main = ps.main;
                            main.startLifetime = value.GetValue();
                        });
                    }).WithPriority(-1)),
        ConfigurationManager.RegisterConfigType(
            new ColourConfigType("Colour", "particles_colour",
                    (o, value) =>
                    {
                        o.ApplyToAllComponents<ParticleSystem>(ps =>
                        {
                            var main = ps.main;
                            main.startColor = value.GetValue();
                            
                            var cbs = ps.colorBySpeed;
                            cbs.enabled = false;

                            var col = ps.colorOverLifetime;
                            col.enabled = false;
                        });
                    }, true).WithPriority(-1)),
        ConfigGroup.PngUrl,
        ConfigGroup.Aa,
        ConfigurationManager.RegisterConfigType(
            new DoubleIntConfigType("Frame Counts", "particles_allframecount",
                    (o, value) =>
                    {
                        var val = value.GetValue();
                        o.ApplyToAllComponents<ParticleSystem>(ps =>
                        {
                            var tsa = ps.textureSheetAnimation;
                            tsa.enabled = true;

                            tsa.numTilesX = val.Item1;
                            tsa.numTilesY = val.Item2;
                        });
                    })
                .WithPriority(-1)),
        ConfigurationManager.RegisterConfigType(
            new IntConfigType("Frame Cycles", "particles_frame_cycles",
                    (o, value) =>
                    {
                        o.ApplyToAllComponents<ParticleSystem>(ps =>
                        {
                            var tsa = ps.textureSheetAnimation;
                            tsa.enabled = true;

                            tsa.cycleCount = value.GetValue();
                        });
                    })
                .WithPriority(-1))
    ]);

    public static readonly List<ConfigType> Fish = GroupUtils.Merge(Particle, [
        ConfigurationManager.RegisterConfigType(
            new BoolConfigType("Foreground", "fish_fore",
                    (o, value) =>
                    {
                        if (!value.GetValue()) o.transform.GetChild(1).gameObject.SetActive(false);
                    })
                .WithDefaultValue(true)),
        ConfigurationManager.RegisterConfigType(
            new BoolConfigType("Background", "fish_back",
                    (o, value) =>
                    {
                        if (!value.GetValue()) o.transform.GetChild(0).gameObject.SetActive(false);
                    })
                .WithDefaultValue(true))
    ]);
}