using Friflo.Engine.ECS.Systems;
using ReadyM.Relay.Common.Wukong.ECS.Components;
using System;
using UnrealEngine.Runtime;
using WukongMp.Api.Configuration;
using WukongMp.Api.ECS.Components;
using WukongMp.Api.State;
using WukongMp.Api.WukongUtils;

namespace WukongMp.Api.ECS.Systems.MainCharacters;

public class UpdateMainCharacterMarkerSystem() : QuerySystem<LocalMainCharacterComponent, MainCharacterComponent>
{
    protected override void OnUpdate()
    {
        var localPlayerController = GameUtils.GetPlayerController();
        if (localPlayerController == null)
            return;
        var viewTarget = localPlayerController.GetViewTarget();
        var viewTargetLocation = viewTarget.GetActorLocation();

        Query.ForEachEntity((ref localMainComp, ref mainComp, _) =>
        {
            if (localMainComp.MarkerActor == null)
                return;

            if (localMainComp.HasPawn)
            {
                var location = localMainComp.Pawn!.GetActorLocation();
                var distance = FVector.Dist2D(viewTargetLocation, location);
                var coefficient = Math.Min(distance / Constants.MaxMarkerHeightDistance, 1);
                var markerHeight = localMainComp.Pawn!.CapsuleComponent.GetScaledCapsuleHalfHeight() * (1 + Constants.BaseMarkerHeightCoefficient + coefficient);
                localMainComp.MarkerActor.SetActorLocation(location + new FVector(0, 0, markerHeight), false, out var _, true);
            }
        });
    }
}
