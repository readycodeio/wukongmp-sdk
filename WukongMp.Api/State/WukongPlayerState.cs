using System;
using Friflo.Engine.ECS;
using Microsoft.Extensions.Logging;
using ReadyM.Api.ECS.Worlds;
using ReadyM.Api.Idents;
using ReadyM.Api.Multiplayer.ECS.Managers;
using ReadyM.Api.Multiplayer.ECS.Values;
using ReadyM.Api.State;
using ReadyM.Relay.Client.State;
using ReadyM.Wukong.Common.ECS.Components;
using WukongMp.Api.ECS.Archetypes;
using WukongMp.Api.ECS.Entities;

namespace WukongMp.Api.State;

internal class WukongPlayerState(
    Store world,
    ClientWukongArchetypeRegistration wukongArchetype,
    IClientEntityManager clientNetEntity,
    INetworkedEntityManager netEntity,
    ClientState state,
    ILogger logger
)
{
    private readonly ComponentIndex<MainCharacterComponent, PlayerId> _ix
        = world.ComponentIndex<MainCharacterComponent, PlayerId>();

    public event Action<MainCharacterEntity>? OnMainCharacterEntityInitialized;

    internal void InvokeMainCharacterEntityInitialized(MainCharacterEntity mainCharacterEntity)
    {
        OnMainCharacterEntityInitialized?.Invoke(mainCharacterEntity);
    }

    public PlayerId? LocalPlayerId
        => state.LocalPlayerId;

    public PlayerEntity? LocalPlayerEntity
    {
        get
        {
            var playerEntity = state.LocalPlayerEntity;
            if (playerEntity == null)
                return null;
            return new PlayerEntity(playerEntity.Value);
        }
    }

    public PlayerEntity? GetPlayerById(PlayerId playerId)
    {
        if (!state.PlayerEntries.TryGetValue(playerId, out var playerEntry))
            return null;

        var playerEntity = playerEntry.PlayerEntity;
        return new PlayerEntity(playerEntity);
    }

    public MainCharacterEntity? LocalMainCharacter
    {
        get
        {
            if (state.LocalPlayerId == null)
                return null;

            var matching = _ix[state.LocalPlayerId.Value];

            switch (matching.Count)
            {
                case 0:
                    return null;
                case 1:
                    return new MainCharacterEntity(matching[0]);
                default:
                    logger.LogError("Multiple entities found with MainCharacterComponent for local player {PlayerId}. This should not happen.", state.LocalPlayerId);
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
                logger.LogError("Multiple entities found with MainCharacterComponent {PlayerId}. This should not happen.", playerId);
                return null;
        }
    }

    public MainCharacterEntity? GetMainCharacterById(NetworkId netId)
    {
        if (!netEntity.TryGetEntityByNetworkId(netId, out var entity))
            return null;

        if (!MainCharacterEntity.TryGetMainCharacter(entity.Value, out var mainEntity))
            return null;

        return mainEntity;
    }

    public MainCharacterEntity CreateLocalMainCharacter()
    {
        if (state.LocalPlayerId == null)
            throw new InvalidOperationException("Local player ID is not set. Cannot create local main character.");

        var mainEntity = LocalMainCharacter;
        if (mainEntity != null)
            return mainEntity.Value;

        var entity = clientNetEntity.CreateAreaEntity(wukongArchetype.MainCharacterArchetype, b =>
        {
            b.Add(new MainCharacterComponent
            {
                PlayerId = state.LocalPlayerId.Value,
            });
        });
        return new MainCharacterEntity(entity);
    }
}