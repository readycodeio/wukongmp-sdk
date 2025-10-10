using HarmonyLib;
using UnrealEngine.Runtime;
using WukongMp.Api.Configuration;

namespace WukongMp.Api.Patches;

[HarmonyPatch(typeof(UObject), nameof(UObject.GetName))]
[HarmonyPatchCategory(Constants.GlobalPatches)]
public static class PatchGetName
{
    public static bool Prefix(UObject? __instance, ref string? __result)
    {
        if (__instance == null)
        {
            Logging.LogError("Trying to call GetName on invalid UObject");
            __result = "Invalid";
            return false;
        }
        return true;
    }
}

[HarmonyPatch(typeof(UObject), nameof(UObject.GetPathName))]
[HarmonyPatchCategory(Constants.GlobalPatches)]
public static class PatchGetPathName
{
    public static bool Prefix(UObject? __instance, ref string? __result)
    {
        if (__instance == null)
        {
            Logging.LogError("Trying to call GetPathName on invalid UObject");
            __result = "Invalid";
            return false;
        }
        return true;
    }
}

[HarmonyPatch(typeof(UObject), nameof(UObject.GetFullName))]
[HarmonyPatchCategory(Constants.GlobalPatches)]
public static class PatchGetFullName
{
    public static bool Prefix(UObject? __instance, ref string? __result)
    {
        if (__instance == null)
        {
            Logging.LogError("Trying to call GetFullName on invalid UObject");
            __result = "Invalid";
            return false;
        }
        return true;
    }
}
