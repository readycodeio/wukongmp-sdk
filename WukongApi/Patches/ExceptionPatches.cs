using System;
using System.Threading;
using b1;
using HarmonyLib;
using UnrealEngine.Runtime;

namespace WukongApi.Patches
{
    // NOTE: This type occurs in the original code, it is "Exc-PE-tion" not "Exception"
    [HarmonyPatch(typeof(BGW_ExceptionUIMgr), "HandleUSharpInvokeFunctionExcpetion")]
    [HarmonyPatchCategory(Constants.GlobalPatches)]
    public class ExceptionPatches
    {
        public static void Postfix(Exception e)
        {
            Logging.LogCriticalException(e);
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

    [HarmonyPatch(typeof(SysLogUtil.SysLogInstance), nameof(SysLogUtil.SysLogInstance.LogInfo))]
    [HarmonyPatchCategory(Constants.GlobalPatches)]
    public class SysLogUtilPatches1
    {
        public static void Postfix(string LogMessage)
        {
            Logging.LogInformation("[SysLogInstance] {Message}", LogMessage);
        }
    }

    [HarmonyPatch(typeof(SysLogUtil.SysLogInstance), nameof(SysLogUtil.SysLogInstance.LogDebug))]
    [HarmonyPatchCategory(Constants.GlobalPatches)]
    public class SysLogUtilPatches2
    {
        public static void Postfix(string LogMessage)
        {
            Logging.LogDebug("[SysLogInstance] {Message}", LogMessage);
        }
    }

    [HarmonyPatch(typeof(SysLogUtil.SysLogInstance), nameof(SysLogUtil.SysLogInstance.LogWarning))]
    [HarmonyPatchCategory(Constants.GlobalPatches)]
    public class SysLogUtilPatches3
    {
        public static void Postfix(string LogMessage)
        {
            Logging.LogWarning("[SysLogInstance] {Message}", LogMessage);
        }
    }

    [HarmonyPatch(typeof(SysLogUtil.SysLogInstance), nameof(SysLogUtil.SysLogInstance.LogError))]
    [HarmonyPatchCategory(Constants.GlobalPatches)]
    public class SysLogUtilPatches4
    {
        public static void Postfix(string LogMessage)
        {
            Logging.LogError("[SysLogInstance] {Message}", LogMessage);
        }
    }

    [HarmonyPatch(typeof(SysLogUtil.SysLogInstance), nameof(SysLogUtil.SysLogInstance.LogShipping))]
    [HarmonyPatchCategory(Constants.GlobalPatches)]
    public class SysLogUtilPatches5
    {
        public static void Postfix(string LogMessage)
        {
            Logging.LogDebug("[SysLogInstance | Shipping] {Message}", LogMessage);
        }
    }

    [HarmonyPatch(typeof(SysLogUtil.SysLogInstance), nameof(SysLogUtil.SysLogInstance.LogShippingError))]
    [HarmonyPatchCategory(Constants.GlobalPatches)]
    public class SysLogUtilPatches6
    {
        public static void Postfix(string LogMessage)
        {
            Logging.LogError("[SysLogInstance | Shipping] {Message}", LogMessage);
        }
    }
}