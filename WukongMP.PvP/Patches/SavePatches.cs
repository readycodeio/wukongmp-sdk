using ArchiveB1;
using b1;
using B1UI.GSUI;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using PreludeLib.Attributes;
using UnrealEngine.Runtime;
using WukongMp.Api.Configuration;
using WukongMp.Api.WukongUtils;
using WukongMp.Api;

namespace WukongMp.PvP.Patches; 

/// Replace Steam save folder with ours.
[HarmonyPatch(typeof(GSWindowsPlatformSaveGame), nameof(GSWindowsPlatformSaveGame.GetFileFullName))]
[HarmonyPatchCategory(Constants.GlobalPatches)]
public class PatchWindowsSaveGame
{
    public static bool Prefix(ref string __result, string SlotName, string UserId)
    {
        if (!PvpDI.Instance.SaveManager.ShouldRedirectSaveFiles)
            return true;

        if (!SlotName.StartsWith("ArchiveSaveFile"))
            return true;

        __result = GameSaveUtils.GetSaveFileFullName(SlotName);
        return false;
    }
}

/// When "Load game" (save selector list ) is selected in main menu.
[HarmonyPatch]
[HarmonyPatchCategory(Constants.GlobalPatches)]
public class PatchUIArchives
{
    [HarmonyTargetMethodHint("B1UI.GSUI.UIArchives", "LoadArchive")]
    private static MethodBase TargetMethod()
    {
        return AccessTools.Method("B1UI.GSUI.UIArchives:LoadArchive");
    }

    public static void Prefix()
    {
        PvpDI.Instance.SaveManager.OnSavedGameLoad();
    }
}

/// Load our custom save on new game.
[HarmonyPatch(typeof(GSB1UIUtil), nameof(GSB1UIUtil.StartNewGame))]
[HarmonyPatchCategory(Constants.GlobalPatches)]
public class PatchStartNewGame
{
    public static bool Prefix(UObject WorldContext)
    {
        PvpDI.Instance.SaveManager.OnNewGameLoad(WorldContext);
        return false;
    }
}

/// Read the world save and character save data, clear spells and set the birth point.
[HarmonyPatch(typeof(BGW_GameArchiveMgr), nameof(BGW_GameArchiveMgr.LoadArchive))]
[HarmonyPatchCategory(Constants.GlobalPatches)]
public class PatchGameArchive
{
    public static void Postfix(BGW_GameArchiveMgr __instance, ref ReadArchiveResult __result, int ArchiveId, ref FUStBEDArchivesData? OutArchiveData)
    {
        if (__result != ReadArchiveResult.Success)
        {
            Logging.LogError("Original readArchiveData Failed, Result: {Result}", __result);
            return;
        }

        if (OutArchiveData == null)
        {
            Logging.LogError("Original OutArchiveData is null");
            return;
        }

        DI.Instance.EventBus.TryInvokeBeginLoadGameplayLevel();

        PvpDI.Instance.SaveManager.OnLoadArchive(__instance, ref __result, ArchiveId, ref OutArchiveData);
    }
}

/// Disable game saves while multiplayer is enabled
[HarmonyPatch(typeof(BGW_ArchiveReadWriteWorker), "CheckSaveTask")]
[HarmonyPatchCategory(Constants.GlobalPatches)]
public class PatchArchiveReadWriter
{
    public static bool DisableArchiveSave;

    public static bool Prefix(Dictionary<string, ArchiveAsyncRequest> ___PendingRequests)
    {
        if (DisableArchiveSave)
        {
            return false;
        }

        if (___PendingRequests.Count == 0)
        {
            DisableArchiveSave = true;
            return false;
        }

        return true;
    }
}

// Disable adding save game requests
[HarmonyPatch(typeof(BGW_ArchiveReadWriteWorker), nameof(BGW_ArchiveReadWriteWorker.AppendArchiveSaveRequest), typeof(int), typeof(GSArchiveFileContainer), typeof(List<ArchiveSaveRequestOne>))]
[HarmonyPatchCategory(Constants.GlobalPatches)]
public class PatchArchiveReadWriteWorkerAppendArchiveSaveRequest
{
    public static bool Prefix(int ArchiveId, GSArchiveFileContainer ArchiveWriteContainer, List<ArchiveSaveRequestOne> saveArchiveRequests)
    {
        return false;
    }
}
