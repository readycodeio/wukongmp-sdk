using b1;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using ReadyM.Relay.Common.Wukong.ECS.Components;
using WukongMp.Api.ECS.Components;

namespace WukongMp.Api.ECS.Systems.Tamers;

public sealed class UnloadTamersSystem : QuerySystem<TamerComponent, LocalTamerComponent>
{
    protected override void OnUpdate()
    {
        Query.ForEachEntity((
            ref TamerComponent tamerComp, 
            ref LocalTamerComponent localTamerComp,
            Entity entity) =>
        {
            if (!localTamerComp.IsTamerSynced || localTamerComp.Tamer == null || localTamerComp.Tamer.CurrentRef == null || localTamerComp.Pawn == null)
            {
                return;
            }

            if (localTamerComp.IsMonsterActive
            && !localTamerComp.IsLocallySpawned
            && !tamerComp.ShouldBeSpawned
            && localTamerComp.Tamer.CurrentRef.Phase != ETamerPhase.Loaded
            && !BGUFunctionLibraryCS.BGUHasUnitState(localTamerComp.Pawn, EBGUUnitState.Dead)
            && !BGUFunctionLibraryCS.BGUHasUnitSimpleState(localTamerComp.Pawn, EBGUSimpleState.PendingDeathInAnimationSyncing))
            {
                localTamerComp.Tamer.CurrentRef.TurnBack2Loaded();
            }
        });
    }
}
