using System;
using LiteNetLib;
using ReadyM.Api.Idents;
using WukongMp.Sdk.Entities;

namespace WukongMp.Sdk.Api;

/// <summary>
/// Provides events related to gameplay, player actions, and multiplayer interactions in Wukong Multiplayer.
/// </summary>
public interface IWukongEventApi
{
    /// <summary>
    /// Fired when the player enters a gameplay level.
    /// </summary>
    event Action? OnBeginPlayGameplayLevel;

    /// <summary>
    /// Fired when the player leaves a gameplay level, either by exiting to the main menu or by loading another level.
    /// </summary>
    event Action? OnEndPlayGameplayLevel;

    /// <summary>
    /// Fired when the loading screen is closed after loading a gameplay level.
    /// </summary>
    event Action? OnLoadingScreenClose;

    /// <summary>
    /// Fired when a gameplay level is loaded, but before the loading screen is closed.
    /// This is a good event to use for initializing custom widgets, so that they are ready to be shown as soon as the loading screen is closed.
    /// </summary>
    event Action? OnLevelLoaded;

    /// <summary>
    /// Fired when the player exits a level, either by exiting to the main menu or by loading another level.
    /// </summary>
    event Action? OnExitLevel;

    /// <summary>
    /// Fired when the player enters an area.
    /// </summary>
    event Action<AreaId>? OnJoinedArea;

    /// <summary>
    /// Fired when the player leaves an area.
    /// </summary>
    event Action<AreaId>? OnLeftArea;

    /// <summary>
    /// Fired when the player's pawn (the in-game character they control) is spawned.
    /// This can happen when you enter a new area, or when another player connects to the game and their pawn is spawned for you.
    /// </summary>
    event Action<ReadyMainCharacter>? OnPlayerPawnSpawned;

    /// <summary>
    /// Fired when the player's main character ECS entity is initialized and ready.
    /// This is fired before <see cref="OnPlayerPawnSpawned"/>, so it can be used to set up things that need to be ready before the pawn is spawned.
    /// </summary>
    event Action<ReadyMainCharacter>? OnMainCharacterEntityInitialized;

    /// <summary>
    /// Fired when the player changes team, either by rebirthing or by joining a game in progress.
    /// </summary>
    event Action<ReadyMainCharacter>? OnPlayerChangedTeam;

    /// <summary>
    /// Fired when the local player's character is about to rebirth.
    /// This is fired before the rebirth actually happens, so the player's character will still be dead at this point.
    /// </summary>
    event Action? OnLocalPlayerBeforeRebirth;

    /// <summary>
    /// Fired when another player enters the same area as the local player.
    /// </summary>
    event Action<PlayerId, AreaId>? OnOtherPlayerInsideArea;

    /// <summary>
    /// Fired when another player leaves the area that the local player is in.
    /// </summary>
    event Action<PlayerId, AreaId>? OnOtherPlayerOutsideArea;

    /// <summary>
    /// Fired when any player connects to the server.
    /// </summary>
    event Action<PlayerId>? OnConnected;

    /// <summary>
    /// Fired when any player disconnects from the server, either voluntarily or involuntarily.
    /// </summary>
    event Action<PlayerId, DisconnectReason>? OnDisconnected;

    /// <summary>
    /// Fired when any player dies.
    /// The first parameter is the player character that died, and the second parameter is the entity that killed them (if applicable).
    /// </summary>
    event Action<ReadyMainCharacter, ReadyCharacter?>? OnPlayerDead;

    /// <summary>
    /// Fired when any monster dies.
    /// The first parameter is the monster that died, and the second parameter is the entity that killed it (if applicable).
    /// </summary>
    event Action<ReadyTamer, ReadyCharacter?>? OnMonsterDead;
    
    /// <summary>
    /// Fired when a monster is removed from the game world, either by dying or by being despawned for other reasons (e.g. the player leaving the area).
    /// </summary>
    event Action<ReadyTamer>? OnMonsterDestroyed;
}