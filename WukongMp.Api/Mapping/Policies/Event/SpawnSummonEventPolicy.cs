using System.Numerics;
using Friflo.Engine.ECS;
using ReadyM.Api.ECS.Worlds;
using ReadyM.Api.Helpers;
using ReadyM.Api.Multiplayer.Mapping.Policies.Event;
using ReadyM.Relay.Client.State;
using ReadyM.Wukong.Common.ECS.Components;
using UnrealEngine.Runtime;
using WukongMp.Api.Configuration;
using WukongMp.Api.ECS.Entities;
using WukongMp.Api.State;

namespace WukongMp.Api.Mapping.Policies.Event;

public class SpawnSummonEventPolicy<TEvent>(
    ClientOwnershipManager ownership,
    WukongPlayerState playerState,
    WukongAreaState areaState,
    Store world,
    DataSideChannel sideChannel
) : MappingEventPolicyBase<TEvent, SpawnSummonContext>(sideChannel)
{
    private bool CanSummon(Entity? summonerEntity, FVector summonLocation)
    {
        if (summonerEntity != null && (MainCharacterEntity.IsMainCharacter(summonerEntity.Value) || TamerEntity.IsTamer(summonerEntity.Value)))
        {
            // If a player is the summoner, apply ownership semantics.
            return ownership.OwnsEntity(summonerEntity.Value);
        }

        // Summoner is not a mapped entity, e.g. a BGU_QuestActor spawn point
        var localMainEntity = playerState.LocalMainCharacter;
        if (localMainEntity == null)
            return false;

        if (playerState.LocalPlayerId == null)
            return false;

        if (areaState.IsMasterClient) // Master client can always summon, to avoid issues with distant summons and no players around.
            return true;

        var localPlayerId = playerState.LocalPlayerId.Value;
        var localPosition = localMainEntity.Value.GetState().Location;
        var squaredDistanceToSummon = FVector.DistSquared(localPosition.ToFVector(), summonLocation);
        const float squaredSpawnOwnershipRadius = Constants.SpawnOwnershipRadius * Constants.SpawnOwnershipRadius;
        if (squaredDistanceToSummon > squaredSpawnOwnershipRadius)
        {
            return false; // Distant summon -> master as owner
        }

        // Check if master or another player with lower id is nearby
        var canSummon = true;
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

    protected override bool CanGameEventNotifyEcsImpl(in SpawnSummonContext context)
    {
        return CanSummon(context.SummonerEntity, context.SummonLocation);
    }

    protected override bool CanEcsInvokeGameEventImpl(in SpawnSummonContext context)
    {
        return true;
    }

    protected override bool CanGameEventRunLocallyImpl(in SpawnSummonContext context)
    {
        return CanSummon(context.SummonerEntity, context.SummonLocation);
    }
}