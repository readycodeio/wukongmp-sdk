using b1;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using ReadyM.Relay.Common.ECS;
using ReadyM.Relay.Common.Wukong.Components;
using WukongMp.Api.Patches;

namespace WukongMp.Api.ECS.Systems;

public sealed class DestroyDeadMonstersMarkersSystem : QuerySystem<HpComponent, LocalTamerComponent, MarkerComponent>
{
    protected override void OnUpdate()
    {
        Query.ForEachEntity((
            ref hpComp,
            ref tamer,
            ref marker, entity) =>
        {
            if (tamer.IsMonsterSpawned && hpComp.Hp <= 0 && !marker.DestroyQueued)
            {
                Logging.LogDebug("Monster {Id} died", entity.Id);
                marker.DestroyQueued = true;

                var markerActor = marker.MarkerActor;
                if (markerActor != null)
                {
                    GameLoopPatch.QueueOnGameThread(() => { BGU_UnrealWorldUtil.DestroyActor(markerActor); }, "DestroyMarkerActor");
                }
            }
        });
    }
}