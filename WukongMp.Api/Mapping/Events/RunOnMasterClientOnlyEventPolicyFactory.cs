using System;
using System.Diagnostics;
using Friflo.Engine.ECS;
using ReadyM.Api.Helpers;
using ReadyM.Api.Mapping.Events;
using ReadyM.Relay.Client.State;
using ReadyM.Relay.Common.Mapping;
using WukongMp.Api.State;

namespace WukongMp.Api.Mapping.Events;

public class RunOnMasterClientOnlyEventPolicyFactory(
    ClientOwnershipManager ownership,
    WukongAreaState areaState, 
    DataSideChannel sideChannel) : IMappingEventPolicyFactory
{
    public bool Supports(Type eventType, Type contextType)
        => contextType == typeof(Entity) && typeof(IRunOnMasterClientOnly).IsAssignableFrom(eventType);

    public IMappingEventPolicyBase CreatePolicy(Type eventType, Type contextType)
    {
        Debug.Assert(contextType == typeof(Entity));
        var policyType = typeof(RunOnMasterClientOnlyEventPolicy<>).MakeGenericType(eventType);
        return (IMappingEventPolicyBase)Activator.CreateInstance(policyType, ownership, areaState, sideChannel);
    }

    public IMappingEventPolicy<TContext> CreatePolicy<TContext>(Type eventType)
        where TContext : struct
    {
        Debug.Assert(typeof(TContext) == typeof(Entity));
        return (IMappingEventPolicy<TContext>)CreatePolicy(eventType, typeof(TContext));
    }
}