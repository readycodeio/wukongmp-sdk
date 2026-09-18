using System;
using System.Collections.Generic;
using ReadyM.Api.Idents;
using ReadyM.Relay.Client.State;
using ReadyM.SDK.Archetypes;
using ReadyM.SDK.Client.Entity;
using ReadyM.SDK.Core;
using ReadyM.SDK.Entity;
using ReadyM.SDK.Exceptions;
using WukongMp.Api.State;
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

    public MainCharacter LocalMainCharacter
    {
        get
        {
            if (playerState.LocalPlayerId is { } id && entities.TryLookup(id, out MainCharacterData main))
            {
                return EntityHandle.Of(main).As<MainCharacter>();
            }

            return default;
        }
    }

    public IReadOnlyList<PlayerId> AllPlayers
        => state.AllPlayers;

    public IReadOnlyList<PlayerId> AreaPlayers
        => state.AreaPlayers;

    public IEnumerable<Tamer> AllTamers
    {
        get
        {
            foreach (var tamer in entities.Query<Tamer>())
            {
                yield return tamer;
            }
        }
    }

    public IEnumerable<Tamer> AreaTamers
    {
        get
        {
            if (state.CurrentAreaId is {} areaId && entities.TryLookup(areaId, out Area area))
            {
                foreach (var tamer in entities.Query<Tamer>())
                {
                    yield return tamer; // TODO: Filter
                }
            }
        }
    }
}