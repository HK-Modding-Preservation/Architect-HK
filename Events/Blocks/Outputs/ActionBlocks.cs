using Architect.Behaviour.Utility;
using Architect.Events.Blocks.Config;
using Architect.Events.Blocks.Objects;

namespace Architect.Events.Blocks.Outputs;

public static class ActionBlocks
{
    public static void Init()
    {
        Category.World.RegisterBlock<HpBlock>("Health Control", ConfigGroup.HealthHook);
        Category.World.RegisterBlock<InvulBlock>("Invulnerable Control", ConfigGroup.InvulHook, InvulBlock.Init);
        Category.World.RegisterBlock<SilkBlock>("Soul Control", ConfigGroup.SilkHook, SilkBlock.Init);
        Category.World.RegisterBlock<CurrencyBlock>("Currency Control", ConfigGroup.CurrencyHook);
        Category.World.RegisterBlock<JournalEntryBlock>("Journal Control", ConfigGroup.EntryControl);
        Category.World.RegisterBlock<AchievementBlock>("Achievement Control", ConfigGroup.AchievementControl);
        Category.World.RegisterBlock<CharmBlock>("Charm Control", ConfigGroup.CharmControl);
        Category.World.RegisterBlock<ItemBlock>("Item Control", ConfigGroup.ItemControl);
        Category.World.RegisterBlock<EnemyBlock>("Enemy Control", ConfigGroup.EnemyControl);
        Category.Visual.RegisterBlock<TextBlock>("Text Display", ConfigGroup.TextDisplay, TextDisplay.Init);
        Category.Visual.RegisterBlock<ChoiceBlock>("Choice Display", ConfigGroup.ChoiceDisplay);
        Category.Visual.RegisterBlock<TitleBlock>("Title Display", ConfigGroup.TitleDisplay);
        Category.Visual.RegisterBlock<ThoughtBlock>("Thought Display", ConfigGroup.ThoughtDisplay);
        Category.Visual.RegisterBlock<PngBlock>("Custom PNG", ConfigGroup.Png);
        
        Category.Visual.RegisterBlock<ShakeCameraBlock>("Camera Shake", ConfigGroup.CameraShaker);
        
        Category.Visual.RegisterBlock<UIBlock>("UI Control");
        Category.Visual.RegisterBlock<TimeSlowerBlock>("Time Slowdown", ConfigGroup.TimeSlower, TimeSlowerBlock.Init);
        Category.Visual.RegisterBlock<AnimatorBlock>("Animator Controller", ConfigGroup.AnimPlayer, PlayerAnimPlayer.Init);
        Category.World.RegisterBlock<TransitionBlock>("Transition", ConfigGroup.Transition);
        Category.Visual.RegisterBlock<SetLightingBlock>("Set Lighting", ConfigGroup.Lighting, SetLightingBlock.Init);
        Category.World.RegisterBlock<SpawnObjectBlock>("Spawn Object", ConfigGroup.SpawnObject);
        Category.World.RegisterBlock<SpawnPrefabBlock>("Spawn Prefab", ConfigGroup.Prefab);
        Category.World.RegisterBlock<ObjectMoverBlock>("Move Object");
        Category.World.RegisterBlock<ObjectScalerBlock>("Scale Object");

        Category.Visual.RegisterHiddenBlock<ShopBlock.ShopItemBlock>("Shop Item", ConfigGroup.ShopItem);
        Category.Visual.RegisterBlock<ShopBlock>("Shop", ConfigGroup.Shop, ShopBlock.Init);

        /*Category.Visual.RegisterHiddenBlock<TravelBlock.TravelItemBlock>("Travel Item");
        Category.Visual.RegisterBlock<TravelBlock>("Travel");*/

        // Category.Visual.RegisterBlock<StatueBlock>("Statue UI", ConfigGroup.Statue, StatueBlock.Init);
    }
}