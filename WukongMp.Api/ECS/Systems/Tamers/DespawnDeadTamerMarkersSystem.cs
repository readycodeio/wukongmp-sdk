using b1;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using ReadyM.Relay.Common.Wukong.ECS.Components;
using WukongMp.Api.ECS.Components;
using WukongMp.Api.Patches;

namespace WukongMp.Api.ECS.Systems;

public sealed class DespawnDeadTamerMarkersSystem : QuerySystem<HpComponent, LocalTamerComponent, MarkerComponent>
{
    protected override void OnUpdate()
    {
        Query.ForEachEntity((
            ref HpComponent hpComp,
            ref LocalTamerComponent localTamerComp,
            ref MarkerComponent markerComp,
            Entity entity) =>
        {
            if (localTamerComp.IsMonsterSynced && hpComp.Hp <= 0 && !markerComp.DestroyQueued)
            {
                Logging.LogDebug("Monster {Id} died, destroying marker", entity.Id);
                markerComp.DestroyQueued = true;

                var markerActor = markerComp.MarkerActor;
                if (markerActor != null)
                {
                    GameLoopPatch.QueueOnGameThread(() =>
                    {
                        // NOTE: Could have been destroyed since the moment the action was scheduled
                        if (!markerActor.IsNullOrDestroyed()) BGU_UnrealWorldUtil.DestroyActor(markerActor);
                    }, "DestroyMarkerActor");
                }
            }
        });
    }
}