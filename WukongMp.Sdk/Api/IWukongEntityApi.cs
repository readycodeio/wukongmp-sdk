using System.Collections.Generic;
using ReadyM.Api.Idents;
using ReadyM.SDK.Archetypes;
using ReadyM.SDK.Client.Entities;
using ReadyM.SDK.Core;
using UnrealEngine.Engine;
using WukongMp.Sdk.Common.Archetypes;

namespace WukongMp.Sdk.Api;

public interface IWukongEntityApi
{
    /// <summary>
    /// Gets a reference to a mixin on the global entity. Throws if there is no global entity, which is
    /// the case whenever the client is not in an area.
    /// </summary>
    T GetGlobalMixin<T>() where T : struct, IArchetypeMixin;

    /// <summary>
    /// Copies a mixin off the global entity, returning false if there is no global entity. Safe to call
    /// from a system, which keeps ticking after a disconnect.
    /// </summary>
    bool TryGetGlobalMixin<T>(out T value) where T : struct, IArchetypeMixin;
    
    /// <summary>
    /// Gets the current area entity if it exists.
    /// </summary>
    Area? CurrentArea { get; }
    
    /// <summary>
    /// Gets the local main character entity if it exists.
    /// </summary>
    MainCharacter? LocalMainCharacter { get; }
    
    /// <summary>
    /// Gets a list of all players on the server.
    /// </summary>
    IReadOnlyList<PlayerId> AllPlayers { get; }

    /// <summary>
    /// Gets a list of players in the current area.
    /// </summary>
    IReadOnlyList<PlayerId> AreaPlayers { get; }

    /// <summary>
    /// Gets a list of all tamers (monsters).
    /// </summary>
    EntityQuery<Tamer> AllTamers { get; }

    /// <summary>
    /// Gets a list of tamers (monsters) in the current area.
    /// </summary>
    ScopedQuery<Tamer> AreaTamers { get; }

    MainCharacter? GetPlayerEntityByActor(AActor? actor);
}