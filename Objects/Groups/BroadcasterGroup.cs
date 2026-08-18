using System.Collections.Generic;

namespace Architect.Objects.Groups;

public static class BroadcasterGroup
{
    public static readonly List<string> TriggerZone = ["ZoneEnter", "ZoneExit"];
    
    public static readonly List<string> Damageable = ["OnDamage"];
    
    public static readonly List<string> Shades = ["OnDeath", "OnDamage"];
    
    public static readonly List<string> Enemies = ["OnDeath", "FirstDeath", "LoadedDead", "OnDamage"];
    
    public static readonly List<string> ZoteHead = ["OnHit", "OnAir", "OnLand"];
    
    public static readonly List<string> Landable = ["OnAir", "OnLand"];
    
    public static readonly List<string> Transitions = ["OnExit"];
    
    public static readonly List<string> Npcs = ["OnFinish"];
    
    public static readonly List<string> EnemyDamager = ["OnDamage"];
    
    public static readonly List<string> FsmHook = ["OnChange", "OnTarget"];
    
    public static readonly List<string> Activatable = ["OnActivate"];
    
    public static readonly List<string> Feather = ["OnActivate", "OnFinish"];
    
    public static readonly List<string> Finishable = ["OnFinish"];
    
    public static readonly List<string> Png = ["OnFinish", "OnFrameChange"];
    
    public static readonly List<string> Toll = ["OnPay", "FirstPay", "LoadedPaid"];
    
    public static readonly List<string> Breakable = ["OnBreak"];
    
    public static readonly List<string> PersistentBreakable = ["OnBreak", "FirstBreak", "LoadedBroken"];
    
    public static readonly List<string> ActiveDeactivatable = ["OnActivate", "OnDeactivate"];
    
    public static readonly List<string> Levers = ["OnPull", "FirstPull", "LoadedPulled"];
    
    public static readonly List<string> Buttons = ["OnPress", "FirstPress", "LoadedPressed"];
    
    public static readonly List<string> Bindings = ["OnBind", "OnUnbind"];
    
    public static readonly List<string> Binoculars = ["OnStart", "OnStop"];
    
    public static readonly List<string> Callable = ["OnCall"];
    
    public static readonly List<string> Benches = ["OnSit", "OnLeave", "OnSpawnAt"];
    
    public static readonly List<string> Item = ["BeforePickup"];
    
    public static readonly List<string> Hittable = ["OnHit"];
    
    public static readonly List<string> GeoRock = ["OnHit", "OnBreak"];
    
    public static readonly List<string> AbilityCrystal = ["OnCollect", "OnRegen"];
    
    public static readonly List<string> ObjectAnchor = ["OnReverse", "OnTrackEnd"];
    
    public static readonly List<string> Interaction = ["OnInteract"];
    
    public static readonly List<string> BounceShroom = ["OnBounce", "OnLargeBounce"];
    
    public static readonly List<string> Openable = ["OnOpen", "FirstOpen", "LoadedOpen"];
}
