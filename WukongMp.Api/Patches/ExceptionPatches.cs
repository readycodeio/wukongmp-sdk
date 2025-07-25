using System;
using System.Threading;
using b1;
using HarmonyLib;
using UnrealEngine.Runtime;
using WukongMp.Api.Configuration;
using WukongMp.Api.Old;

namespace WukongMp.Api.Patches;

// NOTE: This type occurs in the original code, it is "Exc-PE-tion" not "Exception"
[HarmonyPatch(typeof(BGW_ExceptionUIMgr), "HandleUSharpInvokeFunctionExcpetion")]
[HarmonyPatchCategory(Constants.GlobalPatches)]
public class ExceptionPatches
{
    public static void Postfix(Exception e)
    {
        Logging.LogCritical(e);
    }
}

[HarmonyPatch(typeof(BGW_DebugMgr), nameof(BGW_DebugMgr.UpdateUserConfigToSentry))]
[HarmonyPatchCategory(Constants.GlobalPatches)]
public class SentryPatches
{
    public static bool Prefix()
    {
        return false;
    }
}

[HarmonyPatch(typeof(NativeReflectionCached), "FindFieldInfo")]
[HarmonyPatchCategory(Constants.GlobalPatches)]
public class NativeReflectionCachedPatches
{
    private static readonly SemaphoreSlim Semaphore = new(1, 1);

    public static void Prefix()
    {
        Semaphore.Wait();
    }

    public static void Postfix()
    {
        Semaphore.Release();
    }
}