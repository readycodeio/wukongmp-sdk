using System;
using System.Collections.Generic;
using b1;
using b1.BGW;
using B1UI;
using GSDispLib;
using GSE.GSUI;
using HarmonyLib;
using Microsoft.Extensions.Logging;
using WukongMp.Api.Configuration;

namespace WukongMp.Api.Patches;

[HarmonyPatch(typeof(BUS_DispLibUnitMaterialsManageComp), "Internal_AddMaterialInfoForNewPrimComp")]
[HarmonyPatchCategory(PatchCategory.Connected)]
internal static class PatchRandomCrashOnMeshAssignedOnTamerReset
{
    public static Exception? Finalizer()
    {
        // suppress System.ArgumentException: An item with the same key has already been added. Key: 274753
        return null;
    }
}

[HarmonyPatch(typeof(BUS_OSSCollectComp), "OnOSSCollectBattleData_AiUnit")]
[HarmonyPatchCategory(PatchCategory.Connected)]
internal static class PatchRandomCrashOnOSSCollectBattleData_AiUnit
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

[HarmonyPatch(typeof(BUS_DeadZoneLogicComp), "PlayerCliffFallRollBack")]
[HarmonyPatchCategory(PatchCategory.Connected)]
internal static class PatchRandomCrashOnPlayerCliffFallRollBack
{
    public static Exception? Finalizer(Exception? __exception)
    {
        if (__exception != null)
        {
            DI.Instance.Logger.LogError(__exception, "Suppressed crash in BUS_DeadZoneLogicComp.PlayerCliffFallRollBack");
        }

        // suppress System.NullReferenceException
        return null;
    }
}

[HarmonyPatch(typeof(GSG), "OnTopPageChange")]
[HarmonyPatchCategory(PatchCategory.Connected)]
internal static class PatchGSGOnTopPageChange
{
    public static bool Prefix(GSUIPage? NewValue)
    {
        return NewValue != null;
    }
}

[HarmonyPatch(typeof(PreloadAssetHelper), "Change2ValidPathList")]
[HarmonyPatchCategory(PatchCategory.Global)]
internal static class PatchChange2ValidPathListSeparateDictionary
{
    public static bool Prefix(Dictionary<string, EAssetPriority> PathInfoDic, ref Dictionary<string, EAssetPriority> __result)
    {
        __result = [];
        foreach (var kvp in PathInfoDic)
        {
            string objectPath = BGW_PreloadAssetMgr.ExportTextPathToObjectPath(kvp.Key);
            if (PreloadAssetHelper.IsPathValid(objectPath, false) && !__result.ContainsKey(objectPath))
                __result.Add(objectPath, kvp.Value);
        }

        return false;
    }
}