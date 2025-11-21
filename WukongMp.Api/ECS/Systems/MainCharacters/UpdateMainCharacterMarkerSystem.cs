using Friflo.Engine.ECS.Systems;
using ReadyM.Relay.Common.Wukong.ECS.Components;
using UnrealEngine.Runtime;
using WukongMp.Api.ECS.Components;

namespace WukongMp.Api.ECS.Systems.MainCharacters;

public class UpdateMainCharacterMarkerSystem() : QuerySystem<LocalMainCharacterComponent, MainCharacterComponent>
{
    protected override void OnUpdate()
    {
        Query.ForEachEntity((ref localMainComp, ref mainComp, _) =>
        {
            if (localMainComp.MarkerActor == null)
                return;

            if (localMainComp.HasPawn)
            {
                var markerHeight = localMainComp.Pawn!.CapsuleComponent.GetScaledCapsuleHalfHeight() * 1.1f;
                localMainComp.MarkerActor.SetActorLocation(mainComp.Location.ToFVector() + new FVector(0, 0, markerHeight), false, out var _, true);
            }
        });
    }
}
