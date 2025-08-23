using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using WukongMp.Api.ECS.Components;
using WukongMp.Api.ECS.Entities;
using WukongMp.Api.State;

namespace WukongMp.Api.ECS.Systems;

public class SyncPlayersSystem(WukongPlayerState playerState, WukongPlayerModeManager modeManager) : QuerySystem<PlayerComponent>
{
    protected override void OnUpdate()
    {
        Query.ForEachEntity((ref PlayerComponent playerComp, Entity entity) =>
        {
            // FIXME: (refactor) Have to check for state changes

            var playerEntity = new PlayerEntity(entity);
            SyncPlayerState(playerEntity);
        });
    }

    private void SyncPlayerState(PlayerEntity playerEntity)
    {
        // empty
    }
}