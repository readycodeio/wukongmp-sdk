using b1;
using UnrealEngine.Runtime;
using WukongMp.Api.Old.Api;
using WukongMp.Api.Patches;

namespace WukongMp.Api.WukongUtils
{
    public static class PlayerUtils
    {
        public static void TeleportLocalPlayer(FVector location, FRotator rotation, bool sweep)
        {
            GameLoopPatch.QueueOnGameThread(() =>
            {
                var playerState = WukongMpModBase.Client.LocalPlayerState;
                BUS_EventCollectionCS.Get(playerState.Pawn)?.Evt_UnitStateTrigger.Invoke(EBUStateTrigger.TeleportBegin, -1f);
                playerState.TeleportFinishFrames = 5;
                playerState.Pawn?.SetActorTransform(new FTransform(rotation, location), sweep, out _, true);
                GameUtils.GetPlayerController().SetControlRotation(rotation);
            }, nameof(TeleportLocalPlayer));
        }
    }
}
