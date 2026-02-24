using System;
using Friflo.Engine.ECS;
using ReadyM.Api.Helpers;
using ReadyM.Api.Mapping.Events;
using ReadyM.Relay.Client.State;
using WukongMp.Api.State;

namespace WukongMp.Api.Mapping.Events;

public class RunOnMasterClientOnlyEventPolicy<TEvent>(
    ClientOwnershipManager ownership,
    WukongAreaState areaState, 
    DataSideChannel sideChannel) : IMappingEventPolicy<Entity>
{
    public Type ContextType
        => typeof(Entity);
    
    public bool ShouldEventPropagateToEcs(in Entity context)
    {
        if (sideChannel.HasData<PropagatingToGameScope<TEvent>>())
            return false;

        if (areaState.IsMasterClient)
        {
            // NOTE(api): on master client we simply run the event locally, without propagation
            return false;
        }
        else
        {
            if (ownership.OwnsEntity(context))
            {
                // NOTE(api): on non-master client, if we own entity, we propagate to ECS in order to send to
                // the master client
                return true;
            }
            else
            {
                // otherwise we ignore
                return false;
            }
        }
    }

    public bool ShouldEventPropagateToGame(in Entity context)
    {
        if (sideChannel.HasData<PropagatingToEcsScope<TEvent>>())
            return false;
        
        if (areaState.IsMasterClient)
        {
            // NOTE(api): on master client propagate event to the game to be run locally
            return true;
        }
        else
        {
            // NOTE(api): on non-master client, we should ignore the event. In fact this should never execute 
            return false;
        }
    }

    public bool ShouldGameEventRunLocally(in Entity context, out EventSource eventSource)
    {
        eventSource = sideChannel.HasData<PropagatingToGameScope<TEvent>>()
            ? EventSource.Trigger
            : EventSource.Game;

        // NOTE(api): on non-master client, we don't run the event at all as it is meant to be run on the master
        if (!areaState.IsMasterClient)
            return false;
        
        return true;
    }
}