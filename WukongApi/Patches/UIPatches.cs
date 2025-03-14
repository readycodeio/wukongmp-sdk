using System.Collections.Generic;
using System.IO;
using System.Reflection;
using b1;
using b1.UI.Comm;
using B1UI.GSUI;
using GSE.GSUI;
using HarmonyLib;
using UnrealEngine.Runtime;
using UnrealEngine.UMG;
using WukongApi.UI;

namespace WukongApi.Patches
{
    [HarmonyPatch(typeof(BUS_BeAttackedComp), "CanShowDmgNumUI")]
    [HarmonyPatchCategory(Constants.ConnectedPatches)]
    public static class PatchCanShowDamage
    {
        public static bool Prefix(ref bool __result)
        {
            if (!WukongMP.Instance.ShouldRunConnectedPatches())
                return true;

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
            if (!WukongMP.Instance.ShouldRunConnectedPatches())
                return true;

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

        public static void Postfix(GSUIView __instance, ref List<VIButtonBaseV2> ___StartGameBtnList, ref UTextBlock ___TxtMainName, ref UTextBlock ___TxtSubName, DSStartGame ___DataStore)
        {
            for (int j = 0; j < ___DataStore.BtnDataList.Count; j++)
            {
                DSButtonBase BtnBase2 = ___DataStore.BtnDataList[j];

                Logging.LogDebug("Button name: {Name}, id: {Id}", BtnBase2.Name.Value, BtnBase2.Id.Value);

                if (BtnBase2.Name.Value.ToString() == GSB1UIUtil.GetUIWordDescFText(EUIWordID.CONTINUE_GAME).ToString())
                {
                    Logging.LogDebug("Continue UI name desc: {Description}", GSB1UIUtil.GetUIWordDescFText(EUIWordID.CONTINUE_GAME));
                    if (File.Exists(GameUtils.GetSaveFileFullName(GSE_SaveGameUtil.GetArchiveSlotName(SaveFileType.Archive, Constants.CharacterArchiveId))))
                    {
                        ___StartGameBtnList[j].SetTxtName(FText.FromString(Texts.QuickJoin));
                    }
                    else
                    {
                        ___StartGameBtnList[j].GetBUIButton().SetVisibility(ESlateVisibility.Collapsed);
                    }

                    // Clear OnGSButtonUnFocused event form the continue game button.
                    var type = ___StartGameBtnList[j].GetBUIButton().GetType();
                    var field = type.GetField(nameof(BUI_Button.OnGSButtonUnFocused), BindingFlags.Instance | BindingFlags.NonPublic);
                    field?.SetValue(___StartGameBtnList[j].GetBUIButton(), null);
                }
                else if (BtnBase2.Name.Value.ToString() == GSB1UIUtil.GetUIWordDescFText(EUIWordID.NEW_GAME).ToString())
                {
                    Logging.LogDebug("New game UI name desc: {Description}", GSB1UIUtil.GetUIWordDescFText(EUIWordID.NEW_GAME));
                    ___StartGameBtnList[j].SetTxtName(FText.FromString(Texts.NewCharacter));
                }
                else if (BtnBase2.Name.Value.ToString() == GSB1UIUtil.GetUIWordDescFText(EUIWordID.LOAD_GAME).ToString())
                {
                    Logging.LogDebug("Load game UI name desc : {Description}", GSB1UIUtil.GetUIWordDescFText(EUIWordID.LOAD_GAME));
                    ___StartGameBtnList[j].SetTxtName(FText.FromString(Texts.SelectCharacter));
                }
                else if (BtnBase2.Name.Value.ToString() != GSB1UIUtil.GetUIWordDescFText(EUIWordID.EXIT_GAME).ToString() && BtnBase2.Name.Value.ToString() != GSB1UIUtil.GetUIWordDescFText(EUIWordID.START_GAME_SETTING).ToString())
                {
                    Logging.LogDebug("UI name desc to hide: {Description}", GSB1UIUtil.GetUIWordDescFText(EUIWordID.EXIT_GAME));
                    ___StartGameBtnList[j].GetBUIButton().SetVisibility(ESlateVisibility.Collapsed);
                }
            }

            __instance.GSAnimKeyToState("GSAKBContinueBtn", "CBtnFocus");

            ___TxtMainName.SetText(FText.FromString(""));
            ___TxtSubName.SetText(FText.FromString("Wukong Multiplayer Mod"));
            ___TxtSubName.SetRenderScale(new FVector2D(1.2, 1.2));
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
            if (!WukongMP.Instance.ShouldRunConnectedPatches())
                return true;

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
            Logging.LogDebug("ShowPage: {NewPageID}, {Source}, {Reason}, {ExParam}", NewPageID, Source, Reason, exParam);
        }
    }

    [HarmonyPatch(typeof(UISaveTips), "OnChangeSaveTipsStat")]
    [HarmonyPatchCategory(Constants.ConnectedPatches)]
    public class PatchOnChangeSaveTipsStat
    {
        public static bool Prefix(UWidget ___RootCon)
        {
            ___RootCon.SetVisibility(ESlateVisibility.Collapsed);
            return false;
        }
    }
}