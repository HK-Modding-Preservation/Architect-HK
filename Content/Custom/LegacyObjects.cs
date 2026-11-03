using System.Collections.Generic;
using Architect.Behaviour.Utility;
using Architect.Config;
using Architect.Config.Types;
using Architect.Events;
using Architect.Objects.Categories;
using Architect.Objects.Groups;
using Architect.Objects.Placeable;
using UnityEngine;

namespace Architect.Content.Custom;

public static class LegacyObjects
{
    public static void Init()
    {
        Categories.Legacy.Add(CreateTimer());
        Categories.Legacy.Add(CreateKeyListener());
        Categories.Legacy.Add(CreateRelay());
        Categories.Legacy.Add(CreateTextDisplay());
        Categories.Legacy.Add(CreateChoiceDisplay());
        Categories.Legacy.Add(CreateObjectExtractor());
    }

    private static PlaceableObject CreateObjectExtractor()
    {
        var extractor = new GameObject("Object Extractor");
        extractor.SetActive(false);
        Object.DontDestroyOnLoad(extractor);
        
        ObjectExtractor.Init();
        extractor.AddComponent<ObjectExtractor>();

        return new CustomObject("Object Extractor", "object_extractor",
                extractor,
                sprite: ResourceUtils.LoadSpriteResource("object_extractor", FilterMode.Point),
                description: "Can extract any vanilla object from a scene and spawn it elsewhere.\n" +
                             "Useful for spawning things like vanilla textures.\n" +
                             "Not everything will work outside of its normal location.")
            .WithConfigGroup(ConfigGroup.ObjectExtractor)
            .WithOutputGroup(OutputGroup.ObjectExtractor);
    }

    private static PlaceableObject CreateTimer()
    {
        var timer = new GameObject("Timer");
        timer.SetActive(false);
        Object.DontDestroyOnLoad(timer);
        
        timer.AddComponent<Timer>();

        return new CustomObject("Timer", "timer",
                timer,
                sprite: ResourceUtils.LoadSpriteResource("timer", FilterMode.Point, ppu:10),
                description: "[LEGACY]" +
                             "\n\nBroadcasts an event periodically.")
            .WithConfigGroup(Timer)
            .WithBroadcasterGroup(BroadcasterGroup.Callable)
            .WithReceiverGroup(ReceiverGroup.Generic);
    }

    private static PlaceableObject CreateTextDisplay()
    {
        var display = new GameObject("Text Display");
        display.SetActive(false);
        Object.DontDestroyOnLoad(display);
        
        display.AddComponent<TextDisplay>();

        return new CustomObject("Text Display", "text_display",
                display,
                sprite: ResourceUtils.LoadSpriteResource("text_display", FilterMode.Point, ppu:10),
                description: "[LEGACY]" +
                             "\n\nDisplays a piece of text.")
            .WithReceiverGroup(Displayable)
            .WithConfigGroup(TextDisplay)
            .WithBroadcasterGroup(["OnClose"]);
    }

    private static PlaceableObject CreateChoiceDisplay()
    {
        var display = new GameObject("Choice Display");
        display.SetActive(false);
        Object.DontDestroyOnLoad(display);
        
        display.AddComponent<TextDisplay>().mode = 2;

        return new CustomObject("Choice Display", "choice_display",
                display,
                sprite: ResourceUtils.LoadSpriteResource("choice_display", FilterMode.Point, ppu:10),
                description: "[LEGACY]\n\n" +
                             "Displays a piece of text and prompts the player to choose Yes or No.")
            .WithReceiverGroup(Displayable)
            .WithBroadcasterGroup(["Yes", "No"])
            .WithConfigGroup(Choice);
    }

    private static PlaceableObject CreateKeyListener()
    {
        var keyListener = new GameObject("Key Listener");

        keyListener.AddComponent<KeyListener>();

        keyListener.SetActive(false);
        Object.DontDestroyOnLoad(keyListener);

        return new CustomObject("Key Listener", "key_listener", keyListener,
            description:"[LEGACY]\n\n" +
                        "Can listen and broadcast events when keys are pressed and released.",
            sprite:ResourceUtils.LoadSpriteResource("key_listener", FilterMode.Point, ppu:10))
            .WithConfigGroup(ConfigGroup.KeyListener)
            .WithBroadcasterGroup(["KeyPressed", "KeyReleased"]);
    }

    private static PlaceableObject CreateRelay()
    {
        var relay = new GameObject("Relay");

        relay.AddComponent<Relay>();

        relay.SetActive(false);
        Object.DontDestroyOnLoad(relay);

        return new CustomObject("Relay", "relay",
                relay,
                sprite: ResourceUtils.LoadSpriteResource("relay", FilterMode.Point, ppu:10),
                description: "[LEGACY]\n\n" +
                             "Broadcasts the OnCall event when the Call trigger is run.")
            .WithConfigGroup(Relay)
            .WithBroadcasterGroup(BroadcasterGroup.Callable)
            .WithReceiverGroup(RelayReceivers);
    }
    
    public static readonly List<ConfigType> Timer =  GroupUtils.Merge(ConfigGroup.Generic, [
        ConfigurationManager.RegisterConfigType(
            new FloatConfigType("Start Delay", "timer_start_delay", (o, value) =>
            {
                o.GetComponent<Timer>().startDelay = value.GetValue();
            }).WithDefaultValue(0)),
        ConfigurationManager.RegisterConfigType(
            new FloatConfigType("Repeat Delay", "timer_repeat_delay", (o, value) =>
            {
                o.GetComponent<Timer>().repeatDelay = value.GetValue();
            }).WithDefaultValue(1)),
        ConfigurationManager.RegisterConfigType(
            new FloatConfigType("Random Delay", "timer_rand_delay", (o, value) =>
            {
                o.GetComponent<Timer>().randDelay = value.GetValue();
            }).WithDefaultValue(0)),
        ConfigurationManager.RegisterConfigType(
            new IntConfigType("Max Calls", "timer_limit", (o, value) =>
            {
                o.GetComponent<Timer>().maxCalls = value.GetValue();
            }))
    ]);
    
    public static readonly List<EventReceiverType> Displayable = GroupUtils.Merge(ReceiverGroup.Generic, [
        EventManager.RegisterReceiverType(new EventReceiverType("display_text", "Show", o =>
        {
            o.GetComponent<IDisplayable>().Display();
        }))
    ]);

    public static readonly List<ConfigType> TextDisplay = GroupUtils.Merge(ConfigGroup.Generic, [
        ConfigurationManager.RegisterConfigType(
            new StringConfigType("Text", "display_text", (o, value) =>
            {
                o.GetComponent<TextDisplay>().text = value.GetValue();
            }).WithDefaultValue("Sample Text").WithPriority(-1))
    ]);
    
    public static readonly List<ConfigType> Choice = GroupUtils.Merge(TextDisplay, [
        ConfigurationManager.RegisterConfigType(
            new IntConfigType("Required Amount", "choice_cost", (o, value) =>
            {
                o.GetComponent<TextDisplay>().cost = value.GetValue();
            }).WithDefaultValue(0))
    ]);

    public static readonly List<ConfigType> Relay = [
        ConfigurationManager.RegisterConfigType(
            new StringConfigType("Relay ID", "relay_id", (o, value) =>
            {
                o.GetComponent<Relay>().id = value.GetValue();
            }).WithPriority(-1)),
        ConfigurationManager.RegisterConfigType(
            new BoolConfigType("Reset on Bench", "relay_bench_reset", (o, value) =>
            {
                o.GetComponent<Relay>().semiPersistent = value.GetValue();
            }).WithPriority(-1)),
        ConfigurationManager.RegisterConfigType(
            new FloatConfigType("Delay", "relay_delay", (o, value) =>
            {
                o.GetComponent<Relay>().delay = value.GetValue();
            }).WithDefaultValue(0).WithPriority(-1)),
        ConfigurationManager.RegisterConfigType(
            new FloatConfigType("Relay Chance", "relay_chance", (o, value) =>
            {
                o.GetComponent<Relay>().relayChance = value.GetValue();
            }).WithDefaultValue(1).WithPriority(-1)),
        ConfigurationManager.RegisterConfigType(
            new BoolConfigType("Trigger on Load", "relay_load_trigger", (o, value) =>
            {
                o.GetComponent<Relay>().broadcastImmediately = value.GetValue();
            }).WithDefaultValue(false).WithPriority(-1)),
        ConfigurationManager.RegisterConfigType(
            new BoolConfigType("Start Enabled", "relay_start_active", (o, value) =>
            {
                o.GetComponent<Relay>().startActivated = value.GetValue();
            }).WithDefaultValue(true).WithPriority(-1))
    ];
    
    public static readonly List<EventReceiverType> RelayReceivers = [
        EventManager.RegisterReceiverType(new EventReceiverType("do_relay", "Call", o =>
        {
            o.GetComponent<Relay>().DoRelay();
        })),
        EventManager.RegisterReceiverType(new EventReceiverType("relay_off", "Disable", o =>
        {
            o.GetComponent<Relay>().DisableRelay();
        })),
        EventManager.RegisterReceiverType(new EventReceiverType("relay_on", "Enable", o =>
        {
            o.GetComponent<Relay>().EnableRelay();
        }))
    ];
}