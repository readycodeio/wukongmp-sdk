using System;
using GSDispLib;
using HarmonyLib;
using WukongMp.Api.Configuration;

namespace WukongMp.Api.Patches;

[HarmonyPatch(typeof(BUS_DispLibUnitMaterialsManageComp), "Internal_AddMaterialInfoForNewPrimComp")]
[HarmonyPatchCategory(Constants.ConnectedPatches)]
public static class PatchRandomCrashOnMeshAssignedOnTamerReset
{
    public static Exception? Finalizer()
    {
        // suppress System.ArgumentException: An item with the same key has already been added. Key: 274753
        return null;
    }
}