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
using ReadyM.Relay.Common.ECS.Jobs;
using WukongMp.Api;
using WukongMp.Api.ECS.Archetypes;
using WukongMp.Api.ECS.Components;
using WukongMp.Api.ECS.Entities;
using WukongMp.Api.ECS.Jobs;
using WukongMp.Api.ECS.Managers;
using WukongMp.Api.ECS.Systems;
using WukongMp.Api.ECS.Systems.MainCharacters;
using WukongMp.Api.State;
using WukongMp.Api.UI;
using WukongMp.Api.WukongUtils;

namespace WukongMp.Coop;

public class CoopSynchronizer : WukongSynchronizer
{
    private readonly SystemGroup _modeGroup;

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
        ILogger logger)
        : base(archetypeEvent, state, wukongArchetype, world, areaState, playerState, playerPawnState, modeManager, netManager, clientOwnership, jobRegistry, netComponentRegistry, relayClient, ecsLoop, eventBus, widgetManager, rpc, logger)
    {
        State.OnJoinedArea += OnJoinedAreaHandler;
        JobRegistry.OnApplySnapshot += OnApplySnapshot;
        playerPawnState.OnPlayerPawnSpawned += OnPlayerPawnSpawned;

        _modeGroup = new SystemGroup("Coop");

        _modeGroup.Add(new ScaleMonsterHpSystem());
        _modeGroup.Add(new RespawnMainCharacterSystem(areaState, playerState, rpc, Logger));

        _modeGroup.SetMonitorPerf(true);
        EcsLoop.AddSystem(_modeGroup);
    }

    protected override void OnDispose()
    {
        State.OnJoinedArea -= OnJoinedAreaHandler;
        JobRegistry.OnApplySnapshot -= OnApplySnapshot;

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

    private void OnJoinedAreaHandler(AreaId areaId, Entity entity)
    {
        var isFirst = AreaState.IsMasterClient;

        Logger.LogDebug("Joined area {AreaId}, is master client: {IsMasterClient}", areaId, isFirst);

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