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
            Helpers.LogError("-------------- EXCEPTION --------------");
            Helpers.LogError(e.Message);
            Helpers.LogError(e.StackTrace);
            Helpers.LogError("---------------------------------------");
        }
    }
}