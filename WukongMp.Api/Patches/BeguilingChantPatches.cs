using b1;
using HarmonyLib;
using WukongMp.Api.Configuration;

namespace WukongMp.Api.Patches;

[HarmonyPatch(typeof(BUS_IntervalTriggerImpl), "OnTickAction")]
[HarmonyPatchCategory(Constants.ConnectedPatches)]
public class PatchEventActiveTick
{
    public static bool Prefix()
    {
        return DI.Instance.AreaState.IsMasterClient;
    }
}

[HarmonyPatch(typeof(BUS_IntervalTriggerImpl), "SetIsActive")]
[HarmonyPatchCategory(Constants.ConnectedPatches)]
public class PatchOnIntervalEventBegin
{
    public static void Postfix(bool IsActive)
    {
        if (DI.Instance.AreaState.IsMasterClient)
        {
            DI.Instance.Rpc.SendBeguilingChant(IsActive);
        }
    }
}