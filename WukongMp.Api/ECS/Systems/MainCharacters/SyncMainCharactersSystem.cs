using b1;
using Friflo.Engine.ECS.Systems;
using Microsoft.Extensions.Logging;
using ReadyM.Api.Mapping.Data;
using ReadyM.Api.Multiplayer.ECS.Components;
using ReadyM.Wukong.Common.ECS.Components;
using WukongMp.Api.Configuration;
using WukongMp.Api.ECS.Entities;
using WukongMp.Api.State;
using WukongMp.Api.WukongUtils;

namespace WukongMp.Api.ECS.Systems.MainCharacters;

internal class SyncMainCharactersSystem(
    WukongPlayerState playerState,
    WukongPlayerModeManager modeManager,
    WukongEventBus eventBus,
    GameplayConfiguration configuration,
    GameplayEventRouter eventRouter,
    IComponentFieldMappingRegistry mappedField,
    ILogger logger
)
    : QuerySystem<MainCharacterComponent>
{
    protected override void OnUpdate()
    {
        if (!eventBus.IsGameplayLevel)
            return;

        Query.ForEachEntity((ref mainComp, entity) =>
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

        var isSpectator = mainEntity.GetState().IsSpectator;

        if (isSpectator != localMainComp.IsSpectatorLocally)
        {
            localMainComp.IsSpectatorLocally = isSpectator;
            if (modeManager.HandleBecameSpectator(mainEntity, isSpectator))
            {
                var playerId = playerEntity.Entity.GetComponent<MetadataComponent>().Owner;
                Logging.LogInformation("Player {Id} spectator status changed: {Spectator}", playerId, isSpectator);
            }
        }

        ref readonly var teamComp = ref mainEntity.GetTeam();
        var pawnTeamId = mainEntity.Pawn!.GetTeamIDInCS();
        if (pawnTeamId != teamComp.TeamId)
        {
            logger.LogInformation("Assigning team ID {TeamId} to player {Name}", teamComp.TeamId, playerComp.Nickname);
            ClientUtils.RegisterAndSetTeam(mainEntity.Pawn, teamComp.TeamId);
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
                logger.LogDebug("Assigning team ID {TeamId} to player {Name} from player to character", playerTeamId, playerComp.Nickname);
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

        if (mainEntity.Pawn == null)
            return;

        if (mappedField.CanSyncToGame<MainCharacterComponent>(mainEntity.Entity, out var sync))
        {
            sync.SyncToGame(MainCharacterComponent.Fields.Equipment.In<BGUCharacterCS>(), mainEntity.Pawn);
        }
    }
}