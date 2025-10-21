using System;
using b1;
using CsB1;
using GSDispLib;
using HarmonyLib;
using Microsoft.Extensions.Logging;
using ResB1;
using UnrealEngine.Engine;
using WukongMp.Api.Configuration;

namespace WukongMp.Api.Patches;

[HarmonyPatch(typeof(BUS_DispLibUnitMaterialsManageComp), "Internal_AddMaterialInfoForNewPrimComp")]
[HarmonyPatchCategory(Constants.ConnectedPatches)]
public static class PatchRandomCrashOnMeshAssignedOnTamerReset
{
    public static Exception? Finalizer()
    {
        // suppress System.ArgumentException: An item with the same key has already been added. Key: 274753
        return null;
    }
}

[HarmonyPatch(typeof(BUS_OSSCollectComp), "OnOSSCollectBattleData_AiUnit")]
[HarmonyPatchCategory(Constants.ConnectedPatches)]
public static class PatchRandomCrashOnOSSCollectBattleData_AiUnit
{
    public static Exception? Finalizer(Exception? __exception)
    {
        if (__exception != null)
        {
            DI.Instance.Logger.LogError(__exception, "Suppressed crash in BUS_OSSCollectComp.OnOSSCollectBattleData_AiUnit");
        }

        // suppress System.NullReferenceException
        return null;
    }
}

[HarmonyPatch(typeof(FSMState_GI_Loading_NextChapterReqAndArchive), nameof(FSMState_GI_Loading_NextChapterReqAndArchive.OnEnter))]
[HarmonyPatchCategory(Constants.ConnectedPatches)]
public static class PatchFSMState_GI_Loading_NextChapterReqAndArchive
{
    public static bool Prefix(FSMState_GI_Loading_NextChapterReqAndArchive __instance, FSMContext_GI_Loading ___Context)
    {
        APlayerController playerController = UGSE_EngineFuncLib.GetFirstLocalPlayerController(___Context.OwnerUObj);
        BTF_EventCollectionCS eventCollectionCs = BTF_EventCollectionCS.Get(playerController.PlayerState);
        CSMsgChapterEnterNextReq ChapterEnterNext = new CSMsgChapterEnterNextReq();
        BPC_PlayerRoleData? playerRoleData = BGU_DataUtil.GetReadOnlyData<BPC_PlayerRoleData>(playerController);
        int? CurChapterCache = playerRoleData?.RoleData.RoleCs.Chapter.CurChapter;
        eventCollectionCs.Evt_ChapterEnterNextReq(ChapterEnterNext, (Code, Req, Res) =>
        {
            if (Code != MsgErrCode.ErrSuccess)
            {
                DI.Instance.Logger.LogError(new FSMException(__instance, $"ChapterEnterNextReq Code == {Code}"), "");
                return;
            }

            if (playerRoleData == null)
                return;

            playerRoleData.MapId = ___Context.TargetLevelId;
            BGW_EventCollection.Get(___Context.OwnerUObj).Evt_NextChapterTravelBegin(CurChapterCache!.Value);
            __instance.OwningInstance.TriggerEvent(EGI_Loading.Finish);
        });

        return false;
    }
}