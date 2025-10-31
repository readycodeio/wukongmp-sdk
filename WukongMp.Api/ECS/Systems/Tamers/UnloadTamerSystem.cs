using b1;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using ReadyM.Relay.Common.Wukong.ECS.Components;
using WukongMp.Api.ECS.Components;

namespace WukongMp.Api.ECS.Systems.Tamers;

public sealed class UnloadTamersSystem() : QuerySystem<TamerComponent, LocalTamerComponent>
{
    protected override void OnUpdate()
    {
        Query.ForEachEntity((
            ref TamerComponent tamerComp, 
            ref LocalTamerComponent localTamerComp,
            Entity entity) =>
        {
            if (!localTamerComp.IsTamerSynced || localTamerComp.Tamer == null)
            {
                return;
            }

            if (localTamerComp.IsMonsterActive && !tamerComp.ShouldBeSpawned && localTamerComp.Tamer.CurrentRef.Phase != ETamerPhase.Loaded)
            {
                localTamerComp.Tamer.CurrentRef.TurnBack2Loaded();
            }
        });
    }
}
