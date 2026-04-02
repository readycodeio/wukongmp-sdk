using System;
using System.Diagnostics;
using ReadyM.Api.Helpers;
using ReadyM.Api.Multiplayer.Mapping.Policies.Event;
using ReadyM.Api.Multiplayer.Mapping.Tags;
using WukongMp.Api.Mapping.Tags;
using WukongMp.Api.State;

namespace WukongMp.Api.Mapping.Policies.Event;

internal class MasterClientEventPolicyFactory(
    WukongAreaState areaState,
    DataSideChannel sideChannel) : IMappingEventPolicyFactory
{
    public bool Supports(Type eventType, Type contextType)
        => contextType == typeof(EmptyContext) && typeof(IMasterClientManaged).IsAssignableFrom(eventType);

    public IMappingEventPolicyBase CreatePolicy(Type eventType, Type contextType)
    {
        Debug.Assert(contextType == typeof(EmptyContext), "contextType == typeof(EmptyContext)");
        var policyType = typeof(MasterClientEventPolicy<>).MakeGenericType(eventType);
        return (IMappingEventPolicyBase)Activator.CreateInstance(policyType, areaState, sideChannel);
    }

    public IMappingEventPolicy<TContext> CreatePolicy<TContext>(Type eventType)
    {
        Debug.Assert(typeof(TContext) == typeof(EmptyContext));
        return (IMappingEventPolicy<TContext>)CreatePolicy(eventType, typeof(TContext));
    }
}