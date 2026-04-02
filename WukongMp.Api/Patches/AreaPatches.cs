using b1;
using HarmonyLib;
using WukongMp.Api.Configuration;

namespace WukongMp.Api.Patches;

[HarmonyPatch(typeof(BUS_AreaOverlapComp), "EnableOverlap")]
[HarmonyPatchCategory(PatchCategory.Connected)]
internal class PatchEnableOverlap
{
    public static bool Prefix(BUS_AreaOverlapComp __instance)
    {
        if (!DI.Instance.AreaState.InRoom)
            return true;

        var owner = __instance.GetOwner();
        var guid = BGU_DataUtil.GetActorGuid(owner);
        if(DI.Instance.GameplayConfiguration.IsAreaOverlapDisabled(guid))
        {
            Logging.LogDebug("Preventing enabling area overlap for actor {Actor}", guid);
            return false;
        }
        return true;
    }
}

[HarmonyPatch(typeof(BUS_AreaOverlapComp), "OnActorEnter_EnterArea")]
[HarmonyPatchCategory(PatchCategory.Connected)]
internal class PatchOnActorEnter_EnterArea
{
    public static bool Prefix(BUS_AreaOverlapComp __instance)
    {
        if (!DI.Instance.AreaState.InRoom)
            return true;

        var owner = __instance.GetOwner();
        var guid = BGU_DataUtil.GetActorGuid(owner);
        if (DI.Instance.GameplayConfiguration.IsAreaOverlapDisabled(guid))
        {
            Logging.LogDebug("Preventing OnActorEnter_EnterArea for actor {Actor}", guid);
            return false;
        }
        return true;
    }
}
