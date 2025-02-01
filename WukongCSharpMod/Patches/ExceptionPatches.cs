using System;
using b1;
using HarmonyLib;

namespace WukongCSharpMod.Patches
{
    // NOTE: This type occurs in the original code, it is "Exc-PE-tion" not "Exception"
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
}