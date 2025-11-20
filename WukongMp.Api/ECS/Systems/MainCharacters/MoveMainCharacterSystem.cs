using Friflo.Engine.ECS.Systems;
using ReadyM.Relay.Common.Wukong.ECS.Components;
using WukongMp.Api.Configuration;
using WukongMp.Api.ECS.Components;
using WukongMp.Api.State;

namespace WukongMp.Api.ECS.Systems.MainCharacters;

public class MoveMainCharacterSystem(WukongPlayerState playerState) : QuerySystem<LocalMainCharacterComponent, MainCharacterComponent>
{
    protected override void OnUpdate()
    {
        Query.ForEachEntity((ref localMainComp, ref mainComp, _) =>
        {
            if (mainComp.PlayerId == playerState.LocalPlayerId)
                return;

            if (!localMainComp.HasPawn)
                return;

        });
    }
}
