using Friflo.Engine.ECS.Systems;
using ReadyM.Relay.Common.Wukong.ECS.Components;
using UnrealEngine.Runtime;
using WukongMp.Api.ECS.Components;
using WukongMp.Api.State;

namespace WukongMp.Api.ECS.Systems.MainCharacters;

public class EnableCollisionAfterCutsceneSystem(WukongPlayerState playerState) : QuerySystem<MainCharacterComponent, LocalMainCharacterComponent>
{
    protected override void OnUpdate()
    {
        if (!playerState.LocalMainCharacter.HasValue)
            return;

        var myPawn = playerState.LocalMainCharacter.Value.GetLocalState().Pawn;
        if (myPawn == null)
            return;

        var myCapsuleCenter = myPawn.GetActorLocation();
        var myCapsuleRadius = myPawn.CapsuleComponent.GetScaledCapsuleRadius();

        Query.ForEachEntity((ref main, ref local, _) =>
        {
            if (playerState.LocalPlayerId == main.PlayerId)
                return;

            if (local.Pawn == null)
                return;

            if (local.IsCollisionDisabledDuringCutscene)
            {
                // check if we can disable it now if the player is no longer intersecting with the local player
                var playerCenter = local.Pawn.GetActorLocation();
                var capsuleRadius = local.Pawn.CapsuleComponent.GetScaledCapsuleRadius();

                // check if the two capsules are intersecting
                var distanceSq = FVector.Dist2D(myCapsuleCenter, playerCenter);
                var radiusSum = myCapsuleRadius + capsuleRadius;
                if (distanceSq > radiusSum)
                {
                    // we are far enough away, enable collision
                    local.Pawn.SetActorEnableCollision(true);
                    local.IsCollisionDisabledDuringCutscene = false;
                }
            }
        });
    }
}