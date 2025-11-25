using System.Linq;
using System.Reflection;
using b1;
using HarmonyLib;
using PreludeLib.Attributes;
using WukongMp.Api;
using WukongMp.Api.Configuration;
using WukongMp.PvP.Configuration;

namespace WukongMp.PvP.Patches;

[HarmonyPatch(typeof(BPC_PlayerRoleData), "GetNewGamePlusCount")]
[HarmonyPatchCategory(Constants.ConnectedPatches)]
public static class PatchGetNewGamePlusCount
{
    public static bool Prefix(ref int __result)
    {
        if (!DI.Instance.AreaState.InRoom)
            return true;
        if (DI.Instance.AreaState.CurrentArea == null)
            return true;

        __result = DI.Instance.AreaState.CurrentArea.Value.GetRoom().EnemiesNgPlusLevel + 1;
        return false;
    }
}

/// <summary>
/// Only reset character Team ID if it was not set by us.
/// This prevents the game from resetting the team ID of monsters assigned to player teams in PvP.
/// </summary>
[HarmonyPatch]
[HarmonyPatchCategory(Constants.ConnectedPatches)]
public class TamerResetPatch
{
    [HarmonyTargetMethodHint("b1.BUS_TeamIDManageComp", "OnResetTeamID")]
    private static MethodBase TargetMethod()
    {
        return AccessTools.Method("b1.BUS_TeamIDManageComp:OnResetTeamID");
    }

    public static bool Prefix(BGUCharacterCS ___OwnerAsCharacterCS)
    {
        if (!DI.Instance.AreaState.InRoom)
            return true;

        var teamId = ___OwnerAsCharacterCS.GetTeamIDInCS();
        return !PvpConstants.AvailableTeamIds.Contains(teamId);
    }
}