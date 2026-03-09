using b1;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using Microsoft.Extensions.Logging;
using ReadyM.Api.ECS.Worlds;
using ReadyM.Api.Multiplayer.Client;
using ReadyM.Api.Multiplayer.ECS.Components;
using ReadyM.Api.Multiplayer.ECS.Jobs;
using ReadyM.Api.Multiplayer.ECS.Managers;
using ReadyM.Api.Multiplayer.ECS.Registry;
using ReadyM.Api.Multiplayer.Mapping.Data;
using ReadyM.Api.Multiplayer.Mapping.Events;
using ReadyM.Relay.Client;
using ReadyM.Relay.Client.State;
using WukongMp.Api.Configuration;
using WukongMp.Api.ECS.Archetypes;
using WukongMp.Api.ECS.Entities;
using WukongMp.Api.ECS.Managers;
using WukongMp.Api.ECS.Systems;
using WukongMp.Api.ECS.Systems.MainCharacters;
using WukongMp.Api.ECS.Systems.Tamers;
using WukongMp.Api.FreeCamera;
using WukongMp.Api.Mapping;
using WukongMp.Api.State;
using WukongMp.Api.UI;

namespace WukongMp.Api;

public class WukongSynchronizer : ClientNetworkedStateSynchronizer
{
    protected readonly WukongAreaState AreaState;
    protected readonly WukongPlayerPawnState PlayerPawnState;
    protected readonly ClientWukongArchetypeRegistration WukongArchetype;
    protected readonly Store World;
    private readonly SystemGroup _syncGroup;
    private readonly ClientState _state;

    public WukongSynchronizer(
        ArchetypeEventRouter archetypeEvent,
        ClientState state,
        ClientWukongArchetypeRegistration wukongArchetype,
        Store world,
        IComponentFieldMappingRegistry mappedField,
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
        IMappedEventManager mappedEvent,
        WukongEventBus eventBus,
        WukongWidgetManager widgetManager,
        GameplayEventRouter gameplayEventRouter,
        GameplayConfiguration configuration,
        FreeCameraManager freeCameraManager,
        FreeCameraController freeCameraController,
        ILogger logger)
        : base(netManager, state, jobRegistry, netComponentRegistry, relayClient, ecsLoop, clientOwnership, logger)
    {
        AreaState = areaState;
        PlayerPawnState = playerPawnState;
        WukongArchetype = wukongArchetype;
        _state = state;
        World = world;

        _syncGroup = new SystemGroup("Sync");

        _syncGroup.Add(new SpawnTamersSystem(state, gameplayEventRouter, configuration));
        _syncGroup.Add(new SyncTamersSystem(mappedEvent));
        _syncGroup.Add(new UnloadTamersSystem());
        _syncGroup.Add(new KillAlreadyDeadMonstersSystem(clientOwnership, playerState));
        _syncGroup.Add(new UpdateTamerMarkersSystem());

        _syncGroup.Add(new SyncMonsterTeamSystem());
        _syncGroup.Add(new ChangeTamerTargetSystem(clientOwnership));

        _syncGroup.Add(new CreateLocalMainCharacterEntitySystem(state, playerState, eventBus, Logger));
        _syncGroup.Add(new SpawnOtherMainCharactersSystem(state, playerState, playerPawnState, eventBus, clientOwnership, Logger));
        // _syncGroup.Add(new DeleteOrphanedMainCharactersSystem(state, playerState, eventBus, policyDir, clientOwnership, Logger));
        _syncGroup.Add(new DespawnOtherMainCharactersSystem(archetypeEvent, playerState, wukongArchetype, playerPawnState, eventBus, Logger));
        _syncGroup.Add(new SyncMainCharactersSystem(playerState, modeManager, eventBus, configuration, gameplayEventRouter, mappedField, logger));
        _syncGroup.Add(new EnableCollisionAfterCutsceneSystem(playerState));
        _syncGroup.Add(new UpdateMainCharacterMarkerSystem());
        _syncGroup.Add(new UpdateCooldownSystem(playerState, eventBus, areaState));
        _syncGroup.Add(new FreeCameraMovementSystem(eventBus, freeCameraManager, freeCameraController));
        _syncGroup.Add(new AfterMainCharacterDeathSystem(eventBus, playerState));

        _syncGroup.Add(new DebugViewSystem(eventBus, widgetManager));

        _syncGroup.SetMonitorPerf(true);
        EcsLoop.AddSystem(_syncGroup);
    }

    protected override void OnDispose()
    {
        EcsLoop.RemoveSystem(_syncGroup);
        base.OnDispose();
    }

    protected override void OnOwnershipChanged(Entity entity)
    {
        var meta = entity.GetComponent<MetadataComponent>();

        if (meta.Archetype == WukongArchetype.TamerArchetype)
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

        if (meta.Owner == _state.LocalPlayerId)
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