using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Architect.Behaviour.Abilities;
using Architect.Behaviour.Custom;
using Architect.Editor;
using Architect.Events.Blocks.Events;
using Architect.Objects.Categories;
using Architect.Objects.Groups;
using Architect.Objects.Placeable;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using MonoMod.RuntimeDetour;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;
using Satchel;

namespace Architect.Content.Custom;

public static class AbilityObjects
{
    private static GameObject _canvasObj;
    
    // Currently active ability objects
    public static readonly Dictionary<string, List<Binding>> ActiveBindings = [];
    public static readonly Dictionary<string, List<Binding>> ActiveVisibleBindings = [];
    public static readonly Dictionary<string, int> ActiveCrystals = [];
    
    // Registered ability objects
    private static readonly List<(string, Sprite)> Bindings = [];
    private static readonly List<(string, int, Sprite)> Crystals = [];
    
    // UI Icons
    private static readonly List<Image> BindingIcons = [];
    private static readonly List<Image> CrystalIcons = [];
    
    public static void Init()
    {
        AbilityCrystal.Init();
        MakeCrystals("Dash", "dash");
        MakeCrystals("Shade Dash", "shadow_dash");
        MakeCrystals("Crystal Dash", "crystal_dash");
        MakeCrystals("Double Jump", "wings");
        
        Binding.Init();
        
        Categories.Abilities.Add(MakeAbilityBinding("nail", "Nail Binding", "Reduces nail damage."));
        Categories.Abilities.Add(MakeAbilityBinding("shell", "Shell Binding", "Reduces max health."));
        Categories.Abilities.Add(MakeAbilityBinding("charms", "Charm Binding", "Prevents charm usage."));
        Categories.Abilities.Add(MakeAbilityBinding("soul", "Soul Binding", "Reduces max soul."));
        Categories.Abilities.Add(MakeAbilityBinding("focus", "Focus Binding"));
        Categories.Abilities.Add(MakeAbilityBinding("spirit", "Vengeful Spirit Binding"));
        Categories.Abilities.Add(MakeAbilityBinding("dive", "Desolate Dive Binding"));
        Categories.Abilities.Add(MakeAbilityBinding("wraiths", "Howling Wraiths Binding"));
        Categories.Abilities.Add(MakeAbilityBinding("shade_soul", "Shade Soul Binding"));
        Categories.Abilities.Add(MakeAbilityBinding("descending_dark", "Descending Dark Binding"));
        Categories.Abilities.Add(MakeAbilityBinding("abyss_shriek", "Abyss Shriek Binding"));
        Categories.Abilities.Add(MakeAbilityBinding("pogo", "Pogo Binding"));
        Categories.Abilities.Add(MakeAbilityBinding("attack", "Attack Binding"));
        Categories.Abilities.Add(MakeAbilityBinding("lantern", "Lantern Binding"));
        Categories.Abilities.Add(MakeAbilityBinding("dash", "Dash Binding"));
        Categories.Abilities.Add(MakeAbilityBinding("shadow_dash", "Shadow Dash Binding"));
        Categories.Abilities.Add(MakeAbilityBinding("claw", "Mantis Claw Binding"));
        Categories.Abilities.Add(MakeAbilityBinding("cdash", "Crystal Dash Binding"));
        Categories.Abilities.Add(MakeAbilityBinding("tear", "Isma's Tear Binding"));
        Categories.Abilities.Add(MakeAbilityBinding("jump", "Jump Binding"));
        Categories.Abilities.Add(MakeAbilityBinding("wings", "Monarch Wings Binding"));
        Categories.Abilities.Add(MakeAbilityBinding("dnail", "Dream Nail Binding"));
        Categories.Abilities.Add(MakeAbilityBinding("gate", "Dreamgate Binding"));
        Categories.Abilities.Add(MakeAbilityBinding("map", "Quickmap Binding"));
        Categories.Abilities.Add(MakeAbilityBinding("gslash", "Great Slash Binding"));
        Categories.Abilities.Add(MakeAbilityBinding("cslash", "Cyclone Slash Binding"));
        Categories.Abilities.Add(MakeAbilityBinding("dslash", "Dash Slash Binding"));
        
        SetupBindingHooks();
        SetupCrystalHooks();
        
        SetupVisuals();
    }
    
    public static void Update()
    {
        if (!GameManager.instance) return;
        _canvasObj.SetActive(!GameManager.instance.isPaused && GameManager.instance.IsGameplayScene()
                                                                  && !EditManager.IsEditing);
    }

    #region Make Crystals
    private static void MakeCrystals(string name, string id)
    {
        MakeCrystal($"{name} Crystal", id, $"Crystals.{id}_s", 1);
        MakeCrystal($"Double {name} Crystal", id, $"Crystals.{id}_m", 2);
        MakeCrystal($"Triple {name} Crystal", id, $"Crystals.{id}_l", 3);
    }

    private static void MakeCrystal(string name, string id, string spriteTexture, int count)
    {
        var crystalObj = new GameObject(name);
        
        crystalObj.SetActive(false);
        Object.DontDestroyOnLoad(crystalObj);

        var col = crystalObj.AddComponent<CircleCollider2D>();
        col.radius = 0.48f;
        col.isTrigger = true;
        col.offset = new Vector2(0, -0.1f * (3-count));

        var sprite = ResourceUtils.LoadSpriteResource(spriteTexture, FilterMode.Point, ppu:15);

        crystalObj.transform.position = new Vector3(0, 0, 0.005f);
        var child = new GameObject("Sprite");
        child.transform.SetParent(crystalObj.transform, false);
        
        child.AddComponent<SpriteRenderer>().sprite = sprite;
        child.AddComponent<FloatAnim>();
        
        var crystal = crystalObj.AddComponent<AbilityCrystal>();
        crystal.type = id;
        crystal.count = count;
        
        Crystals.Add((id, count, sprite));
        
        Categories.Abilities.Add(new CustomObject(name, $"{id}_{count}", crystalObj,
            description: "Lets the player use an ability a limited number of times without needing to unlock it.\n\n" +
                         "Can also be used to refresh abilities midair or without silk.\n\n" +
                         "Ability crystals override any active bindings.")
            .WithConfigGroup(ConfigGroup.AbilityCrystal)
            .WithBroadcasterGroup(BroadcasterGroup.AbilityCrystal)
            .WithReceiverGroup(ReceiverGroup.AbilityCrystal));
    }
    #endregion

    #region Make Bindings

    private static PlaceableObject MakeAbilityBinding(string id, string name, string overrideDesc = null)
    {
        var obj = new GameObject(name);
        Object.DontDestroyOnLoad(obj);
        obj.SetActive(false);
        
        obj.transform.position = new Vector3(0, 0, 0.005f);
        
        var binding = obj.AddComponent<Binding>();
        binding.bindingType = id;
        
        var enabledSprite = ResourceUtils.LoadSpriteResource($"Bindings.{id}_enabled");
        binding.enabledSprite = enabledSprite;
        binding.disabledSprite = ResourceUtils.LoadSpriteResource($"Bindings.{id}_disabled");
        
        obj.AddComponent<SpriteRenderer>().sprite = enabledSprite;
        Bindings.Add((id, enabledSprite));
        
        var collider = obj.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;
        collider.radius = 0.65f;

        return new CustomObject(name, $"{id}_binding", obj,
                description: (overrideDesc ?? "Temporarily disables a skill."+ " Touching the binding will toggle it.\n\n") +
                             "Bindings are active by default, set 'Binding Active' to false\n" +
                             "for a binding the player must touch to enable.\n\n" +
                             "Set 'Reversible' to true to allow the player to toggle the binding multiple times.")
            .WithBroadcasterGroup(BroadcasterGroup.Bindings)
            .WithConfigGroup(ConfigGroup.Bindings);
    }
    #endregion

    #region Binding Checks
    private static bool BindingCheck(bool orig, string type)
    {
        if (!orig) return false;

        if (!ActiveBindings.TryGetValue(type, out var list) || list.Count == 0) return true;

        list.RemoveAll(binding => !binding);

        return list.Count(binding => binding.active && binding.gameObject.activeInHierarchy) == 0;
    }
    
    private static bool VisibleBindingCheck(string type)
    {
        if (!ActiveVisibleBindings.TryGetValue(type, out var list) || list.Count == 0) return true;

        list.RemoveAll(binding => !binding);

        return list.Count(binding => binding.active && binding.gameObject.activeInHierarchy) == 0;
    }
    #endregion

    #region UI
    
    private static void SetupVisuals()
    {
        _canvasObj = new GameObject("[Architect] Ability Canvas");
        _canvasObj.SetActive(false);
        Object.DontDestroyOnLoad(_canvasObj);

        _canvasObj.AddComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = _canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        
        _canvasObj.AddComponent<GraphicRaycaster>();

        for (var i = 0; i <= Bindings.Count; i++)
        {
            BindingIcons.Add(UIUtils.MakeImage($"Binding Icon {i}", _canvasObj, new Vector2(26 * i + 22, 22),
                Vector2.zero, Vector2.zero, new Vector2(45, 45)));
        }

        for (var i = 0; i < Crystals.Count/3; i++)
        {
            var img = UIUtils.MakeImage($"Crystal Icon {i}", _canvasObj, new Vector2(18 * i + 22, -22),
                new Vector2(0, 1), new Vector2(0, 1), new Vector2(45, 45));
            img.preserveAspect = true;
            CrystalIcons.Add(img);
        }
        
        RefreshBindingUI();
        RefreshCrystalUI();
    }
    
    public static void RefreshBindingUI()
    {
        var i = 0;
        foreach (var bind in Bindings
                     .Where(bind => !VisibleBindingCheck(bind.Item1)))
        {
            BindingIcons[i].sprite = bind.Item2;
            i++;
        }

        for (; i <= Bindings.Count; i++) BindingIcons[i].sprite = ArchitectPlugin.BlankSprite;
    }

    public static void RefreshCrystalUI()
    {
        var i = 0;
        foreach (var bind in Crystals
                     .Where(bind => ActiveCrystals.GetValueOrDefault(bind.Item1) == bind.Item2))
        {
            CrystalIcons[i].sprite = bind.Item3;
            i++;
        }

        for (var k = i; k < Crystals.Count/3; k++) CrystalIcons[k].sprite = ArchitectPlugin.BlankSprite;
    }
    #endregion

    private static bool _shadowDashCheck;
    
    #region Hooks
    private static void SetupBindingHooks()
    {
        On.HeroController.CanDash += (orig, self) => BindingCheck(orig(self), "dash");
        On.HeroController.CanFocus += (orig, self) => BindingCheck(orig(self), "focus");
        On.HeroController.CanAttack += (orig, self) => BindingCheck(orig(self), "attack");
        On.HeroController.CanJump += (orig, self) => BindingCheck(orig(self), "jump");
        On.HeroController.CanDoubleJump += (orig, self) => BindingCheck(orig(self), "wings");
        On.HeroController.CanDreamNail += (orig, self) => BindingCheck(orig(self), "dnail");
        On.HeroController.CanQuickMap += (orig, self) => BindingCheck(orig(self), "map");
        
        ModHooks.GetPlayerBoolHook += (name, orig) =>
        {
            return name switch
            {
                "hasShadowDash" => BindingCheck(orig, "shadow_dash")
                                   || ActiveCrystals.GetValueOrDefault("shadow_dash") > 0
                                   || _shadowDashCheck,
                "hasLantern" => BindingCheck(orig, "lantern"),
                "hasSuperDash" => BindingCheck(orig, "cdash") 
                                  || ActiveCrystals.GetValueOrDefault("crystal_dash") > 0,
                "hasWalljump" => BindingCheck(orig, "claw"),
                _ => orig
            };
        };

        On.ShadowGateColliderControl.FixedUpdate += (orig, self) =>
        {
            self.unlocked = GameManager.instance.playerData.GetBool("hasShadowDash");
            orig(self);
        };

        On.HeroController.Bounce += (orig, self) =>
        {
            if (BindingCheck(true, "pogo")) orig(self);
        };

        On.HeroController.CanNailCharge += (orig, self) =>
        {
            if (!orig(self)) return false;
            return BindingCheck(PlayerData.instance.hasUpwardSlash, "dslash") ||
                   BindingCheck(PlayerData.instance.hasDashSlash, "gslash") ||
                   BindingCheck(PlayerData.instance.hasCyclone, "cslash");
        };
        On.HeroController.CanNailArt += (orig, self) =>
        {
            if (!orig(self)) return false;
            return BindingCheck(PlayerData.instance.hasUpwardSlash, "dslash") ||
                   BindingCheck(PlayerData.instance.hasDashSlash, "gslash") ||
                   BindingCheck(PlayerData.instance.hasCyclone, "cslash");
        };
        
        On.PlayMakerFSM.Awake += (orig, self) =>
        {
            orig(self);
            switch (self.FsmName)
            {
                case "Spell Control":
                    InitSpellHooks(self);
                    break;
                case "Nail Arts":
                    InitNailArtBindings(self);
                    break;
                case "Dream Nail":
                    InitDreamNailBindings(self);
                    break;
                case "Acid Armour Check":
                    InitTearBinding(self);
                    break;
            }
        };

        SetupPantheonBindingHooks();
    }

    private static void InitSpellHooks(PlayMakerFSM fsm)
    {
        fsm.InsertCustomAction("Has Fireball?", playMakerFsm =>
        {
            var val = playMakerFsm.FsmVariables.FindFsmInt("Spell Level");
            if (!BindingCheck(true, "spirit")) val.Value = 0;
            else if (val.Value == 2 && !BindingCheck(true, "shade_soul")) val.Value = 1;
        }, 1);
        fsm.InsertCustomAction("Has Quake?", playMakerFsm =>
        {
            var val = playMakerFsm.FsmVariables.FindFsmInt("Spell Level");
            if (!BindingCheck(true, "dive")) val.Value = 0;
            else if (val.Value == 2 && !BindingCheck(true, "descending_dark")) val.Value = 1;
        }, 1);
        fsm.InsertCustomAction("Has Scream?", playMakerFsm =>
        {
            var val = playMakerFsm.FsmVariables.FindFsmInt("Spell Level");
            if (!BindingCheck(true, "wraiths")) val.Value = 0;
            else if (val.Value == 2 && !BindingCheck(true, "abyss_shriek")) val.Value = 1;
        }, 1);

        fsm.InsertCustomAction("Level Check", () => { PlayerBlock.TriggerEvent("Spirit"); }, 0);
        fsm.InsertCustomAction("Level Check 2", () => { PlayerBlock.TriggerEvent("Dive"); }, 0);
        fsm.InsertCustomAction("Level Check 3", () => { PlayerBlock.TriggerEvent("Wraiths"); }, 0);
    }
    
    private static void InitNailArtBindings(PlayMakerFSM fsm)
    {
        fsm.AddCustomAction("Dash Slash Ready", playMakerFsm =>
        {
            if (!BindingCheck(true, "dslash")) playMakerFsm.SetState("Inactive");
        });

        fsm.InsertCustomAction("Has Cyclone?", playMakerFsm =>
        {
            if (!BindingCheck(true, "cslash")) playMakerFsm.FsmVariables.FindFsmBool("Has Cyclone").Value = false;
        }, 2);
        fsm.InsertCustomAction("Has Cyclone?", playMakerFsm =>
        {
            if (!BindingCheck(true, "gslash")) playMakerFsm.FsmVariables.FindFsmBool("Has G Slash").Value = false;
        }, 2);

        fsm.InsertCustomAction("Has G Slash?", playMakerFsm =>
        {
            if (!BindingCheck(true, "cslash")) playMakerFsm.FsmVariables.FindFsmBool("Has Cyclone").Value = false;
        }, 2);
        fsm.InsertCustomAction("Has G Slash?", playMakerFsm =>
        {
            if (!BindingCheck(true, "gslash")) playMakerFsm.FsmVariables.FindFsmBool("Has G Slash").Value = false;
        }, 2);
    }

    private static readonly SetCollider DisableSwim = new()
    {
        active = false,
        gameObject = new FsmOwnerDefault
        {
            OwnerOption = OwnerDefaultOption.UseOwner
        }
    };

    private static readonly SetDamageHeroAmount Damage = new()
    {
        damageDealt = 1,
        target = new FsmOwnerDefault
        {
            OwnerOption = OwnerDefaultOption.UseOwner
        }
    };

    private static void InitTearBinding(PlayMakerFSM fsm)
    {
        fsm.DisableAction("Check", 0);
        fsm.AddCustomAction("Check", CustomTearAction);
        fsm.AddGlobalTransition("REFRESH ACID ARMOUR", "Check");
        var damager = fsm.TryGetState("Disable", out _);
        FsmUtil.AddState(fsm, "Lost Tear").AddAction(damager ? Damage : DisableSwim);

        fsm.AddTransition("Check", "LOST", "Lost Tear");
    }

    private static void CustomTearAction(PlayMakerFSM makerFsm)
    {
        if (!BindingCheck(PlayerData.instance.hasAcidArmour, "tear"))
        {
            makerFsm.SendEvent("LOST");
            return;
        }

        makerFsm.SendEvent("DISABLE");
        makerFsm.SendEvent("ENABLE");
    }

    private static void InitDreamNailBindings(PlayMakerFSM fsm)
    {
        fsm.InsertCustomAction("Dream Gate?", makerFsm =>
        {
            if (!BindingCheck(true, "gate") || EditManager.IsEditing) makerFsm.SendEvent("FINISHED");
        }, 1);
    }
    
    private static void SetupCrystalHooks()
    {
        On.HeroController.CanDash += (orig, self) => ActiveCrystals.GetValueOrDefault("dash") > 0
                                                     || ActiveCrystals.GetValueOrDefault("shadow_dash") > 0
                                                     || orig(self);

        On.PlayMakerFSM.Awake += (orig, self) =>
        {
            orig(self);
            if (self.FsmName == "Superdash" && self.TryGetState("Dashing", out var d))
                d.AddAction(() =>
                {
                    if (ActiveCrystals.ContainsKey("crystal_dash"))
                    {
                        ActiveCrystals["crystal_dash"] -= 1;
                        RefreshCrystalUI();
                    }
                });
        };
        
        On.HeroController.HeroDash += (orig, self) =>
        {
            orig(self);
            
            if (ActiveCrystals.ContainsKey("dash"))
            {
                ActiveCrystals["dash"] -= 1;
                RefreshCrystalUI();
            }

            if (ActiveCrystals.ContainsKey("shadow_dash"))
            {
                ActiveCrystals["shadow_dash"] -= 1;
                RefreshCrystalUI();
                _shadowDashCheck = true;
            }
        };
        On.HeroController.FinishedDashing += (orig, self) =>
        {
            orig(self);
            _shadowDashCheck = false;
            if (ActiveCrystals.GetValueOrDefault("shadow_dash") > 0) RechargeShadowDash();
        };
        On.HeroController.CanDoubleJump += (orig, self) => ActiveCrystals.GetValueOrDefault("wings") > 0 || orig(self);
        On.HeroController.DoDoubleJump += (orig, self) =>
        {
            orig(self);
            
            if (ActiveCrystals.ContainsKey("wings"))
            {
                ActiveCrystals["wings"] -= 1;
                RefreshCrystalUI();
            }
        };

        On.HeroController.TakeDamage += (orig, self, go, side, amount, type) =>
        {
            if (type != 1)
            {
                ActiveCrystals.Clear();
                RefreshCrystalUI();
            }
            orig(self, go, side, amount, type);
        };
    }

    private static void SetupPantheonBindingHooks()
    {
        On.HeroController.SceneInit += (orig, self) =>
        {
            orig(self);
            
            RefreshCharmsBinding();
            RefreshNailBinding();
            RefreshShellBinding();
            RefreshSoulBinding();
        };
        
        _ = new Hook(typeof(BossSequenceController).GetProperty(nameof(BossSequenceController.BoundSoul))!.GetGetMethod(),
            (Func<bool> orig) => orig() || !BindingCheck(true, "soul"));
        _ = new Hook(typeof(BossSequenceController).GetProperty(nameof(BossSequenceController.BoundShell))!.GetGetMethod(),
            (Func<bool> orig) => orig() || !BindingCheck(true, "shell"));
        _ = new Hook(typeof(BossSequenceController).GetProperty(nameof(BossSequenceController.BoundNail))!.GetGetMethod(),
            (Func<bool> orig) => orig() || !BindingCheck(true, "nail"));
        _ = new Hook(typeof(BossSequenceController).GetProperty(nameof(BossSequenceController.BoundCharms))!.GetGetMethod(),
            (Func<bool> orig) => orig() || !BindingCheck(true, "charms"));
        
        _ = new Hook(typeof(BossSequenceController).GetProperty(nameof(BossSequenceController.BoundNailDamage))!.GetGetMethod(),
            (Func<int> orig) =>
            {
                if (!BindingCheck(true, "nail"))
                {
                    var num = GameManager.instance.playerData.GetInt("nailDamage");
                    return num >= 13 ? 13 : Mathf.RoundToInt(num * 0.8f);
                }
                return orig();
            });
        _ = new Hook(typeof(BossSequenceController).GetProperty(nameof(BossSequenceController.BoundMaxHealth))!.GetGetMethod(),
            (Func<int> orig) =>
            {
                if (!BindingCheck(true, "shell"))
                {
                    var num = !GameManager.instance.playerData.GetBool("equippedCharm_23") ||
                              GameManager.instance.playerData.GetBool("brokenCharm_23")
                        ? 0
                        : 2;
                    return 4 + num;
                }
                return orig();
            });

        On.GGCheckBoundSoul.OnEnter += (orig, self) =>
        {
            if (BossSequenceController.IsInSequence)
            {
                orig(self);
                return;
            }

            if (BossSequenceController.BoundSoul)
            {
                self.Fsm.Event(self.boundEvent);
                self.Finish();
            }
            else orig(self);
        };
    }

    private static bool _nailBound;
    private static bool _shellBound;
    private static bool _charmsBound;
    private static bool _soulBound;

    private static void RefreshNailBinding()
    {
        if (_nailBound == BossSequenceController.BoundNail) return;
        _nailBound = !_nailBound;

        EventRegister.SendEvent(_nailBound ? "SHOW BOUND NAIL" : "HIDE BOUND NAIL");
        PlayMakerFSM.BroadcastEvent("UPDATE NAIL DAMAGE");
    }

    private static void RefreshShellBinding()
    {
        if (_shellBound == BossSequenceController.BoundShell) return;
        _shellBound = !_shellBound;
        
        var health = GameManager.instance.playerData.health;
        GameManager.instance.playerData.MaxHealth();
        PlayMakerFSM.BroadcastEvent("CHARM INDICATOR CHECK");
        PlayMakerFSM.BroadcastEvent("HUD IN");
        GameManager.instance.playerData.health = health;
    }

    private static int[] _previousEquippedCharms;
    private static bool _wasOvercharmed;

    private static void RefreshCharmsBinding()
    {
        if (BossSequenceController.IsInSequence) return;
        if (BossSequenceController.BoundCharms)
        {
            if (_charmsBound) return;

            _previousEquippedCharms =
                GameManager.instance.playerData.GetVariable<List<int>>("equippedCharms").ToArray();
            GameManager.instance.playerData.GetVariable<List<int>>("equippedCharms").Clear();
            _wasOvercharmed = GameManager.instance.playerData.GetBool("overcharmed");
            GameManager.instance.playerData.SetBool("overcharmed", false);

            foreach (var previousEquippedCharm in _previousEquippedCharms)
                GameManager.instance.SetPlayerDataBool("equippedCharm_" + previousEquippedCharm, false);

            EventRegister.SendEvent("SHOW BOUND CHARMS");

            _charmsBound = true;
        }
        else if (_charmsBound)
        {
            GameManager.instance.playerData.SetVariable(
                "equippedCharms",
                new List<int>(_previousEquippedCharms));

            foreach (var previousEquippedCharm in _previousEquippedCharms)
                GameManager.instance.SetPlayerDataBool("equippedCharm_" + previousEquippedCharm, true);

            GameManager.instance.playerData.SetBool("overcharmed", _wasOvercharmed);
            EventRegister.SendEvent("HIDE BOUND CHARMS");

            _charmsBound = false;
        }
    }

    private static void RefreshSoulBinding()
    {
        if (BossSequenceController.IsInSequence) return;
        if (BossSequenceController.BoundSoul == _soulBound) return;
        _soulBound = !_soulBound;

        if (_soulBound)
        {
            var soul = PlayerData.instance.MPCharge;
            PlayerData.instance.ClearMP();
            PlayerData.instance.AddMPCharge(Math.Min(soul, 33));
        }
        
        GameManager.instance.StartCoroutine(RefreshSoulBindingRoutine());
    }
    
    private static IEnumerator RefreshSoulBindingRoutine()
    {
        while (GameManager.instance.soulOrb_fsm.ActiveStateName != "Idle") yield return null;
        if (_soulBound)
        {
            EventRegister.SendEvent("BIND VESSEL ORB");
            GameManager.instance.soulOrb_fsm.SendEvent("MP LOSE");
            GameManager.instance.soulVessel_fsm.SendEvent("MP RESERVE DOWN");
        }
        else
        {
            GameManager.instance.soulOrb_fsm.SendEvent("MP LOSE");
            EventRegister.SendEvent("UNBIND VESSEL ORB");
        }
    }

    public static void RefreshBinding(string type)
    {
        switch (type)
        {
            case "charms":
                RefreshCharmsBinding();
                break;
            case "shell":
                RefreshShellBinding();
                break;
            case "soul":
                RefreshSoulBinding();
                break;
            case "nail":
                RefreshNailBinding();
                break;
            case "tear":
                RefreshTearBinding();
                break;
            case "lantern":
                RefreshLanternBinding();
                break;
        }
        
        RefreshBindingUI();
    }

    private static void RefreshTearBinding()
    {
        PlayMakerFSM.BroadcastEvent("REFRESH ACID ARMOUR");
    }

    public static void RefreshLanternBinding()
    {
        PlayMakerFSM.BroadcastEvent("RESET");
        if (GameManager.instance.sm.GetDarknessLevel() > 0 &&
            BindingCheck(PlayerData.instance.GetBool("hasLantern"), "lantern"))
            HeroController.instance.SetWieldingLantern(true);
        else HeroController.instance.SetWieldingLantern(false);
    }

    public static void OnCollect(string type)
    {
        if (type == "shadow_dash") RechargeShadowDash();
    }
    
    public static void RechargeShadowDash()
    {
        if (HeroController.instance.shadowDashTimer <= 0) return;
        HeroController.instance.shadowDashTimer = 0.0f;
        FSMUtility.LocateFSM(HeroController.instance.shadowRechargePrefab, "Recharge Effect").SetState("Burst");
        HeroController.instance.spriteFlash.FlashShadowRecharge();
    }
    #endregion
}