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

[HarmonyPatch(typeof(AActor), nameof(AActor.SetActorEnableCollision))]
[HarmonyPatchCategory(Constants.GlobalPatches)]
public class SetActorEnableCollision
{
    public static void Postfix(AActor __instance, bool bNewActorEnableCollision)
    {
        if (DI.Instance.PawnState.GetEntityByPlayerPawn(__instance).HasValue)
        {
            Logging.LogDebug("SetActorEnableCollision called for player pawn actor: {0}, collision enabled: {1}", BGU_DataUtil.GetActorGuid(__instance), bNewActorEnableCollision);
            Logging.LogDebug(new System.Diagnostics.StackTrace().ToString());
        }
    }
}

[HarmonyPatch(typeof(UPrimitiveComponent), nameof(UPrimitiveComponent.SetCollisionEnabled))]
[HarmonyPatchCategory(Constants.GlobalPatches)]
public class SetCollisionEnabled
{
    public static void Postfix(UPrimitiveComponent __instance, ECollisionEnabled NewType)
    {
        Logging.LogDebug("SetCollisionEnabled called for component: {0}, new collision type: {1}",__instance.GetName(), NewType);
    }
}

#endif