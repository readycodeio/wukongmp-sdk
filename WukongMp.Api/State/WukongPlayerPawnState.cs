using System;
using b1;
using Friflo.Engine.ECS;
using Microsoft.Extensions.Logging;
using ReadyM.Api.ECS.Worlds;
using ReadyM.Api.Idents;
using ReadyM.Wukong.Common.ECS.Components;
using UnrealEngine.Engine;
using WukongMp.Api.ECS.Entities;
using WukongMp.Api.ECS.Jobs;
using WukongMp.Api.FreeCamera;
using WukongMp.Api.WukongUtils;

namespace WukongMp.Api.State;

// FIXME: This should be merged with `WukongPawnState`. In addition, this class does to many things. It should exclusively
// deal with placing and removing pawns.
internal class WukongPlayerPawnState(FreeCameraManager freeCameraManager, Store world, WukongPlayerState playerState, ILogger logger)
{
    public event Action<MainCharacterEntity, BGUCharacterCS>? OnPlayerPawnSpawned;
    
    public void AddPlayerPawn(PlayerId playerId)
    {
        logger.LogDebug("SPAWN OTHER MAIN CHARACTER ENTITY: {PlayerId}", playerId);
        
        var mainEntity = playerState.GetMainCharacterByPlayerId(playerId);
        if (mainEntity == null)
        {
            logger.LogError("Main character for player {PlayerId} not found in player state.", playerId);
            return;
        }

        var pawn = SpawningUtils.SpawnCloneForPlayer(freeCameraManager, playerState, mainEntity.Value);
        if (pawn == null)
        {
            logger.LogError("Failed to spawn pawn for player {PlayerId}.", playerId);
            return;
        }
        
        OnPlayerPawnSpawned?.Invoke(mainEntity.Value, pawn);
        logger.LogDebug("Spawn successful: {PlayerId}", playerId);
    }

    public void RemovePlayerPawn(PlayerId playerId, BGUCharacterCS? playerPawn)
    {
        logger.LogDebug("DESPAWN OTHER MAIN CHARACTER ENTITY: {PlayerId}", playerId);

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