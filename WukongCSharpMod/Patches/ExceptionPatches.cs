using System;
using b1;
using HarmonyLib;

namespace WukongCSharpMod.Patches
{
    [HarmonyPatch(typeof(BGW_ExceptionUIMgr), "HandleUSharpInvokeFunctionExcpetion")]
    public class ExceptionPatches
    {
        public static void Postfix(Exception e)
        {
            Logging.LogError("-------------- EXCEPTION --------------");
            Logging.LogError(e.Message);
            Logging.LogError(e.StackTrace);
            Logging.LogError("---------------------------------------");
        }
    }

    [HarmonyPatch(typeof(BGW_ExceptionUIMgr), "HandleFatalExceptionUIClose")]
    public class FatalExceptionPatches
    {
        public static bool Prefix()
        {
            Logging.LogError("Would close the game here");
            return false;
        }
    }
}