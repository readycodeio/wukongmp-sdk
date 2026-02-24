using ArchiveB1;
using b1;
using B1UI.GSUI;
using CommB1;
using HarmonyLib;
using UnrealEngine.Runtime;
using WukongMp.Api;
using WukongMp.Api.Configuration;
using WukongMp.Sdk.Api;

namespace WukongMp.Coop.Patches;

/// Replace Steam save folder with ours.
[HarmonyPatch(typeof(GSWindowsPlatformSaveGame), nameof(GSWindowsPlatformSaveGame.GetFileFullName))]
[HarmonyPatchCategory(Constants.GlobalPatches)]
public class PatchWindowsSaveGame
{
    public static bool Prefix(ref string __result, string SlotName)
    {
        if (!Mod.Instance.SaveManager.ShouldRedirectSaveFiles)
            return true;

        if (!SlotName.StartsWith("ArchiveSaveFile"))
            return true;

        __result = WukongFileApi.GetSaveFileFullName(Mod.Instance, SlotName);
        return false;
    }
}

/// Load our custom save on new game.
[HarmonyPatch(typeof(GSB1UIUtil), nameof(GSB1UIUtil.StartNewGame))]
[HarmonyPatchCategory(Constants.GlobalPatches)]
public class PatchStartNewGame
{
    public static bool Prefix(UObject WorldContext)
    {
        Mod.Instance.SaveManager.OnNewGameLoad(WorldContext);
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

        Mod.Instance.SaveManager.OnLoadArchive(__instance, ref __result, ArchiveId, ref OutArchiveData);
    }
}

[HarmonyPatch(typeof(GSWindowsPlatformSaveGame), nameof(GSWindowsPlatformSaveGame.SaveDataToSlot))]
[HarmonyPatchCategory(Constants.GlobalPatches)]
public class PatchGSWindowsPlatformSaveGame
{
    private static bool Prefix(List<byte> InSaveData, string SlotName, string UserId, ref bool __result)
    {
        if (!Mod.Instance.ClientApi.InRoom)
            return true;

        if (!SlotName.StartsWith("ArchiveSaveFile"))
            return true; // only handle game save, not settings etc.

        Mod.Instance.SaveManager.OnSaveData(InSaveData, SlotName);

        __result = true;
        return false;
    }
}

[HarmonyPatch(typeof(BGW_GameArchiveMgr), nameof(BGW_GameArchiveMgr.GetLatestArchive))]
[HarmonyPatchCategory(Constants.GlobalPatches)]
public class PatchGetLatestArchive
{
    public static bool Prefix(BGW_GameArchiveMgr __instance, ref ArchiveSummaryData? __result)
    {
        ArchiveSummaryData? archiveSummaryData = null;
        List<ArchiveSummaryData> archiveInfoList = (List<ArchiveSummaryData>)AccessTools.Method(typeof(BGW_GameArchiveMgr), "_GetArchiveInfoList").Invoke(__instance, []);
        for (int index = 0; index < archiveInfoList.Count; ++index)
        {
            if (archiveSummaryData == null || archiveInfoList[index].ArchiveId > archiveSummaryData.ArchiveId)
                archiveSummaryData = archiveInfoList[index];
        }

        __result = archiveSummaryData?.Clone();
        return false;
    }
}