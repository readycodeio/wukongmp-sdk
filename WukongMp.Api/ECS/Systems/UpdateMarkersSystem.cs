using System;
using Friflo.Engine.ECS.Systems;
using UnrealEngine.Runtime;
using WukongMp.Api.Configuration;
using WukongMp.Api.ECS.Components;
using WukongMp.Api.ECS.Entities;
using WukongMp.Api.WukongUtils;

namespace WukongMp.Api.ECS.Systems;

internal class UpdateMarkersSystem : QuerySystem<MarkerComponent>
{
    protected override void OnUpdate()
    {
        var localPlayerController = GameUtils.GetPlayerController();
        if (localPlayerController == null)
            return;

        var viewTarget = localPlayerController.GetViewTarget();
        var viewTargetLocation = viewTarget.GetActorLocation();

        Query.ForEachEntity((ref marker, entity) =>
        {
            if (marker.MarkerActor == null)
                return;

            var pawn = entity.HasComponent<LocalMainCharacterComponent>() ? new MainCharacterEntity(entity).Pawn : new TamerEntity(entity).Pawn;

            if (!pawn.IsNullOrDestroyed())
            {
                var location = pawn.GetActorLocation();
                var distance = FVector.Dist2D(viewTargetLocation, location);
                var coefficient = Math.Min(distance / Constants.MaxMarkerHeightDistance, 1);
                var markerHeight = pawn.CapsuleComponent.GetScaledCapsuleHalfHeight() * (1 + Constants.BaseMarkerHeightCoefficient + coefficient);
                marker.MarkerActor.SetActorLocation(location + new FVector(0, 0, markerHeight), false, out _, true);
            }
        });
    }
}