using System;
using ReadyM.Api.Helpers;
using ReadyM.Api.Mapping.Events;
using ReadyM.Relay.Common.Mapping;

namespace WukongMp.Api.Mapping.Events;

public class AlwaysPropagatesToEcsOnlyEventPolicy<TEvent>(DataSideChannel sideChannel) : IMappingEventPolicy<EmptyContext>
{
    public Type ContextType
        => typeof(EmptyContext);
    
    public bool ShouldEventPropagateToEcs(in EmptyContext context)
    {
        if (sideChannel.HasData<PropagatingToGameScope<TEvent>>())
            return false;
        
        return true;
    }

    public bool ShouldEventPropagateToGame(in EmptyContext context)
    {
        return false;
    }

    public bool ShouldGameEventRunLocally(in EmptyContext context, out EventSource eventSource)
    {
        eventSource = sideChannel.HasData<PropagatingToGameScope<TEvent>>()
            ? EventSource.Trigger
            : EventSource.Game;
        return true;
    }
}