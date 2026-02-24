using System;
using System.Numerics;
using Friflo.Engine.ECS;
using ReadyM.Api.ECS.Worlds;
using ReadyM.Api.Helpers;
using ReadyM.Api.Mapping.Events;
using ReadyM.Relay.Client.State;
using ReadyM.Relay.Common.Wukong.ECS.Components;
using UnrealEngine.Runtime;
using WukongMp.Api.Configuration;
using WukongMp.Api.ECS.Entities;
using WukongMp.Api.State;

namespace WukongMp.Api.Mapping.Events;

public class SpawnSummonEventPolicy<TEvent>(
    ClientOwnershipManager ownership,
    WukongPlayerState playerState,
    WukongAreaState areaState,
    Store world,
    DataSideChannel sideChannel) : IMappingEventPolicy<SpawnSummonContext>
{
    private bool CanSummon(Entity? summonerEntity, FVector summonLocation)
    {
        if (summonerEntity != null && MainCharacterEntity.IsMainCharacter(summonerEntity.Value))
        {
            // Local player summons.
            // Other player summons.
            return ownership.OwnsEntity(summonerEntity.Value);
        }
        else // Summoner is not a player e.g. spawn point
        {
            var localMainEntity = playerState.LocalMainCharacter;
            if (localMainEntity == null)
            {
                return false;
            }

            if (playerState.LocalPlayerId == null)
                return false;

            if (areaState.IsMasterClient)
                return true;

            var localPlayerId = playerState.LocalPlayerId.Value;
            var localPosition = localMainEntity.Value.GetState().Location;
            var squaredDistanceToSummon = FVector.DistSquared(localPosition.ToFVector(), summonLocation);
            var squaredSpawnOwnershipRadius = Constants.SpawnOwnershipRadius * Constants.SpawnOwnershipRadius;
            if (squaredDistanceToSummon > squaredSpawnOwnershipRadius)
            {
                return false; // Distant summon -> master as owner
            }

            // Check if master or another player with lower id is nearby
            bool canSummon = true;
            world.Query<MainCharacterComponent>().ForEachEntity((ref mainComp, entity) =>
            {
                if (entity == localMainEntity.Value.Entity)
                    return;

                var squaredDistance = Vector3.DistanceSquared(localPosition, mainComp.Location);
                if (squaredDistance < squaredSpawnOwnershipRadius && (areaState.MasterClientId == mainComp.PlayerId || mainComp.PlayerId.RawValue < localPlayerId.RawValue))
                {
                    canSummon = false;
                }
            });
            return canSummon;
        }
    }

    public Type ContextType
        => typeof(Entity);
    
    public bool ShouldEventPropagateToEcs(in SpawnSummonContext context)
    {
        if (sideChannel.HasData<PropagatingToGameScope<TEvent>>())
            return false;

        return CanSummon(context.SummonerEntity, context.SummonLocation);
    }

    public bool ShouldEventPropagateToGame(in SpawnSummonContext context)
    {
        if (sideChannel.HasData<PropagatingToEcsScope<TEvent>>())
            return false;
        
        return true;
    }

    public bool ShouldGameEventRunLocally(in SpawnSummonContext context, out EventSource eventSource)
    {
        eventSource = sideChannel.HasData<PropagatingToGameScope<TEvent>>()
            ? EventSource.Trigger
            : EventSource.Game;

        return CanSummon(context.SummonerEntity, context.SummonLocation);
    }
}