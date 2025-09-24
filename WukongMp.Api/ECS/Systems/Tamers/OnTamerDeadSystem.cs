using b1;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using ReadyM.Relay.Common.Wukong.ECS.Components;
using WukongMp.Api.ECS.Components;

namespace WukongMp.Api.ECS.Systems.Tamers;

public sealed class OnTamerDeadSystem : QuerySystem<HpComponent, LocalTamerComponent, MarkerComponent>
{
    protected override void OnUpdate()
    {
        Query.ForEachEntity((
            ref HpComponent hpComp,
            ref LocalTamerComponent localTamerComp,
            ref MarkerComponent markerComp,
            Entity entity) =>
        {
            // TODO: We set IsMonsterSynced = false on unit dead so this system never does anything
            if (localTamerComp.IsMonsterSynced && hpComp.Hp <= 0)
            {
                if (!markerComp.DestroyQueued)
                {
                    Logging.LogDebug("Monster {Id} died, destroying marker", entity.Id);
                    markerComp.DestroyQueued = true;

                    var markerActor = markerComp.MarkerActor;
                    if (!markerActor.IsNullOrDestroyed())
                    {
                        BGU_UnrealWorldUtil.DestroyActor(markerActor);
                    }
                }
            }
        });
    }
}