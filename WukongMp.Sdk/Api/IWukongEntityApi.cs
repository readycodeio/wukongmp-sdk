using System.Collections.Generic;
using ReadyM.Api.Idents;
using ReadyM.SDK.Archetypes;
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
    /// Gets the local main character.
    /// </summary>
    /// <remarks>Use <see cref="MainCharacter.IsValid"/> to check if the entity exists.</remarks>
    MainCharacter LocalMainCharacter { get; }
    
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
    IEnumerable<Tamer> AllTamers { get; }

    /// <summary>
    /// Gets a list of tamers (monsters) in the current area.
    /// </summary>
    IEnumerable<Tamer> AreaTamers { get; }
}