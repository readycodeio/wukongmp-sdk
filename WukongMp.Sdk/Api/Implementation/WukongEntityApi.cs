using System;
using System.Collections.Generic;
using ReadyM.Api.Idents;
using ReadyM.Relay.Client.State;
using ReadyM.SDK.Archetypes;
using ReadyM.SDK.Client.Entities;
using ReadyM.SDK.Core;
using ReadyM.SDK.Entities;
using ReadyM.SDK.Exceptions;
using UnrealEngine.Engine;
using WukongMp.Api.State;
using WukongMp.Sdk.Archetypes.Mixins;
using WukongMp.Sdk.Common.Archetypes;
using WukongMp.Sdk.Common.Archetypes.Mixins;

namespace WukongMp.Sdk.Api.Implementation;

internal sealed class WukongEntityApi(IEntities entities, WukongPlayerState playerState, ClientState state) : IWukongEntityApi
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
        => state.CurrentAreaId is {} id && entities.TryLookup(id, out Area area)
            ? entities.Query<Tamer>().InScope(area)
            : default;

    public MainCharacter? GetPlayerEntityByActor(AActor? actor)
    {
        if (actor == null)
            return null;
        
        if (entities.TryLookup(actor, out MappedActor mapped))
        {
            return EntityHandle.Of(mapped).As<MainCharacter>();
        }

        return null;
    }
}