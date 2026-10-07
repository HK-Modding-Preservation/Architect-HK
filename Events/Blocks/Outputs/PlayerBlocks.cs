using System;
using System.Collections.Generic;
using Architect.Events.Blocks.Events;
using GlobalEnums;
using UnityEngine;
using Math = System.Math;

namespace Architect.Events.Blocks.Outputs;

public class HpBlock : PlayerBlock
{
    public static int LastHit;
    
    protected override IEnumerable<string> Inputs => [
        "Max", 
        "Give", 
        "Take",
        "TakeHazard"];
    
    protected override IEnumerable<string> Outputs => [
        "OnHazardRespawn",
        "OnDamage",
        "OnDeath",
        "OnHeal"];
    
    protected override IEnumerable<(string, string)> OutputVars => [
        ("LastHit", "Number"),
        Space,
        ("Amount", "Number"),
        Space,
        ("MaxAmount", "Number"),
        Space,
        ("Lifeblood", "Number")
    ];
    
    protected override string Name => "Health Control";

    public override void Reset()
    {
        Amount = 0;
    }

    public int Amount;
    public CollisionSide CollisionSide = CollisionSide.other;

    protected override void Trigger(string trigger)
    {
        switch (trigger)
        {
            case "Max":
                HeroController.instance.AddHealth(99999);
                break;
            case "Give":
                HeroController.instance.AddHealth(Amount);
                break;
            case "Take":
                HeroController.instance.TakeDamage(HeroController.instance.gameObject, CollisionSide, Amount, 1);
                break;
            case "TakeHazard":
                HeroController.instance.TakeHealth(Amount - 1);
                HeroController.instance.TakeDamage(HeroController.instance.gameObject, CollisionSide, 
                    1, 2);
                break;
        }
    }

    public override object GetValue(string id)
    {
        return id switch
        {
            "Amount" => PlayerData.instance.health,
            "LastHit" => LastHit,
            "MaxAmount" => PlayerData.instance.maxHealth,
            "Lifeblood" => PlayerData.instance.healthBlue,
            _ => 0
        };
    }
}

public class InvulBlock : ScriptBlock
{
    public static bool Invulnerable;

    public static void Init()
    {
        HookUtils.OnHeroAwake += _ => Invulnerable = false;
    }

    public float Duration;
    
    protected override string Name => "Invulnerable Control";
    
    protected override IEnumerable<string> Inputs => [
        "MakeInvulnerable",
        "MakeVulnerable",
        "SetInvulTime"
    ];
    
    protected override void Trigger(string trigger)
    {
        switch (trigger)
        {
            case "MakeInvulnerable":
                Invulnerable = true;
                PlayerData.instance.isInvincible = true;
                break;
            case "MakeVulnerable":
                Invulnerable = false;
                PlayerData.instance.isInvincible = false;
                break;
            case "SetInvulTime":
                HeroController.instance.StartCoroutine(HeroController.instance.Invulnerable(Duration));
                break;
        }
    }
}

public class SilkBlock : ScriptBlock
{
    public static void Init()
    {
        typeof(PlayerData).Hook(nameof(PlayerData.AddMPCharge),
            (Func<PlayerData, int, bool> orig, PlayerData self, int amount) =>
            {
                _onSilkGain?.Invoke();
                return orig(self, amount);
            });
    }
    
    protected override IEnumerable<string> Inputs => ["Max", "Give", "Take"];
    protected override IEnumerable<string> Outputs => ["OnGain"];
    protected override IEnumerable<(string, string)> OutputVars => [("Amount", "Number")];
    
    protected override string Name => "Soul Control";

    public override void Reset()
    {
        Amount = 0;
    }

    public override void SetupReference()
    {
        var scr = new GameObject("[Architect] Silk Control Ref");
        scr.AddComponent<SilkControl>().Block = this;
    }

    private static Action _onSilkGain;

    public class SilkControl : MonoBehaviour
    {
        public SilkBlock Block;
        
        private void Start()
        {
            _onSilkGain += OnGain;
        }

        private void OnDisable()
        {
            _onSilkGain -= OnGain;
        }

        public void OnGain() => Block.Event("OnGain");
    }

    public int Amount;

    protected override void Trigger(string trigger)
    {
        switch (trigger)
        {
            case "Max":
                HeroController.instance.AddMPCharge(9999);
                break;
            case "Give":
                HeroController.instance.AddMPCharge(Amount);
                break;
            case "Take":
                HeroController.instance.TakeMP(Amount);
                break;
        }
    }

    public override object GetValue(string id)
    {
        return PlayerData.instance.MPCharge;
    }
}

public class CurrencyBlock : ScriptBlock
{
    protected override IEnumerable<string> Inputs => ["Give", "Take"];
    protected override IEnumerable<(string, string)> OutputVars => [("Amount", "Number")];
    
    protected override string Name => "Currency Control";

    public override void Reset()
    {
        Amount = 0;
    }

    public int Amount;

    protected override void Trigger(string trigger)
    {
        if (trigger == "Give")
        {
            HeroController.instance.AddGeo(Amount);
        }
        else
        {
            var a = Math.Min(PlayerData.instance.geo, Amount);
            HeroController.instance.TakeGeo(a);
        }
    }

    public override object GetValue(string id)
    {
        return PlayerData.instance.geo;
    }
}
