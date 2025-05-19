using b1;
using ReadyM.Relay.Common.ECS;
using ReadyM.Relay.Common.Wukong.Components;
using WukongMp.Api.Patches;

namespace WukongMp.Api.ECS.Systems;

public sealed class DestroyDeadMonstersMarkersSystem : SystemBase
{
    public override void OnUpdate()
    {
        Entities.ForEach((
            EntityId entityId,
            ref HpComponent hpComp,
            ref LocalTamerComponent tamer,
            ref MarkerComponent marker) =>
        {
            if (tamer.IsMonsterSpawned && hpComp.Hp <= 0 && !marker.DestroyQueued)
            {
                Logging.LogDebug("Monster {Id} died", entityId);
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