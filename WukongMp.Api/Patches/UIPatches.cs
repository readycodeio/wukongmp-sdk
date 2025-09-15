using b1;
using b1.Localization;
using b1.UI.Comm;
using B1UI.GSSvc;
using B1UI.GSUI;
using GSE.GSUI;
using HarmonyLib;
using ResB1;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using b1.GSMUI.GSWidget;
using UnrealEngine.Runtime;
using UnrealEngine.UMG;
using WukongMp.Api.Configuration;
using WukongMp.Api.Resources;
using WukongMp.Api.UI;
using WukongMp.Api.WukongUtils;
using CultureInfo = System.Globalization.CultureInfo;

namespace WukongMp.Api.Patches
{
    [HarmonyPatch(typeof(BUS_BeAttackedComp), "CanShowDmgNumUI")]
    [HarmonyPatchCategory(Constants.ConnectedPatches)]
    public static class PatchCanShowDamage
    {
        public static bool Prefix(ref bool __result)
        {
            if (!DI.Instance.AreaState.InRoom)
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
            if (!DI.Instance.AreaState.InRoom)
                return true;

            if (!DI.Instance.AreaState.IsMasterClient)
                return false;

            DI.Instance.Rpc.SendDamageNum(Param);
            return true;
        }
    }

    [HarmonyPatch]
    [HarmonyPatchCategory(Constants.CoopPatches)]
    public static class PatchStartGameUiCoop
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

                if (BtnBase2.Name.Value.ToString() == GSB1UIUtil.GetUIWordDescFText(EUIWordID.NEW_GAME).ToString())
                {
                    if (DI.Instance.State.IsConnected)
                    {
                        Logging.LogDebug("New game UI name desc: {Description}", GSB1UIUtil.GetUIWordDescFText(EUIWordID.NEW_GAME));
                        ___StartGameBtnList[j].SetTxtName(GSB1UIUtil.GetUIWordDescFText(EUIWordID.CONTINUE_GAME));
                    }
                    else
                    {
                        ___StartGameBtnList[j].GetBUIButton().SetVisibility(ESlateVisibility.Collapsed);
                        InfoMessageWidget.Instance.SetVisibility(true);
                        InfoMessageWidget.Instance.SetText(Texts.Disconnected);
                    }
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
    [HarmonyPatchCategory(Constants.PvpPatches)]
    public static class PatchStartGameUiPvP
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
                    if (File.Exists(GameSaveUtils.GetSaveFileFullName(GSE_SaveGameUtil.GetArchiveSlotName(SaveFileType.Archive, Constants.CharacterArchiveId))))
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
            if (!DI.Instance.AreaState.InRoom)
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
            Logging.LogInformation("ShowPage: {NewPageID}, {Source}, {Reason}, {ExParam}", NewPageID, Source, Reason, exParam);
        }
    }

    [HarmonyPatch(typeof(UISaveTips), "OnChangeSaveTipsStat")]
    [HarmonyPatchCategory(Constants.DisabledPatches)]
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
            if (!DI.Instance.AreaState.InRoom)
                return true;

            var areaState = DI.Instance.AreaState;
            var areaEntity = areaState.CurrentArea;
            if (areaEntity == null)
                return true;

            if (areaEntity.Value.GetRoom().InPvP)
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
            if (!DI.Instance.AreaState.InRoom)
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
            if (!DI.Instance.AreaState.InRoom)
                return true;

            if (PauseEvent == EPauseEvent.OpenUI || PauseEvent == EPauseEvent.TakePhoto)
            {
                return false;
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(UIBattleMainCon), "OnClickOpenMapUI")]
    [HarmonyPatchCategory(Constants.PvpPatches)]
    public class PatchOnClickOpenMapUI
    {
        public static bool Prefix()
        {
            if (!DI.Instance.AreaState.InRoom)
                return true;

            return false;
        }
    }

    [HarmonyPatch]
    [HarmonyPatchCategory(Constants.PvpPatches)]
    public class PatchShrineRegisterFunc
    {
        public static MethodBase TargetMethod()
        {
            var specializedType = typeof(FMenuHelper<EShrineMenuTag>);
            return specializedType.GetMethod("RegisterFunc")!;
        }

        public static bool Prefix(int FuncId)
        {
            if (!DI.Instance.AreaState.InRoom)
                return true;

            InteractionFuncDesc interactionFuncDesc = GameDBRuntime.GetInteractionFuncDesc(FuncId);
            return interactionFuncDesc.MenuBtnActionType != EMenuBtnActionType.Teleport
                   && interactionFuncDesc.MenuBtnActionType != EMenuBtnActionType.BossIterations
                   && interactionFuncDesc.MenuBtnActionType != EMenuBtnActionType.BossRechallenge;
        }
    }

    [HarmonyPatch(typeof(GSEUtil), "GetCanTeleportGroupMapList")]
    [HarmonyPatchCategory(Constants.PvpPatches)]
    public class PatchGetCanTeleportGroupMapList
    {
        public static bool Prefix(ref List<int> __result)
        {
            if (!DI.Instance.AreaState.InRoom)
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
            Logging.LogInformation("Culture changed to: {Culture}", Culture);
            Thread.CurrentThread.CurrentUICulture = new CultureInfo(Culture);
        }
    }

    [HarmonyPatch(typeof(GSProcBar), "SetParamValue")]
    [HarmonyPatchCategory(Constants.ConnectedPatches)]
    public class ThreadSafeHealthBarPatch
    {
        // add a semaphore to make SetParamValue thread safe
        // this is a writing method
        public static readonly ReaderWriterLockSlim GsProcBarSemaphore = new();

        public static void Prefix()
        {
            GsProcBarSemaphore.EnterWriteLock();
        }

        public static void Postfix()
        {
            GsProcBarSemaphore.ExitWriteLock();
        }
    }

    [HarmonyPatch(typeof(GSProcBar), "GetParamValue")]
    [HarmonyPatchCategory(Constants.ConnectedPatches)]
    public class ThreadSafeHealthBarPatch2
    {
        public static void Prefix()
        {
            ThreadSafeHealthBarPatch.GsProcBarSemaphore.EnterReadLock();
        }

        public static void Postfix()
        {
            ThreadSafeHealthBarPatch.GsProcBarSemaphore.ExitReadLock();
        }
    }

    [HarmonyPatch(typeof(GSProcBar), "SetParamPercent")]
    [HarmonyPatchCategory(Constants.ConnectedPatches)]
    public class ThreadSafeHealthBarPatch3
    {
        public static void Prefix()
        {
            ThreadSafeHealthBarPatch.GsProcBarSemaphore.EnterReadLock();
        }

        public static void Postfix()
        {
            ThreadSafeHealthBarPatch.GsProcBarSemaphore.ExitReadLock();
        }
    }
}