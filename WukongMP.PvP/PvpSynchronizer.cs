using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using Microsoft.Extensions.Logging;
using ReadyM.Api.ECS.Worlds;
using ReadyM.Api.Idents;
using ReadyM.Api.Mapping.Events;
using ReadyM.Api.Multiplayer.Client;
using ReadyM.Api.Multiplayer.ECS.Managers;
using ReadyM.Api.Multiplayer.ECS.Registry;
using ReadyM.Relay.Client;
using ReadyM.Relay.Client.State;
using ReadyM.Relay.Common.ECS.Jobs;
using WukongMp.Api;
using WukongMp.Api.Configuration;
using WukongMp.Api.ECS.Archetypes;
using WukongMp.Api.ECS.Managers;
using WukongMp.Api.ECS.Systems;
using WukongMp.Api.ECS.Systems.Tamers;
using WukongMp.Api.FreeCamera;
using WukongMp.Api.Mapping;
using WukongMp.Api.State;
using WukongMp.PvP.ECS.Systems;
using WukongMp.PvP.GameMode;
using WukongMp.PvP.UI;
using WukongMp.PvP.WukongUtils;

namespace WukongMp.PvP;

internal class PvpSynchronizer : WukongSynchronizer
{
    private readonly SystemGroup _modeGroup;
    private readonly ClientNetworkedEntityManager _clientNetEntity;

    public PvpSynchronizer(
        ArchetypeEventRouter archetypeEvent,
        ClientState state,
        ClientWukongArchetypeRegistration wukongArchetype,
        Store world,
        WukongAreaState areaState,
        WukongMappingPolicyDirectory policyDir,
        WukongPlayerState playerState,
        WukongPlayerPawnState playerPawnState,
        WukongPlayerModeManager modeManager,
        NetworkedEntityManager netManager,
        ClientOwnershipManager clientOwnership,
        ClientNetworkedEntityManager clientNetEntity,
        JobRegistry jobRegistry,
        INetworkedComponentRegistry netComponentRegistry,
        IRelayClient relayClient,
        IClientEcsUpdateLoop ecsLoop,
        IMappedEventManager mappedEvent,
        WukongEventBus eventBus,
        WukongClientRpcCallbacks rpc,
        PvpWidgetManager widgetManager,
        GameplayEventRouter gameplayEventRouter,
        GameplayConfiguration configuration,
        FreeCameraManager freeCameraManager,
        FreeCameraController freeCameraController,
        PvpMode pvpMode,
        ILogger logger)
        : base(
            archetypeEvent, 
            state, 
            wukongArchetype, 
            world, 
            policyDir, 
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
            widgetManager.WidgetManager, 
            gameplayEventRouter, 
            configuration, 
            freeCameraManager,
            freeCameraController,
            logger)
    {
        _clientNetEntity = clientNetEntity;
        
        State.OnJoinedArea += OnJoinedAreaHandler;

        _modeGroup = new SystemGroup("Pvp");

        _modeGroup.Add(new DespawnTamerSystem(archetypeEvent, playerState, wukongArchetype, eventBus, Logger));
        _modeGroup.Add(new ReadinessSystem(world, areaState, widgetManager, playerState, pvpMode));
        _modeGroup.Add(new PlayerListSystem(playerState, areaState, widgetManager));
        _modeGroup.Add(new PvpRoundEndSystem(world, areaState, pvpMode, ecsLoop));
        _modeGroup.Add(new PvpAntiStallSystem(areaState, rpc));

        _modeGroup.SetMonitorPerf(true);
        EcsLoop.AddSystem(_modeGroup);
    }

    protected override void OnDispose()
    {
        State.OnJoinedArea -= OnJoinedAreaHandler;

        EcsLoop.RemoveSystem(_modeGroup);
        base.OnDispose();
    }

    private void OnJoinedAreaHandler(AreaId areaId, Entity entity)
    {
        var isFirst = AreaState.IsMasterClient;

        Logger.LogDebug("Joined area {AreaId}, is master client: {IsMasterClient}", areaId, isFirst);

        if (isFirst)
        {
            PvpUtils.CreatePvpStateEntity(AreaState, _clientNetEntity, WukongArchetype);
        }
    }
}
