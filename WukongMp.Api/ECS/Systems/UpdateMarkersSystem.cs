using Friflo.Engine.ECS.Systems;
using ReadyM.Relay.Common.Wukong.Components;
using UnrealEngine.Runtime;
using WukongMp.Api.Old;

namespace WukongMp.Api.ECS.Systems;

public sealed class UpdateMarkersSystem : QuerySystem<LocalTamerComponent, MarkerComponent, TranslationComponent>
{
    protected override void OnUpdate()
    {
        Query.ForEachEntity((ref tamer, ref marker, ref trans, _) =>
        {
            if (marker.MarkerActor == null)
                return;

            var markerHeight = 0f;
            if (tamer.Pawn != null)
            {
                markerHeight = tamer.Pawn.CapsuleComponent.GetScaledCapsuleHalfHeight() * 1.1f;
                marker.MarkerActor.SetActorLocation(trans.Position.ToFVector() + new FVector(0, 0, markerHeight), false, out var _, true);
            }
            else if (tamer.Tamer != null)
            {
                markerHeight = tamer.Tamer.CapsuleComponent.GetScaledCapsuleHalfHeight() * 1.1f;
                marker.MarkerActor.SetActorLocation(trans.Position.ToFVector() + new FVector(0, 0, markerHeight), false, out var _, true);
            }
        });
    }
}