using System.Diagnostics;
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
public class DeleteOrphanedMainCharactersSystem(
    ClientState clientState,
    WukongPlayerState playerState,
    WukongEventBus eventBus,
    WukongMappingPolicyDirectory policyDir,
    // NOTE(api): API refactoring only
    ClientOwnershipManager ownershipManager,
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

            // NOTE: orphan player character entity after disconnection (missing global player entity)
            if (!mainEntity.HasUnsyncedPawn &&
                mainComp.PlayerId != playerId &&
                playerState.GetPlayerById(mainComp.PlayerId) == null)
            {
                if (policyDir.MainCharacterCreateDelete().ShouldGameDeletePropagateToEcs(mainEntity))
                {
                    // NOTE(api): API refactoring only
                    Debug.Assert(ownershipManager.OwnsEntity(entity));
                    
                    CommandBuffer.DeleteEntity(entity.Id);
                }
                else
                {
                    // NOTE(api): API refactoring only
                    Debug.Assert(!ownershipManager.OwnsEntity(entity));
                }
            }
        });
    }
}