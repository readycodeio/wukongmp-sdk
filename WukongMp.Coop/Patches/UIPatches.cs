using System.Collections.Generic;
using System.Reflection;
using b1;
using b1.BGW;
using B1UI.GSUI;
using CSharpModBase;
using GSE.GSUI;
using HarmonyLib;
using LiteNetLib;
using PreludeLib.Attributes;
using ResB1;
using UnrealEngine.Runtime;
using UnrealEngine.UMG;
using WukongMp.Api;
using WukongMp.Api.Configuration;
using WukongMp.Api.Resources;
using WukongMp.Api.WukongUtils;

namespace WukongMp.Coop.Patches;

[HarmonyPatch]
[HarmonyPatchCategory(Constants.GlobalPatches)]
public static class PatchStartGameUiCoop
{
    [HarmonyTargetMethodHint("B1UI.GSUI.UIStartGame", "OnUIPageConstructImpl")]
    private static MethodBase TargetMethod()
    {
        return AccessTools.Method("B1UI.GSUI.UIStartGame:OnUIPageConstructImpl");
    }

    public static void Postfix(GSUIView __instance, ref List<VIButtonBaseV2> ___StartGameBtnList, ref UTextBlock ___TxtMainName, ref UTextBlock ___TxtSubName, DSStartGame ___DataStore)
    {
        for (var j = ___DataStore.BtnDataList.Count - 1; j >= 0; j--)
        {
            var BtnBase2 = ___DataStore.BtnDataList[j];

            Logging.LogDebug("Button name: {Name}, id: {Id}", BtnBase2.Name.Value, BtnBase2.Id.Value);
            var buttonName = BtnBase2.Name.Value.ToString();

            if (buttonName == GSB1UIUtil.GetUIWordDescFText(EUIWordID.NEW_GAME).ToString())
            {
                var widgetManagerActorClass = BGW_PreloadAssetMgr.Get(GameUtils.GetWorld()).TryGetCachedResourceObj<UClass>(Constants.WidgetManagerActorPath, ELoadResourceType.SyncLoadAndCache);
                if (widgetManagerActorClass == null)
                {
                    ___StartGameBtnList[j].GetBUIButton().SetVisibility(ESlateVisibility.Collapsed);
                    ___StartGameBtnList.RemoveAt(j);
                    UiUtils.ShowTip(Texts.MissingPak, false);
                    Logging.LogError("WukongMP.pak is not loaded. Could not continue game.");
                }
                else if (!DI.Instance.State.IsConnected)
                {
                    ___StartGameBtnList[j].GetBUIButton().SetVisibility(ESlateVisibility.Collapsed);
                    ___StartGameBtnList.RemoveAt(j);

                    DI.Instance.RelayClient.Scheduler.Schedule(ctx => { Utils.TryRunOnGameThread(() => { DI.Instance.WidgetManager.ShowInfoMessage(ctx.LastDisconnectReason == DisconnectReason.ConnectionRejected ? Texts.ConnectionRejectedByServer : Texts.Disconnected); }); });
                    Logging.LogError("Disconnected. Could not continue game.");
                }
                else
                {
                    Logging.LogDebug("New game UI name desc: {Description}", GSB1UIUtil.GetUIWordDescFText(EUIWordID.NEW_GAME));
                    ___StartGameBtnList[j].SetTxtName(GSB1UIUtil.GetUIWordDescFText(EUIWordID.CONTINUE_GAME));
                }
            }
            else if (buttonName != GSB1UIUtil.GetUIWordDescFText(EUIWordID.EXIT_GAME).ToString()
                     && buttonName != GSB1UIUtil.GetUIWordDescFText(EUIWordID.START_GAME_SETTING).ToString()
                     && buttonName != GSB1UIUtil.GetUIWordDescFText(EUIWordID.NEW_GAME_PLUS).ToString())
            {
                Logging.LogDebug("UI name desc to hide: {Description}", GSB1UIUtil.GetUIWordDescFText(EUIWordID.EXIT_GAME));
                ___StartGameBtnList[j].GetBUIButton().SetVisibility(ESlateVisibility.Collapsed);
                ___StartGameBtnList.RemoveAt(j);
            }
        }

        __instance.GSAnimKeyToState("GSAKBContinueBtn", "CBtnFocus");

        ___TxtMainName.SetText(FText.FromString(""));
        ___TxtSubName.SetText(FText.FromString("Wukong Multiplayer Mod"));
        ___TxtSubName.SetRenderScale(new FVector2D(1.2, 1.2));
    }
}

/// <summary>
/// Hide challenges shrine options in coop mode.
/// </summary>
[HarmonyPatch]
[HarmonyPatchCategory(Constants.GlobalPatches)]
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

        var interactionFuncDesc = GameDBRuntime.GetInteractionFuncDesc(FuncId);
        return interactionFuncDesc.MenuBtnActionType != EMenuBtnActionType.BossIterations
               && interactionFuncDesc.MenuBtnActionType != EMenuBtnActionType.BossRechallenge;
    }
}