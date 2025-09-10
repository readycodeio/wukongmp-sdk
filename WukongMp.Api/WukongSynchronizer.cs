using b1;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using Microsoft.Extensions.Logging;
using ReadyM.Api.ECS.Worlds;
using ReadyM.Api.Multiplayer.Client;
using ReadyM.Api.Multiplayer.ECS.Components;
using ReadyM.Api.Multiplayer.ECS.Managers;
using ReadyM.Api.Multiplayer.ECS.Registry;
using ReadyM.Api.Multiplayer.Idents;
using ReadyM.Relay.Client;
using ReadyM.Relay.Client.State;
using ReadyM.Relay.Common.ECS.Archetypes;
using ReadyM.Relay.Common.ECS.Jobs;
using WukongMp.Api.ECS.Archetypes;
using WukongMp.Api.ECS.Components;
using WukongMp.Api.ECS.Managers;
using WukongMp.Api.ECS.Systems;
using WukongMp.Api.ECS.Systems.MainCharacters;
using WukongMp.Api.ECS.Systems.Tamers;
using WukongMp.Api.State;
using WukongMp.Api.UI;
using WukongMp.Api.WukongUtils;

namespace WukongMp.Api;

public class WukongSynchronizer : ClientNetworkedStateSynchronizer
{
    private readonly WukongAreaState _areaState;
    private readonly SystemGroup _syncGroup;
    private readonly ClientWukongArchetypeRegistration _wukongArchetype;
    private readonly ClientState _state;

    public WukongSynchronizer(
        ArchetypeEventRouter archetypeEvent,
        ClientState state,
        ClientWukongArchetypeRegistration wukongArchetype,
        DefaultPlayerArchetypeRegistration playerArchetype,
        Store world,
        WukongAreaState areaState,
        WukongPlayerState playerState,
        WukongPlayerPawnState playerPawnState,
        WukongPlayerModeManager modeManager,
        NetworkedEntityManager netManager,
        ClientOwnershipManager clientOwnership,
        JobRegistry jobRegistry,
        INetworkedComponentRegistry netComponentRegistry,
        IRelayClient relayClient,
        IClientEcsUpdateLoop ecsLoop,
        WukongEventBus eventBus,
        WukongWidgetManager widgetManager,
        WukongRpcCallbacks rpc,
        ILogger logger)
        : base(netManager, state, jobRegistry, netComponentRegistry, relayClient, ecsLoop, clientOwnership, logger)
    {
        _areaState = areaState;
        _wukongArchetype = wukongArchetype;
        _state = state;

        State.OnJoinedArea += OnJoinedAreaHandler;

        _syncGroup = new SystemGroup("Sync");

        _syncGroup.Add(new SpawnTamersSystem(state));
        _syncGroup.Add(new OnTamerDeadSystem());
        _syncGroup.Add(new SyncTamersSystem());
        _syncGroup.Add(new UpdateTamerMarkersSystem());
        _syncGroup.Add(new ScaleMonsterHpSystem());
        _syncGroup.Add(new SyncMonsterTeamSystem());
        _syncGroup.Add(new ChangeTamerTargetSystem());

        _syncGroup.Add(new CreateLocalMainCharacterEntitySystem(state, playerState, eventBus, Logger));
        _syncGroup.Add(new SpawnOtherMainCharactersSystem(state, playerState, playerPawnState, eventBus, clientOwnership, Logger));
        _syncGroup.Add(new DespawnOtherMainCharactersSystem(archetypeEvent, playerState, wukongArchetype, playerPawnState, widgetManager, eventBus, Logger));
        _syncGroup.Add(new SyncMainCharactersSystem(playerState, modeManager, eventBus, Logger));
        _syncGroup.Add(new RespawnMainCharacterSystem(areaState, playerState, rpc, Logger));

        _syncGroup.Add(new SyncPlayersSystem(playerState, modeManager));

        _syncGroup.SetMonitorPerf(true);
        EcsLoop.AddSystem(_syncGroup);
    }

    protected override void OnDispose()
    {
        State.OnJoinedArea -= OnJoinedAreaHandler;

        EcsLoop.RemoveSystem(_syncGroup);
        base.OnDispose();
    }

    protected override void OnOwnershipChanged(Entity entity)
    {
        var meta = entity.GetComponent<MetadataComponent>();

        if (meta.Archetype == _wukongArchetype.MonsterArchetype)
        {
            OnMonsterOwned(entity, meta);
        }
    }

    private void OnMonsterOwned(Entity entity, MetadataComponent meta)
    {
        // if we are now the owner of a monster, we must re-enable its AI
        var localTamerComp = entity.GetComponent<LocalTamerComponent>();

        if (!localTamerComp.IsMonsterSynced)
            return;

        if (localTamerComp.Tamer == null)
        {
            Logging.LogError("LocalTamerComponent.Tamer is null for entity {EntityId}", meta.NetId);
            return;
        }

        var events = BUS_EventCollectionCS.Get(localTamerComp.Tamer);
        if (events == null)
        {
            Logging.LogError("events are null");
            return;
        }

        if (meta.Owner == _state.LocalPlayerId)
        {
            events.Evt_AIPauseBT.Invoke(false);
            events.Evt_AIPerceptionSetting.Invoke(true);
            Logging.LogDebug("Tamer actor enabled, guid: {Guid}.", BGU_DataUtil.GetActorGuid(localTamerComp.Tamer));
        }
    }

    private void OnJoinedAreaHandler(AreaId areaId, Entity entity)
    {
        if (_areaState.IsMasterClient)
        {
            TamerUtils.DiscoverTamers();
        }
    }
}