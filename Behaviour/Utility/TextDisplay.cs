using System.Collections;
using Architect.Events.Blocks;
using Architect.Utils;
using Satchel;
using UnityEngine;

namespace Architect.Behaviour.Utility;

public class TextDisplay : MonoBehaviour, IDisplayable
{
    public ScriptBlock Block;
    public int mode;
    public int cost;

    public bool takeControl = true;
    
    private static TextDisplay _current;
    private static TextDisplay _prev;
    
    public string text = "";
    
    private static PlayMakerFSM NormalManager => 
        field ??= GameObject.Find("_GameCameras/HudCamera/DialogueManager").LocateMyFSM("Box Open");
    private static PlayMakerFSM DreamManager => 
        field ??= GameObject.Find("_GameCameras/HudCamera/DialogueManager").LocateMyFSM("Box Open Dream");
    private static PlayMakerFSM YnManager => 
        field ??= GameObject.Find("_GameCameras/HudCamera/DialogueManager").LocateMyFSM("Box Open YN");
    private DialogueBox DialogueBox => 
        field ??= GameObject.Find("_GameCameras/HudCamera/DialogueManager/Text").GetComponent<DialogueBox>();
    
    private DialogueBox YnDialogueBox => 
        field ??= GameObject.Find("_GameCameras/HudCamera/DialogueManager/Text YN").GetComponent<DialogueBox>();

    public static void Init()
    {
        On.PlayMakerFSM.Awake += (orig, self) =>
        {
            orig(self);
            if (self.FsmName != "Dialogue Page Control") return;

            self.InsertCustomAction("End Conversation", () =>
            {
                if (!_current) return;
                _current.Stop();
                _prev = _current;
                _current = null;
            }, 0);

            if (self.name != "Text YN") return;

            self.InsertCustomAction("Activate Geo Text?", fsm =>
            {
                if (_current) fsm.FsmVariables.FindFsmInt("Toll Cost").Value = _current.cost;
            }, 0);

            self.InsertCustomAction("Yes", () =>
            {
                if (!_prev) return;
                _prev.gameObject.BroadcastEvent("Yes");
                _prev.Block.Event("Yes");
                _prev = null;
            }, 0);

            self.InsertCustomAction("No", () =>
            {
                if (!_prev) return;
                _prev.gameObject.BroadcastEvent("No");
                _prev.Block.Event("No");
                _prev = null;
            }, 0);
        };
    }

    public void Display()
    {
        StartCoroutine(DoDisplay());
    }

    public void Stop()
    {
        HeroController.instance.RegainControl();
        
        switch (mode)
        {
            case 0:
                DreamManager.SendEvent("BOX DOWN DREAM");
                break;
            case 1:
                NormalManager.SendEvent("BOX DOWN");
                break;
            case 2:
                YnManager.SendEvent("BOX DOWN YN");
                break;
        }
        
        Block.Event("OnClose");
        gameObject.BroadcastEvent("OnClose");
    }

    private IEnumerator DoDisplay()
    {
        yield return HeroController.instance.FreeControl();

        switch (mode)
        {
            case 0:
                DreamManager.SendEvent("BOX UP DREAM");
                break;
            case 1:
                NormalManager.SendEvent("BOX UP");
                break;
            case 2:
                YnManager.SendEvent("BOX UP YN");
                break;
        }

        _current = this;
        if (takeControl) HeroController.instance.RelinquishControl();
        (mode == 2 ? YnDialogueBox : DialogueBox).StartConversation(text, "ArchitectMod");
    }
}