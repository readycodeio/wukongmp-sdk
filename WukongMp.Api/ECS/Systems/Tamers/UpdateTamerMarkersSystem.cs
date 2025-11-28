using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using ReadyM.Relay.Common.Wukong.ECS.Components;
using System;
using UnrealEngine.Runtime;
using WukongMp.Api.Configuration;
using WukongMp.Api.ECS.Components;
using WukongMp.Api.WukongUtils;

namespace WukongMp.Api.ECS.Systems.Tamers;

// FIXME: In the future this should support both TamerEntities and MainCharacterEntities
public sealed class UpdateTamerMarkersSystem : QuerySystem<LocalTamerComponent, MarkerComponent, TranslationComponent, NicknameComponent, TamerComponent>
{
    protected override void OnUpdate()
    {
        var localPlayerController = GameUtils.GetPlayerController();
        if (localPlayerController == null)
            return;
        var viewTarget = localPlayerController.GetViewTarget();
        var viewTargetLocation = viewTarget.GetActorLocation();

        Query.ForEachEntity((
            ref localTamerComp,
            ref markerComp,
            ref transComp,
            ref nameComp,
            ref tamerComp, _) =>
        {
            if (markerComp.MarkerActor == null)
                return;

            if (localTamerComp.Tamer != null && localTamerComp.Pawn != null)
            {
                var location = localTamerComp.Pawn.GetActorLocation();
                var distance = FVector.Dist2D(viewTargetLocation, location);
                var coefficient = Math.Min(distance / Constants.MaxMarkerHeightDistance, 1);
                var markerHeight = localTamerComp.Tamer.CapsuleComponent.GetScaledCapsuleHalfHeight() * (1 + Constants.BaseMarkerHeightCoefficient + coefficient);
                markerComp.MarkerActor.SetActorLocation(location + new FVector(0, 0, markerHeight), false, out var _, true);
            }
#if TESTING
            string title = localTamerComp.Tamer?.GetClass()?.GetName() ?? "";
            if (localTamerComp.Pawn != null)
            {
                markerComp.MarkerActor.CallFunctionByNameWithArguments($"SetText {title} {Constants.BlueTeamColor}", true);
            }
            else if (localTamerComp.Tamer != null)
            {
                markerComp.MarkerActor.CallFunctionByNameWithArguments($"SetText {title} {Constants.RedTeamColor}", true);
            }
#endif
        });
    }
}