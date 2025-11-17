using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using ReadyM.Relay.Common.Wukong.ECS.Components;
using UnrealEngine.Runtime;
using WukongMp.Api.Configuration;
using WukongMp.Api.ECS.Components;

namespace WukongMp.Api.ECS.Systems.Tamers;

// FIXME: In the future this should support both TamerEntities and MainCharacterEntities
public sealed class UpdateTamerMarkersSystem : QuerySystem<LocalTamerComponent, MarkerComponent, TranslationComponent, NicknameComponent, TamerComponent>
{
    protected override void OnUpdate()
    {
        Query.ForEachEntity((
            ref localTamerComp,
            ref markerComp,
            ref transComp,
            ref nameComp,
            ref tamerComp, _) =>
        {
            if (markerComp.MarkerActor == null)
                return;

            if (localTamerComp.Tamer != null)
            {
                var markerHeight = localTamerComp.Tamer.CapsuleComponent.GetScaledCapsuleHalfHeight() * 1.1f;
                markerComp.MarkerActor.SetActorLocation(transComp.Position.ToFVector() + new FVector(0, 0, markerHeight), false, out var _, true);
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