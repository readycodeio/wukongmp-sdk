using System;
using b1;
using B1UI;
using CsB1;
using GSDispLib;
using GSE.GSUI;
using HarmonyLib;
using Microsoft.Extensions.Logging;
using ResB1;
using UnrealEngine.Engine;
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

[HarmonyPatch(typeof(BUS_OSSCollectComp), "OnOSSCollectBattleData_AiUnit")]
[HarmonyPatchCategory(Constants.ConnectedPatches)]
public static class PatchRandomCrashOnOSSCollectBattleData_AiUnit
{
    public static Exception? Finalizer(Exception? __exception)
    {
        if (__exception != null)
        {
            DI.Instance.Logger.LogError(__exception, "Suppressed crash in BUS_OSSCollectComp.OnOSSCollectBattleData_AiUnit");
        }

        // suppress System.NullReferenceException
        return null;
    }
}

[HarmonyPatch(typeof(GSG), "OnTopPageChange")]
[HarmonyPatchCategory(Constants.ConnectedPatches)]
public static class PatchGSGOnTopPageChange
{
    public static bool Prefix(GSUIPage? NewValue)
    {
        return NewValue != null;
    }
}