using System;
using Friflo.Engine.ECS;
using ReadyM.Api.Helpers;
using ReadyM.Api.Mapping.Events;
using ReadyM.Relay.Client.State;

namespace WukongMp.Api.Mapping.Events;

public class OwnershipEventPolicy<TEvent>(
    ClientOwnershipManager ownership,
    DataSideChannel sideChannel) : IMappingEventPolicy<Entity>
{
    public Type ContextType
        => typeof(Entity);
    
    public bool ShouldEventPropagateToEcs(in Entity context)
    {
        if (sideChannel.HasData<PropagatingToGameScope<TEvent>>())
            return false;
        
        return ownership.OwnsEntity(context);
    }

    public bool ShouldEventPropagateToGame(in Entity context)
    {
        if (sideChannel.HasData<PropagatingToEcsScope<TEvent>>())
            return false;
        
        return !ownership.OwnsEntity(context);
    }

    public bool ShouldGameEventRunLocally(in Entity context, out EventSource eventSource)
    {
        eventSource = sideChannel.HasData<PropagatingToGameScope<TEvent>>()
            ? EventSource.Trigger
            : EventSource.Game;
        return true;
    }
}