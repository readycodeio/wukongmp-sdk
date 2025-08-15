using System.Diagnostics;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using ReadyM.Relay.Client.State;
using ReadyM.Relay.Common.Wukong.ECS.Components;
using WukongMp.Api.ECS.Components;
using WukongMp.Api.ECS.Entities;
using WukongMp.Api.State;

namespace WukongMp.Api.ECS.Systems;

/// <summary>
/// Spawns pawns for the MainCharacterEntities corresponding to other players. Doesn't affect the MainCharacterEntity
/// or pawn of the local player.
/// </summary>
/// <param name="playerPawn"></param>
public class SpawnOtherMainCharactersSystem(ClientState clientState, WukongPlayerState playerState, WukongPlayerPawnState playerPawn)
    : QuerySystem<LocalMainCharacterComponent, MainCharacterComponent, TeamComponent>
{
    protected override void OnUpdate()
    {
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
