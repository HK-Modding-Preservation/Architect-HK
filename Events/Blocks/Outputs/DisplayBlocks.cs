using System.Collections.Generic;
using Architect.Behaviour.Utility;
using UnityEngine;

namespace Architect.Events.Blocks.Outputs;

public class TitleBlock : ScriptBlock
{
    protected override IEnumerable<string> Inputs => ["Display", "Cancel"];
    
    protected override string Name => "Title Display";

    public override void Reset()
    {
        Header = "";
        Body = "";
        Footer = "";
    }

    public string Header = "";
    public string Body = "";
    public string Footer = "";
    public int TitleType;
    public bool WaitForCancel;

    protected override void Trigger(string trigger)
    {
        if (trigger == "Display") TitleUtils.DisplayTitle(Header, Body, Footer, TitleType, WaitForCancel);
        else TitleUtils.CancelTitle();
    }
}

public class TextBlock : ScriptBlock
{
    protected override IEnumerable<string> Inputs => ["Display", "Stop"];
    protected override IEnumerable<string> Outputs => ["OnClose"];
    
    protected override string Name => "Text Display";

    private TextDisplay _display;

    public override void Reset()
    {
        Text = "";
        Dream = false;
    } 
    
    public string Text = "";
    public bool Dream;
    public bool TakeControl = true;

    public override void SetupReference()
    {
        _display = new GameObject("[Architect] Text Display").AddComponent<TextDisplay>();
        _display.Block = this;

        _display.text = Text;
        _display.mode = Dream ? 0 : 1;
        _display.takeControl = TakeControl;
    }

    protected override void Trigger(string trigger)
    {
        if (trigger == "Stop") _display.Stop();
        else _display.Display();
    }
}

public class ChoiceBlock : ScriptBlock
{
    protected override IEnumerable<string> Inputs => ["Display"];
    protected override IEnumerable<string> Outputs => ["Yes", "No"];
    
    protected override string Name => "Choice Display";
    
    public string Text;
    public bool TakeControl = true;
    public int Cost;

    private TextDisplay _display;

    public override void SetupReference()
    {
        _display = new GameObject("[Architect] Text Display").AddComponent<TextDisplay>();
        _display.Block = this;

        _display.mode = 2;
        _display.text = Text;
        _display.cost = Cost;
        _display.takeControl = TakeControl;
    }

    protected override void Trigger(string trigger)
    {
        _display.Display();
    }
}

public class ThoughtBlock : ScriptBlock
{
    protected override IEnumerable<string> Inputs => ["Display"];
    
    protected override string Name => "Thought Display";

    public string Text = "";

    public override void Reset()
    {
        Text = "";
    }
    
    private static PlayMakerFSM DreamDialogue => field ??= GameCameras.instance.hudCamera.transform
        .Find("DialogueManager").Find("Dream Msg").gameObject.LocateMyFSM("Display");

    protected override void Trigger(string trigger)
    {
        var dd = DreamDialogue;
        dd.FsmVariables.FindFsmString("Convo Title").Value = Text;
        dd.FsmVariables.FindFsmString("Sheet").Value = "ArchitectMod";
        dd.SendEvent("DISPLAY DREAM MSG ALT");
    }
}
