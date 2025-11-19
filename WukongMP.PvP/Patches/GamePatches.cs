using b1;
using HarmonyLib;
using WukongMp.Api;
using WukongMp.Api.Configuration;

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