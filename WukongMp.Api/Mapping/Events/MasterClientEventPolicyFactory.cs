using System;
using System.Diagnostics;
using ReadyM.Api.Helpers;
using ReadyM.Api.Mapping.Events;
using ReadyM.Relay.Client.Mapping;
using WukongMp.Api.State;

namespace WukongMp.Api.Mapping.Events;

public class MasterClientEventPolicyFactory(
    WukongAreaState areaState,
    DataSideChannel sideChannel) : IMappingEventPolicyFactory
{
    public bool Supports(Type eventType, Type contextType)
        => contextType == typeof(EmptyContext) && typeof(IMasterClientManaged).IsAssignableFrom(eventType);

    public IMappingEventPolicyBase CreatePolicy(Type eventType, Type contextType)
    {
        Debug.Assert(contextType == typeof(EmptyContext));
        var policyType = typeof(MasterClientEventPolicy<>).MakeGenericType(eventType);
        return (IMappingEventPolicyBase)Activator.CreateInstance(policyType, areaState, sideChannel);
    }

    public IMappingEventPolicy<TContext> CreatePolicy<TContext>(Type eventType)
        where TContext : struct
    {
        Debug.Assert(typeof(TContext) == typeof(EmptyContext));
        return (IMappingEventPolicy<TContext>)CreatePolicy(eventType, typeof(TContext));
    }
}