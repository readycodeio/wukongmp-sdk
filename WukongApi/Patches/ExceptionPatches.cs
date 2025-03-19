using System;
using b1;
using HarmonyLib;

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
}