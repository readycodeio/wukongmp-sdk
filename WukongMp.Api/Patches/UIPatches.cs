using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using b1;
using b1.Localization;
using b1.UI.Comm;
using B1UI.GSSvc;
using B1UI.GSUI;
using GSE.GSUI;
using HarmonyLib;
using ResB1;
using UnrealEngine.Runtime;
using UnrealEngine.UMG;
using WukongMp.Api.Resources;
using CultureInfo = System.Globalization.CultureInfo;

namespace WukongMp.Api.Patches
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
    public static class UiPatches
    {
        private static MethodBase TargetMethod()
        {
            return AccessTools.Method("b1.BUS_UIControlSystemV2:OnDisplayDamageNumUI");
        }

        public static bool Prefix(DamageNumParam Param)
        {
            if (!WukongMP.Instance.ShouldRunConnectedPatches())
                return true;

            var client = WukongMP.Instance.Client;

            if (!client.IsMasterClient)
                return false;

            client.SendDamageNum(Param);
            return true;
        }
    }

    [HarmonyPatch]
    [HarmonyPatchCategory(Constants.GlobalPatches)]
    public static class PatchStartGameUi
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
        public static void Prefix(int NewPageID, string Source, ChangeReason Reason, object exParam)
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

    [HarmonyPatch(typeof(UBGWFunctionLibraryCS), "IsShowSettingUiOnly")]
    [HarmonyPatchCategory(Constants.ConnectedPatches)]
    public class PatchIsShowSettingUiOnly
    {
        public static bool Prefix(ref bool __result)
        {
            if (!WukongMP.Instance.ShouldRunConnectedPatches())
                return true;

            var client = WukongMP.Instance.Client;

            if (client.RoomState.InPvP)
            {
                __result = true;
                return false;
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(B1BattleLogicSvc), "UISetGamePaused")]
    [HarmonyPatchCategory(Constants.ConnectedPatches)]
    public class PatchUISetGamePaused
    {
        public static bool Prefix()
        {
            if (!WukongMP.Instance.ShouldRunConnectedPatches())
                return true;

            return false;
        }
    }

    [HarmonyPatch(typeof(BGW_PauseGameMgr), "SetGamePause")]
    [HarmonyPatchCategory(Constants.ConnectedPatches)]
    public class PatchSetGamePause
    {
        public static bool Prefix(EPauseEvent PauseEvent)
        {
            if (!WukongMP.Instance.ShouldRunConnectedPatches())
                return true;

            if (PauseEvent == EPauseEvent.OpenUI || PauseEvent == EPauseEvent.TakePhoto)
            {
                return false;
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(UIBattleMainCon), "OnClickOpenMapUI")]
    [HarmonyPatchCategory(Constants.ConnectedPatches)]
    public class PatchOnClickOpenMapUI
    {
        public static bool Prefix()
        {
            if (!WukongMP.Instance.ShouldRunConnectedPatches())
                return true;

            return false;
        }
    }

    [HarmonyPatch]
    [HarmonyPatchCategory(Constants.ConnectedPatches)]
    public class PatchShrineRegisterFunc
    {
        public static MethodBase TargetMethod()
        {
            var specializedType = typeof(FMenuHelper<EShrineMenuTag>); 
            return specializedType.GetMethod("RegisterFunc")!;
        }

        public static bool Prefix(int FuncId)
        {
            if (!WukongMP.Instance.ShouldRunConnectedPatches())
                return true;

            InteractionFuncDesc interactionFuncDesc = GameDBRuntime.GetInteractionFuncDesc(FuncId);
            return interactionFuncDesc.MenuBtnActionType != EMenuBtnActionType.Teleport
                   && interactionFuncDesc.MenuBtnActionType != EMenuBtnActionType.BossIterations
                   && interactionFuncDesc.MenuBtnActionType != EMenuBtnActionType.BossRechallenge;
        }
    }
    
    [HarmonyPatch(typeof(GSEUtil), "GetCanTeleportGroupMapList")]
    [HarmonyPatchCategory(Constants.ConnectedPatches)]
    public class PatchGetCanTeleportGroupMapList
    {
        public static bool Prefix(ref List<int> __result)
        {
            if (!WukongMP.Instance.ShouldRunConnectedPatches())
                return true;

            __result = [];
            return false;
        }
    }

    [HarmonyPatch(typeof(GSLocalization), "SetCurrentCulture")]
    [HarmonyPatchCategory(Constants.GlobalPatches)]
    public class PatchSetCurrentCulture
    {
        public static void Postfix(string Culture)
        {
            Logging.LogDebug("Culture changed to: {Culture}", Culture);
            Thread.CurrentThread.CurrentUICulture = new CultureInfo(Culture);
        }
    }
}