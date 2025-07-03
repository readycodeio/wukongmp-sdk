using b1;
using b1.EventDelDefine;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using WukongMp.Api.Configuration;
using WukongMp.Api.Old;

namespace WukongMp.Api.Patches;


[HarmonyPatch]
[HarmonyPatchCategory(Constants.GlobalPatches)]
public class PatchGSNotifyBeginCS_Implementation
{
    private static MethodBase TargetMethod()
    {
        return AccessTools.Method("b1.BANS_GSEnableMontageFootstep:GSNotifyBeginCS_Implementation");
    }

    public static void Prefix(FUStGSNotifyParam NotifyParam, float TotalDuration)
    {
        var owner = NotifyParam.owner;
        Logging.LogWarning($"GSNotifyBeginCS_Implementation called for {owner.GetName()} with duration {TotalDuration}.");
    }
}

[HarmonyPatch(typeof(GSDel_Void_BoolBoolBoolInt), nameof(GSDel_Void_BoolBoolBoolInt.Invoke))]
[HarmonyPatchCategory(Constants.GlobalPatches)]
public class PatchGSDel_Void_BoolBoolBoolIntInvoke
{
    public static bool Prefix(Del_Void_BoolBoolBoolInt ____MultiCastDel)
    {
        if (!____MultiCastDel.Method.IsStatic && ____MultiCastDel.Target == null)
        {
            Logging.LogError("GSDel_Void_BoolBoolBoolInt.Invoke called on a non-static delegate with a null target.");
            return false;
        }

        return true;
    }

    static void Finalizer(Exception __exception)
    {
        if (__exception != null)
        {
            Logging.LogError("GSDel_Void_BoolBoolBoolInt.Invoke encountered an exception: {ExceptionMessage}", __exception.Message);
        }
    }
}

[HarmonyPatch]
[HarmonyPatchCategory(Constants.GlobalPatches)]
public class PatchAllBUS_FootStepCompImpl
{
    public static IEnumerable<MethodBase> TargetMethods()
    {
        var type = AccessTools.TypeByName("b1.BUS_FootStepCompImpl");

        foreach (var method in Traverse.Create(type).Methods())
        {
            if (method == "GetTickGroupMask" ||
                method == "OnTickWithGroup" ||
                method == "FootStepTick" ||
                method == "FootStepTickByFootSpeed" ||
                method == "OnFootStep" ||
                method == "ASyncLineTraceFinish_CallBack" ||
                method == "TickMyriapodsFootStep")
                continue;
            yield return AccessTools.Method(type, method);
        }
    }

    public static void Postfix(UActorCompBaseCS __instance, MethodBase __originalMethod)
    {
        if (__instance.GetOwner() != null)
        {
            var methodName = __originalMethod.Name;
            Logging.LogDebug("BUS_FootStepCompImpl method {MethodName} called on {OwnerName}", methodName, __instance.GetOwner().GetName());
        }
    }
}
