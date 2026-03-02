using ReadyM.Api.Helpers;
using ReadyM.Api.Mapping.Events;
using ReadyM.Relay.Common.Mapping;

namespace WukongMp.Api.Mapping.Events;

/// Used for events that are only sent to clients from the server and therefore are never triggered locally.
public class AlwaysPropagatesToGameOnlyEventPolicy<TEvent>(DataSideChannel sideChannel) : MappingEventPolicyBase<TEvent, EmptyContext>(sideChannel)
{
    protected override bool ShouldEventPropagateToEcsImpl(in EmptyContext context)
    {
        return false;
    }

    protected override bool ShouldEventPropagateToGameImpl(in EmptyContext context)
    {
        return true;
    }

    protected override bool ShouldGameEventRunLocallyImpl(in EmptyContext context)
    {
        return true;
    }
}