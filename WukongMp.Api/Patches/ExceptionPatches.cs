using System;
using System.Diagnostics;
using System.Threading;
using b1;
using Friflo.Engine.ECS;
using HarmonyLib;
using UnrealEngine.Runtime;
using WukongMp.Api.Configuration;

namespace WukongMp.Api.Patches;

// NOTE: This type occurs in the original code, it is "Exc-PE-tion" not "Exception"
[HarmonyPatch(typeof(BGW_ExceptionUIMgr), "HandleUSharpInvokeFunctionExcpetion")]
[HarmonyPatchCategory(Constants.GlobalPatches)]
public class ExceptionPatches
{
    public static void Postfix(Exception e)
    {
        Logging.LogCritical(e);
        Logging.LogWarning("Last query stack traces:");

        foreach (var kvp in QueryEntities.LastQueryTraces)
        {
            Logging.LogWarning("ID {EntityId}: {StackTrace}", kvp.Key, kvp.Value);
        }
#if DEBUG
        Debugger.Break();
#endif
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