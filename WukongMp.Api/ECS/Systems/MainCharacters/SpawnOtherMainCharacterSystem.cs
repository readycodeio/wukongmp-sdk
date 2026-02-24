using System.Diagnostics;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using Microsoft.Extensions.Logging;
using ReadyM.Relay.Client.State;
using ReadyM.Relay.Common.Wukong.ECS.Components;
using WukongMp.Api.ECS.Entities;
using WukongMp.Api.Mapping;
using WukongMp.Api.State;
using WukongMp.Api.WukongUtils;

namespace WukongMp.Api.ECS.Systems.MainCharacters;

/// <summary>
/// Spawns pawns for the MainCharacterEntities corresponding to other players. Doesn't affect the MainCharacterEntity
/// or pawn of the local player.
/// </summary>
/// <param name="playerPawn"></param>
public class SpawnOtherMainCharactersSystem(
    ClientState clientState,
    WukongPlayerState playerState,
    WukongPlayerPawnState playerPawn,
    WukongEventBus eventBus,
    ILogger logger
)
    : QuerySystem<MainCharacterComponent>
{
    protected override void OnUpdate()
    {
        if (!eventBus.IsGameplayLevel)
            return;

        if (GameUtils.GetControlledPawn() == null)
            return;

        var playerId = playerState.LocalPlayerId;
        if (playerId == null)
            return;

        var areaId = clientState.CurrentAreaId;
        if (areaId == null)
            return;

        Query.ForEachEntity((ref mainComp, entity) =>
        {
            var mainEntity = new MainCharacterEntity(entity);

            // NOTE: This only applies to spawning other players' main characters, unless they are orphaned
            if (!mainEntity.HasUnsyncedPawn &&
                mainComp.PlayerId != playerId &&
                playerState.GetPlayerById(mainComp.PlayerId) != null)
            {
                logger.LogDebug("ATTEMPTING TO **SPAWN** OTHER MAIN CHARACTER ENTITY: {PlayerId}", mainComp.PlayerId);
                AddPlayer(mainEntity);
            }
        });
    }

    private void AddPlayer(MainCharacterEntity mainEntity)
    {
        ref var mainComp = ref mainEntity.GetState();
        ref var localMainComp = ref mainEntity.GetLocalState();
        var playerId = mainComp.PlayerId;

        playerPawn.AddPlayerPawn(playerId);

        localMainComp.IsPlayerSynced = true;

        Debug.Assert(mainEntity.HasPawn);
    }
}