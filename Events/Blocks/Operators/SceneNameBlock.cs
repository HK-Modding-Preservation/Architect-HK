using System.Collections.Generic;
using UnityEngine;

namespace Architect.Events.Blocks.Operators;

public class SceneNameBlock : ScriptBlock
{
    protected override string Name => "Scene Name";

    protected override IEnumerable<(string, string)> OutputVars =>
    [
        ("Name", "Text")
    ];

    public override object GetValue(string id)
    {
        return GameManager.instance.sceneName;
    }
}

public class GameplayBlock : ScriptBlock
{
    protected override string Name => "Gameplay Control";

    protected override IEnumerable<string> Inputs => ["SetGravity", "Save", "SaveQuit", "CloseGame"];

    protected override IEnumerable<(string, string)> InputVars =>
    [
        ("GravX", "Number"),
        ("GravY", "Number")
    ];
    
    protected override IEnumerable<(string, string)> OutputVars =>
    [
        ("Scene", "Text"),
        ("Paused", "Boolean")
    ];

    protected override void Trigger(string trigger)
    {
        switch (trigger)
        {
            case "SetGravity":
                Physics2D.gravity = new Vector2(GetVariable<float>("GravX"), GetVariable<float>("GravY", -60));
                break;
            case "Save":
                GameManager.instance.SaveGame(_ => Event("OnSave"));
                break;
            case "SaveQuit":
                GameManager.instance.StartCoroutine(GameManager.instance.ReturnToMainMenu(GameManager.ReturnToMainMenuSaveModes.SaveAndContinueOnFail));
                break;
            case "CloseGame":
                Application.Quit();
                break;
        }
    }

    public override object GetValue(string id)
    {
        if (id == "Paused") return GameManager.instance.isPaused;
        return GameManager.instance.sceneName;
    }
}
