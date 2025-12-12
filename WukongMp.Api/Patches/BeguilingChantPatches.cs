using b1;
using HarmonyLib;
using WukongMp.Api.Configuration;

namespace WukongMp.Api.Patches;

[HarmonyPatch(typeof(BUS_IntervalTriggerLogicComp), "EventActiveTick")]
[HarmonyPatchCategory(Constants.ConnectedPatches)]
public class PatchEventActiveTick
{
    public static bool Prefix()
    {
        return DI.Instance.AreaState.IsMasterClient;
    }
}

[HarmonyPatch(typeof(BUS_IntervalTriggerLogicComp), "OnIntervalEventBegin")]
[HarmonyPatchCategory(Constants.ConnectedPatches)]
public class PatchOnIntervalEventBegin
{
    public static void Postfix()
    {
        if (DI.Instance.AreaState.IsMasterClient)
        {
            DI.Instance.Rpc.SendBeginBeguilingChant();
        }
    }
}

[HarmonyPatch(typeof(BUS_IntervalTriggerLogicComp), "OnIntervalEventEnd")]
[HarmonyPatchCategory(Constants.ConnectedPatches)]
public class PatchOnIntervalEventEnd
{
    public static void Postfix()
    {
        if (DI.Instance.AreaState.IsMasterClient)
        {
            DI.Instance.Rpc.SendEndBeguilingChant();
        }
    }
}