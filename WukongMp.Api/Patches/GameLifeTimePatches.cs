using b1;
using B1UI;
using HarmonyLib;
using System;
using System.Reflection;
using UnrealEngine.Engine;
using WukongMp.Api.Configuration;
using WukongMp.Api.WukongUtils;

namespace WukongMp.Api.Patches;

[HarmonyPatch(typeof(BGW_GameLifeTimeMgr), "OnPostLoadMapWithWorld")]
[HarmonyPatchCategory(Constants.GlobalPatches)]
public class PatchOnPostLoadMapWithWorld
{
    public static void Postfix()
    {
        var world = GameUtils.GetWorld();
        if (world == null)
            return;
        Logging.LogInformation("New level loaded: {LevelName}", world.GetCurrentLevelName());
    }
}

[HarmonyPatch(typeof(GSG), nameof(GSG.OnEnterLevel))]
[HarmonyPatchCategory(Constants.GlobalPatches)]
public class PatchOnEnterLevel
{
    public static void Postfix()
    {
        var world = GameUtils.GetWorld();
        if (world == null)
            return;
        Logging.LogInformation("On enter level: {LevelName}", world.GetCurrentLevelName());
    }
}

[HarmonyPatch(typeof(GSG), nameof(GSG.OnLevelExit))]
[HarmonyPatchCategory(Constants.GlobalPatches)]
public class PatchOnLevelExit
{
    public static void Postfix()
    {
        var world = GameUtils.GetWorld();
        if (world == null)
            return;
        Logging.LogInformation("On exit level: {LevelName}", world.GetCurrentLevelName());
    }
}

[HarmonyPatch]
[HarmonyPatchCategory(Constants.GlobalPatches)]
public class PatchOnLateBeginPlay
{
    private static MethodBase TargetMethod()
    {
        return AccessTools.Method("b1.BUS_MiscInitComp:LateBeginPlay");
    }

    public static void Postfix()
    {
        Logging.LogInformation("Late begin play");
        DI.Instance.EventBus.TryInvokeBeginPlayGameplayLevel();
    }
}

[HarmonyPatch(typeof(BPS_LiftTimeSystem), nameof(BPS_LiftTimeSystem.OnEndPlay))]
[HarmonyPatchCategory(Constants.GlobalPatches)]
public class PatchOnEndPlay
{
    public static void Postfix()
    {
        Logging.LogInformation("End play");
        DI.Instance.EventBus.TryInvokeEndPlayGameplayLevel();
    }
}

[HarmonyPatch]
[HarmonyPatchCategory(Constants.GlobalPatches)]
public class PatchOnLoadingScreenClose
{
    private static MethodBase TargetMethod()
    {
        var innerType = AccessTools.Inner(typeof(BGW_LoadingTipsMgr), "FLoadingScreenTimeTracker");
        return AccessTools.Method(innerType, "OnLoadingScreenClose");
    }

    public static void Postfix()
    {
        Logging.LogInformation("Loading screen close");
        DI.Instance.EventBus.InvokeLoadingScreenClose();
    }
}
