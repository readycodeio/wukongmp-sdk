using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using b1;
using ReadyM.Api.Idents;
using ReadyM.Api.Multiplayer.Protocol;
using ReadyM.Wukong.Common.ECS.Values;
using UnrealEngine.Engine;
using WukongMp.Api.Configuration;
using WukongMp.Sdk.Entities;
using Constants = WukongMp.Api.Configuration.Constants;

namespace WukongMp.Sdk.Api;

/// <summary>
/// Provides methods related to synchronizing game state between players in a multiplayer session.
/// </summary>
public interface IWukongSynchronizationApi
{
    /// <summary>
    /// Retrieves the disconnect reason and invokes the provided callback with it.
    /// </summary>
    /// <param name="callback">The callback to invoke with the disconnect reason.</param>
    void GetDisconnectReasonAndInvoke(Action<DisconnectedReason> callback);

    /// <summary>
    /// Gets a value indicating whether the player is in an area.
    /// </summary>
    bool InArea { get; }

    /// <summary>
    /// Gets a value indicating whether the player is connected to the server.
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// Gets a value indicating whether the player is the master client in an area.
    /// </summary>
    bool IsMasterClient { get; }

    /// <summary>
    /// Gets the local player's ID.
    /// </summary>
    PlayerId? LocalPlayerId { get; }

    /// <summary>
    /// Gets the current area ID.
    /// </summary>
    AreaId? CurrentAreaId { get; }

    /// <summary>
    /// Gets the local main character.
    /// </summary>
    ReadyMainCharacter? LocalMainCharacter { get; }

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
    EntityList<ReadyTamer> AllTamers { get; }

    /// <summary>
    /// Gets a list of tamers (monsters) in the current area.
    /// </summary>
    EntityList<ReadyTamer> AreaTamers { get; }

    /// <summary>
    /// Gets a list of all main characters.
    /// </summary>
    EntityList<ReadyMainCharacter> AllMainCharacters { get; }

    /// <summary>
    /// Gets a list of main characters in the current area.
    /// </summary>
    EntityList<ReadyMainCharacter> AreaMainCharacters { get; }

    /// <summary>
    /// Gets the player entity associated with the specified actor.
    /// </summary>
    /// <param name="actor">The actor to find the player entity for.</param>
    /// <returns>The player entity, or <c>null</c> if not found.</returns>
    ReadyMainCharacter? GetPlayerEntityByActor(AActor? actor);

    /// <summary>
    /// Gets the player entity associated with the last transformation of the specified character.
    /// </summary>
    /// <param name="targetCharacter">The character to find the player entity for.</param>
    /// <returns>The player entity, or <c>null</c> if not found.</returns>
    ReadyMainCharacter? GetPlayerEntityByLastTransformation(BGUCharacterCS? targetCharacter);

    /// <summary>
    /// Tries to get player information by ID.
    /// </summary>
    /// <param name="player">The player ID.</param>
    /// <param name="nickname">The player's nickname, if found.</param>
    /// <param name="team">The player's team ID, if found.</param>
    /// <returns><c>true</c> if the player information was found; otherwise, <c>false</c>.</returns>
    bool TryGetPlayerInfoById(PlayerId player, [NotNullWhen(true)] out string? nickname, [NotNullWhen(true)] out int? team);

    /// <summary>
    /// Gets the main character associated with the specified player ID.
    /// </summary>
    /// <param name="playerId">The player ID.</param>
    /// <returns>The main character, or <c>null</c> if not found.</returns>
    ReadyMainCharacter? GetMainCharacterByPlayerId(PlayerId playerId);

    /// <summary>
    /// Register all monsters in the current area to be synchronized over the network.
    /// This should be called whenever the first player enters a new area.
    /// </summary>
    void SyncMonstersInArea();

    /// <summary>
    /// Spawns an enemy of the specified kind at the given position.
    /// </summary>
    /// <param name="kind">The kind of enemy to spawn.</param>
    /// <param name="position">The position to spawn the enemy at.</param>
    /// <param name="count">The number of enemies to spawn. Defaults to 1.</param>
    /// <param name="teamId">The team ID for the spawned enemies. Defaults to the default monster team ID (2).</param>
    void SpawnEnemy(TamerKind kind, Vector3 position, int count = 1, int teamId = Constants.DefaultMonsterTeamId);

    /// <summary>
    /// Enables spectator mode for the specified character.
    /// </summary>
    /// <param name="character">The character to enable spectator mode for.</param>
    /// <param name="reason">The reason for enabling spectator mode.</param>
    void EnableSpectatorMode(ReadyMainCharacter character, SpectatorReason reason);

    /// <summary>
    /// Disables spectator mode for the specified character.
    /// </summary>
    /// <param name="character">The character to disable spectator mode for.</param>
    void DisableSpectatorMode(ReadyMainCharacter character);
}