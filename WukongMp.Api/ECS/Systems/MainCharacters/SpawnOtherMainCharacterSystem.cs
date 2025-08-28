using System.Diagnostics;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using Microsoft.Extensions.Logging;
using ReadyM.Relay.Client.State;
using ReadyM.Relay.Common.Wukong.ECS.Components;
using WukongMp.Api.ECS.Components;
using WukongMp.Api.ECS.Entities;
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
    : QuerySystem<LocalMainCharacterComponent, MainCharacterComponent, TeamComponent>
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

        Query.ForEachEntity((
            ref LocalMainCharacterComponent localMainComp,
            ref MainCharacterComponent mainComp,
            ref TeamComponent teamComp,
            Entity entity) =>
        {
            if (mainComp.PlayerId == playerId)
                return;
            if (localMainComp.HasPawn)
                return;

            var mainEntity = new MainCharacterEntity(entity);

            var playerEntity = playerState.GetPlayerById(mainComp.PlayerId);
            if (playerEntity == null)
            {
                // orphan player character entity after disconnection (missing global player entity)
                CommandBuffer.DeleteEntity(entity.Id);
                return;
            }

            logger.LogDebug("ATTEMPTING TO **SPAWN** OTHER MAIN CHARACTER ENTITY: {PlayerId}", mainComp.PlayerId);
            AddPlayer(mainEntity);
        });
    }

    private void AddPlayer(MainCharacterEntity mainEntity)
    {
        ref var mainComp = ref mainEntity.GetState();
        ref var localMainComp = ref mainEntity.GetLocalState();
        var playerId = mainComp.PlayerId;

        playerPawn.AddPlayerPawn(playerId);

        localMainComp.IsPlayerSynced = true;

        Debug.Assert(localMainComp.HasPawn);
    }
}