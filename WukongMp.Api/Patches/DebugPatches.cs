using b1;
using HarmonyLib;
using UnrealEngine.Engine;
using WukongMp.Api.Configuration;

namespace WukongMp.Api.Patches;

#if DEBUG

[HarmonyPatch(typeof(BGWConsoleCommands), nameof(BGWConsoleCommands.HasGMFlag))]
[HarmonyPatchCategory(Constants.GlobalPatches)]
public class EnableConsoleCommandsPatch
{
    public static bool Prefix(int Flag, ref bool __result)
    {
        if (Flag == 4)
        {
            __result = true;
            return false;
        }

        return true;
    }
}

[HarmonyPatch(typeof(BGWConsoleCommands), nameof(BGWConsoleCommands.IsPlayerGMInputEnabled), MethodType.Getter)]
[HarmonyPatchCategory(Constants.GlobalPatches)]
public class EnableConsoleCommandsPatch2
{
    public static bool Prefix(ref bool __result)
    {
        __result = true;
        return false;
    }
}

#endif