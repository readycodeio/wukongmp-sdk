using Friflo.Engine.ECS.Systems;
using Microsoft.Extensions.Logging;
using ReadyM.Relay.Common.Wukong.ECS.Components;
using WukongMp.Api.ECS.Components;
using WukongMp.Api.ECS.Entities;
using WukongMp.Api.Old;
using WukongMp.Api.State;
using WukongMp.Api.WukongUtils;

namespace WukongMp.Api.ECS.Systems;

public class SyncOtherMainCharactersSystem(WukongPlayerState playerState, WukongPlayerModeManager modeManager, ILogger logger)
    : QuerySystem<LocalMainCharacterComponent, MainCharacterComponent, TeamComponent>
{
    protected override void OnUpdate()
    {
        Query.ForEachEntity((ref localMainComp, ref mainComp, ref teamComp, entity) =>
        {
            if (localMainComp.Pawn == null)
                return;

            if (mainComp.PlayerId == playerState.LocalPlayerId)
                return;
            
            var playerEntity = playerState.GetPlayerById(mainComp.PlayerId);
            if (playerEntity == null)
                return;
            
            var mainEntity = new MainCharacterEntity(entity);
            SyncMainCharacterState(playerEntity.Value, mainEntity);
        });
    }

    private void SyncMainCharacterState(PlayerEntity playerEntity, MainCharacterEntity mainEntity)
    {
        ref var mainComp = ref mainEntity.GetState();
        ref var localMainComp = ref mainEntity.GetLocalState();
        ref readonly var teamComp = ref mainEntity.GetTeam();

        var pawnTeamId = localMainComp.Pawn!.GetTeamIDInCS();
        if (pawnTeamId != teamComp.TeamId)
        {
            logger.LogDebug("Assigning team ID {TeamId} to player", teamComp.TeamId);
            ClientUtils.RegisterAndSetPlayerTeam(localMainComp.Pawn, teamComp.TeamId);

            modeManager.UpdatePlayerTeam(playerEntity, mainEntity);
        }
        
        var eq = mainComp.Equipment;
        EquipmentUtils.SetActorEquipment(localMainComp.Pawn, eq);
    }
}