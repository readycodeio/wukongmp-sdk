using System.Collections.Generic;
using System.Numerics;
using b1;
using Friflo.Engine.ECS;
using ReadyM.Api.Idents;
using ReadyM.Api.Mapping.Events;
using ReadyM.Relay.Client.State;
using ReadyM.SDK.Archetypes;
using ReadyM.SDK.Client.Entities;
using ReadyM.SDK.Core;
using ReadyM.SDK.Entities;
using ReadyM.SDK.Exceptions;
using ReadyM.Wukong.Common.ECS.Values;
using UnrealEngine.Engine;
using WukongMp.Api;
using WukongMp.Api.Configuration;
using WukongMp.Api.ECS.GameEvents;
using WukongMp.Api.State;
using WukongMp.Api.WukongUtils;
using WukongMp.Sdk.Archetypes.Mixins;
using WukongMp.Sdk.Common.Archetypes;
using WukongMp.Sdk.Common.Archetypes.Mixins;

namespace WukongMp.Sdk.Api.Implementation;

internal sealed class WukongEntityApi(
    IEntities entities,
    WukongPlayerState playerState,
    WukongAreaState areaState,
    ClientState state,
    IMappedEventManager mappedEvent
) : IWukongEntityApi
{
    public T GetGlobalMixin<T>() where T : struct, IArchetypeMixin
    {
        foreach (var world in entities.Query<World>())
        {
            if (world.TryAs(out T mixin))
            {
                return mixin;
            }

            throw new ComponentNotFoundException($"World archetype does not carry the {typeof(T).Name} mixin");
        }

        throw new ComponentNotFoundException("World entity is missing");
    }

    public bool TryGetGlobalMixin<T>(out T value) where T : struct, IArchetypeMixin
    {
        foreach (var world in entities.Query<World>())
        {
            return world.TryAs(out value);
        }

        value = default;
        return false;
    }

    public Area? CurrentArea
    {
        get
        {
            if (state.CurrentAreaId is { } id && entities.TryLookup(id, out Area area))
            {
                return area;
            }

            return null;
        }
    }

    public bool InArea => state.CurrentAreaId is not null;

    public bool IsConnected
        => state.IsConnected;

    public bool IsMasterClient
        => areaState.IsMasterClient;

    public Player? LocalPlayer
    {
        get
        {
            if (state.LocalPlayerId is { } id && entities.TryLookup(id, out Player player))
            {
                return player;
            }

            return null;
        }
    }

    public MainCharacter? LocalMainCharacter
    {
        get
        {
            if (playerState.LocalPlayerId is { } id && entities.TryLookup(id, out MainCharacterData main))
            {
                return EntityHandle.Of(main).As<MainCharacter>();
            }

            return null;
        }
    }

    public IReadOnlyList<PlayerId> AllPlayers
        => state.AllPlayers;

    public IReadOnlyList<PlayerId> AreaPlayers
        => state.AreaPlayers;

    public EntityQuery<Tamer> AllTamers => entities.Query<Tamer>();

    public ScopedQuery<Tamer> AreaTamers
        => state.CurrentAreaId is { } id && entities.TryLookup(id, out Area area)
            ? entities.Query<Tamer>().InScope(area)
            : default;

    public ScopedQuery<MainCharacter> AreaMainCharacters
        => state.CurrentAreaId is { } id && entities.TryLookup(id, out Area area)
            ? entities.Query<MainCharacter>().InScope(area)
            : default;

    public MainCharacter? GetPlayerEntityByActor(AActor? actor)
    {
        if (actor as BGUCharacterCS is not { } character)
            return null;

        // One component maps every kind of actor, so the lookup is by actor and the cast above
        // is what keeps this to player characters.
        if (entities.TryLookup<MappedCharacter, AActor>(character, out var mapped))
        {
            return EntityHandle.Of(mapped).As<MainCharacter>();
        }

        return null;
    }

    public void EnableSpectatorMode(MainCharacter character, SpectatorReason reason)
    {
        var rawEntity = EntityHandle.Of(character).RawEntity;
        var entity = WukongApi.Services.Resolve<EntityStore>().GetEntityByRawEntity(rawEntity);
        PlayerUtils.EnableSpectator(entity, reason);
    }

    public void DisableSpectatorMode(MainCharacter character)
    {
        var rawEntity = EntityHandle.Of(character).RawEntity;
        var entity = WukongApi.Services.Resolve<EntityStore>().GetEntityByRawEntity(rawEntity);
        PlayerUtils.DisableSpectator(entity);
    }

    public void SpawnEnemy(TamerKind kind, Vector3 position, int count, int teamId)
    {
        if (LocalMainCharacter.HasValue && kind.Name != null)
        {
            var rawEntity = EntityHandle.Of(LocalMainCharacter.Value).RawEntity;
            var entity = WukongApi.Services.Resolve<EntityStore>().GetEntityByRawEntity(rawEntity);
            mappedEvent.InvokeInGameAndNotifyEcs(new RequestSpawnUnitsEvent(entity, kind.Name, count, teamId, position.ToFVector()), entity);
        }
    }

    public void SyncMonstersInArea() => TamerUtils.DiscoverTamers();
}