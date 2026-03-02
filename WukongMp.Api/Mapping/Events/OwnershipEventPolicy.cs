using Friflo.Engine.ECS;
using ReadyM.Api.Helpers;
using ReadyM.Api.Mapping.Events;
using ReadyM.Relay.Client.State;

namespace WukongMp.Api.Mapping.Events;

public class OwnershipEventPolicy<TEvent>(
    ClientOwnershipManager ownership,
    DataSideChannel sideChannel
) : MappingEventPolicyBase<TEvent, Entity>(sideChannel)
{
    protected override bool CanGameEventNotifyEcsImpl(in Entity context)
    {
        return ownership.OwnsEntity(context);
    }

    protected override bool CanEcsInvokeGameEventImpl(in Entity context)
    {
        return !ownership.OwnsEntity(context);
    }

    protected override bool CanGameEventRunLocallyImpl(in Entity context)
    {
        return ownership.OwnsEntity(context);
    }
}