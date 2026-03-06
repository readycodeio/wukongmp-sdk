using System;
using System.Diagnostics;
using Friflo.Engine.ECS;
using ReadyM.Api.Helpers;
using ReadyM.Api.Multiplayer.Mapping.Policies.Event;
using ReadyM.Relay.Client.State;
using WukongMp.Api.Mapping.Tags;
using WukongMp.Api.State;

namespace WukongMp.Api.Mapping.Policies.Event;

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
    {
        Debug.Assert(typeof(TContext) == typeof(Entity));
        return (IMappingEventPolicy<TContext>)CreatePolicy(eventType, typeof(TContext));
    }
}