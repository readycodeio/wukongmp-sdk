using b1;
using Friflo.Engine.ECS.Systems;
using ReadyM.Relay.Common.Wukong.ECS.Components;
using UnrealEngine.Runtime;
using WukongMp.Api.ECS.Components;
using WukongMp.Api.ECS.Entities;
using WukongMp.Api.State;
using WukongMp.Api.WukongUtils;

namespace WukongMp.Api.ECS.Systems.MainCharacters;

public class EnableCollisionAfterCutsceneSystem(WukongPlayerState playerState)
    : QuerySystem<MainCharacterComponent, LocalMainCharacterComponent>
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

        Query.ForEachEntity((ref mainComp, ref localMainComp, entity) =>
        {
            var mainEntity = new MainCharacterEntity(entity);
            
            // NOTE(api): It is supposed to affect only the controlled character since it's arena collision-related
            if (playerState.LocalMainCharacter == mainEntity)
                return;

            var pawn = mainEntity.Pawn;
            if (pawn == null)
                return;

            if (!localMainComp.ShouldDisableCollision && pawn.CapsuleComponent.GetCollisionProfileName() == B1GlobalFNames.WindWalk_Pawn) // actually, it's set to Custom in cutscenes
            {
                // check if we can disable it now if the player is no longer intersecting with the local player
                var playerCenter = pawn.GetActorLocation();
                var capsuleRadius = pawn.CapsuleComponent.GetScaledCapsuleRadius();

                // check if the two capsules are intersecting
                var distanceSq = FVector.Dist2D(myCapsuleCenter, playerCenter);
                var radiusSum = myCapsuleRadius + capsuleRadius;
                if (distanceSq > radiusSum)
                {
                    // we are far enough away, enable collision
                    PlayerUtils.SetCollisionEnabled(pawn, true);
                }
            }
        });
    }
}