using System.Diagnostics;
using Friflo.Engine.ECS.Systems;
using ReadyM.Relay.Common.Wukong.ECS.Components;
using WukongMp.Api.ECS.Components;

namespace WukongMp.Api.ECS.Systems;

public class SpawnOtherMainCharactersSystem(WukongPlayerPawnState playerPawn)
    : QuerySystem<LocalMainCharacterComponent, MainCharacterComponent, TeamComponent>
{
    protected override void OnUpdate()
    {
        Query.ForEachEntity((ref localMainComp, ref mainComp, ref teamComp, entity) =>
        {
            if (localMainComp.Pawn != null)
                return;

            var mainEntity = new MainCharacterEntity(entity);
            AddPlayer(mainEntity);
        });
    }
    
    private void AddPlayer(MainCharacterEntity mainEntity)
    {
        ref var mainComp = ref mainEntity.GetState();
        ref var localMainComp = ref mainEntity.GetLocalState();
        var playerId = mainComp.PlayerId;

        playerPawn.AddPlayerPawn(playerId);
        
        Debug.Assert(localMainComp.Pawn != null);
    }
}
