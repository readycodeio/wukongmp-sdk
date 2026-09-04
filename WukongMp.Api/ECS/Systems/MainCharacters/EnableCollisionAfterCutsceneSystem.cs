using b1;
using Friflo.Engine.ECS.Systems;
using ReadyM.Wukong.Common.ECS.Components;
using UnrealEngine.Runtime;
using WukongMp.Api.ECS.Components;
using WukongMp.Api.ECS.Entities;
using WukongMp.Api.State;
using WukongMp.Api.WukongUtils;

namespace WukongMp.Api.ECS.Systems.MainCharacters;

internal class EnableCollisionAfterCutsceneSystem(WukongPlayerState playerState) : QuerySystem<MainCharacterComponent, LocalMainCharacterComponent>
{
    protected override void OnUpdate()
    {
        if (!playerState.LocalMainCharacter.HasValue)
            return;

        var myPawn = playerState.LocalMainCharacter.Value.Pawn;
        if (myPawn == null)
            return;

        var myCapsuleCenter = myPawn.GetActorLocation();
        var myCapsuleRadius = myPawn.CapsuleComponent.GetScaledCapsuleRadius();

        Query.ForEachEntity((ref main, ref local, entity) =>
        {
            if (playerState.LocalPlayerId == main.PlayerId)
                return;

            if (main.IsSpectator)
                return;

            var mainEntity = new MainCharacterEntity(entity);

            if (mainEntity.Pawn == null)
                return;

            if (!local.ShouldDisableCollision && mainEntity.Pawn.CapsuleComponent.GetCollisionProfileName() == B1GlobalFNames.WindWalk_Pawn) // actually, it's set to Custom in cutscenes
            {
                // check if we can disable it now if the player is no longer intersecting with the local player
                var playerCenter = mainEntity.Pawn.GetActorLocation();
                var capsuleRadius = mainEntity.Pawn.CapsuleComponent.GetScaledCapsuleRadius();

                // check if the two capsules are intersecting
                var distanceSq = FVector.Dist2D(myCapsuleCenter, playerCenter);
                var radiusSum = myCapsuleRadius + capsuleRadius;
                if (distanceSq > radiusSum)
                {
                    // we are far enough away, enable collision
                    PlayerUtils.SetCollisionEnabled(mainEntity.Pawn, true);
                }
            }
        });
    }
}