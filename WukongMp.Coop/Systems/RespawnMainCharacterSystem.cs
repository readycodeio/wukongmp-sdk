using Friflo.Engine.ECS;
using Microsoft.Extensions.Logging;
using WukongMp.Sdk;
using WukongMp.Sdk.Api;
using WukongMp.Sdk.Entities;

namespace WukongMp.Coop.Systems;

public sealed class RespawnMainCharacterSystem : ModSystemBase
{
    protected override void OnUpdate(UpdateTick tick)
    {
        var allDead = true;
        var players = 0;

        foreach (var mainCharacter in ClientApi.AllMainCharacters)
        {
            if (mainCharacter.AreaId != ClientApi.CurrentAreaId)
                continue;

            players++;

            // count players who are dead and not yet respawning
            allDead &= mainCharacter is { IsDead: true, IsTransformed: false, IsRespawning: false };
        }

        if (players == 0)
            return;

        var localMainCharacter = ClientApi.LocalMainCharacter;
        if (!localMainCharacter.HasValue)
        {
            Logger.LogWarning("Skipping respawn, no local main character entity");
            return;
        }

        // if all players are dead, respawn the local player
        if (players > 0 && allDead && !localMainCharacter.Value.IsRespawning)
        {
            Logger.LogDebug("All {Players} players are dead, respawning player {Player}", players, ClientApi.LocalPlayerId);
            
            var maxComp = 0;
            foreach (var mainCharacter in ClientApi.AllMainCharacters)
            {
                maxComp = Math.Max(maxComp, mainCharacter.RebirthPointId);
            }

            localMainCharacter.Value.Respawn(maxComp);
        }
    }
}