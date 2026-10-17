using System.Collections;
using System.Collections.Generic;
using HutongGames.PlayMaker.Actions;
using UnityEngine;

namespace Architect.Events.Blocks.Outputs;

public class TravelBlock : CollectionBlock<TravelBlock.TravelItemBlock>
{
    protected override string Name => "Travel UI";
    
    protected override IEnumerable<string> Inputs => ["Open"];
    protected override IEnumerable<string> Outputs => ["OnDismiss"];

    protected override string ChildName => "Travel Item";
    protected override bool NeedsGap => true;

    private static GameObject Prefab
    {
        get
        {
            if (!field) field = ((CreateObject)GameCameras.instance.hudCamera.transform.Find("Menus")
                .gameObject.LocateMyFSM("Open Stag").GetState("Spawn Stag Map").Actions[0]).gameObject.Value;
            return field;
        }
    }

    protected override void Trigger(string trigger)
    {
        ArchitectPlugin.Instance.StartCoroutine(Coroutine());
    }
    
    private IEnumerator Coroutine()
    {
        yield return HeroController.instance.FreeControl(_ => !GameManager.instance.isPaused);
        HeroController.instance.RelinquishControl();

        var stagMenu = Object.Instantiate(Prefab);
        stagMenu.SetActive(true);
    }
    
    public class TravelItemBlock : ChildBlock
    {
        protected override IEnumerable<(string, string)> InputVars => [("Unlocked", "Boolean")];
        protected override IEnumerable<string> Outputs => ["OnChoose"];
    }
}