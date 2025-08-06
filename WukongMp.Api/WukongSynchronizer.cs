using System;
using Friflo.Engine.ECS.Systems;
using Microsoft.Extensions.Logging;
using ReadyM.Api.ECS.Worlds;
using ReadyM.Api.Multiplayer.Client;
using ReadyM.Api.Multiplayer.ECS.Managers;
using ReadyM.Api.Multiplayer.ECS.Registry;
using ReadyM.Relay.Client;
using ReadyM.Relay.Client.State;
using WukongMp.Api.ECS.Systems;
using WukongMp.Api.Old;
using WukongMp.Api.State;

namespace WukongMp.Api;

public class WukongSynchronizer : ClientNetworkedStateSynchronizer, IDisposable
{
    private readonly WukongPlayerState _playerState;
    private readonly WukongPlayerPawnState _playerPawnState;
    private readonly SystemGroup _syncGroup;

    public WukongSynchronizer(
        Store world,
        StoreEventQueue queue,
        ClientState state,
        WukongPlayerState playerState,
        WukongPlayerPawnState playerPawnState,
        WukongPlayerModeManager modeManager,
        NetworkedEntityManager netManager,
        ClientJobRegistry jobRegistry,
        INetworkedComponentRegistry netComponentRegistry,
        IRelayClient relayClient,
        IClientEcsUpdateLoop ecsLoop,
        ILogger logger)
        : base(world, netManager, jobRegistry, netComponentRegistry, relayClient, ecsLoop, logger)
    {
        _playerState = playerState;
        _playerPawnState = playerPawnState;
 
        _syncGroup = new SystemGroup("Sync");
        
        _syncGroup.SystemRoot.Add(new SyncTamersSystem());
        _syncGroup.SystemRoot.Add(new UpdateMarkersSystem());
        _syncGroup.SystemRoot.Add(new DestroyDeadMonstersMarkersSystem());
        _syncGroup.SystemRoot.Add(new SyncMonstersSystem(state));

        _syncGroup.SystemRoot.Add(new CreateLocalMainCharacterEntitySystem(_playerState, Logger));
        _syncGroup.SystemRoot.Add(new DeleteLocalMainCharacterEntitySystem(_playerState));
        _syncGroup.SystemRoot.Add(new SpawnOtherMainCharactersSystem(_playerPawnState));
        _syncGroup.SystemRoot.Add(new DespawnOtherMainCharactersSystem(queue, _playerState, _playerPawnState));
        _syncGroup.SystemRoot.Add(new SyncMainCharacterSystem(_playerState, modeManager, Logger));
        
        EcsLoop.AddSystem(_syncGroup);
    }

    protected override void OnDispose()
    {        
        EcsLoop.RemoveSystem(_syncGroup);
        base.OnDispose();
    }
}
