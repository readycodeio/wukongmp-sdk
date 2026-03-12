using System;
using Friflo.Engine.ECS;
using Microsoft.Extensions.Logging;
using ReadyM.Api.ECS.Worlds;
using ReadyM.Api.Idents;
using ReadyM.Api.Multiplayer.ECS.Values;
using ReadyM.Relay.Client.State;
using ReadyM.Wukong.Common.ECS.Components;
using WukongMp.Api.ECS.Archetypes;
using WukongMp.Api.ECS.Entities;

namespace WukongMp.Api.State;

internal class WukongPlayerState
{
    private readonly ComponentIndex<MainCharacterComponent, PlayerId> _ix;

    private readonly ClientWukongArchetypeRegistration _wukongArchetype;
    private readonly ClientNetworkedEntityManager _clientNetEntity;
    private readonly ClientState _state;
    private readonly ILogger _logger;
    
    public event Action<MainCharacterEntity>? OnMainCharacterEntityInitialized;

    public WukongPlayerState(
        Store world, 
        ClientWukongArchetypeRegistration wukongArchetype, 
        ClientNetworkedEntityManager clientNetEntity, 
        ClientState state, 
        ILogger logger)
    {
        _wukongArchetype = wukongArchetype;
        _clientNetEntity = clientNetEntity;
        _state = state;
        _logger = logger;

        _ix = world.ComponentIndex<MainCharacterComponent, PlayerId>();
    }
    
    internal void InvokeMainCharacterEntityInitialized(MainCharacterEntity mainCharacterEntity)
    {
        OnMainCharacterEntityInitialized?.Invoke(mainCharacterEntity);
    }

    public PlayerId? LocalPlayerId
        => _state.LocalPlayerId;

    public PlayerEntity? LocalPlayerEntity
    {
        get
        {
            var playerEntity = _state.LocalPlayerEntity;
            if (playerEntity == null)
                return null;
            return new PlayerEntity(playerEntity.Value);
        }
    }

    public PlayerEntity? GetPlayerById(PlayerId playerId)
    {
        if (!_state.PlayerEntries.TryGetValue(playerId, out var playerEntry))
            return null;
        
        var playerEntity = playerEntry.PlayerEntity;
        return new PlayerEntity(playerEntity);
    }
    
    public MainCharacterEntity? LocalMainCharacter
    {
        get
        {
            if (_state.LocalPlayerId == null)
                return null;
            
            var matching = _ix[_state.LocalPlayerId.Value];

            switch (matching.Count)
            {
                case 0:
                    return null;
                case 1:
                    return new MainCharacterEntity(matching[0]);
                default:
                    _logger.LogError("Multiple entities found with MainCharacterComponent for local player {PlayerId}. This should not happen.", _state.LocalPlayerId);
                    return null;
            }
        }
    }

    public MainCharacterEntity? GetMainCharacterByPlayerId(PlayerId playerId)
    {
        var matching = _ix[playerId];

        switch (matching.Count)
        {
            case 0:
                return null;
            case 1:
                return new MainCharacterEntity(matching[0]);
            default:
                _logger.LogError("Multiple entities found with MainCharacterComponent {PlayerId}. This should not happen.", playerId);
                return null;
        }
    }

    public MainCharacterEntity? GetMainCharacterById(NetworkId netId)
    {
        if (!_clientNetEntity.TryGetEntityByNetworkId(netId, out var entity))
            return null;
        
        if (!MainCharacterEntity.TryGetMainCharacter(entity.Value, out var mainEntity))
            return null;

        return mainEntity;
    }

    public MainCharacterEntity CreateLocalMainCharacter()
    {
        if (_state.LocalPlayerId == null)
            throw new InvalidOperationException("Local player ID is not set. Cannot create local main character.");
        
        var mainEntity = LocalMainCharacter;
        if (mainEntity != null)
            return mainEntity.Value;

        var entity = _clientNetEntity.CreateAreaEntity(_wukongArchetype.MainCharacterArchetype, b =>
        {
            b.Add(new MainCharacterComponent
            {
                PlayerId = _state.LocalPlayerId.Value,
            });
        });
        return new MainCharacterEntity(entity);
    }
}