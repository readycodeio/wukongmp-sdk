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
    protected override bool ShouldEventPropagateToEcsImpl(in Entity context)
    {
        return ownership.OwnsEntity(context);
    }

    protected override bool ShouldEventPropagateToGameImpl(in Entity context)
    {
        return !ownership.OwnsEntity(context);
    }

    protected override bool ShouldGameEventRunLocallyImpl(in Entity context)
    {
        return ownership.OwnsEntity(context);
    }
}