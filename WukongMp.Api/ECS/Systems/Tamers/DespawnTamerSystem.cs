using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using ReadyM.Relay.Common.Wukong.ECS.Components;
using WukongMp.Api.ECS.Components;
using WukongMp.Api.ECS.Entities;
using WukongMp.Api.WukongUtils;

namespace WukongMp.Api.ECS.Systems.Tamers;

public sealed class DespawnTamerSystem : QuerySystem<LocalTamerComponent, TamerComponent, MarkerComponent>
{
    protected override void OnUpdate()
    {
        Query.ForEachEntity((
            ref LocalTamerComponent localTamerComp,
            ref TamerComponent tamerComp,
            ref MarkerComponent markerComp,
            Entity entity) =>
        {
            if (localTamerComp.IsMonsterActive && !localTamerComp.IsLocallySpawned && !tamerComp.ShouldBeSpawned)
            {
                MarkerUtils.DestroyMarkerForCharacter(new TamerEntity(entity));
                localTamerComp.IsMonsterActive = false;
                Logging.LogDebug("Monster {Guid} despawned", tamerComp.Guid);
            }
        });
    }
}
