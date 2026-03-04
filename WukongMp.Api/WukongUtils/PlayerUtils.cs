using System;
using b1;
using BtlB1;
using BtlShare;
using ReadyM.Api.ECS.Worlds;
using ReadyM.Api.Mapping.Events;
using ReadyM.Relay.Client.State;
using ReadyM.Wukong.Common.ECS.Components;
using UnrealEngine.Engine;
using UnrealEngine.Runtime;
using WukongMp.Api.ECS.Entities;
using WukongMp.Api.ECS.GameEvents;
using WukongMp.Api.ECS.Values;
using WukongMp.Api.State;

namespace WukongMp.Api.WukongUtils
{
    public static class PlayerUtils
    {
        public static void TeleportLocalPlayer(MainCharacterEntity mainEntity, FVector location, FRotator rotation, bool setLookAt = true)
        {
            var pawn = mainEntity.Pawn;
            ref var localMainComp = ref mainEntity.GetLocalState();
            if (pawn == null)
            {
                Logging.LogError("Failed to teleport local player: Pawn is null");
                return;
            }

            BUS_EventCollectionCS.Get(pawn)?.Evt_UnitStateTrigger.Invoke(EBUStateTrigger.TeleportBegin, -1f);
            localMainComp.TeleportFinishFrames = 5;
            var correctedLocation = SpawningUtils.GetCorrectedSpawnLocation(pawn, location);
            pawn.SetActorTransform(new FTransform(rotation, correctedLocation), false, out _, setLookAt);
            if (setLookAt)
            {
                BUS_EventCollectionCS.Get(pawn)?.Evt_ResetCameraSpringArmRot.Invoke();
            }
        }

        public static void SetPlayerInteractionEnabled(MainCharacterEntity mainEntity, bool enabled)
        {
            var pawn = mainEntity.Pawn;

            IBUC_SimpleStateData readOnlyData = BGU_DataUtil.GetReadOnlyData<IBUC_SimpleStateData, BUC_SimpleStateData>(pawn);
            var hasCantInteract = readOnlyData.HasSimpleState(EBGUSimpleState.CantInteract);

            if (!enabled && hasCantInteract)
                return;

            var events = BUS_EventCollectionCS.Get(pawn);
            events?.Evt_UnitSetSimpleState.Invoke(EBGUSimpleState.CantInteract, enabled);
        }

        public static void SetLocalPlayerDamageImmunity(MainCharacterEntity mainEntity, bool enabled)
        {
            var pawn = mainEntity.Pawn;
            var events = BUS_EventCollectionCS.Get(pawn);
            if (events != null)
            {
                events?.Evt_UnitSetSimpleState.Invoke(EBGUSimpleState.ImmueDamage, !enabled);
                Logging.LogDebug("Set local player damage immunity to {Enabled}", enabled);
            }
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

        public static void TeleportLocalPlayerToCurrentRebirthPoint(MainCharacterEntity mainEntity)
        {
            var transform = GetCurrentRebirthPointTransform();
            TeleportLocalPlayer(mainEntity, transform.GetLocation(), transform.GetRotation().Rotator(), true);
        }

        public static void TeleportLocalPlayerToRebirthPoint(MainCharacterEntity mainEntity, int rebirthPointId)
        {
            var transform = GetRebirthPointTransform(rebirthPointId);
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

        private static FTransform GetCurrentRebirthPointTransform()
        {
            BPC_RebirthPointData rebirthPointData = BGU_DataUtil.GetReadOnlyData<BPC_RebirthPointData>(GameUtils.GetPlayerController());
            if (rebirthPointData == null)
            {
                Logging.LogError("rebirthPointData is null");
                return FTransform.Default;
            }

            UBGWFunctionLibraryCS.GetRebirthPointTransform(GameUtils.GetWorld(), rebirthPointData.CurrentBirthPoint.PointID, out var transform);
            return transform;
        }

        private static FTransform GetRebirthPointTransform(int rebirthPointId)
        {
            UBGWFunctionLibraryCS.GetRebirthPointTransform(GameUtils.GetWorld(), rebirthPointId, out var Transform);
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

        public static void RespawnSoftlockedParty(Store world, IMappedEventManager mappedEvent, MainCharacterEntity mainEntity)
        {
            var maxComp = 0;
            world.Query<MainCharacterComponent>().ForEachEntity((ref mainComp, _) => { maxComp = Math.Max(maxComp, mainComp.RebirthPointId); });

            ref var localMainComp = ref mainEntity.GetLocalState();
            localMainComp.IsRespawning = true;

            mappedEvent.InvokeInGameAndNotifyEcs(new PartySoftlockEvent(
                entity: mainEntity.Entity,
                birthPointId: maxComp
            ));
        }

        public static void DisableOtherPlayersCollision(ClientState clientState, WukongPlayerState playerState)
        {
            foreach (var playerId in clientState.OtherAreaPlayers)
            {
                var mainEntity = playerState.GetMainCharacterByPlayerId(playerId);
                if (mainEntity == null)
                    continue;

                var pawn = mainEntity.Value.Pawn;
                if (pawn == null)
                    continue;

                ref var localMainComp = ref mainEntity.Value.GetLocalState();
                SetCollisionEnabled(pawn, false);
                localMainComp.ShouldDisableCollision = true;
            }
        }

        public static void AllowOtherPlayersCollision(ClientState clientState, WukongPlayerState playerState)
        {
            foreach (var playerId in clientState.OtherAreaPlayers)
            {
                var mainEntity = playerState.GetMainCharacterByPlayerId(playerId);
                if (mainEntity == null)
                    continue;
                ref var localMain = ref mainEntity.Value.GetLocalState();
                var pawn = mainEntity.Value.Pawn;
                if (pawn == null)
                    continue;
                localMain.ShouldDisableCollision = false;
            }
        }

        public static void EnableSpectator(MainCharacterEntity mainEntity, SpectatorReason reason)
        {
            Logging.LogDebug("Enabling spectator mode for player {PlayerId} with reason {Reason}", mainEntity.GetState().CharacterNickName, reason);
            ref var pvp = ref mainEntity.GetPvP();
            pvp.IsSpectator = true;
            pvp.SpectatorReason = reason;
        }

        public static void DisableSpectator(MainCharacterEntity mainEntity)
        {
            Logging.LogDebug("Disabling spectator mode for player {PlayerId}", mainEntity.GetState().CharacterNickName);
            mainEntity.GetPvP().IsSpectator = false;
            mainEntity.GetLocalState().IsDuringDeathAnim = false;
        }
    }
}