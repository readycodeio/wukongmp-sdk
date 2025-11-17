using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using Microsoft.Extensions.Logging;
using ReadyM.Api.Multiplayer.ECS.Components;
using ReadyM.Relay.Common.Wukong.ECS.Components;
using WukongMp.Api.Configuration;
using WukongMp.Api.ECS.Components;
using WukongMp.Api.ECS.Entities;
using WukongMp.Api.State;
using WukongMp.Api.UI;
using WukongMp.Api.WukongUtils;

namespace WukongMp.Api.ECS.Systems.MainCharacters;

public class SyncMainCharactersSystem(
    WukongPlayerState playerState,
    WukongPlayerModeManager modeManager,
    WukongEventBus eventBus,
    WukongWidgetManager widgetManager,
    GameplayConfiguration configuration,
    GameplayEventRouter eventRouter,
    ILogger logger
)
    : QuerySystem<LocalMainCharacterComponent, MainCharacterComponent>
{
    protected override void OnUpdate()
    {
        if (!eventBus.IsGameplayLevel)
            return;

        Query.ForEachEntity((
            ref localMainComp,
            ref mainComp, entity) =>
        {
            if (localMainComp.Pawn == null)
                return;

            var playerEntity = playerState.GetPlayerById(mainComp.PlayerId);
            if (playerEntity == null)
                return;

            var mainEntity = new MainCharacterEntity(entity);
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
            if (modeManager.HandleBecameSpectator(playerEntity, mainEntity, isSpectator))
            {
                var playerId = playerEntity.Entity.GetComponent<MetadataComponent>().Owner;
                Logging.LogInformation("Player {Id} spectator status changed: {Spectator}", playerId, isSpectator);
            }
        }

        ref readonly var teamComp = ref mainEntity.GetTeam();
        var pawnTeamId = localMainComp.Pawn!.GetTeamIDInCS();
        if (pawnTeamId != teamComp.TeamId)
        {
            logger.LogInformation("Assigning team ID {TeamId} to player {Name}", teamComp.TeamId, playerComp.NickName);
            ClientUtils.RegisterAndSetPlayerTeam(localMainComp.Pawn, teamComp.TeamId);
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
        ref var localMainComp = ref mainEntity.GetLocalState();

        if (localMainComp.Pawn == null)
            return;

        var eqCopy = mainComp.Equipment;
        if (eqCopy.IsLocallyDirty)
        {
            EquipmentUtils.SetActorEquipment(localMainComp.Pawn, mainComp.Equipment);
            eqCopy.ClearLocallyDirty();
            mainComp.Equipment = eqCopy;
            // Equipment is passed by value, so we need to reassign it
            // This sets the dirty flag, but since we're not the owner of the entity, it won't be sent back to the server
        }
    }
}