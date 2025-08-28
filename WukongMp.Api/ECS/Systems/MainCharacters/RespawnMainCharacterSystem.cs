using b1;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using Microsoft.Extensions.Logging;
using ReadyM.Relay.Common.Wukong.ECS.Components;
using WukongMp.Api.ECS.Components;
using WukongMp.Api.State;
using WukongMp.Api.WukongUtils;

namespace WukongMp.Api.ECS.Systems.MainCharacters;

public class RespawnMainCharacterSystem(WukongPlayerState playerState, ILogger logger) : QuerySystem<LocalMainCharacterComponent, MainCharacterComponent>
{
    private readonly float _delaySeconds = 10;
    private float _elapsedSeconds;

    protected override void OnUpdate()
    {
        var allDead = true;
        var players = 0;

        Query.ForEachEntity((ref LocalMainCharacterComponent localMainComp, ref MainCharacterComponent mainComp, Entity _) =>
        {
            if (!localMainComp.HasPawn)
                return;

            players++;

            // once HP > 0, respawning must have finished
            if (!mainComp.IsDead)
            {
                localMainComp.IsRespawning = false;
            }

            // count players who are dead and not yet respawning
            allDead &= mainComp.IsDead && !localMainComp.IsRespawning;
        });

        var mainEntity = playerState.LocalMainCharacter;
        if (!mainEntity.HasValue)
        {
            logger.LogWarning("Skipping respawn, no local main character entity");
            return;
        }
        ref var localMainComp = ref mainEntity.Value.GetLocalState();

        // if all players are dead, respawn the local player
        if (allDead && players > 0)
        {
            logger.LogDebug("All {Players} players are dead, respawning player {Player}", players, playerState.LocalPlayerId);
            localMainComp.IsRespawning = true;
            _elapsedSeconds = 0;
        }

        if (localMainComp.IsRespawning)
        {
            _elapsedSeconds += Tick.deltaTime;
            if (_elapsedSeconds > _delaySeconds)
            {
                PlayerUtils.RebirthPlayer(localMainComp.Pawn);
            }
        }
    }
}