using ReadyM.Relay.Common.ECS;
using ReadyM.Relay.Common.Wukong.Components;
using UnrealEngine.Runtime;

namespace WukongMp.Api.ECS.Systems;

public sealed class UpdateMarkersSystem : SystemBase
{
    public override void OnUpdate()
    {
        Entities.ForEach((EntityId _,
            ref LocalTamerComponent tamer,
            ref MarkerComponent marker,
            ref TranslationComponent trans) =>
        {
            if (marker.MarkerActor == null)
                return;

            if (tamer.Pawn == null)
            {
                Logging.LogError("Pawn is null");
                return;
            }

            var markerHeight = tamer.Pawn.CapsuleComponent.GetScaledCapsuleHalfHeight() * 1.1f;
            marker.MarkerActor.SetActorLocation(trans.Position.ToFVector() + new FVector(0, 0, markerHeight), false, out var _, true);
        });
    }
}