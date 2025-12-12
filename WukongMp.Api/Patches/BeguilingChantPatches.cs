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
        return DI.Instance.AreaState.IsMasterClient;
    }
}

[HarmonyPatch(typeof(BUS_IntervalTriggerImpl), "SetIsActive")]
[HarmonyPatchCategory(Constants.ConnectedPatches)]
public static class PatchSetIsActive
{
    public static void Postfix(bool IsActive)
    {
        if (DI.Instance.AreaState.IsMasterClient)
        {
            DI.Instance.Rpc.SendBeguilingChant(IsActive);
        }
    }
}