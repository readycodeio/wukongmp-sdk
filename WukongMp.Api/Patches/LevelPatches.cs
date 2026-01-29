using System;
using b1;
using B1UI;
using CommB1;
using HarmonyLib;
using Microsoft.Extensions.Logging;
using UnrealEngine.Engine;
using UnrealEngine.Runtime;
using WukongMp.Api.Configuration;
using WukongMp.Api.WukongUtils;

namespace WukongMp.Api.Patches;

[HarmonyPatch(typeof(UGameplayStatics), "OpenLevel")]
[HarmonyPatchCategory(Constants.GlobalPatches)]
public static class PatchOpenLevel
{
    public static void Postfix(UObject WorldContextObject, FName LevelName, bool bAbsolute = true)
    {
        Logging.LogDebug("OpenLevel called with LevelName {LevelName}, bAbsolute {bAbsolute}", LevelName.ToString(), bAbsolute);
    }
}

[HarmonyPatch(typeof(BPS_PlayerTeleportSystem), "OnPlayerTeleportTo")]
[HarmonyPatchCategory(Constants.GlobalPatches)]
public static class PatchOnPlayerTeleportTo
{
    public static void Postfix(ETeleportTypeV2 TeleportType, ValueType? UserData, EPlayerTeleportReason Reason)
    {
        Logging.LogDebug("OnPlayerTeleportTo called with TeleportType {TeleportType}, UserData {UserData}, Reason {Reason}", TeleportType, UserData?.ToString() ?? "Empty", Reason);

        if (Reason is EPlayerTeleportReason.Rebirth or EPlayerTeleportReason.RebirthPoint)
        {
            if (DI.Instance.PlayerState.LocalMainCharacter.HasValue)
                PlayerUtils.DisableSpectator(DI.Instance.PlayerState.LocalMainCharacter.Value);
        }
    }
}

[HarmonyPatch(typeof(TaskNodeInstance_ChapterClear), "PlayChapterMovie")]
[HarmonyPatchCategory(Constants.ConnectedPatches)]
public static class PatchPlayChapterMovie
{
    public static bool Prefix(TaskNodeInstance_ChapterClear __instance)
    {
        var getCustomData = AccessTools.PropertyGetter(typeof(TaskNodeInstance_ChapterClear), "CustomData");
        var customData = (TaskCustom_ChapterClear)getCustomData.Invoke(__instance, null);

        if (customData.ChapterId != GSG.GamePlayer.RoleData.RoleCs.Chapter.CurChapter)
        {
            DI.Instance.Logger.LogWarning("Corrupted save detected: TaskNodeInstance_ChapterClear with ChapterId {ChapterId} does not match player's current chapter {CurrentChapter}. Skipping task node.", customData.ChapterId, GSG.GamePlayer.RoleData.RoleCs.Chapter.CurChapter);

            var triggerFirstOutput = AccessTools.Method(typeof(TaskNodeInstance_ChapterClear), "TriggerFirstOutput", [typeof(bool)]);
            triggerFirstOutput.Invoke(__instance, [true]);
            return false;
        }

        return true;
    }
}