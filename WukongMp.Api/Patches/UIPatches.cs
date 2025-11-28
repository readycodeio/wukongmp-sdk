using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using b1;
using b1.ECS;
using b1.GSMUI;
using b1.GSMUI.GSWidget;
using b1.Localization;
using b1.Protobuf.DataAPI;
using b1.UI.Comm;
using B1UI.GSSvc;
using B1UI.GSUI;
using BtlShare;
using GSE.GSUI;
using HarmonyLib;
using PreludeLib.Attributes;
using UnrealEngine.Runtime;
using UnrealEngine.UMG;
using WukongMp.Api.Configuration;
using WukongMp.Api.Resources;
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

[HarmonyPatch(typeof(GSLocalization), "SetCurrentCulture")]
[HarmonyPatchCategory(Constants.GlobalPatches)]
public class PatchSetCurrentCulture
{
    public static void Postfix(string Culture)
    {
        Logging.LogInformation("Culture changed to: {Culture}", Culture);
        Texts.Culture = new CultureInfo(Culture);
        DI.Instance.GameplayEventRouter.RaiseOnLanguageChanged(Texts.Culture);
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

[HarmonyPatch(typeof(BGW_GameDB), nameof(BGW_GameDB.GetUnitBattleInfoExtendDesc))]
[HarmonyPatchCategory(Constants.GlobalPatches)]
public static class PatchIsStandAlone
{
    public static void Postfix(ref FUStUnitBattleInfoExtendDesc? __result)
    {
        if (__result is { BloodBarType: EBGUBloodBarType.PlayerBar })
        {
            __result.BloodBarType = EBGUBloodBarType.EnemyBar;
        }
    }
}