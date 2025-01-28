using System.Reflection;
using b1;
using HarmonyLib;

namespace WukongCSharpMod.Patches
{
    [HarmonyPatch]
    [HarmonyPatchCategory(Constants.RoomPatches)]
    public static class UIPatches
    {
        private static MethodBase TargetMethod()
        {
            return AccessTools.Method("b1.BUS_UIControlSystemV2:OnDisplayDamageNumUI");
        }

        public static bool Prefix(DamageNumParam param)
        {
            var photon = MyMod.Instance.Photon;
            if (photon.IsMasterClient)
            {
                photon.SendDamageNum(param);
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}