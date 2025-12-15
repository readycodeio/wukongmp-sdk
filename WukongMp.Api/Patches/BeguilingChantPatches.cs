using b1;
using HarmonyLib;
using WukongMp.Api.Configuration;

namespace WukongMp.Api.Patches;

[HarmonyPatch(typeof(BUS_IntervalTriggerImpl.IntervalTriggerEnableState), "OnTickAction")]
[HarmonyPatchCategory(Constants.ConnectedPatches)]
public static class PatchEventActiveTick
{
    public static bool Prefix()
    {
        return false;
    }
}

[HarmonyPatch(typeof(BUS_StateMachineCompBase), "JumpToState")]
[HarmonyPatchCategory(Constants.ConnectedPatches)]
public static class PatchJumpToState
{
    public static void Postfix(BUS_StateMachineCompBase.GSStateBase TargetState)
    {
        var mainChar = DI.Instance.PlayerState.LocalMainCharacter;
        if (!mainChar.HasValue)
            return;

        ref var state = ref mainChar.Value.GetState();
        state.BeguilingChantEligible = TargetState switch
        {
            BUS_IntervalTriggerImpl.IntervalTriggerEnableState => true,
            BUS_IntervalTriggerImpl.IntervalTriggerDisableState => false,
            _ => state.BeguilingChantEligible
        };
    }
}

[HarmonyPatch(typeof(BUS_IntervalTriggerImpl.IntervalTriggerEnableState), "OnEnterAction")]
[HarmonyPatchCategory(Constants.ConnectedPatches)]
public static class PatchIntervalTriggerEnableStateOnEnterAction
{
    public static bool Prefix(BUS_StateMachineCompBase InOwner)
    {
        return false;
    }
}