using b1;
using HarmonyLib;
using System;
using UnrealEngine.Engine;
using UnrealEngine.Runtime;
using WukongMp.Api.Configuration;
using WukongMp.Api.Old;

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
        Logging.LogDebug("OnPlayerTeleportTo called with TeleportType {TeleportType}, UserData {UserData}, Reason {Reason}",
            TeleportType, UserData?.ToString() ?? "Empty", Reason);
    }
}
