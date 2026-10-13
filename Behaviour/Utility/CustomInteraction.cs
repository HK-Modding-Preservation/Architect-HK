using System.Collections.Generic;
using Architect.Content.Preloads;
using Satchel;
using UnityEngine;

namespace Architect.Behaviour.Utility;

public class CustomInteraction : MonoBehaviour
{
    private static GameObject _arrowPromptNew;
    private static readonly List<CustomInteraction> Interactable = [];

    public float xOffset;
    public float yOffset;
    public string prompt;
    public bool hideOnInteract;
    private PromptMarker _prompt;

    public TransitionPoint door;

    public static readonly string[] Labels =
    [
        "Enter", "Inspect", "Listen",
        "Rest", "Shop", "Travel",
        "Challenge", "Exit", "Descent",
        "Sit", "Trade", "Accept",
        "Watch", "Ascend"
    ];

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.gameObject.GetComponent<HeroController>()) return;
        GoUp();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.gameObject.GetComponent<HeroController>()) return;
        GoDown();
    }

    public static void Init()
    {
        PreloadManager.RegisterPreload(new BasicPreload("Crossroads_47", "RestBench", o =>
        {
            _arrowPromptNew = o.LocateMyFSM("Bench Control")
                .GetAction<ShowPromptMarker>("In Range", 0).prefab.Value;
        }));
        
        ModHooks.HeroUpdateHook += () =>
        {
            if (HeroController.instance.controlReqlinquished) return;
            if (!InputHandler.Instance.inputActions.up.WasPressed) return;
            var willRemove = new List<CustomInteraction>();
            var willDown = new List<CustomInteraction>();
            foreach (var interaction in Interactable)
            {
                if (!interaction)
                {
                    willRemove.Add(interaction);
                    continue;
                }

                if (HeroController.instance.transform.position.x > interaction.transform.position.x)
                    HeroController.instance.FaceLeft();
                else HeroController.instance.FaceRight();

                interaction.gameObject.BroadcastEvent("OnInteract");
                if (interaction.door)
                {
                    interaction.door.isADoor = false;
                    interaction.door.OnTriggerEnter2D(HeroController.instance.col2d);
                    interaction.door.isADoor = true;
                }
                if (interaction.hideOnInteract) willDown.Add(interaction);
            }

            foreach (var interaction in willRemove) Interactable.Remove(interaction);
            foreach (var interaction in willDown) interaction.GoDown();
        };
    }

    public void GoDown()
    {
        _prompt.Hide();
        Interactable.Remove(this);
    }

    public void GoUp()
    {
        var obj = _arrowPromptNew.Spawn();
        obj.transform.position = transform.position + new Vector3(xOffset, yOffset + 2);

        _prompt = obj.GetComponent<PromptMarker>();

        _prompt.SetLabel(prompt);
        _prompt.SetOwner(gameObject);
        _prompt.Show();

        Interactable.Add(this);
    }
}