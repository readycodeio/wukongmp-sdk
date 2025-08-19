using System.Collections.Generic;
using b1;
using Friflo.Engine.ECS;
using Microsoft.Extensions.Logging;
using ReadyM.Api.ECS.Worlds;
using ReadyM.Api.Multiplayer.Idents;
using ReadyM.Relay.Common.Wukong.ECS.Components;
using UnrealEngine.Engine;
using WukongMp.Api.ECS.Jobs;
using WukongMp.Api.WukongUtils;

namespace WukongMp.Api.State;

// FIXME: This should be merged with `WukongPawnState`. In addition, this class does to many things. It should exclusively
// deal with placing and removing pawns.
public class WukongPlayerPawnState(Store world, WukongPlayerState playerState, ILogger logger)
{
    private struct Entry
    {
        public AActor? MarkerActor;
        public BGUCharacterCS? Pawn;
        public string CharacterNickName;
    }

    private readonly Dictionary<PlayerId, Entry> _entries = [];

    public void AddPlayerPawn(PlayerId playerId)
    {
        if (_entries.ContainsKey(playerId))
        {
            logger.LogWarning("Attempted to add player pawn for {PlayerId} but it already exists in entries.", playerId);
            return;
        }

        logger.LogDebug("SPAWN OTHER MAIN CHARACTER ENTITY: {PlayerId}", playerId);

        var playerEntity = playerState.GetPlayerById(playerId);
        if (playerEntity == null)
        {
            logger.LogError("Player with ID {PlayerId} not found in player state.", playerId);
            return;
        }

        var mainEntity = playerState.GetMainCharacterById(playerId);
        if (mainEntity == null)
        {
            logger.LogError("Main character for player {PlayerId} not found in player state.", playerId);
            return;
        }

        var pawn = SpawningUtils.SpawnCloneForPlayer(playerEntity.Value, mainEntity.Value);
        if (pawn == null)
            return;

        var marker = MarkerUtils.CreateMarkerForCharacter(mainEntity.Value); // 3D marker above player
        var nickname = mainEntity.Value.GetState().CharacterNickName;

        var entry = new Entry
        {
            MarkerActor = marker,
            Pawn = pawn,
            CharacterNickName = nickname
        };

        _entries.Add(playerId, entry);

        logger.LogDebug("Spawn successful: {PlayerId}", playerId);
    }

    public void RemovePlayerPawn(PlayerId playerId)
    {
        if (!_entries.Remove(playerId, out var entry))
        {
            logger.LogWarning("Attempted to remove player pawn for {PlayerId} but it was not found in entries.", playerId);
            return;
        }

        logger.LogDebug("DESPAWN OTHER MAIN CHARACTER ENTITY: {PlayerId}", playerId);
        logger.LogDebug("Other main character marker: {Actor}", entry.MarkerActor?.GetName());
        logger.LogDebug("Other main character pawn: {Pawn}", entry.Pawn?.PathName);

        if (entry.MarkerActor != null)
        {
            BGU_UnrealWorldUtil.DestroyActor(entry.MarkerActor);
        }

        if (entry.Pawn != null)
        {
            BGU_UnrealWorldUtil.DestroyActor(entry.Pawn);
        }
        else
        {
            logger.LogWarning("Attempted to remove player pawn for {PlayerId} but it was already null.", playerId);
            return;
        }

        DI.Instance.Logger.LogDebug("DELETE OTHER MAIN CHARACTER ENTITY: {PlayerId}", playerId);

        // FIXME: This seems to be the wrong scope. At the very least it shouldn't be using the nickname as the identifier?
        // LobbyStatusWidget.Instance.RemovePlayerFromTeams(entry.CharacterNickName);

        // FIXME: This seems to be the wrong scope. Player removal should trigger `RemovePlayerPawn` and related actions not the 
        // other way around.

        // LobbyStatusWidget.Instance.SetReadyCount(state.AllPlayers.Select(playerState.GetPlayerById).Count(x => x?.GetState().IsReadyForPvP == true));
        // CoopStatusWidget.Instance.RemovePlayer(entry.CharacterNickName);

        world.Query<TamerComponent>().Each(new ClearPlayerTamerRefCountJob(playerId));
    }
}