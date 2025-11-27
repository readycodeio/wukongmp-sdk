using Friflo.Engine.ECS.Systems;
using ReadyM.Relay.Common.Wukong.ECS.Components;
using System;
using UnrealEngine.Runtime;
using WukongMp.Api.Configuration;
using WukongMp.Api.ECS.Components;
using WukongMp.Api.State;

namespace WukongMp.Api.ECS.Systems.MainCharacters;

public class UpdateMainCharacterMarkerSystem(WukongPlayerState playerState) : QuerySystem<LocalMainCharacterComponent, MainCharacterComponent>
{
    protected override void OnUpdate()
    {
        var localPlayerPosition = playerState.LocalMainCharacter?.GetLocalState().Pawn?.GetActorLocation() ?? new FVector(0, 0, 0);

        Query.ForEachEntity((ref localMainComp, ref mainComp, _) =>
        {
            if (localMainComp.MarkerActor == null)
                return;

            if (localMainComp.HasPawn)
            {
                var location = localMainComp.Pawn!.GetActorLocation();
                var distance = FVector.Dist2D(localPlayerPosition, location);
                var coefficient = Math.Min(distance / Constants.MaxMarkerHeightDistance, 1);
                var markerHeight = localMainComp.Pawn!.CapsuleComponent.GetScaledCapsuleHalfHeight() * (1 + Constants.BaseMarkerHeightCoefficient + coefficient);
                localMainComp.MarkerActor.SetActorLocation(localMainComp.Pawn!.GetActorLocation() + new FVector(0, 0, markerHeight), false, out var _, true);
            }
        });
    }
}
