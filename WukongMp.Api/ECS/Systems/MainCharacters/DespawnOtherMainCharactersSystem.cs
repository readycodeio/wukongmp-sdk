using System;
using System.Collections.Generic;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using ReadyM.Api.Multiplayer.Idents;
using WukongMp.Api.ECS.Archetypes;
using WukongMp.Api.ECS.Entities;
using WukongMp.Api.State;

namespace WukongMp.Api.ECS.Systems;

/// <summary>
/// Despawns the pawns corresponding to MainCharacterEntities for other players. Doesn't affect the main players' MainCharacterEntity.
/// </summary>
public class DespawnOtherMainCharactersSystem : BaseSystem, IDisposable
{
    private struct PendingDeleteEvent
    {
        public PlayerId PlayerId;
    }
    
    private readonly ArchetypeEventRouter _archetypeEvent;
    private readonly WukongPlayerState _playerState;
    private readonly ClientWukongArchetypeRegistration _wukongArchetype;
    private readonly WukongPlayerPawnState _playerPawnState;

    private readonly List<PendingDeleteEvent> _pendingDeleteEvents = new();

    public DespawnOtherMainCharactersSystem(ArchetypeEventRouter archetypeEvent, WukongPlayerState playerState, ClientWukongArchetypeRegistration wukongArchetype, WukongPlayerPawnState playerPawnState)
    {
        _archetypeEvent = archetypeEvent;
        _playerState = playerState;
        _wukongArchetype = wukongArchetype;
        _playerPawnState = playerPawnState;

        _archetypeEvent[_wukongArchetype.MainCharacterArchetype].OnEntityDelete += OnEntityDeleteHandler;
    }

    public void Dispose()
    {
        _archetypeEvent[_wukongArchetype.MainCharacterArchetype].OnEntityDelete -= OnEntityDeleteHandler;
    }

    private void OnEntityDeleteHandler(EntityDelete obj)
    {
        var mainEntity = new MainCharacterEntity(obj.Entity);
        ref var mainComp = ref mainEntity.GetState();
        ref var localMainComp = ref mainEntity.GetLocalState();

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
            // NOTE: Currently it safely handles removing characters that are already despawned
            _playerPawnState.RemovePlayerPawn(pending.PlayerId);
        }
        _pendingDeleteEvents.Clear();
    }
}