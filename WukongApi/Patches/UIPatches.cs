using System.Collections.Generic;
using System.Reflection;
using b1;
using GSE.GSUI;
using B1UI.GSUI;
using HarmonyLib;
using UnrealEngine.UMG;

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
    
    [HarmonyPatch]
    [HarmonyPatchCategory(Constants.GlobalPatches)]
    public static class PatchStartGameUI
    {
        private static MethodBase TargetMethod()
        {
            return AccessTools.Method("B1UI.GSUI.UIStartGame:OnUIPageConstructImpl");
        }

        public static void Postfix(GSE.GSUI.GSUIView __instance, ref List<VIButtonBaseV2> ___StartGameBtnList, ref UTextBlock ___TxtMainName, ref UTextBlock ___TxtSubName)
        {
            ___StartGameBtnList[0].SetTxtName(UnrealEngine.Runtime.FText.FromString("Join Multiplayer"));

            // Clear OnGSButtonUnFocused event form the first button.
            var type = ___StartGameBtnList[0].GetBUIButton().GetType();
            var field = type.GetField(nameof(b1.UI.Comm.BUI_Button.OnGSButtonUnFocused), BindingFlags.Instance | BindingFlags.NonPublic);
            if (field != null)
            {
                field.SetValue(___StartGameBtnList[0].GetBUIButton(), null);
            }
            __instance.GSAnimKeyToState("GSAKBContinueBtn", "CBtnFocus");

            ___TxtMainName.SetText(UnrealEngine.Runtime.FText.FromString(""));
            ___TxtSubName.SetText(UnrealEngine.Runtime.FText.FromString("Wukong Multiplayer Mod"));
            ___TxtSubName.SetRenderScale(new UnrealEngine.Runtime.FVector2D(1.2, 1.2));
        }
    }

    [HarmonyPatch]
    [HarmonyPatchCategory(Constants.ConnectedPatches)]
    public class PatchBossRushTimerCountdown
    {
        private static MethodBase TargetMethod()
        {
            return AccessTools.Method("B1UI.GSUI.UIBossRushTime:GetRemainTimeStr");
        }

        public static bool Prefix(ref string __result)
        {
            __result = "00:00";
            return false;
        }
    }

    [HarmonyPatch(typeof(GenAGPage), nameof(GenAGPage.ShowPage))]
    [HarmonyPatchCategory(Constants.ConnectedPatches)]
    public class PatchShowPage
    {
        public static void Prefix(int NewPageID, string Source, ChangeReason Reason = null, object exParam = null)
        {
            Logging.LogDebug($"ShowPage: {NewPageID}, {Source}, {Reason}, {exParam}");
        }
    }
}