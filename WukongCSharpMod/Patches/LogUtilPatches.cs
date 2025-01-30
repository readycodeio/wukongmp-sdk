using b1;
using HarmonyLib;

namespace WukongCSharpMod.Patches
{
    [HarmonyPatch(typeof(UGSE_EngineFuncLib), "LogError")]
    public class LogErrorPatch
    {
        public static void Postfix(string Str)
        {
            Logging.LogError("-------------- Wukong Error Message --------------");
            Logging.LogError(Str);
            Logging.LogError("--------------------------------------------------");
        }
    }

    [HarmonyPatch(typeof(UGSE_EngineFuncLib), "LogShipping")]
    public class LogShippingPatch
    {
        public static void Postfix(string Str)
        {
            Logging.LogError("------------- Wukong Shipping Message -------------");
            Logging.LogError(Str);
            Logging.LogError("---------------------------------------------------");
        }
    }

    [HarmonyPatch(typeof(UGSE_EngineFuncLib), "LogShippingError")]
    public class LogShippingErrorPatch
    {
        public static void Postfix(string Str)
        {
            Logging.LogError("---------- Wukong Shipping Error Message ----------");
            Logging.LogError(Str);
            Logging.LogError("---------------------------------------------------");
        }
    }
}
