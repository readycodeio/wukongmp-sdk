using b1;
using Friflo.Engine.ECS;
using Microsoft.Extensions.Logging;
using ReadyM.Api.ECS.Worlds;
using ReadyM.Api.Multiplayer.Idents;
using ReadyM.Relay.Common.Wukong.ECS.Components;
using UnrealEngine.Engine;
using WukongMp.Api.ECS.Jobs;
using WukongMp.Api.UI;
using WukongMp.Api.WukongUtils;

namespace WukongMp.Api.State;

// FIXME: This should be merged with `WukongPawnState`. In addition, this class does to many things. It should exclusively
// deal with placing and removing pawns.
public class WukongPlayerPawnState(Store world, WukongPlayerState playerState, ILogger logger)
{
    public void AddPlayerPawn(PlayerId playerId)
    {
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
        {
            logger.LogError("Failed to spawn pawn for player {PlayerId}.", playerId);
            return;
        }

        var marker = MarkerUtils.CreateMarkerForCharacter(mainEntity.Value); // 3D marker above player
        if (marker == null)
        {
            logger.LogError("Failed to create marker for player {PlayerId}.", playerId);
            return;
        }

        logger.LogDebug("Spawn successful: {PlayerId}", playerId);
        
        // refresh lobby widget
        var nickname = mainEntity.Value.GetState().CharacterNickName;
        var team = playerEntity.Value.GetState().TeamId;
        var isSpectator = mainEntity.Value.GetPvP().IsSpectator;
        LobbyStatusWidget.Instance.UpdatePlayerTeam(nickname, team, isSpectator);
    }

    public void RemovePlayerPawn(PlayerId playerId, BGUCharacterCS? playerPawn, AActor? playerMarker)
    {
        logger.LogDebug("DESPAWN OTHER MAIN CHARACTER ENTITY: {PlayerId}", playerId);

        if (!playerMarker.IsNullOrDestroyed())
        {
            logger.LogDebug("Other main character marker: {Actor}", playerMarker?.GetName());
            BGU_UnrealWorldUtil.DestroyActor(playerMarker);
        }

        if (!playerPawn.IsNullOrDestroyed())
        {
            logger.LogDebug("Other main character pawn: {Pawn}", playerPawn?.PathName);
            BGU_UnrealWorldUtil.DestroyActor(playerPawn);
        }
        else
        {
            logger.LogWarning("Attempted to remove player pawn for {PlayerId} but it was already null.", playerId);
            return;
        }

        world.Query<TamerComponent>().Each(new ClearPlayerTamerRefCountJob(playerId));
    }
}