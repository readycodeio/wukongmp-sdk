using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using b1;
using b1.BGW;
using b1.GSMUI;
using b1.GSMUI.GSWidget;
using b1.Localization;
using b1.UI.Comm;
using B1UI.GSSvc;
using B1UI.GSUI;
using CSharpModBase;
using GSE.GSUI;
using HarmonyLib;
using LiteNetLib;
using Microsoft.Extensions.Logging;
using PreludeLib.Attributes;
using ResB1;
using UnrealEngine.Runtime;
using UnrealEngine.UMG;
using WukongMp.Api.Configuration;
using WukongMp.Api.Resources;
using WukongMp.Api.UI;
using WukongMp.Api.WukongUtils;
using CultureInfo = System.Globalization.CultureInfo;

namespace WukongMp.Api.Patches;

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

[HarmonyPatch(typeof(BUS_BeAttackedComp), "CanShowDmgNumUI")]
[HarmonyPatchCategory(Constants.ConnectedPatches)]
public static class PatchDamageNumberDisplayCheck
{
    public static void Postfix(BUS_BeAttackedComp __instance, ref bool __result)
    {
        if (!__result)
            return;

        var owner = __instance.GetOwner();

        if (owner == null)
            return;

        var entity = DI.Instance.PawnState.GetEntityByPlayerPawn(owner);
        if (entity.HasValue && DI.Instance.ClientOwnership.OwnsEntity(entity.Value.Entity))
        {
            return;
        }

        var tamerEntity = DI.Instance.PawnState.GetEntityByTamerMonster(owner);
        if (tamerEntity.HasValue && DI.Instance.ClientOwnership.OwnsEntity(tamerEntity.Value.Entity))
        {
            return;
        }

        __result = false;
    }
}

[HarmonyPatch]
[HarmonyPatchCategory(Constants.ConnectedPatches)]
public static class PatchSendDamageNumbers
{
    [HarmonyTargetMethodHint("b1.BUS_UIControlSystemV2", "OnDisplayDamageNumUI")]
    private static MethodBase TargetMethod()
    {
        return AccessTools.Method("b1.BUS_UIControlSystemV2:OnDisplayDamageNumUI");
    }

    public static void Prefix(DamageNumParam Param)
    {
        if (!DI.Instance.AreaState.InRoom)
            return;

        DI.Instance.Rpc.SendDamageNum(Param);
    }
}

[HarmonyPatch]
[HarmonyPatchCategory(Constants.CoopPatches)]
public static class PatchStartGameUiCoop
{
    [HarmonyTargetMethodHint("B1UI.GSUI.UIStartGame", "OnUIPageConstructImpl")]
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
                var widgetManagerActorClass = BGW_PreloadAssetMgr.Get(GameUtils.GetWorld()).TryGetCachedResourceObj<UClass>(Constants.WidgetManagerActorPath, ELoadResourceType.SyncLoadAndCache);
                if (widgetManagerActorClass == null)
                {
                    ___StartGameBtnList[j].GetBUIButton().SetVisibility(ESlateVisibility.Collapsed);
                    UiUtils.ShowTip(Texts.MissingPak, false);
                    Logging.LogError("WukongMP.pak is not loaded. Could not continue game.");
                }
                else if (!DI.Instance.State.IsConnected)
                {
                    ___StartGameBtnList[j].GetBUIButton().SetVisibility(ESlateVisibility.Collapsed);

                    DI.Instance.RelayClient.Scheduler.Schedule(ctx =>
                    {
                        Utils.TryRunOnGameThread(() =>
                        {
                            InfoMessageWidget.Instance.SetVisibility(true);
                            InfoMessageWidget.Instance.SetText(ctx.LastDisconnectReason == DisconnectReason.ConnectionRejected ? Texts.ConnectionRejectedByServer : Texts.Disconnected);
                        });
                    });
                    Logging.LogError("Disconnected. Could not continue game.");
                }
                else
                {
                    Logging.LogDebug("New game UI name desc: {Description}", GSB1UIUtil.GetUIWordDescFText(EUIWordID.NEW_GAME));
                    ___StartGameBtnList[j].SetTxtName(GSB1UIUtil.GetUIWordDescFText(EUIWordID.CONTINUE_GAME));
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
    [HarmonyTargetMethodHint("B1UI.GSUI.UIStartGame", "OnUIPageConstructImpl")]
    private static MethodBase TargetMethod()
    {
        return AccessTools.Method("B1UI.GSUI.UIStartGame:OnUIPageConstructImpl");
    }

    public static void Postfix(GSUIView __instance, ref List<VIButtonBaseV2> ___StartGameBtnList, ref UTextBlock ___TxtMainName, ref UTextBlock ___TxtSubName, DSStartGame ___DataStore)
    {
        var widgetManagerActorClass = BGW_PreloadAssetMgr.Get(GameUtils.GetWorld()).TryGetCachedResourceObj<UClass>(Constants.WidgetManagerActorPath, ELoadResourceType.SyncLoadAndCache);
        var hasPak = widgetManagerActorClass != null;
        var isConnected = DI.Instance.State.IsConnected;
        if (!hasPak)
        {
            UiUtils.ShowTip(Texts.MissingPak, false);
            Logging.LogError("WukongMP.pak is not loaded. Could not continue game.");
        }
        else if (!isConnected)
        {
            DI.Instance.RelayClient.Scheduler.Schedule(ctx =>
            {
                Utils.TryRunOnGameThread(() =>
                {
                    InfoMessageWidget.Instance.SetVisibility(true);
                    InfoMessageWidget.Instance.SetText(ctx.LastDisconnectReason == DisconnectReason.ConnectionRejected ? Texts.ConnectionRejectedByServer : Texts.Disconnected);
                });
            });
            Logging.LogError("Disconnected. Could not continue game.");
        }

        for (int j = 0; j < ___DataStore.BtnDataList.Count; j++)
        {
            DSButtonBase BtnBase2 = ___DataStore.BtnDataList[j];
            var buttonName = BtnBase2.Name.Value.ToString();

            Logging.LogDebug("Button name: {Name}, id: {Id}", buttonName, BtnBase2.Id.Value);

            if (buttonName == GSB1UIUtil.GetUIWordDescFText(EUIWordID.CONTINUE_GAME).ToString())
            {
                Logging.LogDebug("Continue UI name desc: {Description}", GSB1UIUtil.GetUIWordDescFText(EUIWordID.CONTINUE_GAME));
                if (!hasPak || !isConnected)
                {
                    ___StartGameBtnList[j].GetBUIButton().SetVisibility(ESlateVisibility.Collapsed);
                }
                else if (File.Exists(GameSaveUtils.GetSaveFileFullName(GSE_SaveGameUtil.GetArchiveSlotName(SaveFileType.Archive, Constants.CharacterArchiveId))))
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
            else if (buttonName == GSB1UIUtil.GetUIWordDescFText(EUIWordID.NEW_GAME).ToString())
            {
                Logging.LogDebug("New game UI name desc: {Description}", GSB1UIUtil.GetUIWordDescFText(EUIWordID.NEW_GAME));
                if (!hasPak || !isConnected)
                {
                    ___StartGameBtnList[j].GetBUIButton().SetVisibility(ESlateVisibility.Collapsed);
                }
                else
                {
                    ___StartGameBtnList[j].SetTxtName(FText.FromString(Texts.NewCharacter));
                }
            }
            else if (buttonName == GSB1UIUtil.GetUIWordDescFText(EUIWordID.LOAD_GAME).ToString())
            {
                Logging.LogDebug("Load game UI name desc : {Description}", GSB1UIUtil.GetUIWordDescFText(EUIWordID.LOAD_GAME));
                if (!hasPak || !isConnected)
                {
                    ___StartGameBtnList[j].GetBUIButton().SetVisibility(ESlateVisibility.Collapsed);
                }
                else
                {
                    ___StartGameBtnList[j].SetTxtName(FText.FromString(Texts.SelectCharacter));
                }
            }
            else if (buttonName != GSB1UIUtil.GetUIWordDescFText(EUIWordID.EXIT_GAME).ToString() && buttonName != GSB1UIUtil.GetUIWordDescFText(EUIWordID.START_GAME_SETTING).ToString())
            {
                Logging.LogDebug("UI name desc to hide: {Description}", buttonName);
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
    [HarmonyTargetMethodHint("B1UI.GSUI.UIBossRushTime", "GetRemainTimeStr")]
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
        if (areaState.PvpState is { InPvP: true })
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
    public static bool Prefix(EPauseEvent PauseEvent, bool bPause)
    {
        if (!DI.Instance.Connection.IsRunning)
            return true;

        if (!bPause)
            return true; // always allow unpausing

        if (PauseEvent is EPauseEvent.OpenUI or EPauseEvent.TakePhoto)
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
    [HarmonyTargetMethodHint(typeof(FMenuHelper<EShrineMenuTag>), "RegisterFunc")]
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

[HarmonyPatch]
[HarmonyPatchCategory(Constants.ConnectedPatches)]
public static class PatchOnInfoChange
{
    [HarmonyTargetMethodHint("B1UI.GSUI.UILoadingAdaptor", "OnInfoChange")]
    public static MethodBase TargetMethod()
    {
        return AccessTools.Method("B1UI.GSUI.UILoadingAdaptor:OnInfoChange");
    }

    public static bool Prefix(ChangeReason Reason, FLoadingAdaptorInfo NewValue, UObject ___WorldContext)
    {
        if (Reason == ChangeReason.UiInit)
            return true;

        var chapterDesc = GameDBRuntime.GetChapterDescByLevelId(NewValue.TargetLevelId);
        if (chapterDesc == null)
        {
            return true;
        }

        if (!NewValue.IsFadeIn)
            return true;

        int curLevelId = BGUFuncLibMap.GetCurLevelId(___WorldContext);
        return NewValue.TargetLevelId != curLevelId;
    }
}

[HarmonyPatch(typeof(GSMUITickMgr), nameof(GSMUITickMgr.DoGSTicking))]
[HarmonyPatchCategory(Constants.ConnectedPatches)]
public static class PatchDoGSTicking
{
    public static void Prefix(List<IGSMUITickable> ___TickingQueue)
    {
        for (var i = ___TickingQueue.Count - 1; i >= 0; --i)
        {
            if (___TickingQueue[i] == null)
            {
                ___TickingQueue.RemoveAt(i);
            }
        }
    }
}