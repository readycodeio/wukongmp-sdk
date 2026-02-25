using System;
using System.Diagnostics;
using ReadyM.Api.ECS.Worlds;
using ReadyM.Api.Helpers;
using ReadyM.Api.Mapping;
using ReadyM.Api.Mapping.Events;
using ReadyM.Relay.Client.State;
using WukongMp.Api.State;

namespace WukongMp.Api.Mapping.Events;

public class SpawnSummonEventEventPolicyFactory(
    ClientOwnershipManager ownership,
    WukongPlayerState playerState,
    WukongAreaState areaState,
    Store world,
    DataSideChannel sideChannel) : IMappingEventPolicyFactory
{
    public bool Supports(Type eventType, Type contextType)
        => contextType == typeof(SpawnSummonContext) && typeof(IMappingContext<SpawnSummonContext>).IsAssignableFrom(eventType);

    public IMappingEventPolicyBase CreatePolicy(Type eventType, Type contextType)
    {
        Debug.Assert(contextType == typeof(SpawnSummonContext));
        var policyType = typeof(SpawnSummonEventPolicy<>).MakeGenericType(eventType);
        return (IMappingEventPolicyBase)Activator.CreateInstance(policyType, ownership, playerState, areaState, world, sideChannel);
    }

    public IMappingEventPolicy<TContext> CreatePolicy<TContext>(Type eventType)
        where TContext : struct
    {
        Debug.Assert(typeof(TContext) == typeof(SpawnSummonContext));
        return (IMappingEventPolicy<TContext>)CreatePolicy(eventType, typeof(TContext));
    }
}