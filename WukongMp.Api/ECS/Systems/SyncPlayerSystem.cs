using Friflo.Engine.ECS.Systems;
using WukongMp.Api.ECS.Components;
using WukongMp.Api.Old;
using WukongMp.Api.State;

namespace WukongMp.Api.ECS.Systems;

public class SyncPlayerSystem(WukongPlayerState playerState, WukongPlayerModeManager modeManager) : QuerySystem<PlayerComponent>
{
    protected override void OnUpdate()
    {
        Query.ForEachEntity((ref playerComp, entity) =>
        {
            // FIXME: (refactor) Have to check for state changes

            var playerEntity = new PlayerEntity(entity);
            SyncPlayerState(playerEntity);
        });
    }

    private void SyncPlayerState(PlayerEntity playerEntity)
    {
        var playerId = playerEntity.GetState().PlayerId;
        var mainEntity = playerState.GetMainCharacterById(playerId);
        if (mainEntity == null)
        {
            Logging.LogWarning("Player {Id} has no main character entity, skipping sync.", playerId);
            return;
        }
        
        ref var playerComp = ref playerEntity.GetState();

        var isSpectator = playerComp.IsSpectator;
        if (modeManager.HandleBecameSpectator(playerEntity, mainEntity.Value, isSpectator))
        {
            Logging.LogDebug("Player {Id} spectator status changed: {Spectator}", playerId, isSpectator);
        }
    }
}