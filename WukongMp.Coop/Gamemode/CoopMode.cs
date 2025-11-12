using System;
using Friflo.Engine.ECS;
using ReadyM.Api.Multiplayer.Client;
using ReadyM.Relay.Common.Serialization;
using WukongMp.Api;
using WukongMp.Api.State;

namespace WukongMp.Coop.Gamemode;

public class CoopMode : IDisposable
{
    protected readonly RelaySerializer Serializer;
    protected readonly IRelayClient RelayClient;
    private readonly WukongAreaState _areaState;
    private readonly WukongPlayerState _playerState;
    private readonly WukongPawnState _pawnState;
    private readonly GameplayEventRouter _eventRouter;

    public CoopMode(
        RelaySerializer serializer,
        IRelayClient relayClient,
        WukongAreaState areaState,
        WukongPlayerState playerState,
        WukongPawnState pawnState,
        GameplayEventRouter eventRouter
    )
    {
        Serializer = serializer; 
        RelayClient = relayClient;
        _areaState = areaState;
        _playerState = playerState;
        _pawnState = pawnState;
        _eventRouter = eventRouter;

        _eventRouter.OnRebirthPointChanged += OnRebirthPointChanged;

        // TODO: (refactor) Add job for discovering tamers and creating their corresponding ECS entities
        // Consider marking tamers somehow to avoid repeated iteration over all the actors on the scene
        // alternatively only perform discovery if it has been explicitly requested. Request operations will
        // set a flag that will then result in discovery on the next loop iteration.
    }

    public void Dispose()
    {
        _eventRouter.OnRebirthPointChanged -= OnRebirthPointChanged;

        // TODO: (refactor) Remove the job from above
    }

    private void OnRebirthPointChanged(Entity entity, int rebirthPointId)
    {
        // update RebirthPointId in ECS if this is the local player
        if (_pawnState.TryGetMainCharacterEntity(entity, out var characterEntity) && _playerState.LocalMainCharacter.HasValue)
        {
            if (entity == _playerState.LocalMainCharacter.Value.Entity)
            {
                _playerState.LocalMainCharacter.Value.GetState().RebirthPointId = rebirthPointId;
            }
        }
    }
}