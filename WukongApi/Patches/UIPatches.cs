using System.Reflection;
using b1;
using HarmonyLib;

namespace WukongApi.Patches
{
    [HarmonyPatch(typeof(BUS_BeAttackedComp), "CanShowDmgNumUI")]
    [HarmonyPatchCategory(Constants.ConnectedPatches)]
    public static class PatchCanShowDamage
    {
        public static bool Prefix(ref bool __result)
        {
            __result = true;
            return false;
        }
    }

    [HarmonyPatch]
    [HarmonyPatchCategory(Constants.ConnectedPatches)]
    public static class UIPatches
    {
        private static MethodBase TargetMethod()
        {
            return AccessTools.Method("b1.BUS_UIControlSystemV2:OnDisplayDamageNumUI");
        }

        public static bool Prefix(DamageNumParam Param)
        {
            var photon = WukongMP.Instance.Photon;

            if (!photon.IsMasterClient)
                return false;

            photon.SendDamageNum(Param);
            return true;
        }
    }
}