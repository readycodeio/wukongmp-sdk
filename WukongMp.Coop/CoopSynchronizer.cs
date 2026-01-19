using b1;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using HarmonyLib;
using Microsoft.Extensions.Logging;
using ReadyM.Api.ECS.Worlds;
using ReadyM.Api.Multiplayer.Client;
using ReadyM.Api.Multiplayer.ECS.Components;
using ReadyM.Api.Multiplayer.ECS.Managers;
using ReadyM.Api.Multiplayer.ECS.Registry;
using ReadyM.Api.Multiplayer.Idents;
using ReadyM.Relay.Client;
using ReadyM.Relay.Client.State;
using ReadyM.Relay.Common.ECS.Jobs;
using UnrealEngine.Engine;
using WukongMp.Api;
using WukongMp.Api.Configuration;
using WukongMp.Api.ECS.Archetypes;
using WukongMp.Api.ECS.Components;
using WukongMp.Api.ECS.Entities;
using WukongMp.Api.ECS.Jobs;
using WukongMp.Api.ECS.Managers;
using WukongMp.Api.ECS.Systems;
using WukongMp.Api.ECS.Systems.MainCharacters;
using WukongMp.Api.FreeCamera;
using WukongMp.Api.State;
using WukongMp.Api.UI;
using WukongMp.Api.WukongUtils;
using WukongMp.Coop.ECS.Systems;

namespace WukongMp.Coop;

public class CoopSynchronizer : WukongSynchronizer
{
    private readonly SystemGroup _modeGroup;
    private readonly WukongPlayerState PlayerState;

    public CoopSynchronizer(
        ArchetypeEventRouter archetypeEvent,
        ClientState state,
        ClientWukongArchetypeRegistration wukongArchetype,
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
        GameplayEventRouter gameplayEventRouter,
        GameplayConfiguration configuration,
        ColliderDisableData colliderDisableData,
        FreeCameraManager freeCameraManager,
        FreeCameraMover freeCameraMover,
        ILogger logger)
        : base(archetypeEvent, state, wukongArchetype, world, areaState, playerState, playerPawnState, modeManager, netManager, clientOwnership, jobRegistry, netComponentRegistry, relayClient, ecsLoop, eventBus, widgetManager, gameplayEventRouter, configuration, freeCameraManager, freeCameraMover, logger)
    {
        State.OnJoinedArea += OnJoinedAreaHandler;
        JobRegistry.OnApplySnapshot += OnApplySnapshot;
        PlayerPawnState.OnPlayerPawnSpawned += OnPlayerPawnSpawned;
        PlayerState = playerState;
        PlayerState.OnMainCharacterEntityInitialized += OnMainCharacterEntityInitialized;

        _modeGroup = new SystemGroup("Coop");

        _modeGroup.Add(new ScaleMonsterHpSystem());
        _modeGroup.Add(new ReEnableCollidersSystem(colliderDisableData, eventBus));
        _modeGroup.Add(new RespawnMainCharacterSystem(areaState, playerState, rpc, Logger));
        _modeGroup.Add(new FixYellowbrowSystem(areaState, playerState, freeCameraManager));
        _modeGroup.Add(new DetectSoftlockSystem(areaState, playerState, widgetManager, Logger));

        _modeGroup.SetMonitorPerf(true);
        EcsLoop.AddSystem(_modeGroup);
    }

    protected override void OnDispose()
    {
        State.OnJoinedArea -= OnJoinedAreaHandler;
        JobRegistry.OnApplySnapshot -= OnApplySnapshot;
        PlayerPawnState.OnPlayerPawnSpawned -= OnPlayerPawnSpawned;
        PlayerState.OnMainCharacterEntityInitialized -= OnMainCharacterEntityInitialized;
        
        EcsLoop.RemoveSystem(_modeGroup);
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
            TamerUtils.DiscoverTamers();
        }
    }

    private void OnApplySnapshot()
    {
        // TODO: Probably can be deleted.
        World.Query<LocalTamerComponent, MetadataComponent>().Each(new DiscoverLocallySpawnedMonsters());
    }
}