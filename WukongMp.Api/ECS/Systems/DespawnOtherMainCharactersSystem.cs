using System;
using System.Collections.Generic;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using ReadyM.Api.Multiplayer.Idents;
using ReadyM.Relay.Client.State;
using WukongMp.Api.ECS.Components;
using WukongMp.Api.State;

namespace WukongMp.Api.ECS.Systems;

public class DespawnOtherMainCharactersSystem : BaseSystem, IDisposable
{
    private struct PendingDeleteEvent
    {
        public PlayerId PlayerId;
    }
    
    private readonly StoreEventQueue _queue;
    private readonly WukongPlayerState _playerState;
    private readonly WukongPlayerPawnState _playerPawnState;

    private readonly List<PendingDeleteEvent> _pendingDeleteEvents = new();

    public DespawnOtherMainCharactersSystem(StoreEventQueue queue, WukongPlayerState playerState, WukongPlayerPawnState playerPawnState)
    {
        _queue = queue;
        _playerState = playerState;
        _playerPawnState = playerPawnState;

        _queue[_playerState.MainCharacterArchetype].OnEntityDelete += OnEntityDeleteHandler;
    }

    public void Dispose()
    {
        _queue[_playerState.MainCharacterArchetype].OnEntityDelete -= OnEntityDeleteHandler;
    }

    private void OnEntityDeleteHandler(EntityDelete obj)
    {
        var mainEntity = new MainCharacterEntity(obj.Entity);
        ref var mainComp = ref mainEntity.GetState();

        var playerId = mainComp.PlayerId;
        if (playerId == _playerState.LocalPlayerId)
            return;
        
        _pendingDeleteEvents.Add(new PendingDeleteEvent
        {
            PlayerId = mainComp.PlayerId,
        });
    }

    protected override void OnUpdateGroup()
    {
        foreach (var pending in _pendingDeleteEvents)
        {
            _playerPawnState.RemovePlayerPawn(pending.PlayerId);
        }
        _pendingDeleteEvents.Clear();
    }
}