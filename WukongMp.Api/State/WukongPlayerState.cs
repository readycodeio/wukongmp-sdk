using System;
using Friflo.Engine.ECS;
using Microsoft.Extensions.Logging;
using ReadyM.Api.ECS.Worlds;
using ReadyM.Api.Multiplayer.Idents;
using ReadyM.Relay.Client.State;
using ReadyM.Relay.Common.Wukong.ECS.Components;
using WukongMp.Api.ECS.Archetypes;
using WukongMp.Api.ECS.Entities;

namespace WukongMp.Api.State;

public class WukongPlayerState
{
    private readonly ComponentIndex<MainCharacterComponent, PlayerId> _ix;

    private readonly ClientWukongArchetypeRegistration _wukongArchetype;
    private readonly ClientNetworkedEntityState _clientNetEntity;
    private readonly ClientState _state;
    private readonly ILogger _logger;

    public WukongPlayerState(Store world, ClientWukongArchetypeRegistration wukongArchetype, ClientNetworkedEntityState clientNetEntity, ClientState state, ILogger logger)
    {
        _wukongArchetype = wukongArchetype;
        _clientNetEntity = clientNetEntity;
        _state = state;
        _logger = logger;

        _ix = world.ComponentIndex<MainCharacterComponent, PlayerId>();
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

    public MainCharacterEntity? GetMainCharacterById(PlayerId playerId)
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

    public MainCharacterEntity CreateLocalMainCharacter()
    {
        if (_state.LocalPlayerId == null)
            throw new InvalidOperationException("Local player ID is not set. Cannot create local main character.");
        
        var mainEntity = LocalMainCharacter;
        if (mainEntity != null)
            return mainEntity.Value;

        var result = _clientNetEntity.CreateNetworkedAreaEntity(_wukongArchetype.MainCharacterArchetype, b =>
        {
            b.Add(new MainCharacterComponent()
            {
                PlayerId = _state.LocalPlayerId.Value,
            });
        });
        return new MainCharacterEntity(result.Entity);
    }

    public void DeleteLocalMainCharacter()
    {
        LocalMainCharacter?.Entity.DeleteEntity();
    }
}