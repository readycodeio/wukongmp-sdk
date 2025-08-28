using System;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using Microsoft.Extensions.Logging;
using ReadyM.Relay.Common.Wukong.ECS.Components;
using WukongMp.Api.ECS.Components;
using WukongMp.Api.State;

namespace WukongMp.Api.ECS.Systems.MainCharacters;

public class RespawnMainCharacterSystem(
    WukongAreaState areaState,
    WukongPlayerState playerState,
    WukongRpcCallbacks rpc,
    ILogger logger
) : QuerySystem<LocalMainCharacterComponent, MainCharacterComponent>
{
    private const float DelaySeconds = 10;
    private float _elapsedSeconds;
    private bool _isCountingDown;

    protected override void OnUpdate()
    {
        if (!areaState.IsMasterClient)
            return;

        var allDead = true;
        var players = 0;

        Query.ForEachEntity((ref LocalMainCharacterComponent localMainComp, ref MainCharacterComponent mainComp, Entity _) =>
        {
            if (!localMainComp.HasPawn)
                return;

            players++;

            // count players who are dead and not yet respawning
            allDead &= mainComp.IsDead && !localMainComp.IsRespawning;
        });

        if (players == 0)
            return;

        var mainEntity = playerState.LocalMainCharacter;
        if (!mainEntity.HasValue)
        {
            logger.LogWarning("Skipping respawn, no local main character entity");
            return;
        }

        ref var localMainComp = ref mainEntity.Value.GetLocalState();

        // if all players are dead, respawn the local player
        if (players > 0 && allDead && !_isCountingDown && !localMainComp.IsRespawning)
        {
            logger.LogDebug("All {Players} players are dead, respawning player {Player}", players, playerState.LocalPlayerId);
            _elapsedSeconds = 0;
            _isCountingDown = true;
        }

        if (_isCountingDown)
        {
            _elapsedSeconds += Tick.deltaTime;
            if (_elapsedSeconds > DelaySeconds)
            {
                var maxComp = 0;
                Query.ForEachEntity((ref LocalMainCharacterComponent _, ref MainCharacterComponent mainComp, Entity _) => { maxComp = Math.Max(maxComp, mainComp.RebirthPointId); });

                _isCountingDown = false;
                localMainComp.IsRespawning = true;
                rpc.SendPartyRespawn(maxComp);
            }
        }
    }
}