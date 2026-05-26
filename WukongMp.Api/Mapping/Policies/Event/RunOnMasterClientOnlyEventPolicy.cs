using Friflo.Engine.ECS;
using ReadyM.Api.Helpers;
using ReadyM.Api.Mapping.Policies.Event;
using ReadyM.Relay.Client.State;
using WukongMp.Api.State;

namespace WukongMp.Api.Mapping.Policies.Event;

internal class RunOnMasterClientOnlyEventPolicy<TEvent>(
    ClientOwnershipManager ownership,
    WukongAreaState areaState,
    DataSideChannel sideChannel
) : MappingEventPolicyBase<TEvent, Entity>(sideChannel)
{
    protected override bool CanGameEventNotifyEcsImpl(in Entity context)
    {
        if (areaState.IsMasterClient)
        {
            // on master client we simply run the event locally, without propagation
            return false;
        }

        if (ownership.OwnsEntity(context))
        {
            // on non-master client, if we own entity, we propagate to ECS in order to send to the master client
            return true;
        }

        return false;
    }

    protected override bool CanEcsInvokeGameEventImpl(in Entity context)
    {
        // on master client propagate event to the game to be run locally
        // on non-master client, we should ignore the event. In fact this should never execute 
        return areaState.IsMasterClient;
    }

    protected override bool CanGameEventRunLocallyImpl(in Entity context)
    {
        // on non-master client, we don't run the event at all as it is meant to be run on the master
        return areaState.IsMasterClient;
    }
}