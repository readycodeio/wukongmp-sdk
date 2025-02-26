using b1;
using HarmonyLib;
using System.Reflection;

namespace WukongApi.Patches
{
    [HarmonyPatch(typeof(BUS_PlayerInputActionComp), "TriggerMagicSkill")]
    [HarmonyPatchCategory(Constants.ConnectedPatches)]
    public static class PatchTriggerMagicSkill
    {
        public static bool Prefix()
        {
            return false;
        }
    }

    [HarmonyPatch]
    [HarmonyPatchCategory(Constants.ConnectedPatches)]
    public static class PatchFaBaoSkill
    {
        private static MethodBase TargetMethod()
        {
            return AccessTools.Method("b1.BUIACastFaBaoSkill:OnTriggerInputAction");
        }

        public static bool Prefix()
        {
            return false;
        }
    }
}
