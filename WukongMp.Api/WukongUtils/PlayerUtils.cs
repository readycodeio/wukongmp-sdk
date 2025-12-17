using b1;
using BtlB1;
using BtlShare;
using System;
using CommB1;
using ReadyM.Relay.Common.Wukong.ECS.Components;
using UnrealEngine.Engine;
using UnrealEngine.Runtime;
using WukongMp.Api.ECS.Entities;

namespace WukongMp.Api.WukongUtils
{
    public static class PlayerUtils
    {
        public static void TeleportLocalPlayer(MainCharacterEntity mainEntity, FVector location, FRotator rotation, bool setLookAt = true)
        {
            ref var localMainComp = ref mainEntity.GetLocalState();
            BUS_EventCollectionCS.Get(localMainComp.Pawn)?.Evt_UnitStateTrigger.Invoke(EBUStateTrigger.TeleportBegin, -1f);
            localMainComp.TeleportFinishFrames = 5;
            localMainComp.Pawn?.SetActorTransform(new FTransform(rotation, location), false, out _, true);
            if (setLookAt)
            {
                BUS_EventCollectionCS.Get(localMainComp.Pawn)?.Evt_ResetCameraSpringArmRot.Invoke();
            }
        }

        public static void SetPlayerInteractionEnabled(MainCharacterEntity mainEntity, bool enabled)
        {
            ref var localMainComp = ref mainEntity.GetLocalState();

            IBUC_SimpleStateData readOnlyData = BGU_DataUtil.GetReadOnlyData<IBUC_SimpleStateData, BUC_SimpleStateData>(localMainComp.Pawn);
            var hasCantInteract = readOnlyData.HasSimpleState(EBGUSimpleState.CantInteract);

            if (!enabled && hasCantInteract)
                return;

            var events = BUS_EventCollectionCS.Get(localMainComp.Pawn);
            events?.Evt_UnitSetSimpleState.Invoke(EBGUSimpleState.CantInteract, enabled);
        }

        public static void ResetLocalPlayerCooldown()
        {
            var player = GameUtils.GetControlledPawn();
            if (player == null)
            {
                Logging.LogError("Failed to get local player");
                return;
            }

            ResetCooldown(player);
            ResetMana(player);
        }

        public static void ResetCooldown(APawn playerPawn)
        {
            var events = BUS_EventCollectionCS.Get(playerPawn);
            events?.Evt_ResetSkillCD.Invoke();
            events?.Evt_SetAttrFloat.Invoke(EBGUAttrFloat.CurEnergy, BGUFunctionLibraryCS.BGUGetFloatAttr(playerPawn, EBGUAttrFloat.TransEnergyMax));
            events?.Evt_SetAttrFloat.Invoke(EBGUAttrFloat.VigorEnergy, BGUFunctionLibraryCS.BGUGetFloatAttr(playerPawn, EBGUAttrFloat.VigorEnergyMax));
            events?.Evt_SetAttrFloat.Invoke(EBGUAttrFloat.FabaoEnergy, BGUFunctionLibraryCS.BGUGetFloatAttr(playerPawn, EBGUAttrFloat.FabaoEnergyMax));
        }

        public static void ResetMana(APawn playerPawn)
        {
            var events = BUS_EventCollectionCS.Get(playerPawn);
            var attrContainer = BGU_DataUtil.GetReadOnlyData<IBUC_AttrContainer, BUC_AttrContainer>(playerPawn);
            float maxMana = attrContainer.GetFloatValue(EBGUAttrFloat.MpMax);
            events?.Evt_SetAttrFloat.Invoke(EBGUAttrFloat.Mp, maxMana);
        }

        public static void TeleportLocalPlayerToRebirthPoint(MainCharacterEntity mainEntity)
        {
            var transform = GetLocalRebirthPointTransform();
            TeleportLocalPlayer(mainEntity, transform.GetLocation(), transform.GetRotation().Rotator(), false);
        }

        public static void RebirthDeadPlayer(BGUCharacterCS playerPawn, int rebirthPointId)
        {
            BPS_GSEventCollection.Get(playerPawn.PlayerState)?.Evt_SetCurrentRebirthPoint.Invoke(rebirthPointId);
            var uiControlData = BGU_DataUtil.GetReadOnlyData<BUC_UIControlData>(playerPawn);
            uiControlData.SetActiveDeathUI(NewValue: true);
            BGW_UIEventCollection.Get(playerPawn)?.Evt_UI_ActiveDeathUI(B1: true);
        }

        public static void RebirthAlivePlayer(BGUCharacterCS playerPawn, int rebirthPointId)
        {
            BPS_GSEventCollection.Get(playerPawn.PlayerState)?.Evt_SetCurrentRebirthPoint.Invoke(rebirthPointId);
            BUS_EventCollectionCS.Get(playerPawn)?.Evt_UnitRebirth.Invoke(ERebirthType.RebirthPoint);
        }

        public static bool TryReloadFromSave()
        {
            var world = GameUtils.GetWorld();
            ArchiveSummaryData? latestArchive = BGW_GameArchiveMgr.Get(world)?.GetLatestArchive();
            if (latestArchive != null)
            {
                BGW_EventCollection.Get(GameUtils.GetWorld())?.Evt_BGW_TriggerGlobalFSMEvent(EGI_Global.LoadArchive, new FSMInputData_GI_Global_SubG_GI_Loading_TravelLevel
                {
                    ArchiveId = latestArchive.ArchiveId,
                });
                return true;
            }
            return false;
        }

        public static void RebirthPlayerInPlace(BGUCharacterCS? playerPawn)
        {
            var events = BUS_EventCollectionCS.Get(playerPawn);
            if (events != null)
            {
                events.Evt_OnLeaveFalling.Invoke(); // Reset falling timer.
                events.Evt_RebirthTeleportFinish.Invoke(ERebirthType.RebirthPoint); // Rest state and play anim montage.
                events.Evt_TriggerTeleportResetPlayer.Invoke(); // Reset player stats, will set IsDead flag to false.
            }
        }

        public static void RestPlayer(BGUCharacterCS playerPawn)
        {
            BUS_EventCollectionCS.Get(playerPawn)?.Evt_TriggerPlayerRest.Invoke();
        }

        public static void StartJump(BGUCharacterCS playerPawn, ESkillDirection startJumpDir, FVector2D inputVector)
        {
            BUS_EventCollectionCS.Get(playerPawn)?.Evt_TriggerJumpSkill.Invoke(startJumpDir, inputVector);
        }

        public static void StopJump(BGUCharacterCS playerPawn)
        {
            BUS_EventCollectionCS.Get(playerPawn).Evt_Jump_OnReleased.Invoke();
        }

        private static FTransform GetLocalRebirthPointTransform()
        {
            BPC_RebirthPointData rebirthPointData = BGU_DataUtil.GetReadOnlyData<BPC_RebirthPointData>(GameUtils.GetPlayerController());
            if (rebirthPointData == null)
            {
                Logging.LogError("rebirthPointData is null");
                return FTransform.Default;
            }

            UBGWFunctionLibraryCS.GetRebirthPointTransform(GameUtils.GetWorld(), rebirthPointData.CurrentBirthPoint.PointID, out var Transform);
            return Transform;
        }

        public static void LogRebirthPointChange(AActor worldContext, int rebirthPointID)
        {
            Logging.LogInformation("Rebirth point as current birth point ID updated: {Id}", rebirthPointID);
            FUStRebirthPointDesc fUStRebirthPointDesc = GameDBRuntime.GetFUStRebirthPointDesc(rebirthPointID);
            if (fUStRebirthPointDesc != null && BGUFuncLibMap.IsValidLevelId(fUStRebirthPointDesc.MapID))
            {
                Logging.LogDebug("MapId: {Id}", fUStRebirthPointDesc.MapID);
                Logging.LogDebug("MapAreaId: {Id}", BGUFuncLibMap.GetAreaId(worldContext));
            }
        }

        public static void SetCollisionEnabled(BGUCharacterCS? character, bool enabled)
        {
            if (character == null)
                return;
            character.CapsuleComponent.SetCollisionProfileName(enabled ? B1GlobalFNames.Pawn : B1GlobalFNames.WindWalk_Pawn);
            BUS_EventCollectionCS.Get(character)?.Evt_SetIsEnableCollisionHitMove.Invoke(enabled, ECollisionHitMoveEnableReqType.Interact);
        }

        public static void RespawnSoftlockedParty(MainCharacterEntity mainCharacter)
        {
            var maxComp = 0;
            DI.Instance.World.Query<MainCharacterComponent>().ForEachEntity((ref mainComp, _) => { maxComp = Math.Max(maxComp, mainComp.RebirthPointId); });

            ref var localMainComp = ref mainCharacter.GetLocalState();
            localMainComp.IsRespawning = true;
            DI.Instance.Rpc.SendPartySoftlock(maxComp);
        }
    }
}