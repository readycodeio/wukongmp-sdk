using System;
using Friflo.Engine.ECS.Systems;
using Microsoft.Extensions.Logging;
using ReadyM.Api.Multiplayer.Mapping.Data;
using ReadyM.Api.Multiplayer.Mapping.Events;
using ReadyM.Relay.Client;
using ReadyM.Relay.Client.State;
using WukongMp.Api.Configuration;
using WukongMp.Api.ECS.Archetypes;
using WukongMp.Api.ECS.Managers;
using WukongMp.Api.ECS.Systems;
using WukongMp.Api.ECS.Systems.MainCharacters;
using WukongMp.Api.ECS.Systems.Tamers;
using WukongMp.Api.FreeCamera;
using WukongMp.Api.State;
using WukongMp.Api.UI;

namespace WukongMp.Api;

internal sealed class WukongSystemRegistration(
    ArchetypeEventRouter archetypeEvent,
    ClientState state,
    ClientWukongArchetypeRegistration wukongArchetype,
    IComponentFieldMappingRegistry mappedField,
    WukongAreaState areaState,
    WukongPlayerState playerState,
    WukongPlayerPawnState playerPawnState,
    WukongPlayerModeManager modeManager,
    ClientOwnershipManager clientOwnership,
    IClientEcsUpdateLoop ecsLoop,
    IMappedEventManager mappedEvent,
    WukongEventBus eventBus,
    WukongWidgetManager widgetManager,
    GameplayEventRouter gameplayEventRouter,
    GameplayConfiguration configuration,
    FreeCameraManager freeCameraManager,
    FreeCameraController freeCameraController,
    ILogger logger
) : IScopedLifetime, IDisposable
{
    private readonly SystemGroup _syncGroup = new("Synchronization")
    {
        new SpawnTamersSystem(state, gameplayEventRouter, configuration),
        new SyncTamersSystem(mappedEvent),
        new UnloadTamersSystem(),
        new KillAlreadyDeadMonstersSystem(clientOwnership, playerState),
        new SyncMonsterTeamSystem(),
        new ChangeTamerTargetSystem(clientOwnership),
        new CreateLocalMainCharacterEntitySystem(state, playerState, eventBus, mappedField, logger),
        new SpawnOtherMainCharactersSystem(state, playerState, playerPawnState, eventBus, clientOwnership, logger),
        new DespawnOtherMainCharactersSystem(archetypeEvent, playerState, wukongArchetype, playerPawnState, eventBus, logger),
        new SyncMainCharactersSystem(playerState, modeManager, eventBus, configuration, gameplayEventRouter, mappedField, logger),
        new EnableCollisionAfterCutsceneSystem(playerState),
        new UpdateMarkersSystem(),
        new UpdateCooldownSystem(playerState, eventBus, areaState),
        new FreeCameraMovementSystem(eventBus, freeCameraManager, freeCameraController),
        new AfterMainCharacterDeathSystem(eventBus, playerState),
        new DebugViewSystem(eventBus, widgetManager)
    };

    public void OnScopeStart()
    {
#if DEBUG
        _syncGroup.SetMonitorPerf(true);
#endif
        ecsLoop.AddSystem(_syncGroup);
    }

    public void Dispose()
    {
        ecsLoop.RemoveSystem(_syncGroup);
    }
}