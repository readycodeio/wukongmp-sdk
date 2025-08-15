using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using ReadyM.Relay.Common.Wukong.ECS.Components;
using UnrealEngine.Runtime;
using WukongMp.Api.ECS.Components;

namespace WukongMp.Api.ECS.Systems;

// FIXME: In the future this should support both TamerEntities and MainCharacterEntities
public sealed class UpdateTamerMarkersSystem : QuerySystem<LocalTamerComponent, MarkerComponent, TranslationComponent, NicknameComponent, TamerComponent>
{
    protected override void OnUpdate()
    {
        Query.ForEachEntity((
            ref LocalTamerComponent localTamerComp,
            ref MarkerComponent markerComp,
            ref TranslationComponent transComp,
            ref NicknameComponent nameComp,
            ref TamerComponent tamerComp,
            Entity _) =>
        {
            if (markerComp.MarkerActor == null)
                return;

            if (localTamerComp.Tamer != null)
            {
                var markerHeight = localTamerComp.Tamer.CapsuleComponent.GetScaledCapsuleHalfHeight() * 1.1f;
                markerComp.MarkerActor.SetActorLocation(transComp.Position.ToFVector() + new FVector(0, 0, markerHeight), false, out var _, true);
            }
#if TESTING
            string title = tamer.Tamer?.GetClass().GetName() ?? "";
            if (tamer.Pawn != null)
            {
                marker.MarkerActor.CallFunctionByNameWithArguments($"SetText {title} {Constants.BlueTeamColor}", true);
            }
            else if (tamer.Tamer != null)
            {
                marker.MarkerActor.CallFunctionByNameWithArguments($"SetText {title} {Constants.RedTeamColor}", true);
            }
#endif
        });
    }
}