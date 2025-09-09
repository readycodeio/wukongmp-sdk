using System;
using System.Collections.Generic;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using Microsoft.Extensions.Logging;
using ReadyM.Api.ECS.Worlds;
using ReadyM.Api.Multiplayer.ECS.Components;
using ReadyM.Api.Multiplayer.Idents;
using ReadyM.Relay.Client;
using ReadyM.Relay.Common.ECS.Archetypes;
using ReadyM.Relay.Common.Wukong.ECS.Components;
using WukongMp.Api.ECS.Archetypes;
using WukongMp.Api.ECS.Entities;
using WukongMp.Api.ECS.Managers;
using WukongMp.Api.State;
using WukongMp.Api.UI;

namespace WukongMp.Api.ECS.Systems.MainCharacters;

/// <summary>
/// Despawns the pawns corresponding to MainCharacterEntities for other players. Doesn't affect the main players' MainCharacterEntity.
/// </summary>
public sealed class DespawnOtherMainCharactersSystem : BaseSystem, IDisposable
{
    private struct PendingDeleteEvent
    {
        public PlayerId PlayerId;
    }

    private readonly ArchetypeEventRouter _archetypeEvent;
    private readonly WukongPlayerState _playerState;
    private readonly ClientWukongArchetypeRegistration _wukongArchetype;
    private readonly DefaultPlayerArchetypeRegistration _playerArchetype;
    private readonly WukongPlayerPawnState _playerPawnState;
    private readonly WukongEventBus _eventBus;
    private readonly WukongWidgetManager _widgetManager;
    private readonly ILogger _logger;

    private readonly List<PendingDeleteEvent> _pendingDeleteEvents = [];

    public DespawnOtherMainCharactersSystem(
        ArchetypeEventRouter archetypeEvent,
        WukongPlayerState playerState,
        ClientWukongArchetypeRegistration wukongArchetype,
        DefaultPlayerArchetypeRegistration playerArchetype,
        WukongPlayerPawnState playerPawnState,
        WukongWidgetManager widgetManager,
        WukongEventBus eventBus,
        ILogger logger)
    {
        _archetypeEvent = archetypeEvent;
        _playerState = playerState;
        _wukongArchetype = wukongArchetype;
        _playerArchetype = playerArchetype;
        _playerPawnState = playerPawnState;
        _eventBus = eventBus;
        _widgetManager = widgetManager;
        _logger = logger;

        _archetypeEvent[_wukongArchetype.MainCharacterArchetype].OnEntityDelete += OnEntityDeleteHandler;
    }

    public void Dispose()
    {
        _archetypeEvent[_playerArchetype.PlayerArchetype].OnEntityDelete -= OnEntityDeleteHandler;
    }

    private void OnEntityDeleteHandler(EntityDelete evt)
    {
        if (_playerState.LocalPlayerId == null)
        {
            _logger.LogWarning("Local player ID is null, cannot despawn other main characters.");
            return;
        }

        var mainEntity = new MainCharacterEntity(evt.Entity);
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
        if (!_eventBus.IsGameplayLevel)
            return;

        foreach (var pending in _pendingDeleteEvents)
        {
            _logger.LogDebug("ATTEMPTING TO DESPAWN OTHER MAIN CHARACTER ENTITY: {PlayerId}", pending.PlayerId);

            // NOTE: Currently it safely handles removing characters that are already despawned
            _playerPawnState.RemovePlayerPawn(pending.PlayerId);
        }

        _pendingDeleteEvents.Clear();

        _widgetManager.RefreshWidgets();
    }
}