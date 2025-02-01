using System;
using b1;
using HarmonyLib;

namespace WukongCSharpMod.Patches
{
    [HarmonyPatch(typeof(BGW_ExceptionUIMgr), "HandleUSharpInvokeFunctionExcpetion")]
    [HarmonyPatchCategory(Constants.ConnectedPatches)]
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