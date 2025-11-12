using System;
using ReadyM.Api.Multiplayer.Client;
using ReadyM.Relay.Common.Serialization;
using WukongMp.Api.State;

namespace WukongMp.Coop.Gamemode;

public class CoopMode : IDisposable
{
    protected readonly RelaySerializer Serializer;
    protected readonly IRelayClient RelayClient;
    private readonly WukongAreaState _areaState;
    private readonly WukongPlayerState _playerState;

    public CoopMode(
        RelaySerializer serializer,
        IRelayClient relayClient,
        WukongAreaState areaState,
        WukongPlayerState playerState
    )
    {
        Serializer = serializer; 
        RelayClient = relayClient;
        _areaState = areaState;
        _playerState = playerState;
        
        // TODO: (refactor) Add job for discovering tamers and creating their corresponding ECS entities
        // Consider marking tamers somehow to avoid repeated iteration over all the actors on the scene
        // alternatively only perform discovery if it has been explicitly requested. Request operations will
        // set a flag that will then result in discovery on the next loop iteration.
    }

    public void Dispose()
    {
        // TODO: (refactor) Remove the job from above
    }
}