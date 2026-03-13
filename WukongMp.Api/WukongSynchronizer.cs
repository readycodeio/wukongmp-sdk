using b1;
using Friflo.Engine.ECS;
using Microsoft.Extensions.Logging;
using ReadyM.Api.Multiplayer.Client;
using ReadyM.Api.Multiplayer.ECS.Components;
using ReadyM.Api.Multiplayer.ECS.Jobs;
using ReadyM.Api.Multiplayer.ECS.Managers;
using ReadyM.Api.Multiplayer.ECS.Registry;
using ReadyM.Relay.Client;
using ReadyM.Relay.Client.State;
using WukongMp.Api.ECS.Archetypes;
using WukongMp.Api.ECS.Entities;

namespace WukongMp.Api;

internal sealed class WukongSynchronizer(
    ClientState state,
    ClientWukongArchetypeRegistration wukongArchetype,
    NetworkedEntityManager netManager,
    ClientOwnershipManager clientOwnership,
    JobRegistry jobRegistry,
    INetworkedComponentRegistry netComponentRegistry,
    IRelayClient relayClient,
    IClientEcsUpdateLoop ecsLoop,
    ILogger logger
)
    : ClientNetworkedStateSynchronizer(netManager, state, jobRegistry, netComponentRegistry, relayClient, ecsLoop, clientOwnership, logger)
{
    protected override void OnOwnershipChanged(Entity entity)
    {
        var meta = entity.GetComponent<MetadataComponent>();

        if (meta.Archetype == wukongArchetype.TamerArchetype)
        {
            OnMonsterOwned(new TamerEntity(entity), meta);
        }
    }

    private void OnMonsterOwned(TamerEntity tamerEntity, MetadataComponent meta)
    {
        // if we are now the owner of a monster, we must re-enable its AI
        var localTamerComp = tamerEntity.GetLocalTamer();

        if (!localTamerComp.IsMonsterActive)
            return;

        if (tamerEntity.Tamer == null)
        {
            Logging.LogError("LocalTamerComponent.Tamer is null for entity {EntityId}", meta.NetId);
            return;
        }

        var events = BUS_EventCollectionCS.Get(tamerEntity.Tamer);
        if (events == null)
        {
            Logging.LogError("events are null");
            return;
        }

        if (meta.Owner == State.LocalPlayerId)
        {
            var tamerComp = tamerEntity.GetTamer();
            if (!tamerComp.HasFsmPaused)
            {
                events.Evt_AIPauseBT.Invoke(false);
                events.Evt_AIPauseFsm.Invoke(false);
                events.Evt_AIPerceptionSetting.Invoke(true);
                Logging.LogDebug("Tamer actor enabled, guid: {Guid}.", BGU_DataUtil.GetActorGuid(tamerEntity.Tamer));
            }

            if (tamerComp.Guid == "UGuid.HYS.JiRuHuo01")
            {
                events.Evt_DisablePhysicalMove.Invoke(false);
                var monster = tamerEntity.Tamer.GetMonster();
                monster?.Mesh?.SetSimulatePhysics(true);
            }
        }
    }
}