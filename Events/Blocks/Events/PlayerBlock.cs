using System;
using System.Collections.Generic;
using Architect.Events.Blocks.Outputs;
using GlobalEnums;
using UnityEngine;

namespace Architect.Events.Blocks.Events;

public class PlayerBlock : ToggleableBlock
{
    protected override IEnumerable<string> Outputs => [
        "FaceLeft",
        "FaceRight",
        "Jump",
        "WallJump",
        "DoubleJump",
        "Land",
        "HardLand",
        "Dash",
        "Attack",
        "OnHazardRespawn",
        "OnDamage",
        "OnDeath",
        "OnHeal",
        "OnHealFail"
    ];

    protected override IEnumerable<(string, string)> OutputVars => [
        ("X", "Number"),
        ("Y", "Number"),
        ("Ground", "Boolean"),
        ("Left", "Boolean"),
        ("Right", "Boolean"),
        ("Up", "Boolean"),
        ("Down", "Boolean"),
        ("Self", "Object")
    ];
    protected override string Name => "Player Listener";

    public override void SetupReference()
    {
        var te = new GameObject("[Architect] Player Block").AddComponent<PlayerEvent>();
        te.Block = this;
    }

    public override object GetValue(string id)
    {
        return id switch
        {
            "Ground" => !HeroController.instance.cState.onGround,
            "HasControl" => !HeroController.instance.controlReqlinquished,
            "Left" => !HeroController.instance.cState.facingRight,
            "Right" => HeroController.instance.cState.facingRight,
            "Up" => HeroController.instance.cState.lookingUp,
            "Down" => HeroController.instance.cState.lookingDown,
            "Benched" => PlayerData.instance.atBench,
            "X" => HeroController.instance.transform.GetPositionX(),
            "Y" => HeroController.instance.transform.GetPositionY(),
            "Self" => HeroController.instance.gameObject,
            _ => throw new ArgumentOutOfRangeException(nameof(id), id, null)
        };
    }

    public class PlayerEvent : MonoBehaviour
    {
        public ScriptBlock Block;
    
        private void OnEnable()
        {
            PlayerListenerBlocks.Add(this);
        }

        private void OnDisable()
        {
            PlayerListenerBlocks.Remove(this);
        }
    }
    
    public static readonly List<PlayerEvent> PlayerListenerBlocks = [];
    
    public static void Init()
    {
        typeof(HeroController).Hook(nameof(HeroController.FlipSprite),
            (Action<HeroController> orig, HeroController self) =>
            {
                orig(self);
                TriggerEvent(self.cState.facingRight ? "FaceRight" : "FaceLeft");
            });

        typeof(HeroController).Hook(nameof(HeroController.HeroJump),
            (Action<HeroController> orig, HeroController self) =>
            {
                orig(self);
                TriggerEvent("Jump");
            });

        typeof(HeroController).Hook("DoWallJump",
            (Action<HeroController> orig, HeroController self) =>
            {
                orig(self);
                TriggerEvent("WallJump");
            });

        typeof(HeroController).Hook("DoDoubleJump",
            (Action<HeroController> orig, HeroController self) =>
            {
                orig(self);
                TriggerEvent("DoubleJump");
            });

        typeof(HeroController).Hook("HeroDash",
            (Action<HeroController> orig, HeroController self) =>
            {
                orig(self);
                TriggerEvent("Dash");
                if (self.dashingDown) TriggerEvent("DownDash");
            });
        
        typeof(HeroController).Hook("BackOnGround",
            (Action<HeroController> orig, HeroController self) =>
            {
                orig(self);
                TriggerEvent("Land");
            });

        typeof(HeroController).Hook("DoHardLanding",
            (Action<HeroController> orig, HeroController self) =>
            {
                orig(self);
                TriggerEvent("HardLand");
            });

        typeof(HeroController).Hook("Attack",
            (Action<HeroController, AttackDirection> orig, HeroController self, AttackDirection dir) =>
            {
                orig(self, dir);
                TriggerEvent("Attack");
                
                TriggerEvent(dir switch
                {
                    AttackDirection.upward => "UpAttack",
                    AttackDirection.downward => "DownAttack",
                    _ => "NormalAttack"
                });
            });
        
        On.HeroController.AddHealth += (orig, self, amount) =>
        {
            TriggerEvent("OnHeal");
            orig(self, amount);
        };
        
        ModHooks.BeforePlayerDeadHook += () => { TriggerEvent("OnDeath"); };

        ModHooks.TakeDamageHook += (ref type, damage) =>
        {
            TriggerEvent("OnDamage");
            HpBlock.LastHit = damage;
            if (type != 1) TriggerEvent("OnHazardRespawn");
            return damage;
        };
        
        ModHooks.SetPlayerBoolHook += (name, orig) =>
        {
            if (name == "atBench") TriggerEvent(orig ? "OnBench" : "OnUnbench");
            return orig;
        };
    }

    public static void TriggerEvent(string triggerName)
    {
        foreach (var obj in PlayerListenerBlocks.ToArray())
        {
            obj?.Block.Event(triggerName);
            obj?.Block.Event($"On{triggerName}");
        }
    }
}

public class StateBlock : PlayerBlock
{
    protected override string Name => "Player State";
    
    protected override IEnumerable<string> Outputs => [
        "OnFaceLeft",
        "OnFaceRight",
        "OnLand",
        "OnHardLand",
        "OnBench",
        "OnUnbench"
    ];

    protected override IEnumerable<(string, string)> OutputVars => [
        ("Ground", "Boolean"),
        ("HasControl", "Boolean"),
        ("Left", "Boolean"),
        ("Right", "Boolean"),
        ("Up", "Boolean"),
        ("Down", "Boolean"),
        ("Benched", "Boolean"),
        ("Self", "Object")
    ];
}

public class ActionBlock : PlayerBlock
{
    protected override string Name => "Player Movement";

    protected override IEnumerable<string> Inputs => [
        "RefreshDash",
        "RefreshDoubleJump",
        "Disable",
        "Enable"
    ];
    
    protected override IEnumerable<string> Outputs => [
        "OnJump",
        "OnWallJump",
        "OnDoubleJump",
        "OnDownDash",
        "OnDash"
    ];

    protected override IEnumerable<(string, string)> OutputVars => [
        ("X", "Number"),
        ("Y", "Number"),
        ("Self", "Object")
    ];

    protected override void Trigger(string id)
    {
        if (id == "RefreshDash")
        {
            HeroController.instance.airDashed = false;
            HeroController.instance.dashCooldownTimer = 0;
        }
        else HeroController.instance.doubleJumped = false;
    }
}

public class AttackBlock : PlayerBlock
{
    protected override string Name => "Player Attack";
    
    protected override IEnumerable<string> Outputs => [
        "OnAttack",
        "OnNormalAttack",
        "OnUpAttack",
        "OnDownAttack",
        "OnSpirit",
        "OnDive",
        "OnWraiths"
    ];

    protected override IEnumerable<(string, string)> OutputVars => [("Self", "Object")];
}
