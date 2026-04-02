using System;
using b1;
using B1UI.GSUI;
using CommB1;
using HarmonyLib;
using Microsoft.Extensions.Logging;
using WukongMp.Api.Configuration;

namespace WukongMp.Api.Patches;

[HarmonyPatch(typeof(BGW_GameArchiveMgr), nameof(BGW_GameArchiveMgr.MarkSaveSetting))]
[HarmonyPatchCategory(PatchCategory.Global)]
internal class PatchArchiveReadWriterAppendArchive2
{
    public static bool Prefix(UISettingArchiveData UISettingArchiveData)
    {
        return false;
    }
}

[HarmonyPatch(typeof(GSB1UIUtil), nameof(GSB1UIUtil.CheckArchiveFull))]
[HarmonyPatchCategory(PatchCategory.Connected)]
internal class PatchCheckArchiveFull
{
    public static bool Prefix(ref bool __result)
    {
        __result = false;
        return false;
    }
}

[HarmonyPatch(typeof(BGW_GameArchiveMgr), "TickSaveArchiveSnapshot")]
[HarmonyPatchCategory(PatchCategory.Connected)]
internal class PatchTickSaveArchiveSnapshot
{
    public static Exception? Finalizer(Exception? __exception)
    {
        if (__exception != null)
        {
            DI.Instance.Logger.LogError(__exception, "Suppressed crash in TickSaveArchiveSnapshot");
        }

        return null;
    }
}