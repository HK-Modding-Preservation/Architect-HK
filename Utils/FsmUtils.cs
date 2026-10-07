using System;
using HutongGames.PlayMaker;
using Satchel;

namespace Architect.Utils;

public static class FsmUtils
{
    public static FsmState AddState(this PlayMakerFSM fsm, string state)
    {
        return FsmUtil.AddState(fsm, state);
    }
    
    public static FsmState GetState(this PlayMakerFSM fsm, string state)
    {
        return fsm.Fsm.GetState(state);
    }

    public static T GetAction<T>(this FsmState state, int index) where T : FsmStateAction
    {
        return FsmUtil.GetAction<T>(state, index);
    }
    
    public static T[] GetActionsOfType<T>(this FsmState state) where T : FsmStateAction
    {
        return state.GetActions<T>();
    }

    public static void ChangeTransition(this FsmState state, string eve, string toState)
    {
        FsmUtil.ChangeTransition(state, eve, toState);
    }
    
    public static void DisableAction(this FsmState state, int index)
    {
        state.Actions[index].Enabled = false;
    }
    
    public static void DisableActions(this FsmState state, params int[] indexes)
    {
        foreach (var index in indexes) state.Actions[index].Enabled = false;
    }

    public static void AddEvent(this FsmState state, string eve, int index = 0)
    {
        state.AddAction(() => state.fsm.FsmComponent.SendEvent(eve), index);
    }

    public static void AddAction(this FsmState state, Action action, int index = -1)
    {
        if (index == -1) state.AddCustomAction(action);
        else state.InsertCustomAction(action, index);
    }
    
    public static void AddAction(this FsmState state, FsmStateAction customAction, int index = -1)
    {
        if (index == -1) FsmUtil.AddAction(state, customAction);
        else state.InsertAction(customAction, index);
    }
    
    public static void AddTransition(this FsmState state, string onEventName, string toStateName)
    {
        FsmUtil.AddTransition(state, onEventName, toStateName);
    }

    public class EveryFrameAction(Action method) : FsmStateAction
    {
        public override void OnEnter() => method();
        public override void OnUpdate() => method();
    }
}