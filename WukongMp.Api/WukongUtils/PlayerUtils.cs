using b1;
using BtlShare;
using UnrealEngine.Engine;
using UnrealEngine.Runtime;
using WukongMp.Api.Old;
using WukongMp.Api.Patches;

namespace WukongMp.Api.WukongUtils
{
    public static class PlayerUtils
    {
        public static void TeleportLocalPlayer(FVector location, FRotator rotation, bool sweep)
        {
            GameLoopPatch.QueueOnGameThread(() =>
            {
                var playerState = DI.Instance.Players.LocalPlayerState;
                BUS_EventCollectionCS.Get(playerState.Pawn)?.Evt_UnitStateTrigger.Invoke(EBUStateTrigger.TeleportBegin, -1f);
                playerState.TeleportFinishFrames = 5;
                playerState.Pawn?.SetActorTransform(new FTransform(rotation, location), sweep, out _, true);
            }, nameof(TeleportLocalPlayer));
        }

        public static void DisablePlayerInteraction(BGUPlayerCharacterCS playerCharacter)
        {
            var events = BUS_EventCollectionCS.Get(playerCharacter);
            if (events != null)
            {
                events.Evt_UnitSetSimpleState.Invoke(EBGUSimpleState.CantInteract);
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

        public static void TeleportLocalPlayerToRebirthPoint()
        {
            var transform = GetLocalRebirthPointTransform();
            TeleportLocalPlayer(transform.GetLocation(), transform.GetRotation().Rotator(), false);
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
    }
}
