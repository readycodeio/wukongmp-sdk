using System.Collections.Generic;
using System.Numerics;
using ReadyM.Api.Idents;
using ReadyM.SDK.Archetypes;
using ReadyM.SDK.Client.Entities;
using ReadyM.SDK.Core;
using ReadyM.Wukong.Common.ECS.Values;
using UnrealEngine.Engine;
using WukongMp.Api.Configuration;
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
    /// Gets a value indicating whether the player is in an area.
    /// </summary>
    bool InArea { get; }

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

    /// <summary>
    /// Gets a list of all main characters (players) in the current area.
    /// </summary>
    ScopedQuery<MainCharacter> AreaMainCharacters { get; }

    bool IsMasterClient { get; }
    bool IsConnected { get; }

    MainCharacter? GetPlayerEntityByActor(AActor? actor);
    
    /// <summary>
    /// Enables spectator mode for the specified character.
    /// </summary>
    /// <param name="character">The character to enable spectator mode for.</param>
    /// <param name="reason">The reason for enabling spectator mode.</param>
    void EnableSpectatorMode(MainCharacter character, SpectatorReason reason);

    /// <summary>
    /// Disables spectator mode for the specified character.
    /// </summary>
    /// <param name="character">The character to disable spectator mode for.</param>
    void DisableSpectatorMode(MainCharacter character);

    /// <summary>
    /// Spawns an enemy of the specified kind at the given position.
    /// </summary>
    /// <param name="kind">The kind of enemy to spawn.</param>
    /// <param name="position">The position to spawn the enemy at.</param>
    /// <param name="count">The number of enemies to spawn. Defaults to 1.</param>
    /// <param name="teamId">The team ID for the spawned enemies. Defaults to the default monster team ID (2).</param>
    void SpawnEnemy(TamerKind kind, Vector3 position, int count, int teamId);
}