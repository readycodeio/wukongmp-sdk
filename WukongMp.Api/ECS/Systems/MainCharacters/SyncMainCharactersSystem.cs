using Friflo.Engine.ECS.Systems;
using Microsoft.Extensions.Logging;
using ReadyM.Api.Multiplayer.ECS.Components;
using ReadyM.Relay.Common.Wukong.ECS.Components;
using WukongMp.Api.Configuration;
using WukongMp.Api.ECS.Entities;
using WukongMp.Api.State;
using WukongMp.Api.WukongUtils;

namespace WukongMp.Api.ECS.Systems.MainCharacters;

public class SyncMainCharactersSystem(
    WukongPlayerState playerState,
    WukongPlayerModeManager modeManager,
    WukongEventBus eventBus,
    GameplayConfiguration configuration,
    GameplayEventRouter eventRouter,
    ILogger logger
)
    : QuerySystem<MainCharacterComponent>
{
    protected override void OnUpdate()
    {
        if (!eventBus.IsGameplayLevel)
            return;

        Query.ForEachEntity((
            ref mainComp, 
            entity) =>
        {
            var mainEntity = new MainCharacterEntity(entity);
            if (mainEntity.Pawn == null)
                return;

            var playerEntity = playerState.GetPlayerById(mainComp.PlayerId);
            if (playerEntity == null)
                return;

            if (mainComp.PlayerId != playerState.LocalPlayerId)
            {
                SyncOtherMainCharacterState(playerEntity.Value, mainEntity);
            }
            else
            {
                SyncLocalMainCharacterState(playerEntity.Value, mainEntity);
            }
        });
    }

    private void SyncMainCharacterStateBase(PlayerEntity playerEntity, MainCharacterEntity mainEntity)
    {
        ref var playerComp = ref playerEntity.GetState();
        ref var localMainComp = ref mainEntity.GetLocalState();

        var isSpectator = mainEntity.GetPvP().IsSpectator;

        if (isSpectator != localMainComp.IsSpectatorLocally)
        {
            localMainComp.IsSpectatorLocally = isSpectator;
            if (modeManager.HandleBecameSpectator(mainEntity, isSpectator))
            {
                var playerId = playerEntity.Entity.GetComponent<MetadataComponent>().Owner;
                logger.LogInformation("Player {Id} spectator status changed: {Spectator}", playerId, isSpectator);
            }
        }

        ref readonly var teamComp = ref mainEntity.GetTeam();

        var pawn = mainEntity.Pawn;
        if (pawn == null)
        {
            logger.LogError("Failed to get pawn for main character entity {EntityId}", mainEntity.Entity);
            return;
        }
        
        var pawnTeamId = pawn.GetTeamIDInCS();
        if (pawnTeamId != teamComp.TeamId)
        {
            logger.LogInformation("Assigning team ID {TeamId} to player {Name}", teamComp.TeamId, playerComp.NickName);
            ClientUtils.RegisterAndSetPlayerTeam(pawn, teamComp.TeamId);
            eventRouter.RaiseOnPlayerChangedTeam(playerEntity, mainEntity);
        }
    }

    private void SyncLocalMainCharacterState(PlayerEntity playerEntity, MainCharacterEntity mainEntity)
    {
        SyncMainCharacterStateBase(playerEntity, mainEntity);

        if (configuration.OverrideLocalPlayerTeamFromGlobalEntity)
        {
            ref var playerComp = ref playerEntity.GetState();
            var playerTeamId = playerComp.TeamId;
            if (playerTeamId != mainEntity.GetTeam().TeamId)
            {
                logger.LogDebug("Assigning team ID {TeamId} to player {Name} from player to character", playerTeamId, playerComp.NickName);
                mainEntity.SetTeam(new TeamComponent
                {
                    TeamId = playerTeamId,
                });
            }
        }
    }

    private void SyncOtherMainCharacterState(PlayerEntity playerEntity, MainCharacterEntity mainEntity)
    {
        SyncMainCharacterStateBase(playerEntity, mainEntity);

        ref var mainComp = ref mainEntity.GetState();
        var pawn = mainEntity.Pawn;
        
        if (pawn == null)
            return;
        
        var eqCopy = mainComp.Equipment;
        if (eqCopy.IsLocallyDirty)
        {
            if (pawn.GetClass().PathName != Constants.WukongDashengClassPath)
            {
                EquipmentUtils.SetActorEquipment(pawn, mainComp.Equipment);
            }

            eqCopy.ClearLocallyDirty();
            mainComp.Equipment = eqCopy;
            // Equipment is passed by value, so we need to reassign it
            // This sets the dirty flag, but since we're not the owner of the entity, it won't be sent back to the server
        }
    }
}