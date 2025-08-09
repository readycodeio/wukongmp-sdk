using System;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using Microsoft.Extensions.Logging;
using ReadyM.Api.Multiplayer.Client;
using ReadyM.Api.Multiplayer.ECS.Managers;
using ReadyM.Api.Multiplayer.ECS.Registry;
using ReadyM.Api.Multiplayer.Idents;
using ReadyM.Relay.Client;
using ReadyM.Relay.Client.State;
using ReadyM.Relay.Common.ECS.Jobs;
using WukongMp.Api.ECS.Archetypes;
using WukongMp.Api.ECS.Systems;
using WukongMp.Api.Old;
using WukongMp.Api.State;
using WukongMp.Api.UI;
using WukongMp.Api.WukongUtils;

namespace WukongMp.Api;

public class WukongSynchronizer : ClientNetworkedStateSynchronizer, IDisposable
{
    private readonly WukongAreaState _areaState;
    private readonly SystemGroup _syncGroup;

    public WukongSynchronizer(
        ArchetypeEventRouter archetypeEvent,
        ClientState state,
        ClientWukongArchetypeRegistration wukongArchetype,
        WukongAreaState areaState,
        WukongPlayerState playerState,
        WukongPlayerPawnState playerPawnState,
        WukongPlayerModeManager modeManager,
        NetworkedEntityManager netManager,
        JobRegistry jobRegistry,
        INetworkedComponentRegistry netComponentRegistry,
        IRelayClient relayClient,
        IClientEcsUpdateLoop ecsLoop,
        ILogger logger)
        : base(netManager, state, jobRegistry, netComponentRegistry, relayClient, ecsLoop, logger)
    {
        _areaState = areaState;

        State.OnJoinedArea += OnJoinedAreaHandler;
        
        _syncGroup = new SystemGroup("Sync");
        
        _syncGroup.Add(new SpawnTamersSystem(state));
        _syncGroup.Add(new DespawnDeadTamerMarkersSystem());
        _syncGroup.Add(new SyncTamersSystem());
        _syncGroup.Add(new UpdateTamerMarkersSystem());

        _syncGroup.Add(new CreateLocalMainCharacterEntitySystem(state, playerState, Logger));
        _syncGroup.Add(new DeleteLocalMainCharacterEntitySystem(playerState));
        _syncGroup.Add(new SpawnOtherMainCharactersSystem(state, playerState, playerPawnState));
        _syncGroup.Add(new DespawnOtherMainCharactersSystem(archetypeEvent, playerState, wukongArchetype, playerPawnState));
        _syncGroup.Add(new SyncOtherMainCharactersSystem(playerState, modeManager, Logger));

        _syncGroup.Add(new SyncPlayersSystem(playerState, modeManager));

        EcsLoop.AddSystem(_syncGroup);
    }

    protected override void OnDispose()
    {
        State.OnJoinedArea -= OnJoinedAreaHandler;
        
        EcsLoop.RemoveSystem(_syncGroup);
        base.OnDispose();
    }

    private void OnJoinedAreaHandler(AreaId areaId, Entity entity)
    {
        if (_areaState.IsMasterClient)
        {
            TamerUtils.DiscoverTamers();
        }
        
        CoopStatusWidget.Instance.SetVisibility(true);
    }
}
