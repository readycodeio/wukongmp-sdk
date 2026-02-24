using System;
using Friflo.Engine.ECS;
using ReadyM.Api.Helpers;
using ReadyM.Api.Mapping.Events;
using ReadyM.Relay.Common.Mapping;
using WukongMp.Api.State;

namespace WukongMp.Api.Mapping.Events;

public class MasterClientEventPolicy<TEvent>(
    WukongAreaState areaState, 
    DataSideChannel sideChannel) : IMappingEventPolicy<EmptyContext>
{
    public Type ContextType
        => typeof(Entity);
    
    public bool ShouldEventPropagateToEcs(in EmptyContext clientManaged)
    {
        if (sideChannel.HasData<PropagatingToGameScope<TEvent>>())
            return false;
        
        return areaState.IsMasterClient;
    }

    public bool ShouldEventPropagateToGame(in EmptyContext clientManaged)
    {
        if (sideChannel.HasData<PropagatingToEcsScope<TEvent>>())
            return false;
        
        return !areaState.IsMasterClient;
    }

    public bool ShouldGameEventRunLocally(in EmptyContext clientManaged, out EventSource eventSource)
    {
        eventSource = sideChannel.HasData<PropagatingToGameScope<TEvent>>()
            ? EventSource.Trigger
            : EventSource.Game;

        if (eventSource == EventSource.Trigger)
            return true;

        return areaState.IsMasterClient;
    }
}