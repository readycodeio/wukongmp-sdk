using System;
using Friflo.Engine.ECS.Systems;
using UnrealEngine.Runtime;
using WukongMp.Api.Configuration;
using WukongMp.Api.ECS.Components;
using WukongMp.Api.ECS.Entities;
using WukongMp.Api.WukongUtils;

namespace WukongMp.Api.ECS.Systems.Tamers;

// FIXME: In the future this should support both TamerEntities and MainCharacterEntities
internal sealed class UpdateTamerMarkersSystem : QuerySystem<LocalTamerComponent, MarkerComponent>
{
    protected override void OnUpdate()
    {
        var localPlayerController = GameUtils.GetPlayerController();
        if (localPlayerController == null)
            return;
        var viewTarget = localPlayerController.GetViewTarget();
        var viewTargetLocation = viewTarget.GetActorLocation();

        Query.ForEachEntity((
            ref _,
            ref markerComp,
            entity) =>
        {
            if (markerComp.MarkerActor == null)
                return;

            var tamerEntity = new TamerEntity(entity);
            
            if (tamerEntity.Tamer != null && tamerEntity.Pawn != null)
            {
                var location = tamerEntity.Pawn.GetActorLocation();
                var distance = FVector.Dist2D(viewTargetLocation, location);
                var coefficient = Math.Min(distance / Constants.MaxMarkerHeightDistance, 1);
                var markerHeight = tamerEntity.Tamer.CapsuleComponent.GetScaledCapsuleHalfHeight() * (1 + Constants.BaseMarkerHeightCoefficient + coefficient);
                markerComp.MarkerActor.SetActorLocation(location + new FVector(0, 0, markerHeight), false, out var _, true);
            }
#if TESTING
            // TODO: Duplicaetd in PvP module
            const string redTeamColor = "(R=1,G=0.3,B=0.3)";
            const string blueTeamColor = "(R=0.3,G=0.3,B=1)";
            
            string title = tamerEntity.Tamer?.GetClass()?.GetName() ?? "";
            if (tamerEntity.Pawn != null)
            {
                markerComp.MarkerActor.CallFunctionByNameWithArguments($"SetText {title} {blueTeamColor}", true);
            }
            else if (tamerEntity.Tamer != null)
            {
                markerComp.MarkerActor.CallFunctionByNameWithArguments($"SetText {title} {redTeamColor}", true);
            }
#endif
        });
    }
}