using b1;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using ReadyM.Api.Multiplayer.ECS.Components;
using ReadyM.Relay.Common.Wukong.ECS.Components;
using WukongMp.Api.ECS.Components;

namespace WukongMp.Api.ECS.Systems.Tamers;

public sealed class MonsterLifeSystem(WukongRpcCallbacks rpc) : QuerySystem<LocalTamerComponent, TamerComponent, MetadataComponent>
{
    protected override void OnUpdate()
    {
        Query.ForEachEntity((
            ref LocalTamerComponent localTamerComp,
            ref TamerComponent tamerComp,
            ref MetadataComponent metaComp,
            Entity entity) =>
        {
            if (!localTamerComp.IsTamerSynced || localTamerComp.Tamer == null)
                return;

            var phase = localTamerComp.Tamer.CurrentRef.Phase;
            if (phase == ETamerPhase.Spawned && !localTamerComp.IsLocallySpawned)
            {
                Logging.LogDebug("Monster {Guid} waking up locally", BGU_DataUtil.GetActorGuid(localTamerComp.Tamer));
                localTamerComp.IsLocallySpawned = true;
                rpc.SendUnitSpawned(metaComp.NetId);
            }
            else if (phase != ETamerPhase.Spawned && localTamerComp.IsLocallySpawned)
            {
                Logging.LogDebug("Monster {Guid} unloaded locally", BGU_DataUtil.GetActorGuid(localTamerComp.Tamer));
                localTamerComp.IsLocallySpawned = false;
                rpc.SendUnitDespawn(metaComp.NetId);
            }
        });
    }
}
