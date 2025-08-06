using Friflo.Engine.ECS.Systems;
using ReadyM.Relay.Common.Wukong.ECS.Components;
using UnrealEngine.Runtime;
using WukongMp.Api.ECS.Components;

namespace WukongMp.Api.ECS.Systems;

public sealed class UpdateMarkersSystem : QuerySystem<LocalTamerComponent, MarkerComponent, TranslationComponent, NicknameComponent, TamerComponent>
{
    protected override void OnUpdate()
    {
        Query.ForEachEntity((ref tamer, ref marker, ref trans, ref name, ref tam, _) =>
        {
            if (marker.MarkerActor == null)
                return;

            if (tamer.Tamer != null)
            {
                var markerHeight = tamer.Tamer.CapsuleComponent.GetScaledCapsuleHalfHeight() * 1.1f;
                marker.MarkerActor.SetActorLocation(trans.Position.ToFVector() + new FVector(0, 0, markerHeight), false, out var _, true);
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