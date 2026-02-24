using Friflo.Engine.ECS.Systems;
using System;
using UnrealEngine.Runtime;
using WukongMp.Api.Configuration;
using WukongMp.Api.ECS.Components;
using WukongMp.Api.ECS.Entities;
using WukongMp.Api.WukongUtils;

namespace WukongMp.Api.ECS.Systems.MainCharacters;

public class UpdateMainCharacterMarkerSystem : QuerySystem<LocalMainCharacterComponent>
{
    protected override void OnUpdate()
    {
        var localPlayerController = GameUtils.GetPlayerController();
        if (localPlayerController == null)
            return;
        var viewTarget = localPlayerController.GetViewTarget();
        var viewTargetLocation = viewTarget.GetActorLocation();

        Query.ForEachEntity((ref localMainComp, entity) =>
        {
            var mainEntity = new MainCharacterEntity(entity);
            
            if (localMainComp.MarkerActor == null)
                return;

            if (mainEntity.HasPawn)
            {
                var pawn = mainEntity.Pawn!;
                var location = pawn.GetActorLocation();
                var distance = FVector.Dist2D(viewTargetLocation, location);
                var coefficient = Math.Min(distance / Constants.MaxMarkerHeightDistance, 1);
                var markerHeight = pawn.CapsuleComponent.GetScaledCapsuleHalfHeight() * (1 + Constants.BaseMarkerHeightCoefficient + coefficient);
                localMainComp.MarkerActor.SetActorLocation(location + new FVector(0, 0, markerHeight), false, out var _, true);
            }
        });
    }
}
