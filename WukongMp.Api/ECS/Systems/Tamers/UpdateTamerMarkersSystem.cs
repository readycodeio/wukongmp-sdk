using Friflo.Engine.ECS.Systems;
using System;
using UnrealEngine.Runtime;
using WukongMp.Api.Configuration;
using WukongMp.Api.ECS.Components;
using WukongMp.Api.ECS.Entities;
using WukongMp.Api.WukongUtils;

namespace WukongMp.Api.ECS.Systems.Tamers;

// FIXME: In the future this should support both TamerEntities and MainCharacterEntities
public sealed class UpdateTamerMarkersSystem : QuerySystem<MarkerComponent>
{
    private const string RedTeamColor = "(R=1,G=0.3,B=0.3)";
    private const string BlueTeamColor = "(R=0.3,G=0.3,B=1)";

    protected override void OnUpdate()
    {
        var localPlayerController = GameUtils.GetPlayerController();
        if (localPlayerController == null)
            return;
        var viewTarget = localPlayerController.GetViewTarget();
        var viewTargetLocation = viewTarget.GetActorLocation();

        Query.ForEachEntity((
            ref markerComp,
            entity) =>
        {
            var tamerEntity = new TamerEntity(entity);
            
            if (markerComp.MarkerActor == null)
                return;

            var tamer = tamerEntity.Tamer;
            var pawn = tamerEntity.Pawn;
            
            if (tamer != null && pawn != null)
            {
                var location = pawn.GetActorLocation();
                var distance = FVector.Dist2D(viewTargetLocation, location);
                var coefficient = Math.Min(distance / Constants.MaxMarkerHeightDistance, 1);
                var markerHeight = tamer.CapsuleComponent.GetScaledCapsuleHalfHeight() * (1 + Constants.BaseMarkerHeightCoefficient + coefficient);
                markerComp.MarkerActor.SetActorLocation(location + new FVector(0, 0, markerHeight), false, out _, true);
            }
#if !TESTING
            string title = tamer?.GetClass()?.GetName() ?? "";
            if (pawn != null)
            {
                markerComp.MarkerActor.CallFunctionByNameWithArguments($"SetText {title} {BlueTeamColor}", true);
            }
            else if (tamer != null)
            {
                markerComp.MarkerActor.CallFunctionByNameWithArguments($"SetText {title} {RedTeamColor}", true);
            }
#endif
        });
    }
}