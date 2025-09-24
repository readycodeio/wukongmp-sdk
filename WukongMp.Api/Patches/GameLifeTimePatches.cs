using b1;
using B1UI;
using HarmonyLib;
using System.Reflection;
using PreludeLib.Attributes;
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
        DI.Instance.EventBus.InvokeOnLevelLoaded();
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
        DI.Instance.EventBus.InvokeOnExitLevel();
    }
}

[HarmonyPatch]
[HarmonyPatchCategory(Constants.GlobalPatches)]
public class PatchOnLateBeginPlay
{
    [HarmonyTargetMethodHint("b1.BUS_MiscInitComp", "LateBeginPlay")]
    private static MethodBase TargetMethod()
    {
        return AccessTools.Method("b1.BUS_MiscInitComp:LateBeginPlay");
    }

    public static void Postfix(UActorCompBaseCS __instance) // This is called multiple times since each BGUCharacterCS has BUS_MiscInitComp
    {
        var owner = __instance.GetOwner();
        if (owner == GameUtils.GetControlledPawn())
        {
            Logging.LogInformation("Local player late begin play");
        }
    }
}

[HarmonyPatch(typeof(BUS_DeadComp), nameof(BUS_DeadComp.OnEndPlay))]
[HarmonyPatchCategory(Constants.GlobalPatches)]
public class PatchOnPlayerEndPlay
{
    public static void Postfix(BUS_DeadComp __instance) // This is called multiple times since each BGUCharacterCS has BUS_DeadComp
    {
        var owner = __instance.GetOwner();
        if (owner == GameUtils.GetControlledPawn())
        {
            Logging.LogInformation("Local player end play");
        }
    }
}

[HarmonyPatch(typeof(BPS_LiftTimeSystem), nameof(BPS_LiftTimeSystem.OnBeginPlay))]
[HarmonyPatchCategory(Constants.GlobalPatches)]
public class PatchOnPlayerControllerBeginPlay
{
    public static void Postfix(BPS_LiftTimeSystem __instance) // This is called only for player controller where BPS_LiftTimeSystem is registered
    {
        Logging.LogInformation("Player controller begin play");
        DI.Instance.EventBus.TryInvokeBeginPlayGameplayLevel();
    }
}

[HarmonyPatch(typeof(BPS_LiftTimeSystem), nameof(BPS_LiftTimeSystem.OnEndPlay))]
[HarmonyPatchCategory(Constants.GlobalPatches)]
public class PatchOnPlayerControllerEndPlay
{
    public static void Postfix(BPS_LiftTimeSystem __instance) // This is called only for player controller where BPS_LiftTimeSystem is registered
    {
        Logging.LogInformation("Player controller end play");
        DI.Instance.EventBus.TryInvokeEndPlayGameplayLevel();
    }
}

[HarmonyPatch]
[HarmonyPatchCategory(Constants.GlobalPatches)]
public class PatchOnLoadingScreenClose
{
    [HarmonyTargetMethodHint("b1.BGW_LoadingTipsMgr.FLoadingScreenTimeTracker", "OnLoadingScreenClose")]
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