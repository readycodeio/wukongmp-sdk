using System;
using System.Collections.Generic;
using b1;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using Microsoft.Extensions.Logging;
using UnrealEngine.Engine;
using WukongMp.Api;
using WukongMp.Api.ECS.Archetypes;
using WukongMp.Api.ECS.Entities;
using WukongMp.Api.ECS.Managers;
using WukongMp.Api.State;
using WukongMp.Api.WukongUtils;

namespace WukongMp.PvP.ECS.Systems;

/// <summary>
/// Despawns the pawns corresponding to MainCharacterEntities for other players. Doesn't affect the main players' MainCharacterEntity.
/// </summary>
public sealed class DespawnTamerSystem : BaseSystem, IDisposable
{
    private struct PendingDeleteEvent
    {
        public string Guid;
        public BUTamerActor? Tamer;
        public AActor? Marker;
    }

    private readonly ArchetypeEventRouter _archetypeEvent;
    private readonly WukongPlayerState _playerState;
    private readonly ClientWukongArchetypeRegistration _wukongArchetype;
    private readonly WukongEventBus _eventBus;
    private readonly ILogger _logger;

    private readonly List<PendingDeleteEvent> _pendingDeleteEvents = [];

    public DespawnTamerSystem(
        ArchetypeEventRouter archetypeEvent,
        WukongPlayerState playerState,
        ClientWukongArchetypeRegistration wukongArchetype,
        WukongEventBus eventBus,
        ILogger logger)
    {
        _archetypeEvent = archetypeEvent;
        _playerState = playerState;
        _wukongArchetype = wukongArchetype;
        _eventBus = eventBus;
        _logger = logger;

        _archetypeEvent[_wukongArchetype.TamerArchetype].OnEntityDelete += OnEntityDeleteHandler;
    }

    public void Dispose()
    {
        _archetypeEvent[_wukongArchetype.TamerArchetype].OnEntityDelete -= OnEntityDeleteHandler;
    }

    private void OnEntityDeleteHandler(EntityDelete evt)
    {
        if (_playerState.LocalPlayerId == null)
        {
            _logger.LogWarning("Local player ID is null, cannot despawn monster.");
            return;
        }

        var tamerEntity = new TamerEntity(evt.Entity);
        var tamerComp = tamerEntity.GetTamer();
        var markerComp = tamerEntity.GetMarker();

        _pendingDeleteEvents.Add(new PendingDeleteEvent
        {
            Guid = tamerComp.Guid,
            Tamer = tamerEntity.Tamer,
            Marker = markerComp.MarkerActor
        });
    }

    protected override void OnUpdateGroup()
    {
        if (!_eventBus.IsGameplayLevel)
            return;

        foreach (var pending in _pendingDeleteEvents)
        {
            TamerUtils.DestroyTamer(pending.Guid, pending.Tamer, pending.Marker);
        }

        _pendingDeleteEvents.Clear();
    }
}
