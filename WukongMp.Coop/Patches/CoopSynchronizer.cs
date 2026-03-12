using b1;
using Friflo.Engine.ECS;
using Microsoft.Extensions.Logging;
using ReadyM.Api.ECS.Worlds;
using ReadyM.Api.Idents;
using ReadyM.Api.Multiplayer.Client;
using ReadyM.Api.Multiplayer.ECS.Components;
using ReadyM.Api.Multiplayer.ECS.Jobs;
using ReadyM.Api.Multiplayer.ECS.Managers;
using ReadyM.Api.Multiplayer.ECS.Registry;
using ReadyM.Api.Multiplayer.Mapping.Data;
using ReadyM.Api.Multiplayer.Mapping.Events;
using ReadyM.Relay.Client;
using ReadyM.Relay.Client.State;
using UnrealEngine.Engine;
using WukongMp.Api;
using WukongMp.Api.Configuration;
using WukongMp.Api.ECS.Archetypes;
using WukongMp.Api.ECS.Components;
using WukongMp.Api.ECS.Entities;
using WukongMp.Api.ECS.Jobs;
using WukongMp.Api.ECS.Managers;
using WukongMp.Api.FreeCamera;
using WukongMp.Api.State;
using WukongMp.Api.UI;
using WukongMp.Api.WukongUtils;

namespace WukongMp.Coop.Patches;

public class CoopSynchronizer : WukongSynchronizer
{
    private readonly WukongPlayerState _playerState;
    private readonly WukongPawnState _pawnState;
    private readonly DiscoverLocallySpawnedMonstersJob _discoverLocallySpawnedMonstersJob;

    internal CoopSynchronizer(
        ArchetypeEventRouter archetypeEvent,
        ClientState state,
        ClientWukongArchetypeRegistration wukongArchetype,
        Store world,
        IComponentFieldMappingRegistry mappedField,
        WukongAreaState areaState,
        WukongPawnState pawnState,
        WukongPlayerState playerState,
        WukongPlayerPawnState playerPawnState,
        WukongPlayerModeManager modeManager,
        NetworkedEntityManager netManager,
        ClientOwnershipManager clientOwnership,
        IMappedEventManager mappedEvent,
        JobRegistry jobRegistry,
        INetworkedComponentRegistry netComponentRegistry,
        IRelayClient relayClient,
        IClientEcsUpdateLoop ecsLoop,
        WukongEventBus eventBus,
        WukongWidgetManager widgetManager,
        GameplayEventRouter gameplayEventRouter,
        GameplayConfiguration configuration,
        FreeCameraManager freeCameraManager,
        FreeCameraController freeCameraController,
        ILogger logger)
        : base(
            archetypeEvent,
            state,
            wukongArchetype,
            world,
            mappedField,
            areaState,
            playerState,
            playerPawnState,
            modeManager,
            netManager,
            clientOwnership,
            jobRegistry,
            netComponentRegistry,
            relayClient,
            ecsLoop,
            mappedEvent,
            eventBus,
            widgetManager,
            gameplayEventRouter,
            configuration,
            freeCameraManager,
            freeCameraController,
            logger)
    {
        _playerState = playerState;
        _pawnState = pawnState;
        _discoverLocallySpawnedMonstersJob = new DiscoverLocallySpawnedMonstersJob(mappedEvent, logger);
    }

    internal void Initialize()
    {
        State.OnJoinedArea += OnJoinedAreaHandler;
        JobRegistry.OnApplySnapshot += OnApplySnapshot;
        PlayerPawnState.OnPlayerPawnSpawned += OnPlayerPawnSpawned;
        _playerState.OnMainCharacterEntityInitialized += OnMainCharacterEntityInitialized;
    }

    protected override void OnDispose()
    {
        State.OnJoinedArea -= OnJoinedAreaHandler;
        JobRegistry.OnApplySnapshot -= OnApplySnapshot;
        PlayerPawnState.OnPlayerPawnSpawned -= OnPlayerPawnSpawned;
        _playerState.OnMainCharacterEntityInitialized -= OnMainCharacterEntityInitialized;

        base.OnDispose();
    }

    private void OnPlayerPawnSpawned(MainCharacterEntity mainCharacterEntity, BGUCharacterCS pawn)
    {
        const string whiteTeamColor = "(R=0.9,G=0.9,B=0.9)";
        var marker = MarkerUtils.CreateMarkerForCharacter(mainCharacterEntity, whiteTeamColor); // 3D marker above player
        if (marker == null)
        {
            Logger.LogError("Failed to create marker for player {PlayerId}.", mainCharacterEntity.GetState().CharacterNickName);
        }
    }

    private void OnMainCharacterEntityInitialized(MainCharacterEntity mainCharacterEntity)
    {
        // check if we are in the Pagoda
        var areaActors = UGameplayStatics.GetAllActorsOfClass<BGUIntervalArea>(GameUtils.GetWorld());
        foreach (var area in areaActors)
        {
            var comp = area.GetComponent<BUS_IntervalTriggerImpl>();
            if (comp != null)
            {
                var eligible = comp.CurrentState is BUS_IntervalTriggerImpl.IntervalTriggerEnableState;
                mainCharacterEntity.GetState().BeguilingChantEligible = eligible;
                return;
            }
        }
    }

    private void OnJoinedAreaHandler(AreaId areaId, Entity entity)
    {
        var isFirst = AreaState.IsMasterClient;

        Logger.LogInformation("Joined area {AreaId}, is master client: {IsMasterClient}", areaId, isFirst);

        if (isFirst)
        {
            TamerUtils.DiscoverTamers(_pawnState);
        }
    }

    private void OnApplySnapshot()
    {
        // TODO: Probably can be deleted.
        World.Query<LocalTamerComponent, MetadataComponent>().ForEachEntity((ref _, ref _, entity) => _discoverLocallySpawnedMonstersJob.OnUpdate(entity));
    }
}