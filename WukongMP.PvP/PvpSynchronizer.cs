using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using Microsoft.Extensions.Logging;
using ReadyM.Api.ECS.Worlds;
using ReadyM.Api.Multiplayer.Client;
using ReadyM.Api.Multiplayer.ECS.Managers;
using ReadyM.Api.Multiplayer.ECS.Registry;
using ReadyM.Api.Multiplayer.Idents;
using ReadyM.Relay.Client;
using ReadyM.Relay.Client.State;
using ReadyM.Relay.Common.ECS.Jobs;
using WukongMp.Api;
using WukongMp.Api.Configuration;
using WukongMp.Api.ECS.Archetypes;
using WukongMp.Api.ECS.Managers;
using WukongMp.Api.ECS.Systems.Tamers;
using WukongMp.Api.State;
using WukongMp.PvP.ECS.Systems;
using WukongMp.PvP.Gamemode;
using WukongMp.PvP.UI;
using WukongMp.PvP.WukongUtils;

namespace WukongMp.PvP;

internal class PvpSynchronizer : WukongSynchronizer
{
    private readonly SystemGroup _modeGroup;

    public PvpSynchronizer(
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
        PvpWidgetManager widgetManager,
        GameplayEventRouter gameplayEventRouter,
        GameplayConfiguration configuration,
        PvpMode pvpMode,
        ILogger logger)
        : base(archetypeEvent, state, wukongArchetype, world, areaState, playerState, playerPawnState, modeManager, netManager, clientOwnership, jobRegistry, netComponentRegistry, relayClient, ecsLoop, eventBus, widgetManager.widgetManager, gameplayEventRouter, configuration, logger)
    {
        State.OnJoinedArea += OnJoinedAreaHandler;

        _modeGroup = new SystemGroup("Pvp");

        _modeGroup.Add(new DespawnTamerSystem(archetypeEvent, playerState, wukongArchetype, eventBus, Logger));
        _modeGroup.Add(new ReadinessSystem(areaState, widgetManager, pvpMode));
        _modeGroup.Add(new PlayerListSystem(playerState, areaState, widgetManager));
        _modeGroup.Add(new TeamColorSystem());

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
            PvpUtils.CreatePvpStateEntity();
        }
    }
}
